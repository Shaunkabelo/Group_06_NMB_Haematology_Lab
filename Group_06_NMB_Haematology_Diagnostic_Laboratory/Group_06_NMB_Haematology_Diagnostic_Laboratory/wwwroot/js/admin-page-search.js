(function () {
    const input = document.getElementById('admin-page-search');
    const content = document.getElementById('admin-search-content');
    const status = document.getElementById('admin-search-status');
    if (!input || !content || !status) return;
    const style = document.createElement('style');
    style.textContent = '.admin-search-hidden { display: none !important; } #admin-search-status { margin: 16px 24px; }';
    document.head.appendChild(style);

    function search() {
        const query = input.value.trim().toLocaleLowerCase();
        content.querySelectorAll('.admin-search-hidden').forEach(item => item.classList.remove('admin-search-hidden'));
        const rows = Array.from(content.querySelectorAll('table tbody tr'));
        const cards = Array.from(content.querySelectorAll('.card, .stat-card, .activity-item, .list-row'))
            .filter(item => !item.querySelector('table') && !item.closest('.modal, [role="dialog"]'));
        const items = rows.concat(cards.filter(item => !cards.some(parent => parent !== item && parent.contains(item))));
        let matches = 0;
        items.forEach(item => {
            const text = item.textContent.toLocaleLowerCase();
            const match = !query || text.includes(query);
            item.classList.toggle('admin-search-hidden', !match);
            if (match && item.getClientRects().length) matches++;
        });
        status.hidden = !query;
        status.textContent = !query ? '' : matches ? matches + ' matching items on this page.' : 'No matching items on this page.';
    }
    input.addEventListener('input', search);
    // Run after existing local search/category handlers, retaining their visibility rules.
    content.addEventListener('input', () => { if (input.value) search(); });
    content.addEventListener('change', () => { if (input.value) search(); });
    new MutationObserver(() => { if (input.value) search(); })
        .observe(content, { childList: true, subtree: true });
}());
