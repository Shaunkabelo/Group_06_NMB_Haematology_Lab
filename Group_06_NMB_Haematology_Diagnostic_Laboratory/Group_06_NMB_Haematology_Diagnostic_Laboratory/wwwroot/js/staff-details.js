(() => {
    const interactiveSelector = "button, a, input, select, textarea, label, form";

    const toggle = row => {
        const details = document.getElementById(row.dataset.detailsTarget);
        if (!details) return;

        const opening = details.hidden;
        document.querySelectorAll(".staff-detail-row:not([hidden])").forEach(row => row.hidden = true);
        document.querySelectorAll(".staff-summary-row[aria-expanded='true']")
            .forEach(summary => summary.setAttribute("aria-expanded", "false"));

        if (opening) {
            details.hidden = false;
            row.setAttribute("aria-expanded", "true");
        }
    };

    document.addEventListener("click", event => {
        if (event.target.closest(interactiveSelector)) return;
        const row = event.target.closest(".staff-summary-row");
        if (row) toggle(row);
    });

    document.addEventListener("keydown", event => {
        if (event.key !== "Enter" && event.key !== " ") return;
        const row = event.target.closest(".staff-summary-row");
        if (!row) return;
        event.preventDefault();
        toggle(row);
    });
})();
