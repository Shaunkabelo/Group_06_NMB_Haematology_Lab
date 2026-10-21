(() => {
    const button = document.getElementById("generate-report-btn");
    const startInput = document.getElementById("report-start-date");
    const endInput = document.getElementById("report-end-date");
    const sortInput = document.getElementById("report-sort-order");
    const status = document.getElementById("report-status");

    if (!button || !startInput || !endInput || !sortInput || !status) return;

    const toLocalDate = date => {
        const local = new Date(date.getTime() - date.getTimezoneOffset() * 60000);
        return local.toISOString().slice(0, 10);
    };
    const today = new Date();
    const monthAgo = new Date(today);
    monthAgo.setDate(monthAgo.getDate() - 29);
    startInput.value = toLocalDate(monthAgo);
    endInput.value = toLocalDate(today);

    const showStatus = (message, type = "") => {
        status.textContent = message;
        status.className = `report-status ${type}`.trim();
    };

    button.addEventListener("click", async () => {
        const startDate = startInput.value;
        const endDate = endInput.value;
        if (!startDate || !endDate) {
            showStatus("Select both a start date and an end date.", "error");
            return;
        }
        if (startDate > endDate) {
            showStatus("The start date must be on or before the end date.", "error");
            return;
        }

        const reportUrl = new URL(button.dataset.reportUrl, document.baseURI);
        reportUrl.searchParams.set("startDate", startDate);
        reportUrl.searchParams.set("endDate", endDate);
        reportUrl.searchParams.set("sortBy", sortInput.value);
        button.disabled = true;
        showStatus("Generating your report…");

        try {
            const response = await fetch(reportUrl, { headers: { Accept: "application/pdf, application/json" } });
            if (!response.ok) {
                let message = "The report could not be generated. Please try again.";
                try {
                    const problem = await response.json();
                    if (problem.message) message = problem.message;
                } catch { /* The server may return an HTML authentication page. */ }
                throw new Error(message);
            }

            const blob = await response.blob();
            if (blob.type !== "application/pdf") throw new Error("The server returned an invalid report. Please try again.");
            const objectUrl = URL.createObjectURL(blob);
            const link = document.createElement("a");
            link.href = objectUrl;
            link.download = `doctor-test-requests-${startDate}-to-${endDate}.pdf`;
            document.body.appendChild(link);
            link.click();
            link.remove();
            window.setTimeout(() => URL.revokeObjectURL(objectUrl), 1000);
            showStatus("Your PDF report has been generated.", "success");
        } catch (error) {
            showStatus(error.message || "The report could not be generated. Please try again.", "error");
        } finally {
            button.disabled = false;
        }
    });
})();
