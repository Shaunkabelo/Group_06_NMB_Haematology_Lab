/* ============================================================
   NMB-HLabSys — Lab Manager  |  script.js
   Handles: modals, toasts, search, forms, sidebar active state,
   stock adjust, doctor/category CRUD, order actions, and more.
   Drop this one file into your project and link it on every page.
   ============================================================ */


/* ══════════════════════════════════════════════════════════════
   AUTO SIDEBAR ACTIVE STATE
   Reads the current filename and marks the matching .nav-item
   as active — so you don't need per-page inline logic.
   ══════════════════════════════════════════════════════════════ */
(function () {
    const currentPath = location.pathname.replace(/\/+$/, '').toLowerCase();
    document.querySelectorAll('.nav-item').forEach(link => {
        const linkPath = new URL(link.getAttribute('href') || '', location.origin)
            .pathname
            .replace(/\/+$/, '')
            .toLowerCase();

        if (linkPath) {
            link.classList.toggle('active', linkPath === currentPath);
        }
    });
})();


/* ══════════════════════════════════════════════════════════════
   TOAST
   toast('Saved!')  /  toast('Something went wrong','error')
   toast('Watch out','warning')  /  toast('Note','info')
   ══════════════════════════════════════════════════════════════ */
let _toastTimer = null;

function toast(message, type = 'default') {
    let el = document.getElementById('toast');
    if (!el) {                                     // create if missing
        el = document.createElement('div');
        el.id = 'toast';
        document.body.appendChild(el);
    }
    const colors = { default: '#00C9A7', error: '#FF4757', warning: '#F5A623', info: '#3D9AE8' };
    el.style.borderLeftColor = colors[type] || colors.default;
    el.textContent = message;
    el.classList.add('show');
    if (_toastTimer) clearTimeout(_toastTimer);
    _toastTimer = setTimeout(() => el.classList.remove('show'), 3300);
}


/* ============================================================
   LOGIN
   Demo credentials for all prototype users.
   ============================================================ */
const DEMO_USERS = [
    {
        email: 'manager@nmbhlab.co.za',
        password: 'Manager@123',
        role: 'Laboratory Manager',
        name: 'Shaun Rasebotsa',
        redirect: 'LabManager/Dashboard',
    },
    {
        email: 'doctor@nmbhlab.co.za',
        password: 'Doctor@123',
        role: 'Doctor',
        name: 'Dr. Odwa Sithonga',
        redirect: 'Doctor/Dashboard',
    },
    {
        email: 'technician@nmbhlab.co.za',
        password: 'Tech@1234',
        role: 'Lab Technician',
        name: 'Xola Mbambiso',
        redirect: 'Technician/Dashboard',
    },
    {
        email: 'patient@nmbhlab.co.za',
        password: 'Patient@123',
        role: 'Patient',
        name: 'Sabelo Maphumulo',
        redirect: 'Patient/Dashboard',
    },
    {
        email: 'admin@nmbhlab.co.za',
        password: 'Admin@123',
        role: 'Admin',
        name: 'Admin User',
        redirect: 'Admin/Dashboard',
    },
];

function loginUser(event) {
    event.preventDefault();

    const email = getVal('login-email').toLowerCase();
    const password = getVal('login-password');
    const user = DEMO_USERS.find(u => u.email === email && u.password === password);

    if (!user) {
        markError('login-email');
        markError('login-password');
        toast('Invalid email address or password.', 'error');
        return;
    }

    sessionStorage.setItem('nmb_user_email', user.email);
    sessionStorage.setItem('nmb_user_role', user.role);
    sessionStorage.setItem('nmb_user_name', user.name || 'Guest User');
    toast('Login successful. Redirecting...', 'info');
    setTimeout(() => {
        const appBase = document.querySelector('base')?.href || window.location.origin + '/';
        const redirectPath = user.redirect.replace(/^~\//, '').replace(/^\//, '');
        window.location.href = new URL(redirectPath, appBase).href;
    }, 700);
}

function fillDemoLogin(email) {
    const user = DEMO_USERS.find(u => u.email === email);
    const emailInput = document.getElementById('login-email');
    const passwordInput = document.getElementById('login-password');

    if (!user || !emailInput || !passwordInput) return;

    emailInput.value = user.email;
    passwordInput.value = user.password;
    clearFormErrors();
    toast(`${user.role} demo credentials loaded.`, 'info');
}

function togglePassword(inputId, button) {
    const input = document.getElementById(inputId);
    if (!input) return;

    const showing = input.type === 'text';
    input.type = showing ? 'password' : 'text';
    if (button) button.textContent = showing ? 'Show' : 'Hide';
}

function validatePassword(inputId) {
    const value = getVal(inputId);
    const valid = /^(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z\d]).{8,}$/.test(value);
    const hint = document.getElementById(`${inputId}-hint`);
    if (hint) {
        hint.textContent = valid ? 'Strong password.' : 'Use 8+ characters, uppercase, number and special character.';
        hint.style.color = valid ? '#2ED573' : '';
    }
    if (!valid && value) markError(inputId);
    return valid;
}

function validateEmail(value) {
    return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value);
}

function validatePhoneNumber(value) {
    const digits = (value || '').replace(/[^\d]/g, '');
    return digits.length >= 10 && digits.length <= 13;
}

function validateSAID(value) {
    return /^\d{13}$/.test((value || '').replace(/\s+/g, ''));
}

function submitPatientRegistration() {
    const checks = [
        ['reg-fname', getVal('reg-fname')],
        ['reg-lname', getVal('reg-lname')],
        ['reg-id', getVal('reg-id')],
        ['reg-dob', getVal('reg-dob')],
        ['reg-phone', getVal('reg-phone')],
        ['reg-email', getVal('reg-email')],
        ['reg-addr', getVal('reg-addr')],
        ['reg-password', getVal('reg-password')],
        ['reg-confirm-password', getVal('reg-confirm-password')]
    ];
    if (!validateAll(checks)) return;
    if (!validateEmail(getVal('reg-email'))) { markError('reg-email'); toast('Enter a valid email address.', 'error'); return; }
    if (!validatePhoneNumber(getVal('reg-phone'))) { markError('reg-phone'); toast('Enter a valid cellphone number.', 'error'); return; }
    if (!validateSAID(getVal('reg-id'))) { markError('reg-id'); toast('Enter a valid 13-digit South African ID number.', 'error'); return; }
    if (getVal('reg-password') !== getVal('reg-confirm-password')) { markError('reg-confirm-password'); toast('Passwords do not match.', 'error'); return; }
    if (!validatePassword('reg-password')) { toast('Password needs 8+ chars, uppercase, number and special character.', 'error'); return; }
    const btn = document.getElementById('register-btn');
    if (btn) { btn.disabled = true; btn.textContent = 'Registering...'; }
    setTimeout(() => {
        if (btn) { btn.disabled = false; btn.textContent = 'Register Patient'; }
        toast('Patient registration captured.');
    }, 700);
}

function savePatientProfile() {
    const checks = [
        ['p-fname', getVal('p-fname')],
        ['p-lname', getVal('p-lname')],
        ['p-email', getVal('p-email')],
        ['p-cell', getVal('p-cell')],
        ['p-addr', getVal('p-addr')]
    ];
    if (!validateAll(checks)) return;
    if (!validateEmail(getVal('p-email'))) { markError('p-email'); toast('Please enter a valid email.', 'error'); return; }
    if (!validatePhoneNumber(getVal('p-cell'))) { markError('p-cell'); toast('Enter a valid cellphone number.', 'error'); return; }
    const btn = document.getElementById('profile-save-btn');
    if (btn) { btn.disabled = true; btn.textContent = 'Saving...'; }
    setTimeout(() => {
        if (btn) { btn.disabled = false; btn.textContent = 'Save Personal Details'; }
        toast('Profile details updated successfully.');
    }, 500);
}

