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
