(() => {
    const form = document.getElementById('generate-report-form');
    const list = document.getElementById('recent-reports');
    const message = document.getElementById('report-message');
    const button = document.getElementById('generate-report-button');
    let reports = [];
    function renderReports() {
        list.replaceChildren();
        reports.sort((a, b) => new Date(b.generatedAt) - new Date(a.generatedAt));
        if (!reports.length) {
            const empty = document.createElement('p');
            empty.textContent = 'No reports generated yet.';
            list.append(empty);
        }
        for (const report of reports) {
            const row = document.createElement('div');
            row.className = 'list-row';
            const details = document.createElement('div');
            const title = document.createElement('strong');
            title.textContent = report.title;
            const summary = document.createElement('small');
            summary.textContent = `${report.testCount} tests across ${report.categoryCount} categories`;
            details.append(title, summary);
            const download = document.createElement('a');
            download.className = 'btn btn-secondary btn-sm';
            download.textContent = 'Download';
            download.href = `${list.dataset.downloadUrl}?id=${encodeURIComponent(report.id)}`;
            download.setAttribute('aria-label', `Download ${report.title}`);
            row.append(details, download);
            list.append(row);
        }
    }
    const initialLoad = fetch(list.dataset.listUrl, { cache: 'no-store' })
        .then(async response => {
            if (!response.ok || response.redirected) throw new Error();
            reports = await response.json();
            renderReports();
        }).catch(() => { list.textContent = 'Could not load recent reports. Please refresh to try again.'; });
    form.addEventListener('submit', async event => {
        event.preventDefault();
        const data = new FormData(form);
        if (data.get('From') > data.get('To')) {
            message.textContent = 'Date From must be on or before Date To.';
            return;
        }
        button.disabled = true;
        button.textContent = 'Generating...';
        message.textContent = '';
        try {
            await initialLoad;
            const response = await fetch(form.action, { method: 'POST', body: data });
            if (response.redirected) throw new Error('Your session has expired. Please sign in again.');
            const result = await response.json().catch(() => null);
            if (!response.ok || !result) throw new Error(result?.message || 'Could not generate the report. Please try again.');
            reports.unshift(result);
            renderReports();
            message.textContent = 'PDF report generated. Download it from Recent Reports.';
        } catch (error) {
            message.textContent = error.message || 'Could not generate the report. Please try again.';
        } finally {
            button.disabled = false;
            button.textContent = 'Generate PDF Report';
        }
    });
})();