function changePatientPassword() {
    const current = getVal('current-password');
    const nw = getVal('profile-password');
    const conf = getVal('profile-password-confirm');
    if (!validateAll([['current-password', current], ['profile-password', nw], ['profile-password-confirm', conf]])) return;
    if (nw !== conf) { markError('profile-password-confirm'); toast('Passwords do not match.', 'error'); return; }
    if (!validatePassword('profile-password')) { toast('Password needs 8+ chars, uppercase, number and special character.', 'error'); return; }
    const btn = document.getElementById('password-change-btn');
    if (btn) { btn.disabled = true; btn.textContent = 'Updating...'; }
    setTimeout(() => {
        ['current-password', 'profile-password', 'profile-password-confirm'].forEach(id => setVal(id, ''));
        if (btn) { btn.disabled = false; btn.textContent = 'Change Password'; }
        toast('Password changed successfully.');
    }, 500);
}

document.addEventListener('pointerdown', e => {
    const target = e.target.closest('.btn, .card-link');
    if (target) target.classList.add('active-press');
});

document.addEventListener('pointerup', e => {
    const target = e.target.closest('.btn, .card-link');
    if (target) target.classList.remove('active-press');
});
document.addEventListener('pointercancel', e => {
    const target = e.target.closest('.btn, .card-link');
    if (target) target.classList.remove('active-press');
});

function forgotPassword() {
    const email = getVal('login-email');
    if (!email || !email.includes('@')) {
        markError('login-email');
        toast('Enter your email address first.', 'error');
        return;
    }
    toast(`Password reset link sent to ${email}.`, 'info');
}

function logoutUser() {
    sessionStorage.removeItem('nmb_user_email');
    sessionStorage.removeItem('nmb_user_role');
    sessionStorage.removeItem('nmb_user_name');
    const appBase = document.querySelector('base')?.href || window.location.origin + '/';
    window.location.href = new URL('Account/Login', appBase).href;
}

function renderSidebarUser() {
    const name = sessionStorage.getItem('nmb_user_name') || 'Guest User';
    const role = sessionStorage.getItem('nmb_user_role');
    const nameEl = document.getElementById('sidebar-user-name');
    const roleEl = document.getElementById('sidebar-user-role');
    if (nameEl) nameEl.textContent = name;
    if (roleEl && role) roleEl.textContent = role;
}

document.addEventListener('DOMContentLoaded', renderSidebarUser);


/* ══════════════════════════════════════════════════════════════
   MODAL OPEN / CLOSE
   ══════════════════════════════════════════════════════════════ */
function openModal(id) {
    const el = document.getElementById(id);
    if (el) { el.style.display = 'flex'; requestAnimationFrame(() => el.classList.add('open')); }
}

function closeModal(id) {
    const el = document.getElementById(id);
    if (el) { el.classList.remove('open'); setTimeout(() => { if (!el.classList.contains('open')) el.style.display = 'none'; }, 200); }
    clearFormErrors();
}

// Click backdrop → close
document.addEventListener('click', e => {
    if (e.target.classList.contains('modal-overlay')) closeModal(e.target.id);
});

// ESC → close all
document.addEventListener('keydown', e => {
    if (e.key === 'Escape')
        document.querySelectorAll('.modal-overlay.open').forEach(m => closeModal(m.id));
});


/* ══════════════════════════════════════════════════════════════
   FORM HELPERS
   ══════════════════════════════════════════════════════════════ */
function clearFormErrors() {
    document.querySelectorAll('.field-error').forEach(el => {
        el.style.borderColor = '';
        el.classList.remove('field-error');
    });
}

function markError(id) {
    const el = document.getElementById(id);
    if (!el) return;
    el.style.borderColor = '#FF4757';
    el.classList.add('field-error');
    el.addEventListener('input', () => {
        el.style.borderColor = '';
        el.classList.remove('field-error');
    }, { once: true });
}

function getVal(id) { return (document.getElementById(id)?.value || '').trim(); }
function setVal(id, val) { const el = document.getElementById(id); if (el) el.value = val ?? ''; }


/* ══════════════════════════════════════════════════════════════
   SEARCH & FILTER
   oninput="handleSearch(this.value)" on any topbar input
   ══════════════════════════════════════════════════════════════ */
function handleSearch(query) {
    const q = query.toLowerCase();
    const tables = document.querySelectorAll('table');
    tables.forEach(table => {
        table.querySelectorAll('tbody tr').forEach(row => {
            row.style.display = row.textContent.toLowerCase().includes(q) ? '' : 'none';
        });
    });

    if (!tables.length) {
        document.querySelectorAll('.stat-card, .alert, .activity-item, .list-row').forEach(item => {
            item.style.display = item.textContent.toLowerCase().includes(q) ? '' : 'none';
        });
    }
}

function filterTable(tableId, query) {
    document.querySelectorAll(`#${tableId} tbody tr`).forEach(row => {
        row.style.display = row.textContent.toLowerCase().includes(query.toLowerCase()) ? '' : 'none';
    });
}


/* ══════════════════════════════════════════════════════════════
   NOTIFICATIONS
   ══════════════════════════════════════════════════════════════ */
function showNotifications() {
    toast('3 alerts: 1 critical stock · 2 active orders pending', 'warning');
}


/* ══════════════════════════════════════════════════════════════
   TABS
   onclick="switchTab(this, 'panel-id')"
   ══════════════════════════════════════════════════════════════ */
function switchTab(btnEl, panelId) {
    const tabs = btnEl.closest('.tabs');
    if (tabs) tabs.querySelectorAll('.tab-btn').forEach(b => b.classList.remove('active'));
    btnEl.classList.add('active');

    const scope = btnEl.closest('.page-body') || document.body;
    scope.querySelectorAll('.tab-panel').forEach(p => p.classList.remove('active'));

    const panel = document.getElementById(panelId);
    if (panel) panel.classList.add('active');
}


/* ══════════════════════════════════════════════════════════════
   TEST CATEGORIES
   ══════════════════════════════════════════════════════════════ */
function openAddCategory() {
    setTitleText('modal-category-title', '🗂️ Add Test Category');
    setVal('cat-name', ''); setVal('cat-desc', '');
    openModal('modal-category');
}

function openEditCategory(name, desc) {
    setTitleText('modal-category-title', '✏️ Edit Category');
    setVal('cat-name', name); setVal('cat-desc', desc || '');
    openModal('modal-category');
}

function saveCategory() {
    const name = getVal('cat-name');
    if (!name) { markError('cat-name'); toast('Category name is required.', 'error'); return; }
    closeModal('modal-category');
    toast('Category saved successfully.');
}

// Generic deactivate confirm — categories, test types, etc.
function openDeactivate(name, type) {
    setTitleText('deactivate-title', `Delete "${name}"?`);
    wireConfirm('deactivate-confirm-btn', 'modal-deactivate', `"${name}" deleted.`, 'warning');
    openModal('modal-deactivate');
}


/* ══════════════════════════════════════════════════════════════
   TEST TYPES
   ══════════════════════════════════════════════════════════════ */
function openAddTestType() {
    setTitleText('tt-modal-title', '🔬 Add Test Type');
    ['tt-name', 'tt-tat', 'tt-min', 'tt-max'].forEach(id => setVal(id, ''));
    openModal('modal-testtype');
}

