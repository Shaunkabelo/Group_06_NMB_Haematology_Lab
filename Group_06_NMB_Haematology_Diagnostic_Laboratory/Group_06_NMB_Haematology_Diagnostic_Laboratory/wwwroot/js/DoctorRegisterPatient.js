const registrationForm = document.getElementById('patientRegistrationForm');
        const registrationMessage = document.getElementById('registration-message');
        const createPatientButton = document.getElementById('createPatientButton');
        const addressSearchInput = document.getElementById('patient-address-search');
        const addressValueInput = document.getElementById('HomeAddress');
        const addressSuggestions = document.getElementById('address-suggestions');
        const addressStatus = document.getElementById('address-autocomplete-status');
        let addressSearchTimer = null;
        let addressSearchController = null;
        let lastAddressSearchAt = 0;

        function formatAddress(properties) {
            return [
                properties.housenumber,
                properties.street,
                properties.name && properties.name !== properties.street ? properties.name : null,
                properties.district,
                properties.city || properties.county,
                properties.state,
                properties.postcode,
                properties.country
            ].filter((part, index, all) => part && all.indexOf(part) === index).join(', ');
        }

        function closeAddressSuggestions() {
            addressSuggestions.replaceChildren();
            addressSuggestions.hidden = true;
            addressSearchInput.setAttribute('aria-expanded', 'false');
        }

        function showAddressSuggestions(features) {
            addressSuggestions.replaceChildren();
            const southAfricanPlaces = features.filter(feature =>
                feature.properties?.countrycode?.toLowerCase() === 'za');

            if (!southAfricanPlaces.length) {
                closeAddressSuggestions();
                addressStatus.textContent = 'No matching South African addresses found. Try adding a suburb or city.';
                return;
            }

            southAfricanPlaces.forEach(feature => {
                const label = formatAddress(feature.properties);
                if (!label) return;
                const option = document.createElement('button');
                option.type = 'button';
                option.className = 'address-suggestion';
                option.setAttribute('role', 'option');
                option.textContent = label;
                option.addEventListener('click', () => {
                    addressSearchInput.value = label;
                    addressValueInput.value = label;
                    closeAddressSuggestions();
                    addressStatus.textContent = 'South African address selected.';
                });
                addressSuggestions.appendChild(option);
            });

            addressSuggestions.hidden = addressSuggestions.childElementCount === 0;
            addressSearchInput.setAttribute('aria-expanded', String(!addressSuggestions.hidden));
            addressStatus.textContent = addressSuggestions.hidden
                ? 'No matching South African addresses found. Try a different search.'
                : 'Select the correct address from the suggestions.';
        }

        addressSearchInput.addEventListener('input', function () {
            addressValueInput.value = this.value;
        });

        registrationForm.addEventListener('reset', function () {
            window.setTimeout(() => {
                addressValueInput.value = '';
                addressSearchInput.value = '';
                closeAddressSuggestions();
                addressStatus.textContent = 'Start typing to search South African addresses, then choose a suggestion.';
            });
        });

        addressSearchInput.addEventListener('input', function () {
            const query = this.value.trim();
            addressValueInput.value = query;
            window.clearTimeout(addressSearchTimer);
            addressSearchController?.abort();

            if (query.length < 3) {
                closeAddressSuggestions();
                addressStatus.textContent = 'Type at least 3 characters to search South African addresses.';
                return;
            }

            addressStatus.textContent = 'Searching South African addresses...';
            const wait = Math.max(500, 1100 - (Date.now() - lastAddressSearchAt));
            addressSearchTimer = window.setTimeout(async () => {
                addressSearchController = new AbortController();
                lastAddressSearchAt = Date.now();
                const url = new URL('https://photon.komoot.io/api/');
                url.searchParams.set('q', query);
                url.searchParams.set('countrycode', 'ZA');
                url.searchParams.set('limit', '6');
                url.searchParams.set('lang', 'en');
                url.searchParams.set('lat', '-30.5');
                url.searchParams.set('lon', '24.5');
                url.searchParams.set('zoom', '5');

                try {
                    const response = await fetch(url, { signal: addressSearchController.signal });
                    if (!response.ok) throw new Error('Address service returned an error.');
                    const data = await response.json();
                    if (query !== addressSearchInput.value.trim()) return;
                    showAddressSuggestions(data.features || []);
                } catch (error) {
                    if (error.name === 'AbortError') return;
                    console.error('Address search failed:', error);
                    closeAddressSuggestions();
                    addressStatus.textContent = 'Address search is temporarily unavailable. You can enter the address manually.';
                }
            }, wait);
        });

        registrationForm.addEventListener('submit', async function (event) {
            if (!addressValueInput.value.trim()) {
                event.preventDefault();
                addressStatus.textContent = 'Select a search result or enter the address manually.';
                addressSearchInput.focus();
                return;
            }
            event.preventDefault();
            registrationMessage.hidden = true;

            createPatientButton.disabled = true;
            createPatientButton.textContent = 'Creating account...';

            try {
                const response = await fetch(registrationForm.action, {
                    method: 'POST',
                    body: new FormData(registrationForm),
                    credentials: 'same-origin',
                    headers: { 'Accept': 'application/json' }
                });



                let data;
                try {
                    data = await response.json();
                } catch {
                    throw new Error('The server returned an unexpected response. Please try again.');
                }

                const validationMessages = data.errors
                    ? Object.values(data.errors).flat().join(' ')
                    : '';
                const message = validationMessages || data.message ||
                    'Registration could not be completed.';
                const success = response.ok && data.success === true;


                if (success) registrationForm.reset();
                showRegistrationMessage(message, success);
            } catch (error) {
                console.error('Patient registration error:', error);
                showRegistrationMessage(
                    error.message || 'Unable to contact the server. Please try again.',
                    false);
            } finally {
                createPatientButton.disabled = false;
                createPatientButton.textContent = 'Create Account';
            }
        });

        document.querySelectorAll('[data-digits]').forEach(input => {
            input.addEventListener('input', function () {
                this.value = this.value.replace(/\D/g, '')
                    .slice(0, Number(this.dataset.digits));
            });
        });

        const saIdInput = document.getElementById('SaIdNumber');
        const dateOfBirthInput = document.getElementById('DateOfBirth');
        let dateOfBirthWasAutoFilled = false;

        saIdInput.addEventListener('input', function () {
            const idNumber = this.value;
            if (idNumber.length < 6) {
                if (dateOfBirthWasAutoFilled) dateOfBirthInput.value = '';
                dateOfBirthWasAutoFilled = false;
                return;
            }

            const yearPart = Number(idNumber.slice(0, 2));
            const month = Number(idNumber.slice(2, 4));
            const day = Number(idNumber.slice(4, 6));
            const currentYear = new Date().getFullYear();
            let year = Math.floor(currentYear / 100) * 100 + yearPart;
            let birthDate = new Date(year, month - 1, day);

            if (birthDate > new Date()) {
                year -= 100;
                birthDate = new Date(year, month - 1, day);
            }

            const validDate = month >= 1 && month <= 12 && day >= 1 &&
                birthDate.getFullYear() === year && birthDate.getMonth() === month - 1 &&
                birthDate.getDate() === day && birthDate <= new Date();

            if (!validDate) {
                if (dateOfBirthWasAutoFilled) dateOfBirthInput.value = '';
                dateOfBirthWasAutoFilled = false;
                return;
            }

            const monthText = String(month).padStart(2, '0');
            const dayText = String(day).padStart(2, '0');
            dateOfBirthInput.value = `${year}-${monthText}-${dayText}`;
            dateOfBirthWasAutoFilled = true;
        });

        function showRegistrationMessage(message, success) {
            registrationMessage.textContent = message;
            registrationMessage.hidden = false;
            registrationMessage.classList.remove('success', 'error', 'info');
            registrationMessage.classList.add(success ? 'success' : 'error');
        }
