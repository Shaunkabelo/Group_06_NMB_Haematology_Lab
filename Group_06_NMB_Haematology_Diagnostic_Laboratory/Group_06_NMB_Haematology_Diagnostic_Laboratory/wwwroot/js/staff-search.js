(() => {
    document.addEventListener("DOMContentLoaded", () => {
        document.querySelectorAll("[data-staff-search]").forEach(input => {
            const table = document.querySelector(input.dataset.staffSearch);
            const emptyMessage = table?.parentElement?.parentElement?.querySelector("[data-staff-search-empty]");
            if (!table) return;

            const filterRows = () => {
                const searchTerm = input.value.trim().toLocaleLowerCase();
                let visibleRows = 0;

                table.querySelectorAll("tbody > tr.staff-summary-row").forEach(row => {
                    const visible = !searchTerm || row.textContent.toLocaleLowerCase().includes(searchTerm);
                    row.hidden = !visible;

                    const details = document.getElementById(row.dataset.detailsTarget);
                    if (details && !visible) {
                        details.hidden = true;
                        row.setAttribute("aria-expanded", "false");
                    }

                    if (visible) visibleRows++;
                });

                if (emptyMessage) emptyMessage.hidden = visibleRows !== 0;
            };

            input.addEventListener("input", filterRows);
            input.addEventListener("keydown", event => {
                if (event.key === "Enter") event.preventDefault();
            });
        });
    });
})();
