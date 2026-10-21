document.addEventListener("DOMContentLoaded", () => {
    document.querySelectorAll("form[data-staff-validation]").forEach(form => {
        form.noValidate = true;
        const employeeField = form.elements.namedItem("EmployeeNumber");
        const firstNameField = form.elements.namedItem("User.Name");
        if (employeeField && firstNameField) {
            const employeeGroup = employeeField.closest(".form-group");
            const firstNameGroup = firstNameField.closest(".form-group");
            if (employeeGroup && firstNameGroup) form.insertBefore(employeeGroup, firstNameGroup);
        }
        const idField = form.elements.namedItem("User.SaIdNumber");
        const birthField = form.elements.namedItem("User.DateOfBirth");
        if (idField && birthField) {
            const idGroup = idField.closest(".form-group");
            const birthRow = birthField.closest(".form-row");
            if (idGroup && birthRow) form.insertBefore(idGroup, birthRow);
        }
        const limits = {SupplierName:100, ContactPerson:100, SupplierEmail:150, SampleTypeName:100, CategoryName:100, CategoryDescription:255, Name:100, TestName:100, UnitOfMeasurement:50};
        Object.entries(limits).forEach(([name,max]) => { const field = form.elements.namedItem(name); if(field) field.maxLength = max; });
        const fields = [...form.querySelectorAll('input:not([type=hidden]):not([type=checkbox]), textarea, select')];
        const summary = document.createElement("div");
        summary.className = "alert alert-danger"; summary.hidden = true; summary.setAttribute("role", "alert");
        form.prepend(summary);
        const labels = {
            "User.Name": "First name", "User.Surname": "Surname", "User.Email": "Email address",
            "User.Cellphone": "Cellphone number", "User.SaIdNumber": "SA ID number",
            "User.DateOfBirth": "Date of birth", "User.HomeAddress": "Home address",
            "HpcsaNumber": "HPCSA number", "EmployeeNumber": "Employee number"
        };
        function message(field, text) {
            field.classList.toggle("is-invalid", !!text);
            field.setAttribute("aria-invalid", String(!!text));
            document.getElementById(field.dataset.errorId).textContent = text;
        }
        function validate(field) {
            const value = field.value.trim(), name = field.name; const label = labels[name] || name.replace(/([a-z])([A-Z])/g, "$1 $2");
            let error = "";
            if (field.disabled) { message(field, ""); return true; }
            if (field.required && !value) error = label + " is required.";
            else if (value && field.maxLength > 0 && value.length > field.maxLength)
                error = label + " must be " + field.maxLength + " characters or fewer.";
            else if (field.type === "number" && value && !field.validity.valid)
                error = "Enter a valid number within the allowed range.";
            else if (field.type === "email" && value && field.validity.typeMismatch)
                error = "Enter a valid email address.";
            else if (field.hasAttribute("data-normal-range") && !/^-?[0-9]{1,8}([.,][0-9]{1,4})?$/.test(value))
                error = "Enter numbers only: up to 8 digits before and 4 after one comma or dot. No thousands separators.";
            else if (name === "NormalRangeMax" && value && form.elements.namedItem("NormalRangeMin")?.value && Number(value.replace(",", ".")) < Number(form.elements.namedItem("NormalRangeMin").value.replace(",", ".")))
                error = "Maximum must be greater than or equal to minimum.";
            else if (name === "User.Cellphone" && !/^[0-9]{10}$/.test(value))
                error = "Cellphone number must contain exactly 10 digits, with no spaces or letters.";
            else if (name === "User.SaIdNumber" && !/^[0-9]{13}$/.test(value))
                error = "SA ID number must contain exactly 13 digits, with no spaces or letters.";
            else if ((name === "User.Name" || name === "User.Surname") && value && !/^[\p{L}\p{M} .'\u2019-]+$/u.test(value))
                error = label + " may contain letters, spaces, apostrophes, full stops and hyphens.";
            else if (name === "User.Email" && value && (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value) || field.validity.typeMismatch))
                error = "Enter a valid email address, for example name@example.com.";
            else if (name === "User.DateOfBirth" && value && value > field.max)
                error = "Date of birth cannot be in the future.";
            if (!error && name === "User.SaIdNumber" && value.length === 13) {
                const dob = form.elements.namedItem("User.DateOfBirth").value;
                if (dob && value.substring(0, 6) !== dob.slice(2).replaceAll("-", ""))
                    error = "The first 6 ID digits must match the date of birth (YYMMDD).";
            }
            message(field, error); return !error;
        }
        fields.forEach(field => {
            if (field.type === "number") { field.max = field.max || "2147483647"; }
            if (field.hasAttribute("data-normal-range")) {
                field.addEventListener("beforeinput", event => {
                    if (event.data && /[^0-9,.\-]/.test(event.data)) {
                        event.preventDefault(); message(field, "Only numbers and one decimal comma or dot are allowed.");
                    }
                });
                field.addEventListener("paste", event => {
                    const pasted = event.clipboardData?.getData("text") ?? "";
                    if (/[^0-9,.\-\s]/.test(pasted)) {
                        event.preventDefault(); message(field, "Paste a number, for example 4.5 or 4,5.");
                    }
                });
            }
            const error = document.createElement("span");
            error.id = form.id + "-" + (field.id || field.name) + "-error";
            error.className = "text-danger small d-block mt-1"; error.setAttribute("aria-live", "polite");
            field.after(error); field.dataset.errorId = error.id; field.setAttribute("aria-describedby", error.id);
            const group = field.closest(".form-group");
            const label = group?.querySelector("label");
            if (label) label.htmlFor = field.id;
            field.addEventListener("input", () => {
                if (field.name === "User.Name" || field.name === "User.Surname") {
                    const cleaned = field.value.replace(/[^\p{L}\p{M} .'\u2019-]/gu, "");
                    if (cleaned !== field.value) { field.value = cleaned; message(field, "Numbers and symbols are not allowed in names."); return; }
                }
                if (field.dataset.digits) {
                    const cleaned = field.value.replace(/[^0-9]/g, "").slice(0, Number(field.dataset.digits));
                    const removed = cleaned !== field.value; field.value = cleaned;
                    if (removed) { message(field, "Only digits (0–9) are allowed."); return; }
                }
                if (field.name === "User.SaIdNumber") populateDateOfBirthFromSaId();
                validate(field);
                if (field.name === "NormalRangeMin") validate(form.elements.namedItem("NormalRangeMax"));
                if (field.name === "User.DateOfBirth") validate(form.elements.namedItem("User.SaIdNumber"));
            });
            field.addEventListener("blur", () => validate(field));
        });
        function clearErrors() {
            fields.forEach(field => message(field, ""));
            summary.hidden = true; summary.textContent = "";
        }
        function populateDateOfBirthFromSaId() {
            const idField = form.elements.namedItem("User.SaIdNumber");
            const dobField = form.elements.namedItem("User.DateOfBirth");
            const value = idField?.value ?? "";
            if (!dobField || !/^\d{6}/.test(value)) return;
            const yy = Number(value.slice(0, 2)), month = Number(value.slice(2, 4)), day = Number(value.slice(4, 6));
            const currentYear = new Date().getFullYear();
            const year = 2000 + yy > currentYear ? 1900 + yy : 2000 + yy;
            const candidate = new Date(Date.UTC(year, month - 1, day));
            if (candidate.getUTCFullYear() !== year || candidate.getUTCMonth() !== month - 1 || candidate.getUTCDate() !== day) return;
            dobField.value = `${year}-${String(month).padStart(2, "0")}-${String(day).padStart(2, "0")}`;
            validate(dobField);
        }
        form.addEventListener("reset", clearErrors);
        form.closest(".offcanvas")?.addEventListener("show.bs.offcanvas", clearErrors);
        form.addEventListener("submit", async event => {
            event.preventDefault();
            let valid = true; fields.forEach(field => { if (!field.disabled && !validate(field)) valid = false; });
            if (!form.reportValidity()) valid = false;
            if (!valid) { form.querySelector(".is-invalid")?.focus(); return; }
            summary.hidden = true;
            const button = form.querySelector('[type="submit"]'); button.disabled = true;
            try {
                const response = await fetch(form.action, {
                    method: "POST", body: new FormData(form), headers: { "X-Requested-With": "StaffForm" }
                });
                const data = response.headers.get("content-type")?.includes("application/json") ? await response.json() : null;
                if (response.ok && data?.redirectUrl) { window.location.assign(data.redirectUrl); return; }
                // MVC redirects can be followed by fetch as an HTML response when a
                // deployment or proxy does not preserve the StaffForm request header.
                if (response.ok && response.redirected) { window.location.assign(response.url); return; }
                if (data?.errors) {
                    summary.textContent = "";
                    Object.entries(data.errors).forEach(([key, messages]) => {
                        const field = fields.find(f => f.name.toLowerCase() === key.toLowerCase());
                        if (field) message(field, messages.join(" "));
                        else { summary.hidden = false; summary.textContent += messages.join(" ") + " "; }
                    });
                    if (!summary.hidden) summary.scrollIntoView({ behavior: "smooth", block: "nearest" });
                    form.querySelector(".is-invalid")?.focus();
                } else {
                    summary.hidden = false;
                    summary.textContent = "Could not confirm the save. Check the list before trying again.";
                }
            } catch {
                summary.hidden = false; summary.textContent = "Connection interrupted. Check the list before trying again.";
            } finally { button.disabled = false; }
        });
    });
});



