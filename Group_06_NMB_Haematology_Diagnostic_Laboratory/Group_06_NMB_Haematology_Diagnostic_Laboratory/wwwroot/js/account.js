document.querySelectorAll("[data-password-toggle]").forEach(button => {
    const icon = '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" aria-hidden="true"><path d="M2 12s3.5-7 10-7 10 7 10 7-3.5 7-10 7S2 12 2 12Z"/><circle cx="12" cy="12" r="3"/><path class="eye-slash" d="m3 3 18 18" /></svg>';
    button.innerHTML = icon;
    button.querySelector(".eye-slash").style.display = "none";
    button.addEventListener("click", () => {
        const input = document.getElementById(button.dataset.passwordToggle);
        const show = input.type === "password";
        input.type = show ? "text" : "password";
        button.setAttribute("aria-label", show ? "Hide password" : "Show password");
        button.setAttribute("aria-pressed", String(show));
        button.querySelector(".eye-slash").style.display = show ? "" : "none";
    });
});