function openEditTest(name) {
    setTitleText('tt-modal-title', '✏️ Edit Test Type');
    setVal('tt-name', name);
    openModal('modal-testtype');
}

function openDeactivateTest(name) {
    setTitleText('deactivate-tt-title', `Delete "${name}"?`);
    openModal('modal-deactivate-tt');
}

function saveTestType() {
    const checks = [
        ['tt-name', getVal('tt-name')],
        ['tt-cat', getVal('tt-cat')],
        ['tt-unit', getVal('tt-unit')],
        ['tt-tat', getVal('tt-tat')],
    ];
    if (!validateAll(checks)) return;
    closeModal('modal-testtype');
    toast('Test type saved successfully.');
}


/* ══════════════════════════════════════════════════════════════
   CONSUMABLES
   ══════════════════════════════════════════════════════════════ */
function openAdjust(name, qty, reorder) {
    setTitleText('adjust-name', name);

    const infoEl = document.getElementById('adjust-info');
    if (infoEl) {
        const pct = Math.round((qty / reorder) * 100);
        const col = pct <= 20 ? '#FF4757' : pct <= 100 ? '#F5A623' : '#00C9A7';
        infoEl.innerHTML =
            `Current qty: <strong style="color:${col}">${qty}</strong>&nbsp;·&nbsp;Reorder: <strong>${reorder}</strong>`;
    }

    setVal('adjust-qty', '');
    const typeEl = document.getElementById('adjust-type');
    if (typeEl) { typeEl.value = 'increase'; typeEl.onchange = updateAdjustLabel; }
    updateAdjustLabel();
    openModal('modal-adjust');
}

function updateAdjustLabel() {
    const labels = { increase: 'Amount to add', decrease: 'Amount to remove', set: 'New quantity value' };
    const labelEl = document.getElementById('adjust-qty-label');
    if (labelEl) labelEl.textContent = (labels[getVal('adjust-type')] || 'Quantity') + ' *';
}

function applyAdjustment() {
    const qty = getVal('adjust-qty');
    if (!qty || isNaN(+qty) || +qty < 0) { markError('adjust-qty'); toast('Enter a valid quantity.', 'error'); return; }
    const msgs = {
        increase: `Stock increased by ${qty} units.`,
        decrease: `Stock decreased by ${qty} units.`,
        set: `Stock level set to ${qty} units.`
    };
    closeModal('modal-adjust');
    toast(msgs[getVal('adjust-type')] || 'Stock updated.');
}

function openAddConsumable() {
    setTitleText('cons-modal-title', '📦 Add Consumable');
    ['cons-name', 'cons-reorder', 'cons-qty'].forEach(id => setVal(id, ''));
    setVal('cons-supplier', '');
    openModal('modal-consumable');
}

function openEditConsumable(name, supplier, reorder, qty) {
    setTitleText('cons-modal-title', '✏️ Edit Consumable');
    setVal('cons-name', name); setVal('cons-supplier', supplier);
    setVal('cons-reorder', reorder); setVal('cons-qty', qty);
    openModal('modal-consumable');
}

function saveConsumable() {
    const checks = [
        ['cons-name', getVal('cons-name')],
        ['cons-supplier', getVal('cons-supplier')],
        ['cons-reorder', getVal('cons-reorder')],
        ['cons-qty', getVal('cons-qty')],
    ];
    if (!validateAll(checks)) return;
    closeModal('modal-consumable');
    toast('Consumable saved successfully.');
}


/* ══════════════════════════════════════════════════════════════
   SUPPLIERS
   ══════════════════════════════════════════════════════════════ */
function openAddSupplier() {
    setTitleText('sup-modal-title', '🏭 Add Supplier');
    ['sup-name', 'sup-contact', 'sup-email'].forEach(id => setVal(id, ''));
    openModal('modal-supplier');
}

function openEditSupplier(name, contact, email) {
    setTitleText('sup-modal-title', '✏️ Edit Supplier');
    setVal('sup-name', name); setVal('sup-contact', contact); setVal('sup-email', email);
    openModal('modal-supplier');
}

function openDeleteSupplier(name) {
    setTitleText('delete-sup-title', `Delete "${name}"?`);
    wireConfirm('delete-sup-confirm', 'modal-delete-sup', `${name} deleted.`, 'warning');
    openModal('modal-delete-sup');
}

function saveSupplier() {
    const checks = [['sup-name', getVal('sup-name')], ['sup-email', getVal('sup-email')]];
    if (!validateAll(checks)) return;
    closeModal('modal-supplier');
    toast('Supplier saved successfully.');
}


/* ══════════════════════════════════════════════════════════════
   ORDERS
   ══════════════════════════════════════════════════════════════ */
function placeOrder() {
    if (!getVal('order-supplier')) { markError('order-supplier'); toast('Please select a supplier.', 'error'); return; }
    closeModal('modal-order');
    toast('Order placed — supplier notified via email.');
}

function markReceived(orderId) {
    const el = document.getElementById(`status-${orderId}`);
    if (el) { el.className = 'badge green'; el.textContent = '✔ Complete'; }
    toast(`Order marked as complete. Stock levels updated.`);
}

function openCancelOrder(orderNo) {
    setVal('cancel-order-no', orderNo); setVal('cancel-reason', '');
    openModal('modal-cancel-order');
}

function cancelOrder() {
    const reason = getVal('cancel-reason');
    if (!reason) { markError('cancel-reason'); toast('Cancellation reason is required.', 'error'); return; }
    const orderNo = getVal('cancel-order-no');
    const id = orderNo.replace('ORD-2025-', '');
    const el = document.getElementById(`status-${id}`);
    if (el) { el.className = 'badge red'; el.textContent = '✖ Cancelled'; }
    closeModal('modal-cancel-order');
    toast(`${orderNo} cancelled. Supplier notified.`, 'warning');
}

// Sample order line items for view modal
const _orderItems = {
    'ORD-2025-0041': [
        { name: 'EDTA Tubes (3 mL)', qty: 200, status: 'Received' },
        { name: 'Sodium Citrate Tubes', qty: 100, status: 'Ordered' },
        { name: 'Wright Stain Solution', qty: 50, status: 'Ordered' },
    ],
    'ORD-2025-0040': [
        { name: 'Leishman Stain', qty: 100, status: 'Ordered' },
        { name: 'Glass Slides (Box 50)', qty: 20, status: 'Ordered' },
    ],
    'ORD-2025-0039': [
        { name: 'Heparin Tubes (4 mL)', qty: 150, status: 'Received' },
        { name: 'EDTA Tubes (3 mL)', qty: 50, status: 'Received' },
    ],
};

function openViewOrder(orderNo, supplier, date) {
    setTitleText('view-order-title', `📋 ${orderNo}`);
    setTitleText('view-supplier', supplier);
    setTitleText('view-date', date);

    const sBadge = s =>
        s === 'Received' ? '<span class="badge green">Received</span>' :
            s === 'Cancelled' ? '<span class="badge red">Cancelled</span>' :
                '<span class="badge blue">Ordered</span>';

    const tbody = document.getElementById('view-order-items');
    if (tbody) {
        const rows = _orderItems[orderNo] || [];
        tbody.innerHTML = rows.length
            ? rows.map(i => `
          <tr>
            <td>${i.name}</td>
            <td style="text-align:right">${i.qty}</td>
            <td style="text-align:right">${sBadge(i.status)}</td>
          </tr>`).join('')
            : '<tr><td colspan="3" style="color:#7A8CA0;padding:12px">No item data available.</td></tr>';
    }
    openModal('modal-view-order');
}

