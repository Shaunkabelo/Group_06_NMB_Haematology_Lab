const assetScript = document.currentScript;
const registrationForm = document.getElementById('patientRegistrationForm');
        const registrationMessage = document.getElementById('registration-message');
        const createPatientButton = document.getElementById('createPatientButton');

        registrationForm.addEventListener('submit', async function (event) {
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

        function showRegistrationMessage(message, success) {
            registrationMessage.textContent = message;
            registrationMessage.hidden = false;
            registrationMessage.classList.remove('success', 'error', 'info');
            registrationMessage.classList.add(success ? 'success' : 'error');
        }
