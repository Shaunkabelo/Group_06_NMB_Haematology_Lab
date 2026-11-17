// Installed before page scripts so every same-origin mutation includes the antiforgery token.
(() => {
    const originalFetch = window.fetch.bind(window);
    window.fetch = (input, init = {}) => {
        const url = new URL(input instanceof Request ? input.url : input, location.href);
        const method = (init.method || (input instanceof Request ? input.method : 'GET')).toUpperCase();
        if (url.origin === location.origin && !['GET', 'HEAD', 'OPTIONS', 'TRACE'].includes(method)) {
            const headers = new Headers(init.headers || (input instanceof Request ? input.headers : undefined));
            headers.set('RequestVerificationToken', document.querySelector('meta[name="doctor-csrf"]').content);
            init = { ...init, headers };
        }
        return originalFetch(input, init);
    };
    // Discard obsolete patient data left by earlier versions, including after switching accounts.
    localStorage.removeItem('doctorRequests');
    localStorage.removeItem('doctorResults');
})();
