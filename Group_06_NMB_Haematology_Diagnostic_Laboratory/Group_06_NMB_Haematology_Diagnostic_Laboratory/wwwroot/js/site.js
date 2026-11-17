/* NMBHLabSys — site.js
   Handles: toasts, modals, search, forms, tabs, and all page interactions. */

/* ── AUTO SIDEBAR ACTIVE STATE ── */
(function () {
    const page = location.pathname.split('/').pop() || '';
    document.querySelectorAll('.nav-item').forEach(link => {
        const href = (link.getAttribute('href') || '');
        // Match on the last segment of the URL for MVC routes
        const linkPage = href.split('/').pop();
        const currentPage = location.pathname.split('/').pop();
        if (href && location.pathname.toLowerCase().includes(href.toLowerCase().replace(/^\//, ''))) {
            link.classList.add('active');
        }
    });
})();

/* ── TOAST ── */
let _toastTimer = null;
function toast(message, type = 'default') {
    let el = document.getElementById('toast');
    if (!el) { el = document.createElement('div'); el.id = 'toast'; document.body.appendChild(el); }
    const colors = { default: '#00C9A7', error: '#FF4757', warning: '#F5A623', info: '#3D9AE8' };
    el.style.borderLeftColor = colors[type] || colors.default;
    el.textContent = message;
    el.classList.add('show');
    if (_toastTimer) clearTimeout(_toastTimer);
    _toastTimer = setTimeout(() => el.classList.remove('show'), 3300);
}

/* ── MODAL ── */
function openModal(id) {
    const el = document.getElementById(id);
    if (el) { el.style.display = 'flex'; requestAnimationFrame(() => el.classList.add('open')); }
}

function closeModal(id) {
    const el = document.getElementById(id);
    if (el) { el.classList.remove('open'); setTimeout(() => { if (!el.classList.contains('open')) el.style.display = 'none'; }, 200); }
    clearFormErrors();
}

document.addEventListener('click', e => {
    if (e.target.classList.contains('modal-overlay')) closeModal(e.target.id);
});

document.addEventListener('keydown', e => {
    if (e.key === 'Escape')
        document.querySelectorAll('.modal-overlay.open').forEach(m => closeModal(m.id));
});

/* ── FORM HELPERS ── */
function clearFormErrors() {
    document.querySelectorAll('.field-error').forEach(el => { el.style.borderColor = ''; el.classList.remove('field-error'); });
}

function markError(id) {
    const el = document.getElementById(id);
    if (!el) return;
    el.style.borderColor = '#FF4757';
    el.classList.add('field-error');
    el.addEventListener('input', () => { el.style.borderColor = ''; el.classList.remove('field-error'); }, { once: true });
}

function getVal(id) { return (document.getElementById(id)?.value || '').trim(); }
function setVal(id, val) { const el = document.getElementById(id); if (el) el.value = val ?? ''; }
function setTitleText(id, text) { const el = document.getElementById(id); if (el) el.textContent = text; }

function validateAll(checks) {
    let valid = true;
    checks.forEach(([id, val]) => { if (!val) { markError(id); valid = false; } });
    if (!valid) toast('Please fill in all required fields.', 'error');
    return valid;
}

/* ── SEARCH ── */
function handleSearch(query) {
    const q = query.toLowerCase();
    const tables = document.querySelectorAll('table');
    tables.forEach(table => {
        table.querySelectorAll('tbody tr').forEach(row => {
            row.style.display = row.textContent.toLowerCase().includes(q) ? '' : 'none';
        });
    });
}

/* ── TABS ── */
function switchTab(btnEl, panelId) {
    const tabs = btnEl.closest('.tabs');
    if (tabs) tabs.querySelectorAll('.tab-btn').forEach(b => b.classList.remove('active'));
    btnEl.classList.add('active');
    const scope = btnEl.closest('.page-body') || document.body;
    scope.querySelectorAll('.tab-panel').forEach(p => p.classList.remove('active'));
    const panel = document.getElementById(panelId);
    if (panel) panel.classList.add('active');
}

/* ── NOTIFICATIONS ── */
function showNotifications() {
    toast('3 alerts: 1 critical stock · 2 active orders pending', 'warning');
}

/* ── PASSWORD ── */
function togglePassword(inputId, button) {
    const input = document.getElementById(inputId);
    if (!input) return;
    const showing = input.type === 'text';
    input.type = showing ? 'password' : 'text';
    if (button) button.textContent = showing ? 'Show' : 'Hide';
}

function changePassword() {
    const curr = getVal('pw-current'), nw = getVal('pw-new'), conf = getVal('pw-confirm');
    if (!validateAll([['pw-current', curr], ['pw-new', nw], ['pw-confirm', conf]])) return;
    if (nw !== conf) { markError('pw-confirm'); toast('Passwords do not match.', 'error'); return; }
    if (!/^(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).{8,}$/.test(nw)) { markError('pw-new'); toast('Password needs 8+ chars, uppercase, number and special character.', 'error'); return; }
    ['pw-current', 'pw-new', 'pw-confirm'].forEach(id => setVal(id, ''));
    toast('Password changed successfully.');
}

function changeEmail() {
    const email = getVal('new-email'), pw = getVal('email-pw');
    if (!validateAll([['new-email', email.includes('@') ? email : ''], ['email-pw', pw]])) return;
    toast(`Username updated to ${email}`);
}

/* ── PROFILE ── */
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

/* ── REPORTS ── */
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

/* ── RESULTS CAPTURE ── */
function checkAbnormal(i, min, max) {
    const val = parseFloat(document.getElementById(`param-${i}`)?.value);
    const flagEl = document.getElementById(`flag-${i}`);
    const cellEl = document.getElementById(`flag-cell-${i}`);
    const inputEl = document.getElementById(`param-${i}`);
    if (!inputEl) return;
    if (isNaN(val)) { if (flagEl) flagEl.style.display = 'none'; if (cellEl) cellEl.innerHTML = '—'; inputEl.style.borderColor = ''; return; }
    if (val < min) {
        if (flagEl) flagEl.style.display = 'inline'; inputEl.style.borderColor = '#FF4757';
        if (cellEl) cellEl.innerHTML = '<span class="badge red">Low ↓</span>';
    } else if (val > max) {
        if (flagEl) flagEl.style.display = 'inline'; inputEl.style.borderColor = '#FF4757';
        if (cellEl) cellEl.innerHTML = '<span class="badge red">High ↑</span>';
    } else {
        if (flagEl) flagEl.style.display = 'none'; inputEl.style.borderColor = '#2ED573';
        if (cellEl) cellEl.innerHTML = '<span class="badge green">Normal</span>';
    }
}

function saveDraft() { toast('Draft saved.', 'info'); }

function submitResults() {
    const key = document.getElementById('test-selector')?.value;
    if (!key) { toast('No test selected.', 'error'); return; }
    const inputs = document.querySelectorAll('#result-params input[type="number"]');
    let allFilled = true;
    inputs.forEach(inp => { if (!inp.value) { inp.style.borderColor = '#FF4757'; allFilled = false; } });
    if (!allFilled) { toast('Please fill in all result values.', 'error'); return; }
    toast('Results submitted for verification. Status → Completed.');
    const sel = document.getElementById('test-selector');
    if (sel) { sel.value = ''; loadTestForm(); }
}

/* ── TEST PARAMS ── */
const TEST_PARAMS = {
    hgb: { name: 'Haemoglobin', req: 'REQ-2025-0041 · M. Sithole', urgency: 'STAT', urgencyClass: 'red', tat: 'Started 07:30 · ⚠️ OVERDUE', params: [{ name: 'Haemoglobin', unit: 'g/dL', min: 12.0, max: 16.0 }] },
    wbc: { name: 'WBC Count', req: 'REQ-2025-0040 · L. Govender', urgency: 'Urgent', urgencyClass: 'amber', tat: 'Started 08:00 · Due 09:15', params: [{ name: 'WBC Count', unit: '×10³/μL', min: 4.0, max: 11.0 }] },
    plt: { name: 'Platelet Count', req: 'REQ-2025-0038 · K. Mahlangu', urgency: 'Routine', urgencyClass: 'gray', tat: 'Started 08:15 · Due 11:30', params: [{ name: 'Platelet Count', unit: '×10³/μL', min: 150, max: 400 }] },
    ptinr: { name: 'PT/INR', req: 'REQ-2025-0037 · F. Moyo', urgency: 'Routine', urgencyClass: 'gray', tat: 'Started 08:30 · Due 13:00', params: [{ name: 'Prothrombin Time (PT)', unit: 'seconds', min: 11.0, max: 13.5 }, { name: 'INR', unit: 'ratio', min: 0.8, max: 1.2 }] },
    fib: { name: 'Fibrinogen', req: 'REQ-2025-0035 · S. Khoza', urgency: 'Urgent', urgencyClass: 'amber', tat: 'Started 08:40 · Due 14:00', params: [{ name: 'Fibrinogen', unit: 'g/L', min: 2.0, max: 4.0 }] },
};

function loadTestForm() {
    const sel = document.getElementById('test-selector');
    if (!sel) return;
    const key = sel.value;
    const noTest = document.getElementById('no-test-selected');
    const form = document.getElementById('results-form');
    if (!key) {
        if (noTest) noTest.style.display = '';
        if (form) form.style.display = 'none';
        return;
    }
    const t = TEST_PARAMS[key];
    if (!t) return;
    if (noTest) noTest.style.display = 'none';
    if (form) form.style.display = '';
    setTitleText('rf-test-name', t.name);
    setTitleText('rf-req', t.req);
    const urgEl = document.getElementById('rf-urgency');
    if (urgEl) { urgEl.textContent = t.urgency; urgEl.className = `badge ${t.urgencyClass}`; }
    const tatEl = document.getElementById('rf-tat');
    if (tatEl) { tatEl.textContent = t.tat; tatEl.style.color = t.urgencyClass === 'red' ? '#FF4757' : '#7A8CA0'; }
    const tbody = document.getElementById('result-params');
    if (tbody) {
        tbody.innerHTML = t.params.map((p, i) => `
            <tr>
              <td>${p.name}</td>
              <td>
                <div class="result-input-wrap">
                  <input type="number" step="0.01" class="form-control" id="param-${i}"
                    style="width:100px" oninput="checkAbnormal(${i},${p.min},${p.max})" placeholder="—">
                  <span class="abnormal-flag" id="flag-${i}">⚠️ Abnormal</span>
                </div>
              </td>
              <td class="td-muted">${p.unit}</td>
              <td class="range-hint">${p.min} – ${p.max}</td>
              <td id="flag-cell-${i}">—</td>
            </tr>`).join('');
    }
}

/* ── VERIFY ── */
let _vReq = '', _vTest = '';

const VERIFY_RESULTS = {
    'ESR': [{ name: 'ESR', val: '12', unit: 'mm/hr', min: 0, max: 20 }],
    'WBC Differential': [{ name: 'Neutrophils', val: '78', unit: '%', min: 50, max: 70 }, { name: 'Lymphocytes', val: '14', unit: '%', min: 20, max: 40 }, { name: 'Monocytes', val: '8', unit: '%', min: 2, max: 8 }],
    'Platelet Count': [{ name: 'Platelets', val: '68', unit: '×10³/μL', min: 150, max: 400 }],
};

function openVerify(req, test, by, hasAbnormal) {
    _vReq = req; _vTest = test;
    setTitleText('verify-title', `✅ Verify: ${test}`);
    setTitleText('vm-req', req);
    setTitleText('vm-test', test);
    setTitleText('vm-by', by);
    const rows = VERIFY_RESULTS[test] || [];
    const resultsEl = document.getElementById('vm-results');
    if (resultsEl) {
        resultsEl.innerHTML = `
          <table style="width:100%;font-size:13px;border-collapse:collapse">
            <thead><tr style="background:#F5F7FA">
              <th style="padding:8px;text-align:left;color:#7A8CA0;border-bottom:1px solid #E3E8F0">Parameter</th>
              <th style="padding:8px;text-align:left;color:#7A8CA0;border-bottom:1px solid #E3E8F0">Value</th>
              <th style="padding:8px;text-align:left;color:#7A8CA0;border-bottom:1px solid #E3E8F0">Normal Range</th>
              <th style="padding:8px;text-align:left;color:#7A8CA0;border-bottom:1px solid #E3E8F0">Flag</th>
            </tr></thead>
            <tbody>
              ${rows.map(r => {
            const v = parseFloat(r.val);
            const abn = v < r.min || v > r.max;
            const flag = abn ? `<span class="badge red">${v < r.min ? 'Low ↓' : 'High ↑'}</span>` : `<span class="badge green">Normal</span>`;
            return `<tr>
                    <td style="padding:10px 8px;border-bottom:1px solid #E3E8F0">${r.name}</td>
                    <td style="padding:10px 8px;border-bottom:1px solid #E3E8F0;${abn ? 'color:#FF4757;font-weight:700' : ''}">${r.val} ${r.unit}</td>
                    <td style="padding:10px 8px;border-bottom:1px solid #E3E8F0;color:#7A8CA0">${r.min} – ${r.max}</td>
                    <td style="padding:10px 8px;border-bottom:1px solid #E3E8F0">${flag}</td>
                  </tr>`;
        }).join('')}
            </tbody>
          </table>`;
    }
    const radio = document.querySelector('input[name="verify-decision"][value="verify"]');
    if (radio) radio.checked = true;
    const noteGroup = document.getElementById('return-note-group');
    if (noteGroup) noteGroup.style.display = 'none';
    setVal('verify-note', '');
    openModal('modal-verify');
}

function submitVerification() {
    const decision = document.querySelector('input[name="verify-decision"]:checked')?.value;
    if (decision === 'return') {
        const note = getVal('verify-note');
        if (!note) { markError('verify-note'); toast('Please provide a note for the technician.', 'error'); return; }
        closeModal('modal-verify');
        toast(`${_vTest} returned to technician with note.`, 'warning');
    } else {
        closeModal('modal-verify');
        toast(`${_vTest} verified. Doctor notified via email with PDF results.`);
    }
}

function openReviewEdit(req, test) {
    setTitleText('re-title', `🔄 Edit & Resubmit: ${test} (${req})`);
    openModal('modal-review-edit');
}

function resubmitResult() {
    if (!getVal('re-value')) { markError('re-value'); toast('Enter the corrected value.', 'error'); return; }
    if (!getVal('re-note')) { markError('re-note'); toast('Please add a response note.', 'error'); return; }
    closeModal('modal-review-edit');
    toast('Result resubmitted for verification.');
}

/* ── RECEIVE SAMPLES ── */
function receivesSample() {
    const req = getVal('recv-req-no');
    const bc = getVal('recv-barcode');
    const type = getVal('recv-sample-type');
    const dt = getVal('recv-datetime');
    if (!req) { markError('recv-req-no'); toast('Request number is required.', 'error'); return; }
    if (!bc) { markError('recv-barcode'); toast('Barcode is required.', 'error'); return; }
    if (!type) { markError('recv-sample-type'); toast('Sample type is required.', 'error'); return; }
    if (!dt) { markError('recv-datetime'); toast('Date/time is required.', 'error'); return; }
    toast(`Sample ${bc} received for ${req}. Status → Samples Received.`);
    ['recv-req-no', 'recv-barcode', 'recv-datetime'].forEach(id => setVal(id, ''));
    setVal('recv-sample-type', '');
}

let _recv = {};
function quickReceive(req, barcode) {
    _recv = { req, barcode };
    setTitleText('recv-modal-req', req);
    setTitleText('recv-modal-barcode', `Barcode: ${barcode}`);
    openModal('modal-receive');
}
function confirmReceive() {
    closeModal('modal-receive');
    toast(`Sample received for ${_recv.req}. Status → Samples Received.`);
}

/* ── SELECT TEST (Process) ── */
let _selTest = {};
const CONSUMABLES_MAP = {
    'Full Blood Count': 'EDTA Tube × 1,  Diluent Solution × 1',
    'WBC Differential': 'Glass Slide × 2,  Giemsa Stain × 1',
    'PT/INR': 'Citrate Tube × 1,  PT Reagent × 1',
    'ESR': 'ESR Tube × 1',
    'Fibrinogen': 'Citrate Tube × 1,  Fibrinogen Reagent × 1',
};

function openSelectTest(req, test, patient) {
    _selTest = { req, test, patient };
    setTitleText('sel-req-info', `${req} · ${patient}`);
    setVal('sel-test-name', test);
    const consEl = document.getElementById('sel-consumables');
    if (consEl) {
        consEl.innerHTML = (CONSUMABLES_MAP[test] || 'Standard consumables').split(',').map(c => `• ${c.trim()}`).join('<br>');
    }
    openModal('modal-select');
}

function confirmSelect() {
    closeModal('modal-select');
    toast(`${_selTest.test} assigned to you. Consumables deducted. Test is In Progress.`);
}

function filterUrgency(val) {
    document.querySelectorAll('#available-table tbody tr').forEach(row => {
        row.style.display = (!val || row.textContent.toLowerCase().includes(val)) ? '' : 'none';
    });
}

function openTestDetails(req, test) {
    setTitleText('td-title', `🔬 ${test}`);
    setTitleText('td-req', req);
    setTitleText('td-test', test);
    setTitleText('td-start', '08:30');
    setTitleText('td-due', '10:00');
    openModal('modal-test-details');
}

/* ── DASHBOARD SELECT ── */
let _pendingTest = null;
function selectTest(req, test) {
    _pendingTest = { req, test };
    setTitleText('select-test-title', '🔬 Confirm Test Selection');
    setTitleText('select-test-info', `${test}  ·  ${req}`);
    openModal('modal-select-test');
}
function confirmSelectTest() {
    closeModal('modal-select-test');
    if (_pendingTest) toast(`${_pendingTest.test} selected — consumables deducted. Test is now In Progress.`);
}

/* ── DOM READY ── */
document.addEventListener('DOMContentLoaded', function () {
    // Hide all modals on load
    document.querySelectorAll('.modal-overlay').forEach(el => { if (!el.classList.contains('open')) el.style.display = 'none'; });

    // Pre-fill datetime inputs to now
    const now = new Date();
    const local = new Date(now.getTime() - now.getTimezoneOffset() * 60000).toISOString().slice(0, 16);
    ['recv-datetime', 'recv-modal-dt'].forEach(id => { const el = document.getElementById(id); if (el) el.value = local; });

    // Toggle verify note field
    document.addEventListener('change', e => {
        if (e.target.name === 'verify-decision') {
            const ng = document.getElementById('return-note-group');
            if (ng) ng.style.display = e.target.value === 'return' ? '' : 'none';
        }
    });
});