const _technicianDetails = {
    'Thabo Botha': {
        empno: 'EMP-0011',
        email: 't.botha@nmbhlab.co.za',
        said: '9207155012084',
        contact: '073 100 0011',
        tests: ['Haemoglobin (FBC)', 'Haematocrit (FBC)', 'White Cell Count (FBC)', 'Neutrophils (Diff)', 'Platelet Count (FBC)', 'MPV (FBC)']
    },
    'Lungelo Nkosi': {
        empno: 'EMP-0012',
        email: 'l.nkosi@nmbhlab.co.za',
        said: '9412220123456',
        contact: '073 100 0012',
        tests: ['PT – Prothrombin Time (Coag)', 'APTT (Coag)', 'Fibrinogen (Coag)', 'D-Dimer (Coag)', 'Bleeding Time']
    },
    'Petra van Wyk': {
        empno: 'EMP-0013',
        email: 'p.vanwyk@nmbhlab.co.za',
        said: '9503104512345',
        contact: '073 100 0013',
        tests: ['Peripheral Blood Film', 'Reticulocyte Count', 'ESR', 'Haemoglobin (FBC)']
    },
    'Sipho Mthembu': {
        empno: 'EMP-0014',
        email: 's.mthembu@nmbhlab.co.za',
        said: '9801085678901',
        contact: '073 100 0014',
        tests: ['Haematocrit (FBC)', 'White Cell Count (FBC)', 'Platelet Count (FBC)']
    }
};

function openViewTechnician(name) {
    const tech = _technicianDetails[name];
    setTitleText('view-tech-title', `👤 ${name}`);
    setTitleText('view-tech-name', name);
    setTitleText('view-tech-empno', tech?.empno || 'N/A');
    setTitleText('view-tech-email', tech?.email || 'N/A');
    setTitleText('view-tech-said', tech?.said || 'N/A');
    setTitleText('view-tech-contact', tech?.contact || 'N/A');

    const list = document.getElementById('view-tech-tests');
    if (list) {
        const tests = tech?.tests || [];
        list.innerHTML = tests.length
            ? tests.map(t => `<li>${t}</li>`).join('')
            : '<li style="color:#7A8CA0">No assigned test types available.</li>';
    }

    openModal('modal-view-technician');
}


/* ══════════════════════════════════════════════════════════════
   DOCTORS
   ══════════════════════════════════════════════════════════════ */
function openAddDoctor() {
    setTitleText('doc-modal-title', '👨‍⚕️ Add New Doctor');
    ['doc-fname', 'doc-lname', 'doc-email', 'doc-hpcsa', 'doc-contact'].forEach(id => setVal(id, ''));
    openModal('modal-doctor');
}

function openEditDoctor(fname, lname, email, hpcsa, contact) {
    setTitleText('doc-modal-title', '✏️ Edit Doctor');
    setVal('doc-fname', fname); setVal('doc-lname', lname);
    setVal('doc-email', email); setVal('doc-hpcsa', hpcsa);
    setVal('doc-contact', contact || '');
    openModal('modal-doctor');
}

function openDeactivateDoctor(name) {
    setTitleText('deactivate-doc-title', `Delete ${name}?`);
    openModal('modal-deactivate-doc');
}

function saveDoctor() {
    const fname = getVal('doc-fname');
    const lname = getVal('doc-lname');
    const email = getVal('doc-email');
    const hpcsa = getVal('doc-hpcsa');
    const checks = [
        ['doc-fname', fname],
        ['doc-lname', lname],
        ['doc-email', email && email.includes('@') ? email : ''],
        ['doc-hpcsa', hpcsa],
    ];
    if (!validateAll(checks)) return;
    const isEdit = (document.getElementById('doc-modal-title')?.textContent || '').startsWith('✏️');
    closeModal('modal-doctor');
    toast(isEdit ? `Dr. ${fname} ${lname} updated.` : `Dr. ${fname} ${lname} created — password emailed.`);
}


/* ══════════════════════════════════════════════════════════════
   TECHNICIANS
   ══════════════════════════════════════════════════════════════ */
function openAddTechnician() {
    setTitleText('tech-modal-title', '🧪 Add New Technician');
    ['tech-fname', 'tech-lname', 'tech-email', 'tech-said', 'tech-empno', 'tech-contact'].forEach(id => setVal(id, ''));
    openModal('modal-tech');
}

function openEditTech(fname, lname, email, said, empno, contact) {
    setTitleText('tech-modal-title', '✏️ Edit Technician');
    setVal('tech-fname', fname); setVal('tech-lname', lname);
    setVal('tech-email', email); setVal('tech-said', said);
    setVal('tech-empno', empno); setVal('tech-contact', contact || '');
    openModal('modal-tech');
}

function openDeactivateTech(name) {
    setTitleText('deactivate-tech-name', `Delete ${name}?`);
    openModal('modal-deactivate-tech');
}

function saveTechnician() {
    const said = getVal('tech-said');
    const checks = [
        ['tech-fname', getVal('tech-fname')],
        ['tech-lname', getVal('tech-lname')],
        ['tech-email', getVal('tech-email').includes('@') ? getVal('tech-email') : ''],
        ['tech-said', said.length === 13 ? said : ''],
        ['tech-empno', getVal('tech-empno')],
    ];
    if (!validateAll(checks)) { toast('Please fill in all required fields correctly.', 'error'); return; }
    const isEdit = (document.getElementById('tech-modal-title')?.textContent || '').startsWith('✏️');
    const fname = getVal('tech-fname'), lname = getVal('tech-lname');
    closeModal('modal-tech');
    toast(isEdit ? `${fname} ${lname} updated.` : `${fname} ${lname} created — password emailed.`);
}

function openAssignTests(name) {
    const tech = _technicianDetails[name];
    setTitleText('assign-tech-name', name);
    // Map test names to checkbox IDs
    const testMap = {
        'Haemoglobin (FBC)': 'test-haemoglobin',
        'Haematocrit (FBC)': 'test-haematocrit',
        'White Cell Count (FBC)': 'test-wcc',
        'PT — Prothrombin Time (Coag)': 'test-pt',
        'APTT (Coag)': 'test-aptt',
        'Neutrophils (Diff)': 'test-neutrophils'
    };
    // Uncheck all first
    Object.values(testMap).forEach(id => {
        const el = document.getElementById(id);
        if (el) el.checked = false;
    });
    // Check assigned tests
    const assigned = tech?.tests || [];
    assigned.forEach(test => {
        const id = testMap[test];
        if (id) {
            const el = document.getElementById(id);
            if (el) el.checked = true;
        }
    });
    openModal('modal-assign-tests');
}

function saveAssignedTests() {
    const name = document.getElementById('assign-tech-name')?.textContent || '';
    if (!name) { toast('Technician not found.', 'error'); return; }
    const tech = _technicianDetails[name];
    if (!tech) { toast('Technician data not found.', 'error'); return; }
    // Map checkbox IDs back to test names
    const testMap = {
        'test-haemoglobin': 'Haemoglobin (FBC)',
        'test-haematocrit': 'Haematocrit (FBC)',
        'test-wcc': 'White Cell Count (FBC)',
        'test-pt': 'PT — Prothrombin Time (Coag)',
        'test-aptt': 'APTT (Coag)',
        'test-neutrophils': 'Neutrophils (Diff)'
    };
    const assigned = [];
    Object.entries(testMap).forEach(([id, test]) => {
        const el = document.getElementById(id);
        if (el && el.checked) assigned.push(test);
    });
    if (assigned.length === 0) { toast('At least one test type must be assigned.', 'error'); return; }
    tech.tests = assigned;
    // Update the badge in the table
    const rows = document.querySelectorAll('#tech-table tbody tr');
    rows.forEach(row => {
        const nameCell = row.querySelector('td:nth-child(1) .user-cell-name');
        if (nameCell && nameCell.textContent.trim() === name) {
            const badge = row.querySelector('td:nth-child(5) .badge');
            if (badge) badge.textContent = `${assigned.length} tests`;
        }
    });
    closeModal('modal-assign-tests');
    toast(`Test assignments updated for ${name}.`);
}



