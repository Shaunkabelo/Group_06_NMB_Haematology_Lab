(() => {
    const interactiveSelector = "button, a, input, select, textarea, label, form";

    document.querySelectorAll("table").forEach(table => {
        if (!table.querySelector(".staff-summary-row") || table.previousElementSibling?.classList.contains("expand-row-note")) return;
        const note = document.createElement("p");
        note.className = "expand-row-note";
        note.textContent = "Click a row to view more details.";
        table.before(note);
    });

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
