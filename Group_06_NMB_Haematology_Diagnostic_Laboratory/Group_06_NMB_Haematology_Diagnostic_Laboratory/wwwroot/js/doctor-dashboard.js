(() => {
    const dashboard = document.getElementById("doctor-dashboard");
    if (!dashboard) return;

    const errorPanel = document.getElementById("dashboard-error");
    const requestsHost = document.getElementById("dashboard-recent-requests");
    const alertsHost = document.getElementById("dashboard-recent-alerts");

    const formatDate = value => {
        if (!value) return "Date unavailable";
        const date = new Date(value);
        return Number.isNaN(date.getTime()) ? "Date unavailable" : date.toLocaleDateString();
    };

    const badgeClass = status => {
        switch (String(status || "").toLowerCase()) {
            case "completed":
            case "released":
            case "released by doctor": return "badge green";
            case "in progress": return "badge amber";
            case "cancelled": return "badge red";
            default: return "badge blue";
        }
    };

    function renderRequests(requests) {
        requestsHost.replaceChildren();
        if (!requests.length) {
            requestsHost.textContent = "No test requests have been submitted yet.";
            return;
        }

        for (const request of requests) {
            const row = document.createElement("div");
            row.className = "list-row";

            const details = document.createElement("span");
            const id = document.createElement("strong");
            id.textContent = `Request #${request.id}`;
            const summary = document.createElement("small");
            const tests = request.tests || "No test details";
            summary.textContent = `${request.patientName || "Patient not recorded"} · ${tests} · ${formatDate(request.requestDate)}`;
            details.append(id, summary);

            const status = document.createElement("span");
            status.className = badgeClass(request.status);
            status.textContent = request.status || "Unknown";
            row.append(details, status);
            requestsHost.appendChild(row);
        }
    }

    function renderAlerts(alerts) {
        alertsHost.replaceChildren();
        if (!alerts.length) {
            alertsHost.textContent = "No abnormal results were found in the last 5 days.";
            return;
        }

        for (const alert of alerts) {
            const item = document.createElement("div");
            item.className = "alert danger";
            const marker = document.createElement("span");
            marker.textContent = "!";
            const content = document.createElement("div");
            const title = document.createElement("div");
            title.textContent = `${alert.testName || "Abnormal result"} · ${alert.patientName || "Patient not recorded"}`;
            const meta = document.createElement("small");
            meta.textContent = `Request #${alert.requestId} · ${formatDate(alert.captureDateTime)} · ${alert.urgency || "Urgency not recorded"}`;
            content.append(title, meta);
            item.append(marker, content);
            alertsHost.appendChild(item);
        }
    }

    async function loadDashboard() {
        try {
            const dashboardUrl = new URL(dashboard.dataset.dashboardUrl, document.baseURI);
            const response = await fetch(dashboardUrl, {
                credentials: "same-origin",
                headers: { Accept: "application/json" }
            });
            if (!response.ok || response.redirected) {
                throw new Error(response.status === 401 || response.redirected
                    ? "Sign in as a doctor to view dashboard data."
                    : "Dashboard data could not be loaded. Please try again.");
            }

            const data = await response.json();
            document.getElementById("dashboard-submitted-count").textContent = data.submittedRequests ?? 0;
            document.getElementById("dashboard-submitted-detail").textContent =
                `${data.urgentRequests ?? 0} urgent, ${data.statRequests ?? 0} STAT`;
            document.getElementById("dashboard-results-count").textContent = data.resultsRecorded ?? 0;
            document.getElementById("dashboard-alerts-count").textContent = data.abnormalAlerts ?? 0;
            document.getElementById("dashboard-patients-count").textContent = data.patients ?? 0;
            renderRequests(Array.isArray(data.recentRequests) ? data.recentRequests : []);
            renderAlerts(Array.isArray(data.recentAlerts) ? data.recentAlerts : []);
        } catch (error) {
            console.error("Doctor dashboard:", error);
            errorPanel.textContent = error.message || "Dashboard data could not be loaded.";
            errorPanel.hidden = false;
            requestsHost.textContent = "Unable to load recent requests.";
            alertsHost.textContent = "Unable to load abnormal alerts.";
            for (const id of ["dashboard-submitted-count", "dashboard-results-count", "dashboard-alerts-count", "dashboard-patients-count"]) {
                document.getElementById(id).textContent = "—";
            }
            document.getElementById("dashboard-submitted-detail").textContent = "Data unavailable";
        }
    }

    loadDashboard();
})();