/* ══════════════════════════════════════════════════════════════
   REPORTS
   ══════════════════════════════════════════════════════════════ */
function generateReport() {
    const from = getVal('report-from'), to = getVal('report-to');
    if (!from || !to) { toast('Please select a date range.', 'error'); return; }
    if (new Date(from) > new Date(to)) { toast('Start date must be before end date.', 'error'); return; }

    const btn = document.getElementById('gen-btn');
    const ind = document.getElementById('generating-indicator');
    const suc = document.getElementById('report-success');
    const fmt = d => new Date(d).toLocaleDateString('en-ZA', { day: '2-digit', month: 'short', year: 'numeric' });

    if (btn) btn.style.display = 'none';
    if (suc) suc.style.display = 'none';
    if (ind) ind.classList.add('show');

    setTimeout(() => {
        if (ind) ind.classList.remove('show');
        if (btn) btn.style.display = '';
        if (suc) {
            suc.style.display = 'flex';
            const msgEl = document.getElementById('report-success-msg');
            if (msgEl) msgEl.textContent = `Tests_by_Category_${from}_${to}.pdf  ·  ${fmt(from)} – ${fmt(to)}`;
        }
        toast(`PDF report generated: ${fmt(from)} – ${fmt(to)}`);
    }, 1800);
}


/* ══════════════════════════════════════════════════════════════
   PASSWORD / PROFILE  (shared)
   ══════════════════════════════════════════════════════════════ */
function changePassword() {
    const curr = getVal('pw-current'), nw = getVal('pw-new'), conf = getVal('pw-confirm');
    if (!validateAll([['pw-current', curr], ['pw-new', nw], ['pw-confirm', conf]])) return;
    if (nw !== conf) { markError('pw-confirm'); toast('Passwords do not match.', 'error'); return; }
    if (!/^(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).{8,}$/.test(nw)) {
        markError('pw-new');
        toast('Password needs 8+ chars, uppercase, number and special character.', 'error'); return;
    }
    ['pw-current', 'pw-new', 'pw-confirm'].forEach(id => setVal(id, ''));
    toast('Password changed successfully.');
}

function changeEmail() {
    const email = getVal('new-email'), pw = getVal('email-pw');
    if (!validateAll([['new-email', email.includes('@') ? email : ''], ['email-pw', pw]])) return;
    toast(`Username updated to ${email}`);
}

let _profileEditing = false;
function toggleEdit(cancel = false) {
    if (cancel) _profileEditing = false; else _profileEditing = !_profileEditing;
    ['p-fname', 'p-lname', 'p-cell', 'p-email', 'p-addr'].forEach(id => {
        const el = document.getElementById(id);
        if (el) el.readOnly = !_profileEditing;
    });
    const badge = document.getElementById('edit-status');
    if (badge) { badge.textContent = _profileEditing ? 'Editing' : 'Read Only'; badge.className = _profileEditing ? 'badge amber' : 'badge green'; }
    const actions = document.getElementById('profile-actions');
    if (actions) actions.style.display = _profileEditing ? 'flex' : 'none';
}
function saveProfile() { toggleEdit(true); toast('Profile updated successfully.'); }


/* ══════════════════════════════════════════════════════════════
   CONSENT  (Patient subsystem)
   ══════════════════════════════════════════════════════════════ */
let _revokeCardId = null;

function openRevoke(name, cardId) {
    _revokeCardId = cardId;
    setTitleText('revoke-title', `Revoke ${name}'s access?`);
    const btn = document.getElementById('revoke-confirm-btn');
    if (btn) btn.onclick = () => {
        const card = document.getElementById(_revokeCardId);
        if (card) { card.style.opacity = '0'; card.style.transition = 'opacity .3s'; setTimeout(() => card.remove(), 300); }
        closeModal('modal-revoke');
        toast(`${name}'s access revoked.`, 'warning');
    };
    openModal('modal-revoke');
}

function grantAccess() {
    if (!getVal('grant-hpcsa')) { markError('grant-hpcsa'); toast('Enter an HPCSA number.', 'error'); return; }
    closeModal('modal-grant');
    toast('Access granted — doctor notified via email.');
}

// Live doctor lookup on consent page
document.addEventListener('input', e => {
    if (e.target.id === 'grant-hpcsa') {
        const el = document.getElementById('grant-doctor-found');
        if (el) el.style.display = e.target.value.length >= 6 ? 'block' : 'none';
    }
});


/* ══════════════════════════════════════════════════════════════
   MEDICAL HISTORY  (Patient subsystem)
   ══════════════════════════════════════════════════════════════ */
function saveItem(modalId, msg) { closeModal(modalId); toast(msg); }

function removeItem(id, msg) {
    const el = document.getElementById(id);
    if (el) { el.style.cssText += 'opacity:0;transform:translateX(8px);transition:all .3s'; setTimeout(() => el.remove(), 320); }
    toast(msg, 'warning');
}


/* ══════════════════════════════════════════════════════════════
   PATIENT RESULTS FILTER
   ══════════════════════════════════════════════════════════════ */
function filterResults() {
    const val = getVal('result-filter');
    document.querySelectorAll('.result-group').forEach(g => {
        g.style.display = (!val || g.dataset.req === val) ? '' : 'none';
    });
    const al = document.getElementById('abnormal-alert');
    if (al) al.style.display = (!val || val === 'REQ-2025-0038') ? '' : 'none';
}

function filterByStatus() {
    const val = getVal('filter-status').toLowerCase();
    document.querySelectorAll('#requests-table tbody tr').forEach(row => {
        row.style.display = (!val || row.textContent.toLowerCase().includes(val)) ? '' : 'none';
    });
}


/* ══════════════════════════════════════════════════════════════
   UTILITY FUNCTIONS
   ══════════════════════════════════════════════════════════════ */

/** Set textContent of an element by id (safe, no error if missing) */
function setTitleText(id, text) {
    const el = document.getElementById(id);
    if (el) el.textContent = text;
}

/**
 * Validate an array of [fieldId, value] pairs.
 * Returns true if all pass, false + marks errors if any fail.
 * @param {Array} checks  - [ ['input-id', valueToCheck], ... ]
 */
function validateAll(checks) {
    let valid = true;
    checks.forEach(([id, val]) => {
        if (!val) { markError(id); valid = false; }
    });
    if (!valid) toast('Please fill in all required fields.', 'error');
    return valid;
}

/**
 * Wire a confirm button inside a modal to close + toast.
 * Useful for delete/deactivate confirms without writing separate handlers.
 */
function wireConfirm(btnId, modalId, message, type = 'default') {
    const btn = document.getElementById(btnId);
    if (btn) btn.onclick = () => { closeModal(modalId); toast(message, type); };
}


/* ══════════════════════════════════════════════════════════════
   DYNAMIC STYLE INJECTION
   Adds the minimal CSS needed for toast, modal open/close,
   field errors, tabs, and spinner — in case your style.css
   doesn't yet define them.
   ══════════════════════════════════════════════════════════════ */
