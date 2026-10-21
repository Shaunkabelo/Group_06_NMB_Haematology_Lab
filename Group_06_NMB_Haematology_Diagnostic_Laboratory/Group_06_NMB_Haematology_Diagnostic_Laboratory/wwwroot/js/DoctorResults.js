const assetScript = document.currentScript;
// Loads real laboratory results from the DoctorController endpoint.

    const resultsUrl = assetScript.dataset.getdoctorresultsUrl;
    const resultPdfUrl = assetScript.dataset.downloadresultpdfUrl;
    const releaseResultsUrl = assetScript.dataset.releaseresultsUrl;
    const emailResultsUrl = assetScript.dataset.emailresultsUrl;
    let selectedDeliveryRequest = null;
    let selectedDeliveryIsRelease = true;
    let selectedDelivery = 'Results';
    const expandedResultCards = new Set();
    let resultRequests = [];

    function escapeHtml(value) {
        return String(value ?? '')
            .replaceAll('&', '&amp;')
            .replaceAll('<', '&lt;')
            .replaceAll('>', '&gt;')
            .replaceAll('"', '&quot;')
            .replaceAll("'", '&#39;');
    }

    function formatDate(value) {
        if (!value) return 'Not available';
        const date = new Date(value);
        return Number.isNaN(date.getTime()) ? 'Not available' : date.toLocaleString();
    }

    function normalizeStatus(status, appointmentRequested = false) {
        if (appointmentRequested) return 'Appointment';
        const value = String(status || '').toLowerCase();
        if (value === 'released') return 'Released by doctor';
        return status || 'Unknown';
    }

    function groupResults(rows) {
        const requests = new Map();
        for (const row of rows) {
            const id = row.testRequestId;
            if (!requests.has(id)) {
                requests.set(id, {
                    id,
                    patientName: row.patientName,
                    patientId: row.patientId,
                    patientSaId: row.patientSaId,
                    createdAt: row.requestDate,
                    status: row.requestStatus,
                    appointmentRequested: row.appointmentRequested === true,
                    resultsEmailed: row.resultsEmailed === true,
                    urgency: row.urgency,
                    tests: []
                });
            }
            requests.get(id).tests.push({
                testName: row.testName,
                result: row.resultValue != null
                    ? String(row.resultValue)
                    : (row.resultText || 'Not recorded'),
                notes: row.notes || '',
                abnormal: row.isAbnormal === true,
                verificationStatus: row.verificationStatus || 'Not verified',
                reviewStatus: row.reviewStatus || 'Not reviewed',
                reviewNotes: row.reviewNotes || '',
                captureDateTime: row.captureDateTime
            });
        }
        return [...requests.values()];
    }

    async function loadDoctorResults() {
        const list = document.getElementById('results-list');
        list.innerHTML = '<div class="card"><div class="card-body">Loading results...</div></div>';
        try {
            const response = await fetch(resultsUrl, {
                credentials: 'same-origin',
                headers: { Accept: 'application/json' }
            });
            if (!response.ok || response.redirected) {
                throw new Error(response.status === 401 || response.redirected
                    ? 'Please sign in as a doctor to view results.'
                    : 'The server could not retrieve your results.');
            }
            const rows = await response.json();
            if (!Array.isArray(rows)) throw new Error('Unexpected response from server.');
            resultRequests = groupResults(rows);
            renderResultCards();
        } catch (error) {
            console.error('Doctor results:', error);
            list.textContent = error.message || 'Unable to load results.';
        }
    }

    function renderResultCards(forceRefresh = false) {
        if (forceRefresh) {
            loadDoctorResults();
            return;
        }
        const list = document.getElementById('results-list');
        const searchTerm = document.getElementById('results-search').value.toLowerCase().trim();
        const filtered = resultRequests.filter(item =>
            [item.patientName, item.patientId, item.patientSaId, item.id, item.status, ...item.tests.map(t => t.testName)]
                .some(value => String(value ?? '').toLowerCase().includes(searchTerm))
        );
        if (!filtered.length) {
            list.innerHTML = '<div class="card"><div class="card-body">No laboratory results found.</div></div>';
            return;
        }
        list.innerHTML = filtered.map(item => {
            const testsHtml = item.tests.map(test => `
                <div class="result-item">
                    <div class="result-item-title">
                        <strong>${escapeHtml(test.testName)}</strong>
                        <span class="badge ${test.abnormal ? 'red' : 'blue'}">
                            ${test.abnormal ? 'Abnormal' : 'Not flagged abnormal'}
                        </span>
                    </div>
                    <div class="result-item-details">
                        <div class="result-item-detail"><strong>Result</strong>${escapeHtml(test.result)}</div>
                        <div class="result-item-detail"><strong>Verification</strong>${escapeHtml(test.verificationStatus)}</div>
                        <div class="result-item-detail"><strong>Review</strong>${escapeHtml(test.reviewStatus)}</div>
                        <div class="result-item-detail"><strong>Captured</strong>${escapeHtml(formatDate(test.captureDateTime))}</div>
                    </div>
                    <div class="result-note"><strong>Lab Technician Notes:</strong> ${escapeHtml(test.notes || 'No notes recorded')}</div>
                    ${test.reviewNotes ? `<div class="result-note"><strong>Review notes:</strong> ${escapeHtml(test.reviewNotes)}</div>` : ''}
                </div>
            `).join('');
            const hasAbnormal = item.tests.some(t => t.abnormal);
            const expanded = expandedResultCards.has(item.id);
            const released = String(item.status || '').toLowerCase() === 'released by doctor';
            return `
                <div class="result-card ${expanded ? 'expanded' : ''}">
                    <div class="result-card-header">
                        <div class="result-card-summary">
                            <h2>${escapeHtml(item.patientName)} - Request ${item.id}</h2>
                            <div class="result-meta">${escapeHtml(formatDate(item.createdAt))} · ${item.tests.length} test(s)</div>
                        </div>
                        <div class="result-status">
                            <span class="badge ${item.appointmentRequested ? 'amber' : released ? 'green' : hasAbnormal ? 'amber' : 'blue'}">${escapeHtml(normalizeStatus(item.status, item.appointmentRequested))}</span>
                            <button class="btn btn-secondary result-toggle" type="button" aria-expanded="${expanded}" onclick="toggleResultCard(${item.id})">${expanded ? 'Minimize' : 'Expand'}</button>
                        </div>
                    </div>
                    ${hasAbnormal ? '<div class="result-warning">Contains abnormal results</div>' : ''}
                    <div class="result-card-body">${testsHtml}</div>
                    <div class="result-actions">
                        <a class="btn btn-secondary" href="${resultPdfUrl}?id=${item.id}">Download results PDF</a>
                        ${released
                            ? `<button class="btn btn-primary" type="button" onclick="openResultDelivery(${item.id}, false, 'Results')">Email results</button>
                               <button class="btn btn-secondary" type="button" ${item.resultsEmailed ? 'disabled title="Results have already been emailed"' : ''} onclick="openResultDelivery(${item.id}, false, 'Appointment')">Request appointment</button>`
                            : `<button class="btn btn-primary" type="button" onclick="openResultDelivery(${item.id}, true, 'Results')">Release Results</button>`}
                    </div>
                </div>
            `;
        }).join('');
    }

    function toggleResultCard(requestId) {
        if (expandedResultCards.has(requestId)) expandedResultCards.delete(requestId);
        else expandedResultCards.add(requestId);
        renderResultCards();
    }

    function openResultDelivery(id, release, delivery = 'Results') {
        selectedDeliveryRequest = id;
        selectedDeliveryIsRelease = release;
        selectedDelivery = delivery;
        document.getElementById('result-delivery-title').textContent = `${delivery === 'Appointment' ? 'Request appointment' : release ? 'Release results' : 'Email results'} · #${id}`;
        document.getElementById('result-delivery-description').textContent = delivery === 'Appointment'
            ? 'Send the patient an invitation to contact you and arrange an appointment.'
            : release
                ? 'Release makes the verified results available to the patient. Only release after reviewing every result.'
                : 'Email the released results PDF to the patient.';
        document.getElementById('result-note').value = '';
        document.getElementById('result-delivery-dialog').showModal();
    }
    document.getElementById('result-delivery-form').addEventListener('submit', async event => {
        event.preventDefault();
        const button = document.getElementById('result-send');
        const message = document.getElementById('result-message');
        if (button.disabled) return;
        button.disabled = true;
        try {
            const response = await fetch(selectedDeliveryIsRelease ? releaseResultsUrl : emailResultsUrl, {
                method: 'POST', headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ testRequestId: selectedDeliveryRequest,
                    note: document.getElementById('result-note').value,
                    delivery: selectedDelivery })
            });
            const data = await response.json();
            if (!response.ok) throw new Error(data.message || 'Unable to send results.');
            document.getElementById('result-delivery-dialog').close();
            message.textContent = data.message;
            await loadDoctorResults();
        } catch (error) {
            document.getElementById('result-delivery-dialog').close();
            message.textContent = error.message;
        } finally { button.disabled = false; }
    });
    document.addEventListener('DOMContentLoaded', loadDoctorResults);
