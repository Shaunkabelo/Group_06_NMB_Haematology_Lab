(() => {
    const config = document.getElementById('doctor-requests-script').dataset;
    const tbody = document.getElementById('requests-tbody');
    const dialog = document.getElementById('cancel-request-dialog');
    const form = document.getElementById('cancel-request-form');
    const reasonInput = document.getElementById('cancel-request-reason');
    const errorMessage = document.getElementById('cancel-request-error');
    const actionMessage = document.getElementById('request-action-message');
    const confirmButton = document.getElementById('confirm-cancel-request');
    let requests = [], pendingId = null, saving = false;
    const normalizeStatus = value => String(value || '').trim().toLowerCase().replace('sample(s) received', 'samples received');
    const statusColors = new Map([
        ['submitted', 'blue'],
        ['samples received', 'amber'],
        ['in progress', 'blue'],
        ['completed', 'green'],
        ['released by doctor', 'green'],
        ['cancelled', 'red']
    ]);
    const escape = value => String(value ?? '').replace(/[&<>"']/g, c => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
    function render() {
        const search = document.getElementById('requests-search').value.toLowerCase();
        const status = document.getElementById('requests-filter-status').value.toLowerCase();
        const urgency = document.getElementById('requests-filter-urgency').value.toLowerCase();
        const rows = requests.filter(r => (!status || normalizeStatus(r.status) === status) &&
            (!urgency || r.urgency.toLowerCase() === urgency) &&
            [r.id, r.patientName, ...r.tests].some(v => String(v).toLowerCase().includes(search)));
        tbody.innerHTML = rows.map(r => `<tr><td>${r.id}</td><td>${escape(r.patientName)}</td>
            <td>${escape(new Date(r.createdAt).toLocaleString())}</td><td>${escape(r.urgency)}</td>
            <td><span class="badge ${statusColors.get(normalizeStatus(r.status)) || 'gray'}">${escape(r.status)}</span>${r.cancellationReason ? `<small class="cancel-reason-note">${escape(r.cancellationReason)}</small>` : ''}</td>
            <td>${escape(r.tests.join(', '))}</td><td><a class="btn btn-sm btn-primary" href="${config.detailsUrl}?id=${r.id}">View</a>
            ${['submitted','samples received'].includes(normalizeStatus(r.status)) ? `<button type="button" class="btn btn-sm btn-danger" data-cancel="${r.id}">Cancel</button>` : ''}</td></tr>`).join('') || '<tr><td colspan="7">No matching requests.</td></tr>';
    }
    async function load() {
        try {
            const response = await fetch(config.listUrl, { cache: 'no-store' });
            if (!response.ok || response.redirected) throw new Error('Unable to load requests. Check your connection and sign-in.');
            const rows = await response.json();
            if (!Array.isArray(rows)) throw new Error('The server returned an unexpected request list.');
            requests = rows; render();
        } catch (error) { tbody.innerHTML = `<tr><td colspan="7">${escape(error.message)}</td></tr>`; }
    }
    tbody.addEventListener('click', e => {
        const button = e.target.closest('[data-cancel]');
        if (!button || saving) return;
        const request = requests.find(r => Number(r.id) === Number(button.dataset.cancel));
        if (!request || !['submitted', 'samples received'].includes(normalizeStatus(request.status))) return;
        pendingId = Number(request.id);
        document.getElementById('cancel-request-summary').textContent = `Cancel request #${pendingId} for ${request.patientName}? This stops the test request.`;
        document.getElementById('cancelled-date').value = new Date().toLocaleString();
        reasonInput.value = '';
        reasonInput.removeAttribute('aria-invalid');
        errorMessage.hidden = true;
        actionMessage.hidden = true;
        dialog.showModal();
        reasonInput.focus();
    });
    dialog.querySelectorAll('[data-close-cancel]').forEach(button => button.addEventListener('click', () => {
        if (!saving) dialog.close();
    }));
    dialog.addEventListener('cancel', event => { if (saving) event.preventDefault(); });
    dialog.addEventListener('close', () => { pendingId = null; });
    function showError(message) {
        errorMessage.textContent = message;
        errorMessage.hidden = false;
    }
    form.addEventListener('submit', async event => {
        event.preventDefault();
        const reason = reasonInput.value.trim();
        if (saving || !pendingId) return;
        if (!reason || reason.length > 500) {
            showError('Enter a reason of 1 to 500 characters.');
            reasonInput.setAttribute('aria-invalid', 'true');
            reasonInput.focus();
            return;
        }
        const token = form.querySelector('input[name="__RequestVerificationToken"]')?.value ||
            document.querySelector('meta[name="doctor-csrf"]')?.content;
        if (!token) { showError('Your session could not be verified. Reload the page and try again.'); return; }
        const requestId = pendingId;
        saving = true;
        errorMessage.hidden = true;
        reasonInput.removeAttribute('aria-invalid');
        reasonInput.readOnly = true;
        form.setAttribute('aria-busy', 'true');
        form.querySelectorAll('button').forEach(button => { button.disabled = true; });
        confirmButton.textContent = 'Cancelling...';
        try {
            const response = await fetch(config.cancelUrl, {
                method: 'POST', credentials: 'same-origin',
                headers: { 'Content-Type': 'application/json', 'Accept': 'application/json', 'RequestVerificationToken': token },
                body: JSON.stringify({ testRequestId: requestId, reason })
            });
            if (response.redirected || response.status === 401 || response.status === 403)
                throw new Error('Your session has expired or you do not have access. Sign in again.');
            const data = await response.json().catch(() => null);
            if (!response.ok || data?.success !== true)
                throw new Error(data?.message || 'Cancellation was not confirmed. Refresh the page to check the request status before retrying.');
            dialog.close();
            actionMessage.textContent = `Request #${requestId} cancelled. Your reason has been saved.`;
            actionMessage.hidden = false;
            await load();
            actionMessage.focus();
        } catch (error) {
            showError(error.message || 'Unable to contact the server. Check the request status before retrying.');
        } finally {
            saving = false;
            reasonInput.readOnly = false;
            form.removeAttribute('aria-busy');
            form.querySelectorAll('button').forEach(button => { button.disabled = false; });
            confirmButton.textContent = 'Cancel Request';
        }
    });
    ['requests-search','requests-filter-status','requests-filter-urgency'].forEach(id => document.getElementById(id).addEventListener('input', render));
    load();
    setInterval(() => { if (!document.hidden && !saving && !dialog.open) load(); }, 30000);
})();
