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
        let registrationMessageVersion = 0;

        const registrationFields = ['Name', 'Surname', 'SaIdNumber', 'DateOfBirth', 'Cellphone', 'Email', 'HomeAddress'];

        function clearFieldErrors() {
            registrationFields.forEach(name => {
                const input = document.getElementById(name);
                const message = registrationForm.querySelector(`[data-valmsg-for="${name}"]`);
                input?.removeAttribute('aria-invalid');
                if (message) {
                    message.textContent = '';
                    message.classList.remove('field-validation-error');
                }
            });
        }

        function setFieldErrors(errors = {}) {
            clearFieldErrors();
            Object.entries(errors).forEach(([key, values]) => {
                const name = registrationFields.find(field => field.toLowerCase() === key.toLowerCase());
                if (!name) return;
                const input = document.getElementById(name);
                const message = registrationForm.querySelector(`[data-valmsg-for="${name}"]`);
                const text = Array.isArray(values) ? values.filter(Boolean).join(' ') : String(values || '');
                if (input) input.setAttribute('aria-invalid', 'true');
                if (message) {
                    message.textContent = text;
                    message.classList.add('field-validation-error');
                }
            });
        }

        function getClientValidationErrors() {
            const errors = [];
            registrationForm.querySelectorAll('input:not([type="hidden"]), select, textarea').forEach(input => {
                if (input.disabled || input.checkValidity()) return;
                const label = registrationForm.querySelector(`label[for="${input.id}"]`);
                let message = 'Check this field and try again.';
                if (input.validity.valueMissing) message = `${label?.textContent.replace('*', '').trim() || 'This field'} is required.`;
                else if (input.id === 'SaIdNumber') message = 'Enter a valid 13-digit South African ID number.';
                else if (input.id === 'Cellphone') message = 'Enter a valid 10-digit cellphone number.';
                else if (input.type === 'email') message = 'Enter a valid email address.';
                else if (input.type === 'date') message = 'Enter a valid date of birth.';
                errors.push({ input, message });
            });
            return errors;
        }

        function buildAdministratorEmailLink(email, doctorName, doctorEmail) {
            const fields = [
                ['Name', 'Name'],
                ['Surname', 'Surname'],
                ['SaIdNumber', 'South African ID number'],
                ['DateOfBirth', 'Date of birth'],
                ['Cellphone', 'Cellphone number'],
                ['Email', 'Email address'],
                ['HomeAddress', 'Home address']
            ];
            const patientDetails = fields
                .map(([id, label]) => [label, document.getElementById(id)?.value?.trim()])
                .filter(([, value]) => value)
                .map(([label, value]) => `${label}: ${value}`);
            const doctorAccount = doctorName || registrationMessage.dataset.doctorAccount || 'Doctor';
            const doctorSignature = doctorEmail ? `${doctorAccount} (${doctorEmail})` : doctorAccount;
            const body = [
                'Hello Administrator,',
                '',
                'Patient information entered:',
                ...(patientDetails.length ? patientDetails : ['No patient details were entered.']),
                '',
                `Doctor: ${doctorSignature}`,
                '',
                'Assist in registering this patient.'
            ].join('\n');

            return `mailto:${email}?subject=${encodeURIComponent('Help with patient registration')}&body=${encodeURIComponent(body)}`;
        }

        async function getAdministratorContact() {
            const endpoint = registrationMessage.dataset.administratorContactUrl;
            if (!endpoint) return null;
            const response = await fetch(new URL(endpoint, document.baseURI), {
                credentials: 'same-origin',
                headers: { Accept: 'application/json' }
            });
            if (!response.ok || response.redirected) return null;
            const data = await response.json();
            return typeof data.email === 'string' && data.email.trim()
                ? { email: data.email.trim(), doctorName: data.doctorName, doctorEmail: data.doctorEmail }
                : null;
        }

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
                clearFieldErrors();
                addressValueInput.value = '';
                addressSearchInput.value = '';
                closeAddressSuggestions();
                addressStatus.textContent = 'Start typing to search South African addresses, then choose a suggestion.';
            });
        });

        registrationForm.querySelector('button[type="reset"]')?.addEventListener('click', () => {
            window.setTimeout(() => {
                registrationMessageVersion++;
                registrationMessage.hidden = true;
                registrationMessage.replaceChildren();
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
            event.preventDefault();
            clearFieldErrors();
            registrationMessage.hidden = true;
            registrationMessage.replaceChildren();

            const clientErrors = getClientValidationErrors();
            if (clientErrors.length) {
                clientErrors.forEach(({ input, message }) => {
                    const validationMessage = registrationForm.querySelector(`[data-valmsg-for="${input.id}"]`);
                    input.setAttribute('aria-invalid', 'true');
                    if (validationMessage) {
                        validationMessage.textContent = message;
                        validationMessage.classList.add('field-validation-error');
                    }
                });
                await showRegistrationMessage('Please correct the highlighted fields.', false,
                    clientErrors.map(error => error.message));
                clientErrors[0].input.focus();
                return;
            }

            if (!addressValueInput.value.trim()) {
                addressStatus.textContent = 'Select a search result or enter the address manually.';
                addressSearchInput.focus();
                await showRegistrationMessage('Enter a home address before creating the account.', false);
                return;
            }

            createPatientButton.disabled = true;
            createPatientButton.textContent = 'Creating account...';

            try {
                const response = await fetch(registrationForm.action, {
                    method: 'POST',
                    body: new FormData(registrationForm),
                    credentials: 'same-origin',
                    headers: { 'Accept': 'application/json' }
                });

                if (response.redirected || response.status === 401 || response.status === 403) {
                    throw new Error('Your session may have expired. Sign in again and retry the registration.');
                }

                let data;
                try {
                    if (!(response.headers.get('content-type') || '').includes('application/json')) {
                        throw new Error('The server did not return a validation response. Refresh the page and try again.');
                    }
                    data = await response.json();
                } catch (error) {
                    throw new Error(error.message || 'The server returned an unexpected response. Please try again.');
                }

                const serverErrors = data.errors || {};
                setFieldErrors(serverErrors);
                const validationMessages = Object.values(serverErrors).flat().filter(Boolean);
                const message = data.message || 'Registration could not be completed.';
                const success = response.ok && data.success === true;


                if (success) registrationForm.reset();
                await showRegistrationMessage(message, success, success ? [] : validationMessages);
            } catch (error) {
                console.error('Patient registration error:', error);
                await showRegistrationMessage(
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
                const message = registrationForm.querySelector(`[data-valmsg-for="${this.id}"]`);
                if (message) message.textContent = '';
                this.removeAttribute('aria-invalid');
            });
        });

        registrationFields.forEach(name => {
            document.getElementById(name)?.addEventListener('input', function () {
                const message = registrationForm.querySelector(`[data-valmsg-for="${name}"]`);
                if (message) {
                    message.textContent = '';
                    message.classList.remove('field-validation-error');
                }
                this.removeAttribute('aria-invalid');
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

        async function showRegistrationMessage(message, success, details = []) {
            const messageVersion = ++registrationMessageVersion;
            registrationMessage.replaceChildren();
            const heading = document.createElement('p');
            heading.className = 'registration-result-heading';
            heading.textContent = message;
            registrationMessage.appendChild(heading);
            if (details.length) {
                const list = document.createElement('ul');
                details.forEach(detail => {
                    const item = document.createElement('li');
                    item.textContent = detail;
                    list.appendChild(item);
                });
                registrationMessage.appendChild(list);
            }
            registrationMessage.hidden = false;
            registrationMessage.classList.remove('success', 'error', 'info');
            registrationMessage.classList.add(success ? 'success' : 'error');

            if (!success) {
                try {
                    const administratorContact = await getAdministratorContact();
                    if (messageVersion !== registrationMessageVersion || registrationMessage.hidden) return;
                    if (administratorContact) {
                        const contact = document.createElement('a');
                        contact.className = 'registration-admin-link';
                        contact.textContent = 'Email the Administrator';
                        contact.href = buildAdministratorEmailLink(
                            administratorContact.email,
                            administratorContact.doctorName,
                            administratorContact.doctorEmail);
                        registrationMessage.appendChild(contact);
                    }
                } catch (error) {
                    console.error('Could not load administrator contact:', error);
                }
            }
        }