(function () {
    const s = document.createElement('style');
    s.textContent = `
    /* ── Toast ── */
    #toast {
      position:fixed; bottom:28px; right:28px;
      background:#0B1929; color:#fff;
      padding:12px 18px; border-radius:10px;
      font-size:13px; font-weight:500;
      box-shadow:0 8px 32px rgba(10,30,60,.14);
      transform:translateY(80px); opacity:0;
      transition:all .3s cubic-bezier(.34,1.56,.64,1);
      z-index:9999; max-width:340px;
      border-left:4px solid #00C9A7;
      pointer-events:none;
      font-family:system-ui,sans-serif;
    }
    #toast.show { transform:translateY(0); opacity:1; }

    /* ── Modal overlay ── */
    .modal-overlay {
      display:none; position:fixed; inset:0;
      background:rgba(10,25,50,.55);
      backdrop-filter:blur(3px);
      z-index:200; align-items:center; justify-content:center;
    }
    .modal-overlay.open { display:flex !important; }

    /* ── Field error ── */
    .field-error { border-color:#FF4757 !important; box-shadow:0 0 0 3px rgba(255,71,87,.12) !important; }

    /* ── Tabs ── */
    .tab-panel { display:none; }
    .tab-panel.active { display:block; }

    /* ── Spinner / generating ── */
    .generating { display:none; align-items:center; gap:10px; color:#7A8CA0; font-size:13px; }
    .generating.show { display:flex; }
    .spinner {
      width:16px; height:16px;
      border:2px solid #E3E8F0; border-top-color:#00C9A7;
      border-radius:50%; animation:_sp .7s linear infinite;
    }
    @keyframes _sp { to { transform:rotate(360deg); } }
  `;
    document.head.appendChild(s);
})();


/* ══════════════════════════════════════════════════════════════
   DOM READY — final wiring
   ══════════════════════════════════════════════════════════════ */
document.addEventListener('DOMContentLoaded', function () {

    // Hide all modals on load
    document.querySelectorAll('.modal-overlay').forEach(el => {
        if (!el.classList.contains('open')) el.style.display = 'none';
    });

    // Auto-wire the topbar primary button based on current page
    const page = location.pathname.split('/').pop();
    const topBtn = document.querySelector('.topbar .btn, .topbar-actions .btn, .topbar button.btn');
    if (topBtn && !topBtn.getAttribute('onclick')) {
        const pageMap = {
            'categories.html': openAddCategory,
            'test-types.html': openAddTestType,
            'consumables.html': openAddConsumable,
            'suppliers.html': openAddSupplier,
            'orders.html': () => openModal('modal-order'),
            'doctors.html': openAddDoctor,
            'technicians.html': openAddTechnician,
        };
        if (pageMap[page]) topBtn.addEventListener('click', pageMap[page]);
    }
});


/* ============================================================
   SHARED PAGE WIRING
   Makes sparse pages interactive even when their HTML only has
   tables and placeholder buttons.
   ============================================================ */
const PAGE_CONFIGS = {
    'categories.html': {
        title: 'Test Categories',
        actionLabel: 'Add Category',
        action: () => openAddCategory(),
        tableId: 'categories-table',
    },
    'test-types.html': {
        title: 'Test Types',
        actionLabel: 'Add Test Type',
        action: () => openAddTestType(),
        tableId: 'testtypes-table',
    },
    'consumables.html': {
        title: 'Consumables',
        actionLabel: 'Add Consumable',
        action: () => openAddConsumable(),
        tableId: 'consumables-table',
    },
    'orders.html': {
        title: 'Orders',
        actionLabel: 'Create Order',
        action: () => openModal('modal-order'),
        tableId: 'orders-table',
    },
    'manager-reports.html': {
        title: 'Management Reports',
        actionLabel: 'Generate Report',
        action: () => generateReport(),
        tableId: '',
    },
    'suppliers.html': {
        title: 'Suppliers',
        actionLabel: 'Add Supplier',
        action: () => openAddSupplier(),
        tableId: 'suppliers-table',
    },
    'doctors.html': {
        title: 'Doctor Accounts',
        actionLabel: 'Add Doctor',
        action: () => openAddDoctor(),
        tableId: 'doctors-table',
    },
    'technicans.html': {
        title: 'Lab Technicians',
        actionLabel: 'Add Technician',
        action: () => openAddTechnician(),
        tableId: 'tech-table',
    },
    'technicians.html': {
        title: 'Lab Technicians',
        actionLabel: 'Add Technician',
        action: () => openAddTechnician(),
        tableId: 'tech-table',
    },
};

function currentPageName() {
    const page = (location.pathname.split('/').pop() || 'dashboard').toLowerCase();
    const map = {
        dashboard: 'dashboard.html',
        categories: 'categories.html',
        testtypes: 'test-types.html',
        consumables: 'consumables.html',
        orders: 'orders.html',
        managerreports: 'manager-reports.html',
        suppliers: 'suppliers.html',
        doctors: 'doctors.html',
        technicians: 'technicans.html',
    };
    return map[page] || page;
}

function initPage(options = {}) {
    const page = options.page ? `${options.page}.html` : currentPageName();
    const config = { ...(PAGE_CONFIGS[page] || {}), ...options };
    const topbar = document.getElementById('topbar');
    if (!topbar) return;

    topbar.classList.add('topbar');
    topbar.innerHTML = `
    <h1 class="page-title">${config.title || document.title || 'NMB-HLabSys'}</h1>
    <div class="topbar-actions">
      <input type="search" class="form-control search-control" placeholder="Search..." aria-label="Search this page">
      ${config.actionLabel ? `<button class="btn btn-primary" type="button">${config.actionLabel}</button>` : ''}
      <button class="btn btn-secondary notify-btn" type="button" title="Notifications">Alerts</button>
    </div>
  `;

    const search = topbar.querySelector('input[type="search"]');
    if (search) {
        search.addEventListener('input', () => {
            if (typeof window.onSearch === 'function') window.onSearch(search.value);
            else if (config.tableId) filterTable(config.tableId, search.value);
            else handleSearch(search.value);
        });
    }

    const action = config.action || (config.actionModal ? () => openModal(config.actionModal) : null);
    const actionBtn = topbar.querySelector('.btn-primary');
    if (actionBtn && action) actionBtn.addEventListener('click', action);
    const notifyBtn = topbar.querySelector('.notify-btn');
    if (notifyBtn) notifyBtn.addEventListener('click', showNotifications);
}

function ensureModal(id, title, bodyHtml, footerHtml) {
    let overlay = document.getElementById(id);
    if (overlay) return overlay;

    overlay = document.createElement('div');
    overlay.className = 'modal-overlay';
    overlay.id = id;
    overlay.innerHTML = `
    <div class="modal">
      <div class="modal-header">
        <span class="modal-title" id="${id}-title">${title}</span>
        <button class="modal-close" type="button" onclick="closeModal('${id}')">x</button>
      </div>
      <div class="modal-body">${bodyHtml}</div>
      <div class="modal-footer">${footerHtml}</div>
    </div>
  `;
    document.body.appendChild(overlay);
    return overlay;
}

