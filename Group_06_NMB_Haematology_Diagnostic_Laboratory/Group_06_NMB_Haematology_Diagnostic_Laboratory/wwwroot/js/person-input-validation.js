(function () {
    'use strict';

    function birthDate(id) {
        if (!/^[0-9]{13}$/.test(id)) return '';
        const today = new Date();
        let year = Math.floor(today.getFullYear() / 100) * 100 + Number(id.slice(0, 2));
        const month = Number(id.slice(2, 4));
        const day = Number(id.slice(4, 6));
        if (year > today.getFullYear() || (year === today.getFullYear() &&
            (month > today.getMonth() + 1 || (month === today.getMonth() + 1 && day > today.getDate())))) year -= 100;
        const date = new Date(year, month - 1, day);
        if (date.getFullYear() !== year || date.getMonth() !== month - 1 || date.getDate() !== day) return '';
        return year + '-' + id.slice(2, 4) + '-' + id.slice(4, 6);
    }

    function validId(id) {
        if (!/^[0-9]{10}[01][0-9]{2}$/.test(id) || !birthDate(id)) return false;
        const sum = Array.from(id).reduce(function (total, character, index) {
            const digit = Number(character) * (index % 2 === 1 ? 2 : 1);
            return total + (digit > 9 ? digit - 9 : digit);
        }, 0);
        return sum % 10 === 0;
    }

    window.patientInputValidation = { birthDate, validId };
    const nameRule = /^\p{L}[\p{L}\p{M} .’'\-]*$/u;
    const idMessage = 'Enter a valid 13-digit South African ID number.';
    const nameMessage = 'Use letters, spaces, apostrophes, periods, or hyphens only.';

    document.querySelectorAll('[data-person-name], [data-digits]').forEach(function (input) {
        function validate() {
            let message = '';
            if (input.value) {
                if (input.hasAttribute('data-person-name') && !nameRule.test(input.value)) message = nameMessage;
                else if (input.hasAttribute('data-sa-id') && !validId(input.value)) message = idMessage;
                else if (input.dataset.digits && input.value.length !== Number(input.dataset.digits))
                    message = 'Enter exactly ' + input.dataset.digits + ' digits.';
            }
            input.setCustomValidity(message);
        }
        input.addEventListener('input', function () {
            const start = input.selectionStart;
            const clean = value => input.hasAttribute('data-person-name')
                ? value.replace(/[^\p{L}\p{M} .’'\-]/gu, '')
                : value.replace(/[^0-9]/g, '').slice(0, Number(input.dataset.digits));
            const cleaned = clean(input.value);
            if (cleaned !== input.value) {
                const position = clean(input.value.slice(0, start)).length;
                input.value = cleaned;
                input.setSelectionRange(position, position);
            }
            validate();
        });
        validate();
        // jQuery unobtrusive validation disables native submit validation.
        input.form?.addEventListener('submit', function (event) {
            validate();
            if (!input.checkValidity()) {
                event.preventDefault();
                input.reportValidity();
            }
        });
    });

    // .NET Unicode regex categories need a Unicode-aware JavaScript rule.
    if (window.jQuery?.validator) {
        const $ = window.jQuery;
        $.validator.addMethod('personname', function (value, element) {
            return this.optional(element) || nameRule.test(value);
        }, nameMessage);
        $.validator.addMethod('southafricanid', function (value, element) {
            return this.optional(element) || validId(value);
        }, idMessage);
        $(function () {
            $('[data-person-name]').each(function () {
                $(this).rules('remove', 'regex');
                $(this).rules('add', { personname: true });
            });
            $('[data-sa-id]').each(function () {
                $(this).rules('add', { southafricanid: true });
            });
        });
    }
}());
