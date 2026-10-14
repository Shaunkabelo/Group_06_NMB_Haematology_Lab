// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

function handleSearch(query) {
    const term = String(query || '').trim().toLocaleLowerCase();
    document.querySelectorAll('tbody tr, [data-searchable]').forEach(element => {
        element.hidden = term !== '' &&
            !element.textContent.toLocaleLowerCase().includes(term);
    });
}

function showNotifications() {
    window.alert('Notifications are not available yet.');
}
