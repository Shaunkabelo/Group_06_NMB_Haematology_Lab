const assetScript = document.currentScript;
// Requires an authorized DoctorController.GetDoctorAlerts(startDate, endDate)
    // endpoint returning DoctorAlertDto JSON with camelCase property names.
    const alertsUrl = assetScript.dataset.getdoctoralertsUrl;
    const requestDetailsUrl = assetScript.dataset.requestdetailsUrl;
    let alertLoadVersion = 0;
    let selectedAlertFilter = 'all';
    let loadedAlerts = [];
    // Only the saved technician flag qualifies a result as abnormal.
    const isFlaggedAbnormal = item => item.isAbnormal === true;

    function addDays(date, days) {
        const next = new Date(date);
        next.setDate(next.getDate() + days);
        return next;
    }

    function toDateInputValue(date) {
        const year = date.getFullYear();
        const month = String(date.getMonth() + 1).padStart(2, '0');
        const day = String(date.getDate()).padStart(2, '0');
        return `${year}-${month}-${day}`;
    }

    function formatDate(value) {
        if (!value) return 'Not available';
        const date = new Date(value);
        return Number.isNaN(date.getTime()) ? 'Not available' : date.toLocaleDateString();
    }

    function escapeHtml(value) {
        return String(value ?? '')
            .replaceAll('&', '&amp;')
            .replaceAll('<', '&lt;')
            .replaceAll('>', '&gt;')
            .replaceAll('"', '&quot;')
            .replaceAll("'", '&#39;');
    }

    function getSelectedDateRange() {
        const start = document.getElementById('alerts-start-date').value;
        const end = document.getElementById('alerts-end-date').value;
        return { start, end };
    }

    function updatePriorityCounts(alerts) {
        document.getElementById('alerts-total-count').textContent = alerts.length;
        const counts = { abnormal: alerts.filter(isFlaggedAbnormal).length, urgent: 0, routine: 0, stat: 0 };
        for (const item of alerts) {
            const urgency = String(item.urgency ?? '').trim().toLowerCase();
            if (urgency === 'urgent') counts.urgent++;
            else if (urgency === 'routine') counts.routine++;
            else if (urgency === 'stat') counts.stat++;
        }
        document.getElementById('alerts-abnormal-count').textContent = counts.abnormal;
        document.getElementById('alerts-urgent-count').textContent = counts.urgent;
        document.getElementById('alerts-routine-count').textContent = counts.routine;
        document.getElementById('alerts-stat-count').textContent = counts.stat;
    }

    function displayAlerts(alerts) {
        loadedAlerts = alerts;
        updatePriorityCounts(alerts);
        renderVisibleAlerts();
    }

    function renderVisibleAlerts() {
        const tablePanel = document.getElementById('alerts-table-panel');
        const emptyPanel = document.getElementById('alerts-empty-panel');
        const tbody = document.getElementById('alerts-tbody');
        let visibleAlerts;
        if (selectedAlertFilter === 'abnormal') {
            visibleAlerts = loadedAlerts.filter(isFlaggedAbnormal);
        } else if (selectedAlertFilter === 'all') {
            visibleAlerts = loadedAlerts;
        } else {
            visibleAlerts = loadedAlerts.filter(item =>
                String(item.urgency ?? '').trim().toLowerCase() === selectedAlertFilter);
        }

        if (!visibleAlerts.length) {
            tablePanel.hidden = true;
            emptyPanel.hidden = false;
            document.getElementById('alerts-empty-text').textContent = loadedAlerts.length
                ? `No ${selectedAlertFilter.toUpperCase()} results were found for the selected period.`
                : 'No results were found for the selected period.';
            tbody.innerHTML = '';
            return;
        }

        tablePanel.hidden = false;
        emptyPanel.hidden = true;
        tbody.innerHTML = visibleAlerts.map(item => {
            const id = Number(item.testRequestId);
            if (!Number.isSafeInteger(id) || id <= 0) return '';
            const urgency = String(item.urgency ?? '').trim().toLowerCase();
            const badge = urgency === 'stat' ? 'red' : urgency === 'urgent' ? 'amber' : 'blue';
            return `
                <tr class="alert-row" tabindex="0" data-request-id="${id}"
                    aria-label="Open request ${id} details">
                    <td data-label="Date">${escapeHtml(formatDate(item.captureDateTime))}</td>
                    <td data-label="Patient">${escapeHtml(item.patientName)}</td>
                    <td data-label="Test">${escapeHtml(item.testName)}</td>
                    <td data-label="Flag"><span class="badge ${badge}">
                        ${isFlaggedAbnormal(item) ? 'Abnormal' : 'Not flagged abnormal'} (${escapeHtml(item.urgency || 'Unspecified')})
                    </span></td>
                </tr>`;
        }).join('');
    }

    async function renderAlerts() {
        const version = ++alertLoadVersion;
        const { start, end } = getSelectedDateRange();
        const tablePanel = document.getElementById('alerts-table-panel');
        const emptyPanel = document.getElementById('alerts-empty-panel');
        const tbody = document.getElementById('alerts-tbody');
        const applyButton = document.getElementById('alerts-apply-btn');

        if (start && end && start > end) {
            document.getElementById('alerts-range-note').textContent =
                'Start date must be on or before end date.';
            return;
        }

        document.getElementById('alerts-range-note').textContent = !start && !end ? 'Showing all results' : 'Showing results from ' + (start || 'the beginning') + ' to ' + (end || 'the latest result');

        applyButton.disabled = true;
        tablePanel.hidden = false;
        emptyPanel.hidden = true;
        tbody.innerHTML = '<tr><td colspan="4">Loading results...</td></tr>';

        try {
            const url = new URL(alertsUrl, document.baseURI);
            if (start) url.searchParams.set('startDate', start);
            if (end) url.searchParams.set('endDate', end);
            const response = await fetch(url, {
                credentials: 'same-origin',
                headers: { 'Accept': 'application/json' }
            });
            if (!response.ok) throw new Error(`Unable to load alerts (HTTP ${response.status}).`);
            const alerts = await response.json();
            if (!Array.isArray(alerts)) throw new Error('Invalid response from alerts endpoint.');
            if (version !== alertLoadVersion) return;
            displayAlerts(alerts);
        } catch (error) {
            if (version !== alertLoadVersion) return;
            console.error('Doctor alerts:', error);
            updatePriorityCounts([]);
            loadedAlerts = [];
            tbody.innerHTML = '';
            tablePanel.hidden = true;
            emptyPanel.hidden = false;
            document.getElementById('alerts-empty-text').textContent =
                'Unable to load alerts. Please try again.';
        } finally {
            if (version === alertLoadVersion) applyButton.disabled = false;
        }
    }

    function openAlertRequest(row) {
        const requestId = Number(row.dataset.requestId);
        if (!Number.isSafeInteger(requestId) || requestId <= 0) return;
        const url = new URL(requestDetailsUrl, document.baseURI);
        url.searchParams.set('id', String(requestId));
        window.location.assign(url.toString());
    }

    document.addEventListener('DOMContentLoaded', () => {

        const tbody = document.getElementById('alerts-tbody');
        document.querySelectorAll('[data-alert-filter]').forEach(button => {
            button.addEventListener('click', () => {
                selectedAlertFilter = button.dataset.alertFilter || 'all';
                document.querySelectorAll('[data-alert-filter]').forEach(card => {
                    const selected = card === button;
                    card.classList.toggle('is-selected', selected);
                    card.setAttribute('aria-pressed', String(selected));
                });
                renderVisibleAlerts();
            });
        });
        tbody.addEventListener('click', event => {
            const row = event.target.closest('.alert-row');
            if (row) openAlertRequest(row);
        });
        tbody.addEventListener('keydown', event => {
            if (event.key !== 'Enter' && event.key !== ' ') return;
            const row = event.target.closest('.alert-row');
            if (!row) return;
            event.preventDefault();
            openAlertRequest(row);
        });
        renderAlerts();
        setInterval(() => { if (!document.hidden && !document.getElementById('alerts-apply-btn').disabled) renderAlerts(); }, 30000);
    });
