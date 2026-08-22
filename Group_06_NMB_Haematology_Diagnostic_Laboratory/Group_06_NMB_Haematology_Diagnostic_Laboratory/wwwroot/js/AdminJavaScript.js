// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

function toast(msg) {
    const t = document.getElementById('toast');
    if (!t) return;
    t.textContent = msg;
    t.classList.add('show');
    setTimeout(() => t.classList.remove('show'), 2800);
}

function escapeHtml(str) {
    return String(str ?? '')
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#39;');
}

function setFieldError(input, message) {
    if (!input) return false;
    input.classList.toggle('invalid', Boolean(message));
    let error = input.parentElement.querySelector('.field-error');
    if (message && !error) {
        error = document.createElement('span');
        error.className = 'field-error';
        input.parentElement.appendChild(error);
    }
    if (error) error.textContent = message || '';
    return !message;
}

function validateText(id, label, min, max, pattern) {
    const input = document.getElementById(id);
    const value = input ? input.value.trim() : '';
    let message = '';
    if (!value) message = `${label} is required.`;
    else if (value.length < min) message = `${label} must be at least ${min} characters.`;
    else if (value.length > max) message = `${label} must be ${max} characters or less.`;
    else if (pattern && !pattern.test(value)) message = `${label} contains invalid characters.`;
    return { ok: setFieldError(input, message), value };
}

function validateOptionalText(id, label, max, pattern) {
    const input = document.getElementById(id);
    const value = input ? input.value.trim() : '';
    let message = '';
    if (value.length > max) message = `${label} must be ${max} characters or less.`;
    else if (value && pattern && !pattern.test(value)) message = `${label} contains invalid characters.`;
    return { ok: setFieldError(input, message), value };
}

function validateEmail(id) {
    const input = document.getElementById(id);
    const value = input ? input.value.trim() : '';
    const ok = /^[^\s@]+@[^\s@]+\.[^\s@]{2,}$/.test(value);
    return { ok: setFieldError(input, ok ? '' : 'Enter a valid e-mail address.'), value };
}

function validateSelect(id, label) {
    const input = document.getElementById(id);
    const value = input ? input.value.trim() : '';
    return { ok: setFieldError(input, value ? '' : `${label} is required.`), value };
}

function hasDuplicateName(tableId, name, editRow) {
    return Array.from(document.querySelectorAll(`#${tableId} tbody tr`)).some(row =>
        row !== editRow && row.cells[0].textContent.trim().toLowerCase() === name.toLowerCase()
    );
}

function ensureStatusColumn(tableId) {
    const table = document.getElementById(tableId);
    if (!table || table.dataset.statusReady === 'true') return;
    const header = table.querySelector('thead tr');
    const actionIndex = header ? Array.from(header.cells).findIndex(th => th.textContent.trim() === 'Actions') : -1;
    if (header && !Array.from(header.cells).some(th => th.textContent.trim() === 'Status')) {
        const th = document.createElement('th');
        th.textContent = 'Status';
        header.insertBefore(th, header.cells[actionIndex]);
    }
    table.querySelectorAll('tbody tr').forEach(row => {
        if (!Array.from(row.cells).some(td => td.dataset.role === 'status')) {
            const td = document.createElement('td');
            td.dataset.role = 'status';
            td.innerHTML = '<span class="badge badge-green">Active</span>';
            row.insertBefore(td, row.cells[actionIndex]);
        }
    });
    table.dataset.statusReady = 'true';
}

function setActions(row, editFn) {
    const actions = row.cells[row.cells.length - 1];
    actions.innerHTML = `<div class="action-row">
    <button class="btn btn-secondary btn-sm" type="button" onclick="${editFn}(this)">Edit</button>
    <button class="btn btn-secondary btn-sm" type="button" onclick="toggleRowStatus(this)">Deactivate</button>
  </div>`;
}

function toggleRowStatus(btn) {
    const row = btn.closest('tr');
    if (!row) return;
    const statusCell = Array.from(row.cells).find(td => td.dataset.role === 'status') || row.cells[row.cells.length - 2];
    const active = statusCell.textContent.trim() === 'Active';
    statusCell.innerHTML = active
        ? '<span class="badge badge-gray">Deactivated</span>'
        : '<span class="badge badge-green">Active</span>';
    btn.textContent = active ? 'Activate' : 'Deactivate';
    toast(active ? 'Record deactivated.' : 'Record activated.');
}
