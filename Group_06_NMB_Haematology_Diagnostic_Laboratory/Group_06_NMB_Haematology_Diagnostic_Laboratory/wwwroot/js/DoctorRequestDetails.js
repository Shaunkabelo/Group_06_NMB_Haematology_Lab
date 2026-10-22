const assetScript = document.currentScript ||
    document.querySelector('script[src*="DoctorRequestDetails.js"]');
const detailUrl = assetScript?.dataset.getrequestdataUrl;
    async function loadDetails() {
        const id = assetScript?.dataset.requestId;
        const meta = document.getElementById('request-meta');
        try {
            if (!detailUrl || !id) throw new Error('This request link is incomplete. Return to Test Requests and open it again.');
            const url = new URL(detailUrl, document.baseURI);
            url.searchParams.set('id', id);
            const response = await fetch(url, { cache: 'no-store', credentials: 'same-origin', headers: { Accept: 'application/json' } });
            if (!response.ok || response.redirected) throw new Error('Request not found or access is no longer available.');
            const data = await response.json(), request = data.request;
            meta.textContent = `#${request.id} ? ${request.patientName} ? ${request.status}`;
            document.getElementById('cancellation-panel').textContent = request.cancellationReason
                ? `Cancelled: ${request.cancellationReason} (${new Date(request.cancelledDate).toLocaleString()})` : '';
            function row(body, values) {
                const tr = document.createElement('tr');
                values.forEach(value => { const td = document.createElement('td'); td.textContent = value ?? ''; tr.append(td); });
                body.append(tr);
            }
            const tests = document.getElementById('tests-list'); tests.replaceChildren();
            data.items.forEach(item => row(tests, [item.testType, item.sampleBarcode, item.status]));
            const results = document.getElementById('request-results-list'); results.replaceChildren();
            data.results.forEach(r => row(results, [`${r.testName}: ${r.resultValue ?? r.resultText ?? ''} ${r.unitOfMeasurement || ''}${r.isAbnormal ? ' ? ABNORMAL' : ''}`,
                r.verificationStatus || 'Not verified', r.captureDateTime ? new Date(r.captureDateTime).toLocaleString() : '']));
            if (!data.results.length) row(results, ['No results captured yet.', '', '']);
        } catch (error) { meta.textContent = error.message; }
    }
    loadDetails();
