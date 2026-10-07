/* Optional sidebar behaviour, isolated so the layout can easily be restored. */
(() => {
    "use strict";
    const toggle = document.getElementById("shared-nav-toggle");
    const sidebar = document.getElementById("shared-sidebar");
    if (!toggle || !sidebar) return;
    const root = document.documentElement;
    const desktop = window.matchMedia("(min-width: 861px)");
    const storageKey = "nmb-shared-nav-collapsed";
    const tooltip = document.createElement("div");
    tooltip.className = "shared-nav-tooltip";
    tooltip.setAttribute("role", "tooltip");
    tooltip.hidden = true;
    document.body.appendChild(tooltip);
    const hideTooltip = () => { tooltip.hidden = true; };
    sidebar.querySelectorAll("a.nav-item").forEach(link => {
        const textNodes = Array.from(link.childNodes).filter(node => node.nodeType === Node.TEXT_NODE);
        const existingLabel = link.querySelector(".nav-label");
        const name = (existingLabel ? existingLabel.textContent : textNodes.map(node => node.textContent).join(" ")).trim();
        const label = existingLabel || document.createElement("span");
        label.className = "nav-label";
        label.textContent = name;
        textNodes.forEach(node => node.remove());
        link.appendChild(label);
        link.setAttribute("aria-label", name);
        const icon = link.querySelector("span:not(.nav-label)");
        if (icon) icon.setAttribute("aria-hidden", "true");
        const showTooltip = () => {
            if (!desktop.matches || !root.classList.contains("shared-nav-collapsed")) return;
            tooltip.textContent = name;
            tooltip.hidden = false;
            const rect = link.getBoundingClientRect();
            tooltip.style.left = (rect.right + 8) + "px";
            tooltip.style.top = Math.max(8, Math.min(rect.top, window.innerHeight - tooltip.offsetHeight - 8)) + "px";
        };
        link.addEventListener("mouseenter", showTooltip);
        link.addEventListener("focus", showTooltip);
        link.addEventListener("mouseleave", hideTooltip);
        link.addEventListener("blur", hideTooltip);
    });
    const syncToggle = () => {
        const collapsed = desktop.matches && root.classList.contains("shared-nav-collapsed");
        toggle.setAttribute("aria-expanded", String(!collapsed));
        const label = collapsed ? "Expand navigation" : "Collapse navigation";
        toggle.setAttribute("aria-label", label);
        toggle.title = label;
        hideTooltip();
    };
    try {
        root.classList.toggle("shared-nav-collapsed", localStorage.getItem(storageKey) === "true");
    } catch (_) { /* Storage may be disabled; navigation still works. */ }
    toggle.hidden = false;
    syncToggle();
    toggle.addEventListener("click", () => {
        const collapsed = root.classList.toggle("shared-nav-collapsed");
        try { localStorage.setItem(storageKey, String(collapsed)); } catch (_) { }
        syncToggle();
    });
    desktop.addEventListener("change", syncToggle);
    window.addEventListener("resize", hideTooltip);
    document.addEventListener("scroll", hideTooltip, true);
    document.addEventListener("keydown", event => { if (event.key === "Escape") hideTooltip(); });
})();