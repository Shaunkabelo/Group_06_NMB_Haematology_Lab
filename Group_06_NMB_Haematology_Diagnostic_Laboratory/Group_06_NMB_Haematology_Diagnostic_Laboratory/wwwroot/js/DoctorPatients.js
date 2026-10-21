const assetScript = document.currentScript;
async function viewPatientFromTable(button) {

        const saIdNumber =
            button.dataset.saId;

        if (!saIdNumber) {

            toast(
                'Patient SA ID number was not found.'
            );

            return;
        }


        const originalText =
            button.textContent;


        try {

            // ==============================================
            // BUTTON LOADING STATE
            // ==============================================

            button.disabled = true;

            button.textContent =
                'Loading...';


            // ==============================================
            // GET COMPLETE PATIENT
            // ==============================================

            const response =
                await fetch(
                    `${doctorUrls.getPatient}?saIdNumber=${encodeURIComponent(saIdNumber)}`
                );


            if (!response.ok) {

                throw new Error(
                    'Unable to load patient details.'
                );
            }


            const patient =
                await response.json();


            // ==============================================
            // UPDATE SELECTED PATIENT
            // ==============================================

            selectedPatient =
                patient;


            const select =
                document.getElementById(
                    'doc-pat-id'
                );

            select.value =
                saIdNumber;


            // ==============================================
            // DISPLAY PATIENT INFORMATION ABOVE
            // ==============================================

            displayPatientDetails(
                patient
            );


            document.getElementById(
                'selected-patient-summary'
            ).style.display =
                'block';


            // ==============================================
            // FIND TABLE ROW
            // ==============================================

            const row =
                button.closest('tr');


            if (!row) {

                throw new Error(
                    'Patient table row was not found.'
                );
            }


            // ==============================================
            // CONDITIONS
            // ==============================================

            const conditions =
                patient.knownMedicalConditions ||
                patient.medicalConditions ||
                patient.conditions ||
                'None recorded';


            row.querySelector(
                '.patient-conditions'
            ).innerHTML = `

                <span class="medical-cell">
                    ${escapeHtml(conditions)}
                </span>
            `;


            // ==============================================
            // ALLERGIES
            // ==============================================

            const allergies =
                patient.allergies ||
                'None recorded';


            row.querySelector(
                '.patient-allergies'
            ).innerHTML = `

                <span class="medical-cell">
                    ${escapeHtml(allergies)}
                </span>
            `;


            // ==============================================
            // MEDICATION
            // ==============================================

            const medication =
                patient.medication ||
                patient.medications ||
                'None recorded';


            row.querySelector(
                '.patient-medication'
            ).innerHTML = `

                <span class="medical-cell">
                    ${escapeHtml(medication)}
                </span>
            `;


            // Scroll patient details into view
            document.getElementById(
                'selected-patient-summary'
            ).scrollIntoView({
                behavior: 'smooth',
                block: 'start'
            });

        }
        catch (error) {

            console.error(error);

            toast(
                error.message ||
                'Unable to load patient details.'
            );

        }
        finally {

            button.disabled =
                false;

            button.textContent =
                originalText;
        }
    }

    // ================================================================
    // CONTROLLER URLS
    // ================================================================

    const doctorUrls = {

        getPatients:
            assetScript.dataset.getpatientsUrl,

        getPatient:
            assetScript.dataset.getpatientUrl,

        getConditionCategories:
            assetScript.dataset.getconditioncategoriesUrl,

        getConditionsByCategory:
            assetScript.dataset.getconditionsbycategoryUrl,

        addPatientCondition:
            assetScript.dataset.addpatientconditionUrl,

        deletePatientCondition:
            assetScript.dataset.deletepatientconditionUrl,

        getAllergyCategories:
            assetScript.dataset.getallergycategoriesUrl,

        getAllergiesByCategory:
            assetScript.dataset.getallergiesbycategoryUrl,

        addPatientAllergy:
            assetScript.dataset.addpatientallergyUrl,

        deletePatientAllergy:
            assetScript.dataset.deletepatientallergyUrl,

        getMedicationCategories:
            assetScript.dataset.getmedicationcategoriesUrl,

        getMedicationsByCategory:
            assetScript.dataset.getmedicationsbycategoryUrl,

        addPatientMedication:
            assetScript.dataset.addpatientmedicationUrl,

        deletePatientMedication:
            assetScript.dataset.deletepatientmedicationUrl

    };


    // ================================================================
    // GLOBAL STATE
    // ================================================================

    let selectedPatient = null;


    // ================================================================
    // TOAST / MESSAGE
    // ================================================================

    function toast(message) {

        alert(message);

    }


    // ================================================================
    // LOAD PATIENTS
    // ================================================================

        async function loadPatients() {

        const select =
            document.getElementById('doc-pat-id');

        const tbody =
            document.getElementById('doctor-patients-tbody');

        try {

            const response =
                await fetch(doctorUrls.getPatients);

            if (!response.ok) {

                throw new Error(
                    'Unable to load patients.'
                );
            }

            const patients =
                await response.json();


            // Reset patient search input
            select.value = '';


            // Reset table
            tbody.innerHTML = '';


            if (!patients || patients.length === 0) {

                tbody.innerHTML = `
                    <tr>
                        <td colspan="6">
                            No patients found.
                        </td>
                    </tr>
                `;

                return;
            }


            patients.forEach(patient => {

                // ==============================================
                // CREATE PATIENT TABLE ROW
                // ==============================================

                const row =
                    document.createElement('tr');

                row.id =
                    `patient-row-${patient.patientId}`;


                row.innerHTML = `

                    <td data-label="ID Number">
                        ${escapeHtml(
                            patient.saIdNumber || 'N/A'
                        )}
                    </td>


                    <td data-label="Physical Address">
                        ${escapeHtml(
                            patient.address ||
                            patient.homeAddress ||
                            'N/A'
                        )}
                    </td>


                    <td data-label="Conditions"
                        class="patient-conditions">

                        <span class="medical-empty">
                            Click View
                        </span>

                    </td>


                    <td data-label="Allergies"
                        class="patient-allergies">

                        <span class="medical-empty">
                            Click View
                        </span>

                    </td>


                    <td data-label="Medication"
                        class="patient-medication">

                        <span class="medical-empty">
                            Click View
                        </span>

                    </td>


                    <td data-label="Action">

                        <button
                            type="button"
                            class="btn btn-primary patient-view-btn"
                            data-sa-id="${escapeHtml(
                                patient.saIdNumber || ''
                            )}"
                            onclick="viewPatientFromTable(this)">

                            View

                        </button>

                    </td>
                `;


                tbody.appendChild(row);
            });

        }
        catch (error) {

            console.error(
                'loadPatients error:',
                error
            );


            tbody.innerHTML = `
                <tr>
                    <td colspan="6">
                        Unable to load patients.
                    </td>
                </tr>
            `;


            toast(
                'Unable to load patients from the database.'
            );
        }
    }


    function sanitizeSaIdInput(value) {
        return String(value || '')
            .replace(/\D/g, '')
            .slice(0, 13);
    }


    function validateSaIdNumber(value) {
        return /^\d{13}$/.test(value);
    }


    // ================================================================
    // SELECT PATIENT
    // ================================================================

    async function loadSelectedPatientSummary() {

        const patientIdInput =
            document.getElementById('doc-pat-id');

        const saIdNumber =
            sanitizeSaIdInput(patientIdInput.value);

        patientIdInput.value =
            saIdNumber;


        const panel =
            document.getElementById(
                'selected-patient-summary'
            );


        if (!saIdNumber) {

            selectedPatient = null;

            panel.style.display = 'none';

            clearPatientDetails();

            return;

        }


        if (!validateSaIdNumber(saIdNumber)) {

            selectedPatient = null;

            panel.style.display = 'none';

            clearPatientDetails();

            toast('SA ID must contain exactly 13 numbers.');
            patientIdInput.focus();
            return;

        }


        try {

            const response =
                await fetch(
                    `${doctorUrls.getPatient}?saIdNumber=${encodeURIComponent(saIdNumber)}`
                );


            if (!response.ok) {

                if (response.status === 404) {
                    showPatientNotFoundModal(saIdNumber);
                    return;
                }

                throw new Error(
                    'Patient could not be loaded.'
                );

            }


            const patient =
                await response.json();


            selectedPatient = patient;


            displayPatientDetails(patient);


            panel.style.display = 'block';


            clearMedicalHistoryPills();

        }
        catch (error) {

            console.error(error);

            selectedPatient = null;

            panel.style.display = 'none';

            toast(
                'Unable to load the selected patient.'
            );

        }

    }


    function showPatientNotFoundModal(saIdNumber) {

        document.getElementById(
            'patient-not-found-message'
        ).textContent =
            `Patient with SA ID ${saIdNumber} does not exist in the system.`;

        document.getElementById(
            'patient-not-found-modal'
        ).style.display = 'flex';

    }


    function closePatientNotFoundModal() {

        document.getElementById(
            'patient-not-found-modal'
        ).style.display = 'none';

    }


    // ================================================================
    // DISPLAY PATIENT DETAILS
    // ================================================================

    function displayPatientDetails(patient) {

        document.getElementById(
            'selected-patient-name'
        ).textContent =
            `${patient.name || ''} ${patient.surname || ''}`.trim();


        document.getElementById(
            'selected-patient-id'
        ).textContent =
            patient.saIdNumber || 'N/A';


        document.getElementById(
            'selected-patient-dob'
        ).textContent =
            formatDate(patient.dateOfBirth);


        document.getElementById(
            'selected-patient-address'
        ).textContent =
            patient.address ||
            patient.homeAddress ||
            'N/A';


        document.getElementById(
            'selected-patient-contact'
        ).textContent =
            patient.contactNumber ||
            patient.cellphone ||
            'N/A';


        document.getElementById(
            'selected-patient-email'
        ).textContent =
            patient.email ||
            patient.Email ||
            'N/A';


        document.getElementById(
            'selected-patient-conditions'
        ).textContent =
            formatMedicalList(
                patient.knownMedicalConditions ||
                patient.KnownMedicalConditions
            );


        document.getElementById(
            'selected-patient-allergies'
        ).textContent =
            formatMedicalList(
                patient.allergies ||
                patient.Allergies
            );


        document.getElementById(
            'selected-patient-medication'
        ).textContent =
            formatMedicalList(
                patient.medication ||
                patient.Medication
            );


        renderPatientHistory(patient);

    }


    function formatMedicalList(value) {

        if (value === null || value === undefined || value === '') {

            return 'None recorded';

        }

        const values =
            (Array.isArray(value) ? value : String(value).split(','))
                .map(item => {

                    if (typeof item === 'string') {
                        return item.trim();
                    }

                    return item.name ||
                        item.Name ||
                        item.conditionName ||
                        item.ConditionName ||
                        item.allergyName ||
                        item.AllergyName ||
                        item.medicationName ||
                        item.MedicationName ||
                        '';

                })
                .filter(item => item);

        return values.length
            ? values.sort((a, b) => a.localeCompare(b, undefined, { sensitivity: 'base' })).join(', ')
            : 'None recorded';

    }


    function renderPatientHistory(patient) {
        return window.loadDoctorPatientHistory(patient, 'selected-patient-');
    }

    function clearPatientDetails() {

        document.getElementById(
            'selected-patient-name'
        ).textContent = '';


        document.getElementById(
            'selected-patient-id'
        ).textContent = '';


        document.getElementById(
            'selected-patient-dob'
        ).textContent = '';


        document.getElementById(
            'selected-patient-address'
        ).textContent = '';


        document.getElementById(
            'selected-patient-contact'
        ).textContent = '';


        document.getElementById(
            'selected-patient-email'
        ).textContent = '';


        document.getElementById(
            'selected-patient-conditions'
        ).textContent = 'None recorded';


        document.getElementById(
            'selected-patient-allergies'
        ).textContent = 'None recorded';


        document.getElementById(
            'selected-patient-medication'
        ).textContent = 'None recorded';

    }


    function appendPatientHistory(elementId, value) {

        const element =
            document.getElementById(elementId);

        const currentValue =
            element.textContent.trim();

        if (!value || currentValue === value) {
            return;
        }

        element.textContent =
            currentValue === 'None recorded'
                ? value
                : `${currentValue}, ${value}`;

    }


    // ================================================================
    // SELECT PATIENT FROM TABLE
    // ================================================================

    function selectPatientFromTable(saIdNumber) {

        const select =
            document.getElementById('doc-pat-id');


        select.value = saIdNumber;


        loadSelectedPatientSummary();

    }


    // ================================================================
    // CONDITION CATEGORIES
    // ================================================================

    async function loadConditionCategories() {

        const select =
            document.getElementById(
                'doc-pat-condition-category'
            );


        try {

            const response =
                await fetch(
                    doctorUrls.getConditionCategories
                );


            if (!response.ok) {

                throw new Error(
                    'Unable to load condition categories.'
                );

            }


            const categories =
                await response.json();


            select.innerHTML = `
                <option value="">
                    -- Select Category --
                </option>
            `;


            const uniqueCategories =
                [...new Map(
                    categories.map(item => [
                        item.category,
                        item
                    ])
                ).values()];


            uniqueCategories.forEach(item => {

                const option =
                    document.createElement('option');


                option.value =
                    item.category;


                option.textContent =
                    item.category;


                select.appendChild(option);

            });

        }
        catch (error) {

            console.error(error);

            toast(
                'Unable to load medical condition categories.'
            );

        }

    }


    // ================================================================
    // CONDITIONS BY CATEGORY
    // ================================================================

    async function populateConditionNames() {

        const category =
            document.getElementById(
                'doc-pat-condition-category'
            ).value;


        const nameSelect =
            document.getElementById(
                'doc-pat-condition-name'
            );


        nameSelect.innerHTML = `
            <option value="">
                -- Select Condition --
            </option>
        `;


        nameSelect.disabled = true;


        if (!category) {

            return;

        }


        try {

            const response =
                await fetch(
                    `${doctorUrls.getConditionsByCategory}?category=${encodeURIComponent(category)}`
                );


            if (!response.ok) {

                throw new Error(
                    'Unable to load conditions.'
                );

            }


            const conditions =
                await response.json();


            conditions.forEach(condition => {

                const option =
                    document.createElement('option');


                option.value =
                    condition.medicalConditionId;


                option.textContent =
                    condition.conditionName;


                nameSelect.appendChild(option);

            });


            nameSelect.disabled =
                conditions.length === 0;

        }
        catch (error) {

            console.error(error);

            toast(
                'Unable to load medical conditions.'
            );

        }

    }


    // ================================================================
    // ADD PATIENT CONDITION
    // ================================================================

    async function addPatientCondition() {

        if (!selectedPatient) {

            toast(
                'Please select a patient first.'
            );

            return;

        }


        const conditionId =
            document.getElementById(
                'doc-pat-condition-name'
            ).value;


        const conditionSelect =
            document.getElementById(
                'doc-pat-condition-name'
            );


        const conditionName =
            conditionSelect.options[
                conditionSelect.selectedIndex
            ]?.textContent;


        if (!conditionId) {

            toast(
                'Please select a medical condition.'
            );

            return;

        }


        try {

            const body =
                new URLSearchParams();


            body.append(
                'patientId',
                selectedPatient.patientId
            );


            body.append(
                'medicalConditionId',
                conditionId
            );


            const response =
                await fetch(
                    doctorUrls.addPatientCondition,
                    {
                        method: 'POST',
                        headers: {
                            'Content-Type':
                                'application/x-www-form-urlencoded'
                        },
                        body: body.toString()
                    }
                );


            const result =
                await response.json();


            if (!response.ok || !result.success) {

                throw new Error(
                    result.message ||
                    'Unable to add condition.'
                );

            }


            addPill(
                'condition-pills',
                conditionName,
                result.patientConditionId,
                'condition'
            );

            appendPatientHistory(
                'selected-patient-conditions',
                conditionName
            );


            toast(
                result.message ||
                'Medical condition added successfully.'
            );

        }
        catch (error) {

            console.error(error);

            toast(
                error.message ||
                'Unable to add medical condition.'
            );

        }

    }


    // ================================================================
    // DELETE PATIENT CONDITION
    // ================================================================

    async function deletePatientCondition(
        patientConditionId,
        pill
    ) {

        if (!confirm(
            'Are you sure you want to remove this medical condition?'
        )) {

            return;

        }


        try {

            const body =
                new URLSearchParams();


            body.append(
                'patientConditionId',
                patientConditionId
            );


            const response =
                await fetch(
                    doctorUrls.deletePatientCondition,
                    {
                        method: 'POST',
                        headers: {
                            'Content-Type':
                                'application/x-www-form-urlencoded'
                        },
                        body: body.toString()
                    }
                );


            const result =
                await response.json();


            if (!response.ok || !result.success) {

                throw new Error(
                    result.message ||
                    'Unable to remove condition.'
                );

            }


            pill.remove();


            toast(
                result.message ||
                'Medical condition removed successfully.'
            );

        }
        catch (error) {

            console.error(error);

            toast(
                error.message ||
                'Unable to remove medical condition.'
            );

        }

    }


    // ================================================================
    // ALLERGY CATEGORIES
    // ================================================================

    async function loadAllergyCategories() {

        const select =
            document.getElementById(
                'doc-pat-allergy-category'
            );


        try {

            const response =
                await fetch(
                    doctorUrls.getAllergyCategories
                );


            if (!response.ok) {

                throw new Error(
                    'Unable to load allergy categories.'
                );

            }


            const categories =
                await response.json();


            select.innerHTML = `
                <option value="">
                    -- Select Category --
                </option>
            `;


            const uniqueCategories =
                [...new Map(
                    categories.map(item => [
                        item.category,
                        item
                    ])
                ).values()];


            uniqueCategories.forEach(item => {

                const option =
                    document.createElement('option');


                option.value =
                    item.category;


                option.textContent =
                    item.category;


                select.appendChild(option);

            });

        }
        catch (error) {

            console.error(error);

            toast(
                'Unable to load allergy categories.'
            );

        }

    }


    // ================================================================
    // ALLERGIES BY CATEGORY
    // ================================================================

    async function populateAllergyNames() {

        const category =
            document.getElementById(
                'doc-pat-allergy-category'
            ).value;


        const nameSelect =
            document.getElementById(
                'doc-pat-allergy-name'
            );


        nameSelect.innerHTML = `
            <option value="">
                -- Select Allergy --
            </option>
        `;


        nameSelect.disabled = true;


        if (!category) {

            return;

        }


        try {

            const response =
                await fetch(
                    `${doctorUrls.getAllergiesByCategory}?category=${encodeURIComponent(category)}`
                );


            if (!response.ok) {

                throw new Error(
                    'Unable to load allergies.'
                );

            }


            const allergies =
                await response.json();


            allergies.forEach(allergy => {

                const option =
                    document.createElement('option');


                option.value =
                    allergy.allergyId;


                option.textContent =
                    allergy.allergyName;


                nameSelect.appendChild(option);

            });


            nameSelect.disabled =
                allergies.length === 0;

        }
        catch (error) {

            console.error(error);

            toast(
                'Unable to load allergies.'
            );

        }

    }


    // ================================================================
    // ADD PATIENT ALLERGY
    // ================================================================

    async function addPatientAllergy() {

        if (!selectedPatient) {

            toast(
                'Please select a patient first.'
            );

            return;

        }


        const allergySelect =
            document.getElementById(
                'doc-pat-allergy-name'
            );


        const allergyId =
            allergySelect.value;


        const allergyName =
            allergySelect.options[
                allergySelect.selectedIndex
            ]?.textContent;


        if (!allergyId) {

            toast(
                'Please select an allergy.'
            );

            return;

        }


        try {

            const body =
                new URLSearchParams();


            body.append(
                'patientId',
                selectedPatient.patientId
            );


            body.append(
                'allergyId',
                allergyId
            );


            const response =
                await fetch(
                    doctorUrls.addPatientAllergy,
                    {
                        method: 'POST',
                        headers: {
                            'Content-Type':
                                'application/x-www-form-urlencoded'
                        },
                        body: body.toString()
                    }
                );


            const result =
                await response.json();


            if (!response.ok || !result.success) {

                throw new Error(
                    result.message ||
                    'Unable to add allergy.'
                );

            }


            addPill(
                'allergies-pills',
                allergyName,
                result.patientAllergyId,
                'allergy'
            );

            appendPatientHistory(
                'selected-patient-allergies',
                allergyName
            );


            toast(
                result.message ||
                'Allergy added successfully.'
            );

        }
        catch (error) {

            console.error(error);

            toast(
                error.message ||
                'Unable to add allergy.'
            );

        }

    }


    // ================================================================
    // DELETE PATIENT ALLERGY
    // ================================================================

    async function deletePatientAllergy(
        patientAllergyId,
        pill
    ) {

        if (!confirm(
            'Are you sure you want to remove this allergy?'
        )) {

            return;

        }


        try {

            const body =
                new URLSearchParams();


            body.append(
                'patientAllergyId',
                patientAllergyId
            );


            const response =
                await fetch(
                    doctorUrls.deletePatientAllergy,
                    {
                        method: 'POST',
                        headers: {
                            'Content-Type':
                                'application/x-www-form-urlencoded'
                        },
                        body: body.toString()
                    }
                );


            const result =
                await response.json();


            if (!response.ok || !result.success) {

                throw new Error(
                    result.message ||
                    'Unable to remove allergy.'
                );

            }


            pill.remove();


            toast(
                result.message ||
                'Allergy removed successfully.'
            );

        }
        catch (error) {

            console.error(error);

            toast(
                error.message ||
                'Unable to remove allergy.'
            );

        }

    }


    // ================================================================
    // MEDICATION CATEGORIES
    // ================================================================

    async function loadMedicationCategories() {

        const select =
            document.getElementById(
                'doc-pat-medication-category'
            );


        try {

            const response =
                await fetch(
                    doctorUrls.getMedicationCategories
                );


            if (!response.ok) {

                throw new Error(
                    'Unable to load medication categories.'
                );

            }


            const categories =
                await response.json();


            select.innerHTML = `
                <option value="">
                    -- Select Category --
                </option>
            `;


            const uniqueCategories =
                [...new Map(
                    categories.map(item => [
                        item.category,
                        item
                    ])
                ).values()];


            uniqueCategories.forEach(item => {

                const option =
                    document.createElement('option');


                option.value =
                    item.category;


                option.textContent =
                    item.category;


                select.appendChild(option);

            });

        }
        catch (error) {

            console.error(error);

            toast(
                'Unable to load medication categories.'
            );

        }

    }


    // ================================================================
    // MEDICATION BY CATEGORY
    // ================================================================

    async function populateMedicationNames() {

        const category =
            document.getElementById(
                'doc-pat-medication-category'
            ).value;


        const nameSelect =
            document.getElementById(
                'doc-pat-medication-name'
            );


        nameSelect.innerHTML = `
            <option value="">
                -- Select Medication --
            </option>
        `;


        nameSelect.disabled = true;


        if (!category) {

            return;

        }


        try {

            const response =
                await fetch(
                    `${doctorUrls.getMedicationsByCategory}?category=${encodeURIComponent(category)}`
                );


            if (!response.ok) {

                throw new Error(
                    'Unable to load medications.'
                );

            }


            const medications =
                await response.json();


            medications.forEach(medication => {

                const option =
                    document.createElement('option');


                option.value =
                    medication.medicationId;


                option.textContent =
                    medication.medicationName;


                nameSelect.appendChild(option);

            });


            nameSelect.disabled =
                medications.length === 0;

        }
        catch (error) {

            console.error(error);

            toast(
                'Unable to load medications.'
            );

        }

    }


    // ================================================================
    // ADD PATIENT MEDICATION
    // ================================================================

    async function addPatientMedication() {

        if (!selectedPatient) {

            toast(
                'Please select a patient first.'
            );

            return;

        }


        const medicationSelect =
            document.getElementById(
                'doc-pat-medication-name'
            );


        const medicationId =
            medicationSelect.value;


        const medicationName =
            medicationSelect.options[
                medicationSelect.selectedIndex
            ]?.textContent;


        if (!medicationId) {

            toast(
                'Please select a medication.'
            );

            return;

        }


        try {

            const body =
                new URLSearchParams();


            body.append(
                'patientId',
                selectedPatient.patientId
            );


            body.append(
                'medicationId',
                medicationId
            );


            const response =
                await fetch(
                    doctorUrls.addPatientMedication,
                    {
                        method: 'POST',
                        headers: {
                            'Content-Type':
                                'application/x-www-form-urlencoded'
                        },
                        body: body.toString()
                    }
                );


            const result =
                await response.json();


            if (!response.ok || !result.success) {

                throw new Error(
                    result.message ||
                    'Unable to add medication.'
                );

            }


            addPill(
                'medication-pills',
                medicationName,
                result.patientMedicationId,
                'medication'
            );

            appendPatientHistory(
                'selected-patient-medication',
                medicationName
            );


            toast(
                result.message ||
                'Medication added successfully.'
            );

        }
        catch (error) {

            console.error(error);

            toast(
                error.message ||
                'Unable to add medication.'
            );

        }

    }


    // ================================================================
    // DELETE PATIENT MEDICATION
    // ================================================================

    async function deletePatientMedication(
        patientMedicationId,
        pill
    ) {

        if (!confirm(
            'Are you sure you want to remove this medication?'
        )) {

            return;

        }


        try {

            const body =
                new URLSearchParams();


            body.append(
                'patientMedicationId',
                patientMedicationId
            );


            const response =
                await fetch(
                    doctorUrls.deletePatientMedication,
                    {
                        method: 'POST',
                        headers: {
                            'Content-Type':
                                'application/x-www-form-urlencoded'
                        },
                        body: body.toString()
                    }
                );


            const result =
                await response.json();


            if (!response.ok || !result.success) {

                throw new Error(
                    result.message ||
                    'Unable to remove medication.'
                );

            }


            pill.remove();


            toast(
                result.message ||
                'Medication removed successfully.'
            );

        }
        catch (error) {

            console.error(error);

            toast(
                error.message ||
                'Unable to remove medication.'
            );

        }

    }


    // ================================================================
    // CREATE PILL
    // ================================================================

    function addPill(
        containerId,
        text,
        relationshipId,
        type
    ) {

        const container =
            document.getElementById(containerId);


        // Prevent duplicate display

        const existing =
            Array.from(
                container.children
            ).some(
                pill =>
                    pill.dataset.value === text
            );


        if (existing) {

            return;

        }


        const pill =
            document.createElement('span');


        pill.className = 'pill';


        pill.dataset.value =
            text;


        pill.dataset.id =
            relationshipId;


        pill.textContent =
            text;


        const closeButton =
            document.createElement('button');


        closeButton.type =
            'button';


        closeButton.className =
            'pill-close';


        closeButton.textContent =
            '×';


        closeButton.addEventListener(
            'click',
            function () {

                if (type === 'condition') {

                    deletePatientCondition(
                        relationshipId,
                        pill
                    );

                }

                else if (type === 'allergy') {

                    deletePatientAllergy(
                        relationshipId,
                        pill
                    );

                }

                else if (type === 'medication') {

                    deletePatientMedication(
                        relationshipId,
                        pill
                    );

                }

            }
        );


        pill.appendChild(
            closeButton
        );


        container.appendChild(
            pill
        );

    }


    // ================================================================
    // CLEAR MEDICAL HISTORY DISPLAY
    // ================================================================

    function clearMedicalHistoryPills() {

        document.getElementById(
            'condition-pills'
        ).innerHTML = '';


        document.getElementById(
            'allergies-pills'
        ).innerHTML = '';


        document.getElementById(
            'medication-pills'
        ).innerHTML = '';

    }


    // ================================================================
    // REFRESH
    // ================================================================

    async function refreshPatientData() {

        const selectedSaId = selectedPatient
            ? selectedPatient.saIdNumber
            : null;

        await loadPatients();

        await loadConditionCategories();

        await loadAllergyCategories();

        await loadMedicationCategories();


        if (selectedSaId) {

            document.getElementById(
                'doc-pat-id'
            ).value = selectedSaId;


            await loadSelectedPatientSummary();

        }


        toast(
            'Patient data refreshed.'
        );

    }


    // ================================================================
    // DATE FORMATTER
    // ================================================================

    function formatDate(value) {

        if (!value) {

            return 'N/A';

        }


        const date =
            new Date(value);


        if (isNaN(date.getTime())) {

            return value;

        }


        return date.toLocaleDateString();

    }


    // ================================================================
    // HTML ENCODING
    // ================================================================

    function escapeHtml(value) {

        if (value === null ||
            value === undefined) {

            return '';

        }


        return String(value)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#039;');

    }


    // ================================================================
    // EVENT LISTENERS
    // ================================================================

    function openCreateRequest() {

        const patientSaId =
            sanitizeSaIdInput(
                document.getElementById('doc-pat-id').value
            );

        const createRequestUrl =
            assetScript.dataset.createrequestUrl;

        window.location.href = patientSaId
            ? `${createRequestUrl}?saIdNumber=${encodeURIComponent(patientSaId)}`
            : createRequestUrl;

    }

    const patientIdField =
        document.getElementById('doc-pat-id');


    patientIdField.addEventListener('input', function () {
        const sanitizedValue = sanitizeSaIdInput(this.value);
        if (this.value !== sanitizedValue) {
            this.value = sanitizedValue;
        }
    });


    patientIdField.addEventListener(
        'change',
        loadSelectedPatientSummary
    );


    patientIdField.addEventListener(
        'keydown',
        event => {
            if (event.key === 'Enter') {
                event.preventDefault();
                loadSelectedPatientSummary();
            }
        }
    );


    document
        .getElementById(
            'doc-pat-condition-category'
        )
        .addEventListener(
            'change',
            populateConditionNames
        );


    document
        .getElementById(
            'doc-pat-allergy-category'
        )
        .addEventListener(
            'change',
            populateAllergyNames
        );


    document
        .getElementById(
            'doc-pat-medication-category'
        )
        .addEventListener(
            'change',
            populateMedicationNames
        );


    // ================================================================
    // PAGE INITIALIZATION
    // ================================================================

    document.addEventListener(
        'DOMContentLoaded',
        async function () {

            await Promise.all([
                loadPatients(),
                loadConditionCategories(),
                loadAllergyCategories(),
                loadMedicationCategories()
            ]);

        }
    );