function ensureCategoryModal() {
    ensureModal(
        'modal-category',
        'Add Category',
        `
      <div class="form-group">
        <label class="form-label">Category Name *</label>
        <input class="form-control" id="cat-name" required>
      </div>
      <div class="form-group">
        <label class="form-label">Description</label>
        <textarea class="form-control" id="cat-desc" rows="3"></textarea>
      </div>
    `,
        `
      <button class="btn btn-secondary" type="button" onclick="closeModal('modal-category')">Cancel</button>
      <button class="btn btn-primary" type="button" onclick="saveCategory()">Save Category</button>
    `
    );
    const title = document.getElementById('modal-category-title') || document.getElementById('modal-category-title-fallback');
    if (!title) {
        const el = document.getElementById('modal-category-title') || document.getElementById('modal-category-title');
        void el;
    }
    document.getElementById('modal-category-title')?.removeAttribute('id');
    const modalTitle = document.querySelector('#modal-category .modal-title');
    if (modalTitle) modalTitle.id = 'modal-category-title';
}

function ensureDeactivateModal() {
    ensureModal(
        'modal-deactivate',
        'Delete Item',
        `
      <div class="confirm-icon">!</div>
      <div class="confirm-title" id="deactivate-title">Delete this item?</div>
      <div class="confirm-msg">This item will be deleted from this demo screen.</div>
    `,
        `
      <button class="btn btn-secondary" type="button" onclick="closeModal('modal-deactivate')">Cancel</button>
      <button class="btn btn-danger" type="button" id="deactivate-confirm-btn">Delete</button>
    `
    );
}

function ensureTestTypeModal() {
    ensureModal(
        'modal-testtype',
        'Add Test Type',
        `
      <div class="form-group">
        <label class="form-label">Test Name *</label>
        <input class="form-control" id="tt-name" required>
      </div>
      <div class="form-row">
        <div class="form-group">
          <label class="form-label">Category *</label>
          <select class="form-control" id="tt-cat">
            <option value="">Select category</option>
            <option>Full Blood Count</option>
            <option>Coagulation Studies</option>
            <option>Differential Count</option>
          </select>
        </div>
        <div class="form-group">
          <label class="form-label">Unit *</label>
          <input class="form-control" id="tt-unit" placeholder="e.g. g/dL">
        </div>
      </div>
      <div class="form-row">
        <div class="form-group">
          <label class="form-label">Minimum Range</label>
          <input class="form-control" id="tt-min" type="number" step="0.1">
        </div>
        <div class="form-group">
          <label class="form-label">Maximum Range</label>
          <input class="form-control" id="tt-max" type="number" step="0.1">
        </div>
      </div>
      <div class="form-group">
        <label class="form-label">Turnaround Time *</label>
        <input class="form-control" id="tt-tat" placeholder="e.g. 45 min">
      </div>
    `,
        `
      <button class="btn btn-secondary" type="button" onclick="closeModal('modal-testtype')">Cancel</button>
      <button class="btn btn-primary" type="button" onclick="saveTestType()">Save Test Type</button>
    `
    );
    const modalTitle = document.querySelector('#modal-testtype .modal-title');
    if (modalTitle) modalTitle.id = 'tt-modal-title';
}

function ensureTestDeactivateModal() {
    ensureModal(
        'modal-deactivate-tt',
        'Delete Test Type',
        `
      <div class="confirm-icon">!</div>
      <div class="confirm-title" id="deactivate-tt-title">Delete this test?</div>
      <div class="confirm-msg">The test type will be deleted from new requests.</div>
    `,
        `
      <button class="btn btn-secondary" type="button" onclick="closeModal('modal-deactivate-tt')">Cancel</button>
      <button class="btn btn-danger" type="button" onclick="closeModal('modal-deactivate-tt');toast('Test type deleted.','warning')">Delete</button>
    `
    );
}

function ensureConsumableModal() {
    ensureModal(
        'modal-consumable',
        'Add Consumable',
        `
      <div class="form-group">
        <label class="form-label">Name *</label>
        <input class="form-control" id="cons-name" required>
      </div>
      <div class="form-group">
        <label class="form-label">Supplier *</label>
        <select class="form-control" id="cons-supplier">
          <option value="">Select supplier</option>
          <option>PathSupply SA</option>
          <option>LabChem Distributors</option>
          <option>MedLab Supplies</option>
        </select>
      </div>
      <div class="form-row">
        <div class="form-group">
          <label class="form-label">Reorder Level *</label>
          <input class="form-control" id="cons-reorder" type="number" min="0">
        </div>
        <div class="form-group">
          <label class="form-label">Current Quantity *</label>
          <input class="form-control" id="cons-qty" type="number" min="0">
        </div>
      </div>
    `,
        `
      <button class="btn btn-secondary" type="button" onclick="closeModal('modal-consumable')">Cancel</button>
      <button class="btn btn-primary" type="button" onclick="saveConsumable()">Save Consumable</button>
    `
    );
    const modalTitle = document.querySelector('#modal-consumable .modal-title');
    if (modalTitle) modalTitle.id = 'cons-modal-title';
}

function ensureAdjustModal() {
    ensureModal(
        'modal-adjust',
        'Adjust Stock',
        `
      <div class="form-section-title" id="adjust-name">Stock Item</div>
      <div class="form-hint" id="adjust-info"></div>
      <div class="form-group">
        <label class="form-label">Adjustment Type</label>
        <select class="form-control" id="adjust-type">
          <option value="increase">Increase</option>
          <option value="decrease">Decrease</option>
          <option value="set">Set quantity</option>
        </select>
      </div>
      <div class="form-group">
        <label class="form-label" id="adjust-qty-label">Quantity *</label>
        <input class="form-control" id="adjust-qty" type="number" min="0">
      </div>
    `,
        `
      <button class="btn btn-secondary" type="button" onclick="closeModal('modal-adjust')">Cancel</button>
      <button class="btn btn-primary" type="button" onclick="applyAdjustment()">Apply Adjustment</button>
    `
    );
}

function ensureSampleTypeModal() {
    ensureModal(
        'modal-sample-type',
        'Add Sample Type',
        `
      <div class="form-group">
        <label class="form-label">Sample Type Name *</label>
        <input class="form-control" id="sample-type-name" placeholder="e.g. Cerebrospinal fluid">
      </div>
      <div class="form-group">
        <label class="form-label">Description</label>
        <textarea class="form-control" id="sample-type-desc" rows="3" placeholder="Optional notes for doctors and technicians"></textarea>
      </div>
    `,
        `
      <button class="btn btn-secondary" type="button" onclick="closeModal('modal-sample-type')">Cancel</button>
      <button class="btn btn-primary" type="button" onclick="saveSampleType()">Save Sample Type</button>
    `
    );
}

function openAddSampleType() {
    ensureSampleTypeModal();
    setVal('sample-type-name', '');
    setVal('sample-type-desc', '');
    openModal('modal-sample-type');
}

function saveSampleType() {
    const name = getVal('sample-type-name');
    if (!name) {
        markError('sample-type-name');
        toast('Sample type name is required.', 'error');
        return;
    }

    const list = document.querySelector('.sample-type-list');
    if (list) {
        const badge = document.createElement('span');
        badge.className = 'badge teal';
        badge.textContent = name;
        list.appendChild(badge);
    }
    closeModal('modal-sample-type');
    toast('Sample type saved successfully.');
}

const _openAddCategory = openAddCategory;
openAddCategory = function () {
    ensureCategoryModal();
    _openAddCategory();
};

const _openEditCategory = openEditCategory;
openEditCategory = function (name, desc) {
    ensureCategoryModal();
    _openEditCategory(name, desc);
};

const _openDeactivate = openDeactivate;
openDeactivate = function (name, type) {
    ensureDeactivateModal();
    _openDeactivate(name, type);
};

const _openAddTestType = openAddTestType;
openAddTestType = function () {
    ensureTestTypeModal();
    _openAddTestType();
};

