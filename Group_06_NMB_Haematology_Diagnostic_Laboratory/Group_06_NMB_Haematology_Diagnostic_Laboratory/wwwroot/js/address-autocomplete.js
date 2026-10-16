(function () {
    let loader;
    function loadPlaces() {
        if (window.google?.maps?.importLibrary) return google.maps.importLibrary('places');
        if (loader) return loader;
        const key = document.querySelector('[data-google-maps-key]')?.dataset.googleMapsKey;
        if (!key) return Promise.reject(new Error('Missing address search key'));
        loader = new Promise((resolve, reject) => {
            window.initializeAddressPlaces = () => google.maps.importLibrary('places').then(value => { clearTimeout(timeout); resolve(value); }, error => { clearTimeout(timeout); reject(error); });
            const script = document.createElement('script');
            script.async = true;
            script.src = 'https://maps.googleapis.com/maps/api/js?loading=async&libraries=places&v=weekly&callback=initializeAddressPlaces&key=' + encodeURIComponent(key);
            const timeout = setTimeout(() => reject(new Error('Google address search timed out')), 8000);
            script.onerror = () => { clearTimeout(timeout); reject(new Error('Google address search unavailable')); };
            document.head.appendChild(script);
        });
        return loader;
    }
    // Keep suggestions working when Google is not configured or is unavailable.
    async function findAddresses(query, getToken) {
        try {
            const { AutocompleteSuggestion, AutocompleteSessionToken } = await loadPlaces();
            const result = await Promise.race([
                AutocompleteSuggestion.fetchAutocompleteSuggestions({
                    input: query, includedRegionCodes: ['za'],
                    includedPrimaryTypes: ['street_address', 'premise', 'subpremise', 'route'],
                    language: 'en', region: 'za', sessionToken: getToken(AutocompleteSessionToken)
                }),
                new Promise((_, reject) => setTimeout(() => reject(new Error('Google search timed out')), 8000))
            ]);
            return { predictions: result.suggestions.map(item => item.placePrediction).filter(Boolean), provider: 'google' };
        } catch (_) {
            const controller = new AbortController();
            const timeout = setTimeout(() => controller.abort(), 8000);
            try {
                const response = await fetch('https://photon.komoot.io/api/?limit=8&lang=en&countrycode=ZA&q='
                    + encodeURIComponent(query), { signal: controller.signal });
                if (!response.ok) throw new Error('Address search unavailable');
                const data = await response.json();
                const seen = new Set();
                const predictions = (data.features || []).map(feature => feature.properties || {})
                    .filter(p => (p.countrycode || '').toUpperCase() === 'ZA')
                    .map(p => {
                        const street = [p.housenumber, p.street || p.name].filter(Boolean).join(' ');
                        return [street, p.district, p.city || p.town || p.village, p.state, p.postcode, p.country]
                            .filter((value, index, values) => value && values.indexOf(value) === index).join(', ');
                    }).filter(address => {
                        if (!address || seen.has(address)) return false;
                        seen.add(address);
                        return true;
                    }).map(address => ({ text: address }));
                return { predictions, provider: 'openstreetmap' };
            } finally { clearTimeout(timeout); }
        }
    }
    function attach(input) {
        const wrapper = document.createElement('div');
        wrapper.className = 'address-autocomplete';
        input.before(wrapper);
        wrapper.append(input);
        const list = document.createElement('div');
        list.className = 'address-suggestions';
        list.id = input.id + '-suggestions';
        list.setAttribute('role', 'listbox');
        wrapper.append(list);
        input.setAttribute('aria-controls', list.id);
        input.setAttribute('aria-autocomplete', 'list');
        let timer, token, revision = 0, active = -1;
        function close() {
            revision++;
            list.replaceChildren();
            list.classList.remove('open');
            input.setAttribute('aria-expanded', 'false');
            input.removeAttribute('aria-activedescendant');
            active = -1;
        }
        function status(message) {
            list.replaceChildren();
            const text = document.createElement('div');
            text.className = 'address-suggestion-status';
            text.textContent = message;
            list.append(text);
            list.classList.add('open');
            input.setAttribute('aria-expanded', 'true');
        }
        function highlight(index) {
            const buttons = [...list.querySelectorAll('button')];
            if (!buttons.length) return;
            active = (index + buttons.length) % buttons.length;
            buttons.forEach((button, i) => {
                button.classList.toggle('active', i === active);
                button.setAttribute('aria-selected', String(i === active));
            });
            input.setAttribute('aria-activedescendant', buttons[active].id);
            buttons[active].scrollIntoView({ block: 'nearest' });
        }
        async function search() {
            const query = input.value.trim();
            if (query.length < 3) return;
            const current = ++revision;
            status('Finding South African addresses...');
            try {
                const { predictions, provider } = await findAddresses(query, Token => token ||= new Token());
                if (current !== revision) return;
                if (!predictions.length) { status('No matching addresses. You can type your full home address.'); return; }
                list.replaceChildren();
                predictions.forEach((prediction, index) => {
                    const button = document.createElement('button');
                    button.type = 'button';
                    button.id = list.id + '-' + index;
                    button.className = 'address-suggestion';
                    button.setAttribute('role', 'option');
                    button.textContent = prediction.text.toString();
                    button.addEventListener('mousedown', event => event.preventDefault());
                    button.addEventListener('mouseenter', () => highlight(index));
                    button.addEventListener('click', async () => {
                        input.value = prediction.text.toString();
                        close();
                        const selected = revision;
                        token = undefined;
                        input.dispatchEvent(new Event('change', { bubbles: true }));
                        try {
                            if (!prediction.toPlace) return;
                            const place = prediction.toPlace();
                            await place.fetchFields({ fields: ['formattedAddress'] });
                            if (selected !== revision) return;
                            input.value = place.formattedAddress || input.value;
                            input.dispatchEvent(new Event('change', { bubbles: true }));
                        } catch (_) { /* Keep the selected address if details are unavailable. */ }
                    });
                    list.append(button);
                });
                const attribution = document.createElement('div');
                attribution.className = 'address-suggestion-status';
                if (provider === 'google') {
                    const logo = document.createElement('img');
                    logo.src = 'https://maps.gstatic.com/mapfiles/api-3/images/powered-by-google-on-white3.png';
                    logo.alt = 'Powered by Google';
                    logo.width = 120;
                    logo.height = 14;
                    attribution.append(logo);
                } else {
                    const link = document.createElement('a');
                    link.href = 'https://www.openstreetmap.org/copyright';
                    link.target = '_blank';
                    link.rel = 'noopener noreferrer';
                    link.textContent = 'Address data © OpenStreetMap contributors';
                    attribution.append(link);
                }
                list.append(attribution);
            } catch (_) {
                if (current === revision) status('Address search unavailable. Please type your full home address.');
            }
        }
        close();
        input.addEventListener('input', () => { clearTimeout(timer); close(); timer = setTimeout(search, 350); });
        input.addEventListener('blur', () => { clearTimeout(timer); setTimeout(close, 150); });
        input.addEventListener('keydown', event => {
            const buttons = list.querySelectorAll('button');
            if (event.key === 'Escape') { clearTimeout(timer); close(); }
            else if (buttons.length && event.key === 'ArrowDown') { event.preventDefault(); highlight(active + 1); }
            else if (buttons.length && event.key === 'ArrowUp') { event.preventDefault(); highlight(active - 1); }
            else if (event.key === 'Enter' && active >= 0) { event.preventDefault(); buttons[active].click(); }
        });
    }
    document.addEventListener('DOMContentLoaded', () => document.querySelectorAll('[data-address-autocomplete]').forEach(attach));
})();
