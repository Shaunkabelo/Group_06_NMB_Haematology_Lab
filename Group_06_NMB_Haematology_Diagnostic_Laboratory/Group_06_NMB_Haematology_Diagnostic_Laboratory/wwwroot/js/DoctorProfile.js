const assetScript = document.currentScript;
document.querySelectorAll("[data-manual-password-entry]").forEach(field => {
    // Keeping the field readonly until deliberate interaction prevents browser
    // password managers from inserting the saved login credential on load.
    field.value = "";
    const enableTyping = () => {
        field.removeAttribute("readonly");
        field.value = "";
    };
    field.addEventListener("focus", enableTyping, { once: true });
    field.addEventListener("pointerdown", enableTyping, { once: true });
});
document.querySelectorAll('input[name="name"], input[name="surname"]').forEach(field => {
    field.addEventListener("input", () => { field.value = field.value.replace(/[^\p{L}\p{M} .'\u2019-]/gu, ""); });
});
document.getElementById("cellphone").addEventListener("input", event => {
    event.target.value = event.target.value.replace(/[^0-9]/g, "").slice(0,10);
});

const profilePasswordForm = document.getElementById("profile-password-form");
if (profilePasswordForm) {
    profilePasswordForm.addEventListener("submit", async event => {
        event.preventDefault();
        const error = document.getElementById("profile-password-error");
        error.replaceChildren();
        error.classList.add("validation-summary-valid");

        try {
            const response = await fetch(profilePasswordForm.action, {
                method: "POST",
                body: new FormData(profilePasswordForm),
                headers: { "X-Requested-With": "XMLHttpRequest" }
            });
            const body = await response.json();
            if (response.ok && body.redirectUrl) {
                window.location.assign(body.redirectUrl);
                return;
            }
            error.textContent = body.message || "Your password could not be changed.";
            error.classList.remove("validation-summary-valid");
            error.scrollIntoView({ behavior: "smooth", block: "center" });
        } catch {
            error.textContent = "Your password could not be changed. Please try again.";
            error.classList.remove("validation-summary-valid");
        }
    });
}
