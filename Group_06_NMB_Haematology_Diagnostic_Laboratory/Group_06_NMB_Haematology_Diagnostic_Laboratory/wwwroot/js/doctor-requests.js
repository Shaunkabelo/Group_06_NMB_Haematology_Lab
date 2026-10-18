(() => {
    const config = document.getElementById('doctor-requests-script').dataset;
    const tbody = document.getElementById('requests-tbody');
    let requests = [], pendingId = null, saving = false;
    const escape = value => String(value ?? '').replace(/[&<>"']/g, c => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
    function render() {
        const search = document.getElementById('requests-search').value.toLowerCase();
        const status = document.getElementById('requests-filter-status').value.toLowerCase();
        const urgency = document.getElementById('requests-filter-urgency').value.toLowerCase();
        const rows = requests.filter(r => (!status || r.status.toLowerCase() === status) &&
            (!urgency || r.urgency.toLowerCase() === urgency) &&
            [r.id, r.patientName, ...r.tests].some(v => String(v).toLowerCase().includes(search)));
        tbody.innerHTML = rows.map(r => `<tr><td>${r.id}</td><td>${escape(r.patientName)}</td>
            <td>${escape(new Date(r.createdAt).toLocaleString())}</td><td>${escape(r.urgency)}</td>
            <td>${escape(r.status)}${r.cancellationReason ? `<small class="cancel-reason-note">${escape(r.cancellationReason)}</small>` : ''}</td>
            <td>${escape(r.tests.join(', '))}</td><td><a class="btn btn-sm btn-primary" href="${config.detailsUrl}?id=${r.id}">View</a>
            ${['submitted','samples received'].includes(r.status.toLowerCase()) ? `<button class="btn btn-sm btn-danger" data-cancel="${r.id}">Cancel</button>` : ''}</td></tr>`).join('') || '<tr><td colspan="7">No matching requests.</td></tr>';
    }
    async function load() {
        try {
            const response = await fetch(config.listUrl, { cache: 'no-store' });
            if (!response.ok || response.redirected) throw new Error('Unable to load requests. Check your connection and sign-in.');
            requests = await response.json(); render();
        } catch (error) { tbody.innerHTML = `<tr><td colspan="7">${escape(error.message)}</td></tr>`; }
    }
    tbody.addEventListener('click', e => {
        const button = e.target.closest('[data-cancel]');
        if (!button) return;
        pendingId = Number(button.dataset.cancel);
        document.getElementById('cancel-request-summary').textContent = `Cancel request #${pendingId}`;
        document.getElementById('cancelled-date').value = new Date().toLocaleString();
        document.getElementById('cancel-request-reason').value = '';
        document.getElementById('modal-cancel-request').classList.add('is-open');
    });
    window.closeCancelRequestModal = () => { if (!saving) document.getElementById('modal-cancel-request').classList.remove('is-open'); };
    window.submitCancelRequest = async () => {
        const reason = document.getElementById('cancel-request-reason').value.trim();
        if (saving || !pendingId) return;
        if (!reason || reason.length > 500) { alert('Enter a reason of 1–500 characters.'); return; }
        saving = true;
        try {
            const response = await fetch(config.cancelUrl, { method: 'POST', headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ testRequestId: pendingId, reason }) });
            const data = await response.json();
            if (!response.ok) throw new Error(data.message || 'Cancellation failed.');
            document.getElementById('modal-cancel-request').classList.remove('is-open');
            await load();
        } catch (error) { alert(error.message); }
        finally { saving = false; }
    };
    ['requests-search','requests-filter-status','requests-filter-urgency'].forEach(id => document.getElementById(id).addEventListener('input', render));
    load();
    setInterval(() => { if (!document.hidden && !saving) load(); }, 30000);
})();
