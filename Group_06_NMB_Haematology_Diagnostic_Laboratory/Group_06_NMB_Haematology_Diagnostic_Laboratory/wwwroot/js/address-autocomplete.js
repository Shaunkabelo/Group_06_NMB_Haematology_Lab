(function () {
    function formatAddress(properties) {
        const street = [properties.housenumber, properties.street || properties.name].filter(Boolean).join(' ');
        return [street, properties.district, properties.city || properties.town || properties.village,
            properties.state, properties.postcode, properties.country]
            .filter((value, index, values) => value && values.indexOf(value) === index)
            .join(', ');
    }

    function attachAddressAutocomplete(input) {
        if (!input || input.dataset.addressAutocomplete === 'true') return;
        input.dataset.addressAutocomplete = 'true';
        input.setAttribute('autocomplete', 'street-address');
        input.setAttribute('aria-autocomplete', 'list');
        input.setAttribute('aria-expanded', 'false');

        const wrapper = document.createElement('div');
        wrapper.className = 'address-autocomplete';
        input.parentNode.insertBefore(wrapper, input);
        wrapper.appendChild(input);

        const list = document.createElement('div');
        list.className = 'address-suggestions';
        list.id = input.id + '-suggestions';
        list.setAttribute('role', 'listbox');
        wrapper.appendChild(list);
        input.setAttribute('aria-controls', list.id);

        let timer;
        let request;
        let activeIndex = -1;

        function closeList() {
            list.replaceChildren();
            list.classList.remove('open');
            input.setAttribute('aria-expanded', 'false');
            activeIndex = -1;
        }

        function selectSuggestion(button) {
            input.value = button.dataset.address;
            input.dispatchEvent(new Event('change', { bubbles: true }));
            closeList();
        }

        function setActive(index) {
            const options = Array.from(list.querySelectorAll('[role="option"]'));
            if (!options.length) return;
            activeIndex = (index + options.length) % options.length;
            options.forEach((option, optionIndex) => option.classList.toggle('active', optionIndex === activeIndex));
            options[activeIndex].scrollIntoView({ block: 'nearest' });
        }

        async function search() {
            const query = input.value.trim();
            if (query.length < 3) { closeList(); return; }
            if (request) request.abort();
            request = new AbortController();
            list.innerHTML = '<div class="address-suggestion-status">Finding addresses…</div>';
            list.classList.add('open');
            input.setAttribute('aria-expanded', 'true');

            try {
                const url = 'https://photon.komoot.io/api/?limit=6&lang=en&q=' + encodeURIComponent(query + ', South Africa');
                const response = await fetch(url, { signal: request.signal });
                if (!response.ok) throw new Error('Address lookup failed');
                const data = await response.json();
                const addresses = [...new Set((data.features || []).map(feature => formatAddress(feature.properties || {})).filter(Boolean))];
                list.replaceChildren();
                if (!addresses.length) {
                    list.innerHTML = '<div class="address-suggestion-status">No matching addresses found. You can continue typing.</div>';
                    return;
                }
                addresses.forEach((address, index) => {
                    const button = document.createElement('button');
                    button.type = 'button';
                    button.className = 'address-suggestion';
                    button.setAttribute('role', 'option');
                    button.dataset.address = address;
                    button.textContent = address;
                    button.addEventListener('mousedown', event => event.preventDefault());
                    button.addEventListener('click', () => selectSuggestion(button));
                    button.addEventListener('mouseenter', () => setActive(index));
                    list.appendChild(button);
                });
                activeIndex = -1;
            } catch (error) {
                if (error.name !== 'AbortError') closeList();
            }
        }

        input.addEventListener('input', () => { clearTimeout(timer); timer = setTimeout(search, 350); });
        input.addEventListener('keydown', event => {
            const options = list.querySelectorAll('[role="option"]');
            if (!options.length) return;
            if (event.key === 'ArrowDown') { event.preventDefault(); setActive(activeIndex + 1); }
            else if (event.key === 'ArrowUp') { event.preventDefault(); setActive(activeIndex - 1); }
            else if (event.key === 'Enter' && activeIndex >= 0) { event.preventDefault(); selectSuggestion(options[activeIndex]); }
            else if (event.key === 'Escape') closeList();
        });
        input.addEventListener('blur', () => setTimeout(closeList, 150));
    }

    document.addEventListener('DOMContentLoaded', () => {
        document.querySelectorAll('[data-address-autocomplete]').forEach(attachAddressAutocomplete);
    });
})();