const _openEditTest = openEditTest;
openEditTest = function (name) {
    ensureTestTypeModal();
    _openEditTest(name);
};

const _openDeactivateTest = openDeactivateTest;
openDeactivateTest = function (name) {
    ensureTestDeactivateModal();
    _openDeactivateTest(name);
};

const _openAddConsumable = openAddConsumable;
openAddConsumable = function () {
    ensureConsumableModal();
    _openAddConsumable();
};

const _openEditConsumable = openEditConsumable;
openEditConsumable = function (name, supplier, reorder, qty) {
    ensureConsumableModal();
    _openEditConsumable(name, supplier, reorder, qty);
};

const _openAdjust = openAdjust;
openAdjust = function (name, qty, reorder) {
    ensureAdjustModal();
    _openAdjust(name, qty, reorder);
};

function wireFallbackButtons() {
    document.addEventListener('click', e => {
        const btn = e.target.closest('button');
        if (!btn || btn.getAttribute('onclick') || btn.dataset.wiredFallback) return;
        const row = btn.closest('tr');
        const cells = row ? [...row.children].map(td => td.textContent.trim()) : [];
        const page = currentPageName();

        if (btn.classList.contains('info')) {
            if (page === 'categories.html') openEditCategory(cells[1] || '', cells[2] || '');
            else if (page === 'consumables.html') openEditConsumable(cells[0] || '', cells[1] || '', cells[3] || '', cells[2] || '');
            else toast('Edit action opened.');
        } else if (btn.classList.contains('danger')) {
            openDeactivate(cells[1] || cells[0] || 'item', 'item');
        } else if (btn.classList.contains('success') && page === 'consumables.html') {
            const qty = parseInt(cells[2], 10) || 0;
            const reorder = parseInt(cells[3], 10) || 0;
            openAdjust(cells[0] || 'Consumable', qty, reorder);
        } else {
            toast('Action completed.');
        }
    });
}

function ensureManagerNavLinks() {
    document.querySelectorAll('.nav-menu').forEach(nav => {
        if (!nav.querySelector('a[href="manager-reports.html"]')) {
            const dashboard = nav.querySelector('a[href="dashboard.html"]');
            if (dashboard) {
                dashboard.insertAdjacentHTML(
                    'afterend',
                    '<a href="manager-reports.html" class="nav-item"><span>📄</span> Reports</a>'
                );
            }
        }

        nav.querySelectorAll('.nav-item').forEach(link => {
            if (link.textContent.includes('Supliers')) {
                link.innerHTML = link.innerHTML.replace('Supliers', 'Suppliers');
            }
            if (link.getAttribute('href') === 'doctors.html' && link.textContent.includes('Orders')) {
                link.innerHTML = '<span>👨‍⚕️</span> Doctors';
            }
            if (link.getAttribute('href') === 'technicans.html' && link.textContent.includes('Suppliers')) {
                link.innerHTML = '<span>🧪</span> Technicians';
            }
        });
    });
}

function autoInitCurrentPage() {
    const page = currentPageName();
    const config = PAGE_CONFIGS[page];
    if (config && document.getElementById('topbar') && !document.querySelector('#topbar .topbar-actions')) {
        initPage(config);
    }
}

document.addEventListener('DOMContentLoaded', function () {
    ensureManagerNavLinks();
    autoInitCurrentPage();
    wireFallbackButtons();
});

function normalizeLabManagerUi() {
    document.querySelectorAll('.modal-close').forEach(btn => { btn.innerHTML = '&times;'; });

    document.querySelectorAll('button, .btn-icon').forEach(el => {
        const label = el.textContent.trim();
        if (label === 'Deactivate' || label === 'Remove') el.textContent = 'Delete';
    });

    document.querySelectorAll('.badge').forEach(el => {
        if (el.textContent.trim() === '? Must Change PW') {
            el.innerHTML = '<span aria-hidden="true">&#x26A0;</span> Must Change PW';
        }
    });

    const staticText = [
        ['doc-modal-title', '\uD83D\uDC68\u200D\u2695\uFE0F Add New Doctor'],
        ['tech-modal-title', '\uD83E\uDDEA Add New Technician'],
        ['deactivate-doc-title', 'Delete Doctor?'],
        ['deactivate-tech-name', 'Delete Technician?']
    ];
    staticText.forEach(([id, text]) => {
        const el = document.getElementById(id);
        if (el) el.textContent = text;
    });

    const iconFixes = [
        ['#modal-doctor .alert-icon', '\u2139\uFE0F'],
        ['#modal-deactivate-doc .confirm-icon', '\u26A0'],
        ['#modal-tech .alert-icon', '\u2139\uFE0F'],
        ['#modal-deactivate-tech .confirm-icon', '\u26A0']
    ];
    iconFixes.forEach(([selector, text]) => {
        const el = document.querySelector(selector);
        if (el) el.textContent = text;
    });

    document.querySelectorAll('.modal-title').forEach(el => {
        el.textContent = el.textContent
            .replace('Deactivate Account', 'Delete Account')
            .replace('Remove Supplier', 'Delete Supplier')
            .replace('Remove Manager', 'Delete Manager');
    });
}

function setDangerConfirm(btnId, modalId, message) {
    const btn = document.getElementById(btnId);
    if (!btn) return;
    btn.textContent = 'Delete';
    btn.onclick = () => { closeModal(modalId); toast(message, 'warning'); };
}

openDeactivate = function (name, type) {
    if (typeof ensureDeactivateModal === 'function') ensureDeactivateModal();
    setTitleText('deactivate-title', `Delete "${name}"?`);
    setDangerConfirm('deactivate-confirm-btn', 'modal-deactivate', `"${name}" deleted.`);
    openModal('modal-deactivate');
    normalizeLabManagerUi();
};

openDeactivateTest = function (name) {
    if (typeof ensureTestDeactivateModal === 'function') ensureTestDeactivateModal();
    setTitleText('deactivate-tt-title', `Delete "${name}"?`);
    const btn = document.querySelector('#modal-deactivate-tt .btn-danger');
    if (btn) {
        btn.textContent = 'Delete';
        btn.onclick = () => { closeModal('modal-deactivate-tt'); toast('Test type deleted.', 'warning'); };
    }
    openModal('modal-deactivate-tt');
    normalizeLabManagerUi();
};

openDeleteSupplier = function (name) {
    setTitleText('delete-sup-title', `Delete "${name}"?`);
    setDangerConfirm('delete-sup-confirm', 'modal-delete-sup', `${name} deleted.`);
    openModal('modal-delete-sup');
    normalizeLabManagerUi();
};

openDeactivateDoctor = function (name) {
    setTitleText('deactivate-doc-title', `Delete ${name}?`);
    const btn = document.querySelector('#modal-deactivate-doc .btn-danger');
    if (btn) {
        btn.textContent = 'Delete';
        btn.onclick = () => { closeModal('modal-deactivate-doc'); toast('Account deleted.', 'warning'); };
    }
    openModal('modal-deactivate-doc');
    normalizeLabManagerUi();
};

openDeactivateTech = function (name) {
    setTitleText('deactivate-tech-name', `Delete ${name}?`);
    const btn = document.querySelector('#modal-deactivate-tech .btn-danger');
    if (btn) {
        btn.textContent = 'Delete';
        btn.onclick = () => { closeModal('modal-deactivate-tech'); toast('Account deleted.', 'warning'); };
    }
    openModal('modal-deactivate-tech');
    normalizeLabManagerUi();
};

document.addEventListener('DOMContentLoaded', normalizeLabManagerUi);
