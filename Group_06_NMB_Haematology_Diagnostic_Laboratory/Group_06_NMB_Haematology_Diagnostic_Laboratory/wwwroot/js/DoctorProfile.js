const assetScript = document.currentScript;
document.querySelectorAll('input[name="name"], input[name="surname"]').forEach(field => {
    field.addEventListener("input", () => { field.value = field.value.replace(/[^\p{L}\p{M} .'\u2019-]/gu, ""); });
});
document.getElementById("cellphone").addEventListener("input", event => {
    event.target.value = event.target.value.replace(/[^0-9]/g, "").slice(0,10);
});
