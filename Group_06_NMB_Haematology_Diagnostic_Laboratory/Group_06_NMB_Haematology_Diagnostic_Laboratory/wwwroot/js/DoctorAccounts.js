const assetScript = document.currentScript;
const getDoctorAccountUrl =
        assetScript.dataset.getdoctoraccountUrl;

    const updateDoctorAccountUrl =
        assetScript.dataset.updatedoctoraccountUrl;

    const changeDoctorPasswordUrl =
        assetScript.dataset.changedoctorpasswordUrl;

    const doctorPreferencesStorageKey =
        'doctorPreferences';


    function showAccountMessage(elementId, message, type) {
        const messageElement =
            document.getElementById(elementId);

        if (!messageElement) {
            return;
        }

        messageElement.textContent = message;
        messageElement.className = 'auth-message ' + type;
        messageElement.hidden = false;
    }


    function clearInlineMessage(elementId) {
        const messageElement =
            document.getElementById(elementId);

        if (!messageElement) {
            return;
        }

        messageElement.textContent = '';
        messageElement.className = 'auth-message';
        messageElement.hidden = true;
    }


    function getDoctorInitials(name, surname) {
        const firstInitial =
            name ? name.charAt(0) : '';

        const secondInitial =
            surname ? surname.charAt(0) : '';

        const initials =
            (firstInitial + secondInitial).toUpperCase();

        return initials || 'DR';
    }


    async function loadDoctorProfile() {
        clearInlineMessage('account-profile-message');

        try {
            const response =
                await fetch(
                    getDoctorAccountUrl,
                    {
                        method: 'GET',
                        headers: {
                            'Accept': 'application/json'
                        }
                    }
                );

            const data =
                await response.json();

            if (!response.ok) {
                throw new Error(
                    data.message ||
                    'Could not load doctor profile.'
                );
            }

            const name =
                data.name ?? data.Name ?? '';

            const surname =
                data.surname ?? data.Surname ?? '';

            const email =
                data.email ?? data.Email ?? '';

            const cellphone =
                data.cellphone ?? data.Cellphone ?? '';

            const role =
                data.role ?? data.Role ?? '';

            const hpcsaNumber =
                data.hpcsaNumber ??
                data.HpcsaNumber ??
                '';

            document.getElementById(
                'doctor-account-name'
            ).value = name;

            document.getElementById(
                'doctor-account-surname'
            ).value = surname;

            document.getElementById(
                'doctor-account-email'
            ).value = email;

            document.getElementById(
                'doctor-account-phone'
            ).value = cellphone;

            const fullName =
                (name + ' ' + surname).trim();

            document.getElementById(
                'doctor-profile-name'
            ).textContent =
                fullName
                    ? 'Dr. ' + fullName
                    : 'Doctor';

            document.getElementById(
                'doctor-profile-role'
            ).textContent =
                role || '-';

            document.getElementById(
                'doctor-info-role'
            ).textContent =
                role || '-';

            document.getElementById(
                'doctor-info-hpcsa'
            ).textContent =
                hpcsaNumber || '-';

            document.getElementById(
                'doctor-profile-avatar'
            ).textContent =
                getDoctorInitials(
                    name,
                    surname
                );
        }
        catch (error) {
            console.error(
                'Doctor profile load error:',
                error
            );

            showAccountMessage(
                'account-profile-message',
                error.message ||
                'Could not load doctor profile.',
                'error'
            );
        }
    }


    async function saveDoctorProfile(event) {
        event.preventDefault();

        clearInlineMessage(
            'account-profile-message'
        );

        const model = {
            name:
                document.getElementById(
                    'doctor-account-name'
                ).value.trim(),

            surname:
                document.getElementById(
                    'doctor-account-surname'
                ).value.trim(),

            email:
                document.getElementById(
                    'doctor-account-email'
                ).value.trim(),

            cellphone:
                document.getElementById(
                    'doctor-account-phone'
                ).value.trim()
        };

        try {
            const response =
                await fetch(
                    updateDoctorAccountUrl,
                    {
                        method: 'POST',
                        headers: {
                            'Content-Type':
                                'application/json'
                        },
                        body:
                            JSON.stringify(model)
                    }
                );

            const result =
                await response.json();

            if (!response.ok) {
                throw new Error(
                    result.message ||
                    'Could not update profile.'
                );
            }

            showAccountMessage(
                'account-profile-message',
                result.message ||
                'Profile updated successfully.',
                'success'
            );

            await loadDoctorProfile();
        }
        catch (error) {
            console.error(
                'Doctor profile update error:',
                error
            );

            showAccountMessage(
                'account-profile-message',
                error.message ||
                'Could not update profile.',
                'error'
            );
        }
    }


    function loadDoctorPreferences() {
        let preferences = null;

        try {
            preferences =
                JSON.parse(
                    localStorage.getItem(
                        doctorPreferencesStorageKey
                    )
                );
        }
        catch {
            preferences = null;
        }

        if (!preferences) {
            preferences = {
                results: false,
                urgent: false,
                summary: false
            };
        }

        document.getElementById(
            'preference-results'
        ).checked =
            Boolean(preferences.results);

        document.getElementById(
            'preference-urgent'
        ).checked =
            Boolean(preferences.urgent);

        document.getElementById(
            'preference-summary'
        ).checked =
            Boolean(preferences.summary);
    }


    function saveDoctorPreferences() {
        const preferences = {
            results:
                document.getElementById(
                    'preference-results'
                ).checked,

            urgent:
                document.getElementById(
                    'preference-urgent'
                ).checked,

            summary:
                document.getElementById(
                    'preference-summary'
                ).checked
        };

        localStorage.setItem(
            doctorPreferencesStorageKey,
            JSON.stringify(preferences)
        );

        showAccountMessage(
            'account-notification-message',
            'Notification preferences have been saved.',
            'success'
        );
    }


    async function changeLoggedInDoctorPassword(event) {
        event.preventDefault();

        clearInlineMessage(
            'account-password-message'
        );

        const currentPassword =
            document.getElementById(
                'doctor-current-password'
            ).value;

        const newPassword =
            document.getElementById(
                'doctor-account-new-password'
            ).value;

        const confirmPassword =
            document.getElementById(
                'doctor-account-confirm-password'
            ).value;

        if (newPassword !== confirmPassword) {
            showAccountMessage(
                'account-password-message',
                'New password and confirm password do not match.',
                'error'
            );

            return;
        }

        const passwordPattern =
            /^(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).{8,}$/;

        if (!passwordPattern.test(newPassword)) {
            showAccountMessage(
                'account-password-message',
                'Password must contain at least 8 characters, 1 uppercase letter, 1 number, and 1 special character.',
                'error'
            );

            return;
        }

        const model = {
            currentPassword:
                currentPassword,

            newPassword:
                newPassword,

            confirmPassword:
                confirmPassword
        };

        try {
            const response =
                await fetch(
                    changeDoctorPasswordUrl,
                    {
                        method: 'POST',
                        headers: {
                            'Content-Type':
                                'application/json'
                        },
                        body:
                            JSON.stringify(model)
                    }
                );

            const result =
                await response.json();

            if (!response.ok) {
                throw new Error(
                    result.message ||
                    'Could not update password.'
                );
            }

            showAccountMessage(
                'account-password-message',
                result.message ||
                'Password updated successfully.',
                'success'
            );

            document.getElementById(
                'doctor-current-password'
            ).value = '';

            document.getElementById(
                'doctor-account-new-password'
            ).value = '';

            document.getElementById(
                'doctor-account-confirm-password'
            ).value = '';
        }
        catch (error) {
            console.error(
                'Password update error:',
                error
            );

            showAccountMessage(
                'account-password-message',
                error.message ||
                'Could not update password.',
                'error'
            );
        }
    }


    function togglePassword(inputId, button) {
        const input =
            document.getElementById(inputId);

        if (!input) {
            return;
        }

        if (input.type === 'password') {
            input.type = 'text';
            button.textContent = 'Hide';
        }
        else {
            input.type = 'password';
            button.textContent = 'Show';
        }
    }


    document.addEventListener(
        'DOMContentLoaded',
        function () {
            loadDoctorProfile();
            loadDoctorPreferences();
        }
    );
