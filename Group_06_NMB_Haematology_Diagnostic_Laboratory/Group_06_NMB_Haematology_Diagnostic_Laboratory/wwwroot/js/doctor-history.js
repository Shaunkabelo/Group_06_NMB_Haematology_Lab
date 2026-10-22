(() => {
    const script = document.currentScript || document.querySelector('script[src*="doctor-history.js"]');
    const config = script?.dataset || {};
    window.loadDoctorPatientHistory = async (patient, prefix) => {
        const history = document.getElementById(`${prefix}request-history`);
        const results = document.getElementById(`${prefix}consent-results`);
        if (!history || !results) return;
        const patientId = patient?.patientId ?? patient?.PatientId;
        const marker = String(patientId);
        history.dataset.patient = marker;
        history.textContent = results.textContent = 'Loading history...';
        if (!patientId) { history.textContent = results.textContent = 'Select a patient.'; return; }
        try {
            if (!config.historyUrl) throw new Error('Patient history is not available right now.');
            const historyUrl = new URL(config.historyUrl, document.baseURI);
            historyUrl.searchParams.set('patientId', String(patientId));
            const response = await fetch(historyUrl, { cache: 'no-store', credentials: 'same-origin', headers: { Accept: 'application/json' } });
            if (!response.ok || response.redirected) throw new Error('Unable to load patient history.');
            const rows = await response.json();
            if (history.dataset.patient !== marker) return;
            history.replaceChildren(); results.replaceChildren();
            function append(panel, r, pdf) {
                const item = document.createElement('div'); item.className = 'history-item';
                const label = document.createElement('div');
                label.textContent = `#${r.id} — ${r.status} — ${new Date(r.createdAt).toLocaleDateString()} — ${r.tests.join(', ')}`;
                const link = document.createElement('a'); link.className = 'btn btn-secondary';
                const target = pdf ? config.pdfUrl : config.detailsUrl;
                if (!target) return;
                const targetUrl = new URL(target, document.baseURI);
                targetUrl.searchParams.set('id', String(r.id));
                link.href = targetUrl.toString();
                link.textContent = pdf ? 'Download results PDF' : 'View request';
                item.append(label, link); panel.append(item);
            }
            rows.forEach(r => append(history, r, false));
            const consented = rows.filter(r => r.consentGranted && r.status === 'Released by doctor');
            consented.forEach(r => append(results, r, true));
            if (!rows.length) history.textContent = 'No accessible request history.';
            if (!consented.length) results.textContent = 'No previous results shared with you by this patient.';
        } catch (error) {
            if (history.dataset.patient === marker) history.textContent = results.textContent = error.message;
        }
    };
})();
