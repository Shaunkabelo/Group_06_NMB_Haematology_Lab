const assetScript = document.currentScript;
(() => {
        "use strict";

        // Generate URLs through ASP.NET routing so the application
        // continues working when deployed under a virtual directory.
        const listUrl = assetScript.dataset.getrequestsUrl;
        const detailsUrl = assetScript.dataset.requestdetailsUrl;
        const cancelUrl = assetScript.dataset.canceltestrequestUrl;

        const tbody = document.getElementById("requests-tbody");
        const modal = document.getElementById("modal-cancel-request");
        const summary = document.getElementById("cancel-request-summary");
        const reasonInput = document.getElementById("cancel-request-reason");
        const cancelledDateInput = document.getElementById("cancelled-date");
        const alerts = document.getElementById("technician-cancel-alerts");
        const modalError = document.getElementById("cancel-modal-error");
        const confirmButton = document.getElementById("confirm-cancel-button");

        const searchInput = document.getElementById("requests-search");
        const statusFilter = document.getElementById("requests-filter-status");
        const urgencyFilter = document.getElementById("requests-filter-urgency");

        let requests = [];
        let selectedRequestId = null;
        let isSubmitting = false;

        // Safely encode values before adding them to HTML.
        function escapeHtml(value) {
            return String(value ?? "").replace(/[&<>"']/g, character => ({
                "&": "&amp;",
                "<": "&lt;",
                ">": "&gt;",
                '"': "&quot;",
                "'": "&#39;"
            })[character]);
        }

        function showMessage(message, isError = false) {
            alerts.replaceChildren();

            const notice = document.createElement("div");
            notice.className = "technician-cancel-alert";
            notice.style.background = isError ? "#FFF2F3" : "#EFFAF2";
            notice.style.color = isError ? "#7A1E28" : "#205C36";
            notice.textContent = message;

            alerts.appendChild(notice);
        }

        function showModalError(message) {
            modalError.textContent = message;
            modalError.hidden = false;
        }

        function clearModalError() {
            modalError.textContent = "";
            modalError.hidden = true;
        }

        function getAntiForgeryToken() {
            return document.querySelector(
                'input[name="__RequestVerificationToken"]'
            )?.value ?? "";
        }

        // Must match the statuses accepted by the server-side procedure.
        function canCancel(request) {
            const status = String(request.status ?? "").trim().toLowerCase();

            return status === "submitted" ||
                   status === "samples received";
        }

        function formatDate(value) {
            if (!value) return "—";

            const date = new Date(value);

            if (Number.isNaN(date.getTime())) {
                return "—";
            }

            return date.toLocaleString();
        }

        // Filter the requests currently loaded from the backend.
        function renderRequests() {
            const search = searchInput.value.trim().toLowerCase();
            const selectedStatus = statusFilter.value.trim().toLowerCase();
            const selectedUrgency = urgencyFilter.value.trim().toLowerCase();

            const filteredRequests = requests.filter(request => {
                const searchableText = [
                    request.id,
                    request.patientName,
                    request.status,
                    request.urgency,
                    request.tests
                ].join(" ").toLowerCase();

                const matchesSearch = searchableText.includes(search);

                const matchesStatus =
                    !selectedStatus ||
                    String(request.status ?? "").trim().toLowerCase() === selectedStatus;

                const matchesUrgency =
                    !selectedUrgency ||
                    String(request.urgency ?? "").trim().toLowerCase() === selectedUrgency;

                return matchesSearch && matchesStatus && matchesUrgency;
            });

            if (filteredRequests.length === 0) {
                tbody.innerHTML = `
                    <tr>
                        <td colspan="7" style="text-align:center;padding:24px;">
                            No test requests found.
                        </td>
                    </tr>`;
                return;
            }

            tbody.innerHTML = filteredRequests.map(request => {
                const id = Number(request.id);

                const tests = Array.isArray(request.tests)
                    ? request.tests.join(", ")
                    : String(request.tests ?? "—");

                const cancelButton = canCancel(request)
                    ? `<button type="button"
                               class="btn btn-danger"
                               data-cancel-request="${id}">
                           Cancel
                       </button>`
                    : "";

                const viewUrl =
                    `${detailsUrl}?id=${encodeURIComponent(id)}`;

                return `
                    <tr>
                        <td>${escapeHtml(id)}</td>
                        <td>${escapeHtml(request.patientName || "—")}</td>
                        <td>${escapeHtml(formatDate(request.requestDate ?? request.createdAt))}</td>
                        <td>${escapeHtml(request.urgency || "—")}</td>
                        <td>${escapeHtml(request.status || "—")}</td>
                        <td>${escapeHtml(tests)}</td>
                        <td>
                            <a class="btn btn-primary"
                               href="${escapeHtml(viewUrl)}">
                                View
                            </a>
                            ${cancelButton}
                        </td>
                    </tr>`;
            }).join("");
        }

        // Request the latest list from the controller.
        async function loadRequests() {
            tbody.innerHTML = `
                <tr>
                    <td colspan="7" style="text-align:center;padding:24px;">
                        Loading test requests...
                    </td>
                </tr>`;

            try {
                const response = await fetch(listUrl, {
                    method: "GET",
                    credentials: "same-origin",
                    headers: {
                        "Accept": "application/json"
                    }
                });

                if (!response.ok) {
                    throw new Error(
                        `Unable to load test requests (HTTP ${response.status}).`
                    );
                }

                const data = await response.json();

                if (!Array.isArray(data)) {
                    throw new Error("The server returned an invalid request list.");
                }

                requests = data;
                renderRequests();

            } catch (error) {
                console.error("Loading test requests failed:", error);

                tbody.innerHTML = `
                    <tr>
                        <td colspan="7" style="text-align:center;padding:24px;">
                            Could not load test requests. Refresh the page or check the server.
                        </td>
                    </tr>`;
            }
        }

        // Open the popup after checking the selected request.
        function openCancelRequestModal(requestId) {
            const request = requests.find(
                item => Number(item.id) === Number(requestId)
            );

            if (!request) {
                showMessage(
                    "Test request not found. Refresh the page and try again.",
                    true
                );
                return;
            }

            if (!canCancel(request)) {
                showMessage(
                    "This request can no longer be cancelled.",
                    true
                );
                return;
            }

            selectedRequestId = Number(request.id);
            reasonInput.value = "";
            clearModalError();

            summary.textContent =
                `Cancel request #${request.id} for ${request.patientName || "this patient"}?`;

            // This is a display preview only. The database records the
            // authoritative time when the cancellation is saved.
            cancelledDateInput.value = new Date().toLocaleString();

            modal.classList.add("is-open");
            modal.setAttribute("aria-hidden", "false");
            document.body.style.overflow = "hidden";

            reasonInput.focus();
        }

        // Close the popup without changing the database.
        window.closeCancelRequestModal = function () {
            if (isSubmitting) {
                return;
            }

            modal.classList.remove("is-open");
            modal.setAttribute("aria-hidden", "true");
            document.body.style.overflow = "";

            selectedRequestId = null;
            reasonInput.value = "";
            clearModalError();
        };

        // Submit the cancellation to the ASP.NET controller.
        window.submitCancelRequest = async function () {
            if (isSubmitting) {
                return;
            }

            if (!selectedRequestId) {
                showModalError("Select a test request before cancelling.");
                return;
            }

            const reason = reasonInput.value.trim();

            if (!reason || reason.length > 500) {
                showModalError(
                    "Enter a cancellation reason between 1 and 500 characters."
                );
                reasonInput.focus();
                return;
            }

            const token = getAntiForgeryToken();

            if (!token) {
                showModalError(
                    "Security token not found. Refresh the page and try again."
                );
                return;
            }

            isSubmitting = true;
            confirmButton.disabled = true;
            confirmButton.textContent = "Cancelling...";
            clearModalError();

            try {
                const response = await fetch(cancelUrl, {
                    method: "POST",
                    credentials: "same-origin",
                    headers: {
                        "Content-Type": "application/json",
                        "RequestVerificationToken": token,
                        "Accept": "application/json"
                    },
                    body: JSON.stringify({
                        testRequestId: selectedRequestId,
                        reason: reason
                    })
                });

                const result = await response.json().catch(() => ({}));

                if (!response.ok || result.success !== true) {
                    throw new Error(
                        result.message ||
                        `Cancellation failed (HTTP ${response.status}).`
                    );
                }

                // Close the popup only after the server confirms success.
                modal.classList.remove("is-open");
                modal.setAttribute("aria-hidden", "true");
                document.body.style.overflow = "";

                selectedRequestId = null;
                reasonInput.value = "";

                showMessage(
                    result.message || "Test request cancelled successfully."
                );

                // Fetch the updated status from the server.
                await loadRequests();

            } catch (error) {
                console.error("Cancellation failed:", error);

                showModalError(
                    error.message || "Cancellation failed. Please try again."
                );

            } finally {
                isSubmitting = false;
                confirmButton.disabled = false;
                confirmButton.textContent = "Cancel Request";
            }
        };

        // Handle Cancel buttons added dynamically to the table.
        tbody.addEventListener("click", event => {
            const button = event.target.closest("[data-cancel-request]");

            if (button) {
                openCancelRequestModal(button.dataset.cancelRequest);
            }
        });

        // Search and filters update the displayed rows.
        searchInput.addEventListener("input", renderRequests);
        statusFilter.addEventListener("change", renderRequests);
        urgencyFilter.addEventListener("change", renderRequests);

        // Clicking the dark overlay closes the popup.
        modal.addEventListener("click", event => {
            if (event.target === modal) {
                window.closeCancelRequestModal();
            }
        });

        // Escape closes the popup when no cancellation is in progress.
        document.addEventListener("keydown", event => {
            if (event.key === "Escape" && modal.classList.contains("is-open")) {
                window.closeCancelRequestModal();
            }
        });

        // Load requests when the page finishes loading.
        loadRequests();
    })();
