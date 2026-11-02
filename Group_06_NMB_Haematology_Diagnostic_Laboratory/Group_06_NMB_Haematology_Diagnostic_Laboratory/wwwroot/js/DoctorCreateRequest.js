const assetScript = document.currentScript ||
    document.querySelector('script[src*="DoctorCreateRequest.js"]');
// =============================================================
    // BACKEND ROUTES
    // =============================================================

    const routes = {

        getPatients:
            assetScript?.dataset.getpatientsUrl,

        getPatient:
            assetScript?.dataset.getpatientUrl,

        getTestCategories:
            assetScript?.dataset.gettestcategoriesUrl,

        getTestsByCategory:
            assetScript?.dataset.gettestsbycategoryUrl,

        createTestRequest:
            assetScript?.dataset.createtestrequestUrl,

        requests:
            assetScript?.dataset.requestsUrl
    };

    function routeUrl(route, query = {}) {
        if (!route) {
            throw new Error("A page endpoint is missing. Refresh the page and try again.");
        }
        const url = new URL(route, document.baseURI);
        Object.entries(query).forEach(([key, value]) => {
            if (value !== undefined && value !== null) url.searchParams.set(key, String(value));
        });
        return url.toString();
    }


    // =============================================================
    // DATA
    // =============================================================

    let patientList = [];

    let selectedPatient = null;
    let patientLookupVersion = 0;
    let submittingRequest = false;

    const selectedTests = [];

    const sampleBarcodes = {};

    const sampleColorClasses = [
        "sample-color-lavender",
        "sample-color-blue",
        "sample-color-red",
        "sample-color-green",
        "sample-color-gray",
        "sample-color-gold"
    ];

    const sampleColorValues = {
        "sample-color-lavender": "#8e6bbf",
        "sample-color-blue": "#2878c8",
        "sample-color-red": "#d9485f",
        "sample-color-green": "#258f6b",
        "sample-color-gray": "#68727d",
        "sample-color-gold": "#b8860b"
    };

    function getSampleColorClass(sampleTypeName) {

        const sampleType =
            (sampleTypeName || "sample").toLowerCase();

        if (sampleType.includes("edta") || sampleType.includes("lavender") || sampleType.includes("purple")) {
            return "sample-color-lavender";
        }

        if (sampleType.includes("citrate") || sampleType.includes("blue")) {
            return "sample-color-blue";
        }

        if (sampleType.includes("serum") || sampleType.includes("red")) {
            return "sample-color-red";
        }

        if (sampleType.includes("heparin") || sampleType.includes("green")) {
            return "sample-color-green";
        }

        if (sampleType.includes("fluoride") || sampleType.includes("gray") || sampleType.includes("grey")) {
            return "sample-color-gray";
        }

        if (sampleType.includes("gold") || sampleType.includes("gel")) {
            return "sample-color-gold";
        }

        const colorIndex =
            sampleType
                .split("")
                .reduce((total, character) => total + character.charCodeAt(0), 0)
            % sampleColorClasses.length;

        return sampleColorClasses[colorIndex];
    }


    // =============================================================
    // PAGE LOAD
    // =============================================================

    async function initializeCreateRequestPage() {
            const selectedTestsList = document.getElementById("selected-tests-list");
            if (!selectedTestsList) return;

            document.getElementById("patient-select").addEventListener("input", () => {
                patientLookupVersion++;
                if (selectedPatient) resetSelectedTests();
                selectedPatient = null;
                clearPatientInformation();
                document.getElementById("patient-info-panel").style.display = "none";
            });
            loadTestCategories();


            document
                .getElementById("patient-select")
                .addEventListener(
                    "change",
                    loadPatientDetails
                );


            document
                .getElementById("patient-select")
                .addEventListener(
                    "keydown",
                    function (event) {
                        if (event.key === "Enter") {
                            event.preventDefault();
                            loadPatientDetails();
                        }
                    }
                );


            const patientSaIdFromUrl =
                new URLSearchParams(window.location.search)
                    .get("saIdNumber");


            if (patientSaIdFromUrl) {

                const patientSelect =
                    document.getElementById("patient-select");

                patientSelect.value =
                    sanitizeSaIdInput(patientSaIdFromUrl);

                await loadPatientDetails();

            }


            document
                .getElementById("test-category-select")
                .addEventListener(
                    "change",
                    loadTestsForCategory
                );


            document
                .getElementById("add-test-button")
                .addEventListener(
                    "click",
                    addSelectedTest
                );


            document
                .getElementById("submit-request-button")
                .addEventListener(
                    "click",
                    submitRequest
                );


            document
                .getElementById("back-button")
                .addEventListener(
                    "click",
                    function () {

                        window.location.assign(routeUrl(routes.requests));

                    }
                );

    }

    // The script normally loads at the end of the page. Checking readyState
    // also covers published layouts or optimizers that load it after DOMContentLoaded.
    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", initializeCreateRequestPage, { once: true });
    } else {
        initializeCreateRequestPage();
    }


    // =============================================================
    // LOAD PATIENTS
    // =============================================================

    async function loadPatients() {

        const select =
            document.getElementById(
                "patient-select"
            );

        if (!select) {
            return;
        }

        select.value = '';


        try {

            const response =
                await fetch(routeUrl(routes.getPatients), { credentials: "same-origin" });


            console.log(
                "GetPatients status:",
                response.status
            );


            if (!response.ok) {

                const errorText =
                    await response.text();


                console.error(
                    "GetPatients error:",
                    errorText
                );


                throw new Error(
                    "Unable to load patients."
                );

            }


            const data =
                await response.json();


            console.log(
                "Patients returned:",
                data
            );


            patientList =
                Array.isArray(data)
                    ? data
                    : [];


        }
        catch (error) {

            console.error(
                "loadPatients:",
                error
            );


            alert(
                error.message ||
                "Unable to load patient SA ID numbers."
            );

        }

    }


    function sanitizeSaIdInput(value) {
        return String(value || "")
            .replace(/\D/g, "")
            .slice(0, 13);
    }


    function validateSaIdNumber(value) {
        return /^\d{13}$/.test(value);
    }


    // =============================================================
    // LOAD PATIENT DETAILS BY SA ID
    // =============================================================

    async function loadPatientDetails() {
        const lookupVersion = ++patientLookupVersion;

        const select =
            document.getElementById(
                "patient-select"
            );

        if (!select) {
            return;
        }

        const saIdNumber =
            sanitizeSaIdInput(select.value);

        if (selectedPatient && selectedPatient.saIdNumber && selectedPatient.saIdNumber !== saIdNumber && saIdNumber) {
            resetSelectedTests();
        }

        select.value =
            saIdNumber;


        const panel =
            document.getElementById(
                "patient-info-panel"
            );


        selectedPatient =
            null;

        clearPatientInformation();
        panel.style.display = "none";

        if (!saIdNumber) {

            clearPatientInformation();

            panel.style.display =
                "none";

            return;

        }


        if (!validateSaIdNumber(saIdNumber)) {

            clearPatientInformation();

            panel.style.display =
                "none";

            alert("SA ID must contain exactly 13 numbers.");
            select.focus();
            return;

        }


        try {

            const url = routeUrl(routes.getPatient, { saIdNumber });


            console.log(
                "Loading patient:",
                url
            );


            const response =
                await fetch(url, { credentials: "same-origin" });
            if (lookupVersion !== patientLookupVersion) return;


            console.log(
                "GetPatient status:",
                response.status
            );


            if (!response.ok) {

                if (response.status === 404) {
                    showPatientNotFoundModal(saIdNumber);
                    return;
                }

                let message =
                    "Unable to load patient information.";


                try {

                    const errorData =
                        await response.json();


                    message =
                        errorData.message
                        ?? message;

                }
                catch {
                }


                throw new Error(
                    message
                );

            }


            const data =
                await response.json();
            if (lookupVersion !== patientLookupVersion) return;


            console.log(
                "Patient returned:",
                data
            );


            // =====================================================
            // INTERNAL PATIENT ID
            // =====================================================

            const patientId =
                Number(
                    data.patientId
                    ?? data.patientID
                    ?? data.PatientId
                    ?? data.PatientID
                    ?? 0
                );


            // Store full patient
            selectedPatient = {

                ...data,

                patientId:
                    patientId,

                saIdNumber:
                    data.saIdNumber
                    ?? data.SaIdNumber
                    ?? saIdNumber

            };


            console.log(
                "Selected Patient:",
                selectedPatient
            );


            // =====================================================
            // NAME
            // =====================================================

            const name =
                data.name
                ?? data.Name
                ?? data.firstName
                ?? "";


            const surname =
                data.surname
                ?? data.Surname
                ?? data.lastName
                ?? "";


            document
                .getElementById(
                    "patient-name"
                )
                .textContent =
                    `${name} ${surname}`.trim()
                    || "N/A";


            // =====================================================
            // SA ID
            // =====================================================

            document
                .getElementById(
                    "patient-sa-id"
                )
                .textContent =
                    selectedPatient.saIdNumber
                    || "N/A";


            // =====================================================
            // DATE OF BIRTH
            // =====================================================

            const dob =
                data.dateOfBirth
                ?? data.DateOfBirth;


            document
                .getElementById(
                    "patient-dob"
                )
                .textContent =
                    formatDate(dob);


            // =====================================================
            // CELLPHONE
            // =====================================================

            const cellphone =
                data.cellphone
                ?? data.Cellphone
                ?? data.contactNumber
                ?? data.ContactNumber;


            document
                .getElementById(
                    "patient-cellphone"
                )
                .textContent =
                    cellphone || "N/A";


            // =====================================================
            // EMAIL
            // =====================================================

            const email =
                data.email
                ?? data.Email;


            document
                .getElementById(
                    "patient-email"
                )
                .textContent =
                    email || "N/A";


            // =====================================================
            // ADDRESS
            // =====================================================

            const address =
                data.address
                ?? data.Address
                ?? data.homeAddress
                ?? data.HomeAddress;


            document
                .getElementById(
                    "patient-address"
                )
                .textContent =
                    address || "N/A";


            // =====================================================
            // MEDICAL CONDITIONS
            // =====================================================

            const conditions =
                data.knownMedicalConditions
                ?? data.KnownMedicalConditions
                ?? data.medicalConditions
                ?? data.MedicalConditions
                ?? data.conditions;


            document
                .getElementById(
                    "patient-conditions"
                )
                .textContent =
                    formatMedicalValue(
                        conditions,
                        [
                            "conditionName",
                            "medicalConditionName",
                            "name"
                        ]
                    );


            // =====================================================
            // ALLERGIES
            // =====================================================

            const allergies =
                data.allergies
                ?? data.Allergies;


            document
                .getElementById(
                    "patient-allergies"
                )
                .textContent =
                    formatMedicalValue(
                        allergies,
                        [
                            "allergyName",
                            "name"
                        ]
                    );


            // =====================================================
            // MEDICATION
            // =====================================================

            const medication =
                data.medication
                ?? data.Medication
                ?? data.medications
                ?? data.Medications
                ?? data.currentMedications;


            document
                .getElementById(
                    "patient-medication"
                )
                .textContent =
                    formatMedicalValue(
                        medication,
                        [
                            "medicationName",
                            "name"
                        ]
                    );


                    renderPatientHistory(selectedPatient);


            panel.style.display =
                "block";


            if (
                !selectedPatient.patientId ||
                selectedPatient.patientId <= 0
            ) {

                console.error(
                    "PatientID was not returned by GetPatient.",
                    data
                );

            }

        }
        catch (error) {
            if (lookupVersion !== patientLookupVersion) return;

            console.error(
                "loadPatientDetails:",
                error
            );


            selectedPatient =
                null;


            clearPatientInformation();


            panel.style.display =
                "none";


            alert(error.message || "Unable to load patient information. Please try again.");

        }

    }


    function showPatientNotFoundModal(saIdNumber) {

        document.getElementById(
            "patient-not-found-message"
        ).textContent =
            `Patient with SA ID ${saIdNumber} does not exist in the system.`;

        document.getElementById(
            "patient-not-found-modal"
        ).style.display = "flex";

    }


    function closePatientNotFoundModal() {

        document.getElementById(
            "patient-not-found-modal"
        ).style.display = "none";

    }


    function resetSelectedTests() {

        selectedTests.length = 0;

        Object.keys(sampleBarcodes).forEach(
            key => delete sampleBarcodes[key]
        );

        const list =
            document.getElementById(
                "selected-tests-list"
            );

        if (list) {
            list.innerHTML =
                '<div class="summary-empty">' +
                'No tests selected. ' +
                'Choose a test type to build the sample list.' +
                '</div>';
        }

        const testSelect =
            document.getElementById(
                "test-select"
            );

        if (testSelect) {
            testSelect.innerHTML =
                '<option value="">' +
                '-- Select a category first --' +
                '</option>';
            testSelect.disabled = true;
        }

    }


    function renderPatientHistory(patient) {
        if (typeof window.loadDoctorPatientHistory === "function") {
            return window.loadDoctorPatientHistory(patient, "patient-");
        }

        const history = document.getElementById("patient-request-history");
        const results = document.getElementById("patient-consent-results");
        if (history) history.textContent = "Patient history is temporarily unavailable.";
        if (results) results.textContent = "Previous results are temporarily unavailable.";
    }

    function escapeHtml(value) {

        if (value === null || value === undefined) {

            return '';

        }

        return String(value)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#039;');

    }


    // =============================================================
    // FORMAT MEDICAL VALUES
    // =============================================================

    function formatMedicalValue(
        value,
        nameProperties = []
    ) {

        if (
            value === null ||
            value === undefined ||
            value === ""
        ) {

            return "None recorded";

        }


        // STRING_AGG from SQL
        if (
            typeof value === "string"
        ) {

            const names =
                value.split(",")
                    .map(item => item.trim())
                    .filter(item => item);

            return names.length
                ? names.sort((a, b) => a.localeCompare(b, undefined, { sensitivity: "base" })).join(", ")
                : "None recorded";

        }


        // List returned from API
        if (
            Array.isArray(value)
        ) {

            if (
                value.length === 0
            ) {

                return "None recorded";

            }


            const names =
                value.map(
                    item => {

                        if (
                            typeof item ===
                            "string"
                        ) {

                            return item;

                        }


                        for (
                            const property
                            of nameProperties
                        ) {

                            if (
                                item[property]
                            ) {

                                return item[property];

                            }


                            const pascal =
                                property
                                    .charAt(0)
                                    .toUpperCase()
                                +
                                property.slice(1);


                            if (
                                item[pascal]
                            ) {

                                return item[
                                    pascal
                                ];

                            }

                        }


                        return "";

                    }
                )
                .filter(
                    item =>
                        item &&
                        String(item).trim()
                );


            if (
                names.length === 0
            ) {

                return "None recorded";

            }


            return names
                .sort((a, b) => String(a).localeCompare(String(b), undefined, { sensitivity: "base" }))
                .join(", ");

        }


        return String(value);

    }


    // =============================================================
    // FORMAT DATE
    // =============================================================

    function formatDate(value) {

        if (!value) {

            return "N/A";

        }


        const date =
            new Date(value);


        if (
            Number.isNaN(
                date.getTime()
            )
        ) {

            return value;

        }


        return date.toLocaleDateString(
            "en-ZA"
        );

    }


    // =============================================================
    // CLEAR PATIENT INFORMATION
    // =============================================================

    function clearPatientInformation() {

        document
            .getElementById(
                "patient-sa-id"
            )
            .textContent =
                "N/A";


        document
            .getElementById(
                "patient-name"
            )
            .textContent =
                "N/A";


        document
            .getElementById(
                "patient-dob"
            )
            .textContent =
                "N/A";


        document
            .getElementById(
                "patient-cellphone"
            )
            .textContent =
                "N/A";


        document
            .getElementById(
                "patient-email"
            )
            .textContent =
                "N/A";


        document
            .getElementById(
                "patient-address"
            )
            .textContent =
                "N/A";


        document
            .getElementById(
                "patient-conditions"
            )
            .textContent =
                "None recorded";


        document
            .getElementById(
                "patient-allergies"
            )
            .textContent =
                "None recorded";


        document
            .getElementById(
                "patient-medication"
            )
            .textContent =
                "None recorded";

    }


    // =============================================================
    // LOAD TEST CATEGORIES
    // =============================================================

    async function loadTestCategories() {

        const select =
            document.getElementById(
                "test-category-select"
            );


        select.innerHTML =
            '<option value="">' +
            '-- Select Category --' +
            '</option>';


        try {

            const response =
                await fetch(routeUrl(routes.getTestCategories), { credentials: "same-origin" });


            console.log(
                "GetTestCategories status:",
                response.status
            );


            if (!response.ok) {

                const errorText =
                    await response.text();


                console.error(
                    "GetTestCategories error:",
                    errorText
                );


                throw new Error(
                    "Unable to load test categories."
                );

            }


            const data =
                await response.json();


            console.log(
                "Categories returned:",
                data
            );


            if (!Array.isArray(data)) {

                throw new Error(
                    "Invalid category data returned."
                );

            }


            data.forEach(
                category => {

                    const categoryId =
                        Number(
                            category.testCategoryId
                            ?? category.testCategoryID
                            ?? category.TestCategoryId
                            ?? category.TestCategoryID
                            ?? 0
                        );


                    const categoryName =
                        category.categoryName
                        ?? category.CategoryName
                        ?? "";


                    if (
                        categoryId <= 0 ||
                        !categoryName
                    ) {

                        console.warn(
                            "Invalid category:",
                            category
                        );

                        return;

                    }


                    const option =
                        document.createElement(
                            "option"
                        );


                    option.value =
                        categoryId;


                    option.textContent =
                        categoryName;


                    select.appendChild(
                        option
                    );

                }
            );

        }
        catch (error) {

            console.error(
                "loadTestCategories:",
                error
            );


            alert(
                error.message ||
                "Unable to load test categories."
            );

        }

    }


    // =============================================================
    // LOAD TESTS BY CATEGORY
    // =============================================================

    async function loadTestsForCategory() {

        const categoryId =
            document
                .getElementById(
                    "test-category-select"
                )
                .value;


        const testSelect =
            document.getElementById(
                "test-select"
            );


        if (!categoryId) {

            testSelect.innerHTML =
                '<option value="">' +
                '-- Select a category first --' +
                '</option>';


            testSelect.disabled =
                true;


            return;

        }


        testSelect.disabled =
            true;


        testSelect.innerHTML =
            '<option value="">' +
            '-- Loading Tests --' +
            '</option>';


        try {

            const url = routeUrl(routes.getTestsByCategory, { testCategoryId: categoryId });


            console.log(
                "Loading tests:",
                url
            );


            const response =
                await fetch(url, { credentials: "same-origin" });


            console.log(
                "GetTestsByCategory status:",
                response.status
            );


            if (!response.ok) {

                const errorText =
                    await response.text();


                console.error(
                    "GetTestsByCategory error:",
                    errorText
                );


                throw new Error(
                    "Unable to load tests."
                );

            }


            const data =
                await response.json();


            console.log(
                "Tests returned:",
                data
            );


            testSelect.innerHTML =
                '<option value="">' +
                '-- Select Test --' +
                '</option>';


            if (
                !Array.isArray(data) ||
                data.length === 0
            ) {

                testSelect.innerHTML =
                    '<option value="">' +
                    '-- No tests found --' +
                    '</option>';


                testSelect.disabled =
                    true;


                return;

            }


            data.forEach(
                test => {

                    // =================================================
                    // TEST TYPE ID
                    // =================================================

                    const testTypeId =
                        Number(
                            test.testTypeId
                            ?? test.testTypeID
                            ?? test.TestTypeId
                            ?? test.TestTypeID
                            ?? 0
                        );


                    // =================================================
                    // TEST NAME
                    // =================================================

                    const testName =
                        test.testName
                        ?? test.TestName
                        ?? "";


                    // =================================================
                    // SAMPLE TYPE ID
                    // =================================================

                    const sampleTypeId =
                        Number(
                            test.sampleTypeId
                            ?? test.sampleTypeID
                            ?? test.SampleTypeId
                            ?? test.SampleTypeID
                            ?? 0
                        );


                    // =================================================
                    // SAMPLE TYPE NAME
                    // =================================================

                    const sampleTypeName =
                        test.sampleTypeName
                        ?? test.SampleTypeName
                        ?? "Sample";


                    console.log(
                        "Mapped test:",
                        {
                            testTypeId,
                            testName,
                            sampleTypeId,
                            sampleTypeName
                        }
                    );


                    if (
                        testTypeId <= 0 ||
                        !testName
                    ) {

                        console.warn(
                            "Invalid test returned:",
                            test
                        );

                        return;

                    }


                    const option =
                        document.createElement(
                            "option"
                        );


                    option.value =
                        testTypeId;


                    option.textContent =
                        testName;


                    option.dataset.sampleTypeId =
                        sampleTypeId;


                    option.dataset.sampleTypeName =
                        sampleTypeName;


                    // Disable already-added tests
                    const alreadySelected =
                        selectedTests.some(
                            item =>
                                item.testTypeId ===
                                testTypeId
                        );


                    if (alreadySelected) {

                        option.disabled =
                            true;

                    }


                    testSelect.appendChild(
                        option
                    );

                }
            );


            testSelect.disabled =
                false;

        }
        catch (error) {

            console.error(
                "loadTestsForCategory:",
                error
            );


            testSelect.innerHTML =
                '<option value="">' +
                '-- Unable to load tests --' +
                '</option>';


            testSelect.disabled =
                true;


            alert(
                error.message ||
                "Unable to load tests."
            );

        }

    }


    // =============================================================
    // ADD SELECTED TEST
    // =============================================================

    function addSelectedTest() {

        const testSelect =
            document.getElementById(
                "test-select"
            );


        const selectedOption =
            testSelect.options[
                testSelect.selectedIndex
            ];


        if (
            !selectedOption ||
            !selectedOption.value
        ) {

            alert(
                "Please select a test."
            );

            return;

        }


        const testTypeId =
            Number(
                selectedOption.value
            );


        const testName =
            selectedOption
                .textContent
                .trim();


        const sampleTypeId =
            Number(
                selectedOption
                    .dataset
                    .sampleTypeId
            );


        const sampleTypeName =
            selectedOption
                .dataset
                .sampleTypeName
            || "Sample";


        console.log(
            "Adding selected test:",
            {
                testTypeId,
                testName,
                sampleTypeId,
                sampleTypeName
            }
        );


        // =========================================================
        // VALID TEST
        // =========================================================

        if (
            !testTypeId ||
            testTypeId <= 0
        ) {

            alert(
                "Invalid test selected."
            );

            return;

        }


        // =========================================================
        // VALID SAMPLE TYPE
        // =========================================================

        if (
            !sampleTypeId ||
            sampleTypeId <= 0
        ) {

            console.error(
                "SampleTypeID missing from selected test:",
                selectedOption.dataset
            );


            alert(
                "This test does not have a valid sample type."
            );

            return;

        }


        // =========================================================
        // PREVENT DUPLICATES
        // =========================================================

        const alreadySelected =
            selectedTests.some(
                item =>
                    item.testTypeId ===
                    testTypeId
            );


        if (alreadySelected) {

            alert(
                "This test has already been selected."
            );

            return;

        }


        // =========================================================
        // ADD TEST
        // =========================================================

        selectedTests.push(
            {

                testTypeId:
                    testTypeId,

                testName:
                    testName,

                sampleTypeId:
                    sampleTypeId,

                sampleTypeName:
                    sampleTypeName

            }
        );


        sampleBarcodes[
            testTypeId
        ] = "";


        updateRequestSummary();


        // Reload dropdown so selected test becomes disabled
        loadTestsForCategory();

    }


    // =============================================================
    // UPDATE REQUEST SUMMARY
    // =============================================================

    function updateRequestSummary() {

        const list =
            document.getElementById(
                "selected-tests-list"
            );

        if (!list) return;


        list.innerHTML =
            "";


        if (
            selectedTests.length === 0
        ) {

            list.innerHTML =
                '<div class="summary-empty">' +
                'No tests selected. ' +
                'Choose a test type to build the sample list.' +
                '</div>';


            return;

        }


        selectedTests.forEach(
            test => {

                const row =
                    document.createElement(
                        "div"
                    );


                row.className =
                    "sample-summary-row";


                // =================================================
                // TEST DETAILS
                // =================================================

                const meta =
                    document.createElement(
                        "div"
                    );


                meta.className =
                    "sample-summary-meta";


                const nameContainer =
                    document.createElement(
                        "div"
                    );


                const name =
                    document.createElement(
                        "div"
                    );


                name.className =
                    "sample-summary-label";


                name.textContent =
                    test.testName;


                const sample =
                    document.createElement(
                        "small"
                    );


                const sampleColorClass = getSampleColorClass(test.sampleTypeName);
                sample.className = `vial-chip ${sampleColorClass}`;

                // Apply the color inline so shared/published CSS cannot wash it out.
                sample.style.setProperty(
                    "background-color",
                    sampleColorValues[sampleColorClass],
                    "important"
                );


                sample.textContent =
                    `${test.sampleTypeName} sample`;


                nameContainer.appendChild(
                    name
                );


                nameContainer.appendChild(
                    sample
                );


                meta.appendChild(
                    nameContainer
                );


                // =================================================
                // BARCODE
                // =================================================

                const barcodeContainer =
                    document.createElement(
                        "div"
                    );


                barcodeContainer.className =
                    "sample-summary-barcode";


                const barcodeLabel =
                    document.createElement(
                        "label"
                    );


                barcodeLabel.className =
                    "section-label";


                barcodeLabel.textContent =
                    "Barcode";


                const input =
                    document.createElement(
                        "input"
                    );


                input.type =
                    "text";
                input.maxLength = 50;


                input.className =
                    "form-control";


                input.placeholder =
                    "Enter sample barcode";


                input.value =
                    sampleBarcodes[
                        test.testTypeId
                    ] || "";


                input.addEventListener(
                    "input",
                    function () {

                        updateSampleBarcode(
                            test.testTypeId,
                            this.value
                        );

                    }
                );


                barcodeContainer.appendChild(
                    barcodeLabel
                );


                barcodeContainer.appendChild(
                    input
                );


                // =================================================
                // REMOVE BUTTON
                // =================================================

                const removeButton =
                    document.createElement(
                        "button"
                    );


                removeButton.type =
                    "button";


                removeButton.className =
                    "btn btn-secondary";


                removeButton.textContent =
                    "Remove";


                removeButton.addEventListener(
                    "click",
                    function () {

                        removeSelectedTest(
                            test.testTypeId
                        );

                    }
                );


                // =================================================
                // BUILD ROW
                // =================================================

                row.appendChild(
                    meta
                );


                row.appendChild(
                    barcodeContainer
                );


                row.appendChild(
                    removeButton
                );


                list.appendChild(
                    row
                );

            }
        );

    }


    // =============================================================
    // UPDATE BARCODE
    // =============================================================

    function updateSampleBarcode(
        testTypeId,
        value
    ) {

        sampleBarcodes[
            testTypeId
        ] = value;

    }


    // =============================================================
    // REMOVE TEST
    // =============================================================

    function removeSelectedTest(
        testTypeId
    ) {

        const index =
            selectedTests.findIndex(
                test =>
                    test.testTypeId ===
                    testTypeId
            );


        if (
            index !== -1
        ) {

            selectedTests.splice(
                index,
                1
            );

        }


        delete sampleBarcodes[
            testTypeId
        ];


        updateRequestSummary();


        loadTestsForCategory();

    }


    // =============================================================
    // SUBMIT REQUEST
    // =============================================================

    async function submitRequest() {
        if (submittingRequest) return;

        // =========================================================
        // PATIENT VALIDATION
        // =========================================================

        if (!selectedPatient) {

            alert(
                "Please select a patient."
            );

            return;

        }


        const patientId =
            Number(
                selectedPatient.patientId
            );


        if (
            !patientId ||
            patientId <= 0
        ) {

            console.error(
                "Invalid selected patient:",
                selectedPatient
            );


            alert(
                "Selected patient does not have a valid Patient ID."
            );

            return;

        }


        // =========================================================
        // REQUEST DETAILS
        // =========================================================

        const urgency =
            document
                .getElementById(
                    "urgency-select"
                )
                .value;


        const clinicalNotesInput =
            document
                .getElementById(
                    "clinical-notes"
                );

        const clinicalNotes =
            clinicalNotesInput
                ? clinicalNotesInput.value.trim()
                : "";


        // =========================================================
        // TEST VALIDATION
        // =========================================================

        if (
            selectedTests.length === 0
        ) {

            alert(
                "Please select at least one test."
            );

            return;

        }


        // =========================================================
        // BARCODE VALIDATION
        // =========================================================

        const missingBarcode =
            selectedTests.find(
                test =>
                    !String(
                        sampleBarcodes[
                            test.testTypeId
                        ] || ""
                    ).trim()
            );


        if (missingBarcode) {

            alert(
                `Please enter the barcode for ${missingBarcode.testName}.`
            );

            return;

        }


        // =========================================================
        // CREATE REQUEST OBJECT
        // =========================================================

        const requestData = {

            patientId:
                patientId,

            urgency:
                urgency,

            clinicalNotes:
                clinicalNotes,

            tests:
                selectedTests.map(
                    test => ({

                        testTypeId:
                            test.testTypeId,

                        sampleTypeId:
                            test.sampleTypeId,

                        barcodeNumber:
                            String(
                                sampleBarcodes[
                                    test.testTypeId
                                ]
                            ).trim()

                    })
                )

        };


        console.log(
            "Request being submitted:",
            requestData
        );


        // =========================================================
        // SUBMIT TO CONTROLLER
        // =========================================================

        const submitButton = document.getElementById("submit-request-button");
        const originalLabel = submitButton.textContent;
        submittingRequest = true;
        submitButton.disabled = true;
        submitButton.textContent = "Submitting...";
        try {

            const response =
                await fetch(
                    routeUrl(routes.createTestRequest),
                    {

                        method:
                            "POST",

                        headers: {

                            "Content-Type":
                                "application/json",
                            "RequestVerificationToken":
                                document.querySelector('meta[name="doctor-csrf"]')?.content || ""

                        },

                        credentials: "same-origin",
                        body:
                            JSON.stringify(
                                requestData
                            )

                    }
                );

            if (response.redirected || response.status === 401 || response.status === 403)
                throw new Error("Your session has expired or you do not have access. Sign in again.");

            let result =
                null;


            try {

                result =
                    await response.json();

            }
            catch {
            }


            console.log(
                "Create request response:",
                result
            );


            if (!response.ok) {

                throw new Error(
                    result?.message
                    ||
                    "Unable to create test request."
                );

            }


            if (
                !result ||
                result.success !== true
            ) {

                throw new Error(
                    result?.message
                    ||
                    "Unable to create test request."
                );

            }

            const patientName = [
                selectedPatient.name ?? selectedPatient.Name,
                selectedPatient.surname ?? selectedPatient.Surname
            ]
                .filter(Boolean)
                .join(" ") || "Selected patient";
            const requestedTests = selectedTests.map(test => test.testName).join(", ");
            const successMessage = `Request #${result.testRequestId} for ${patientName}. Tests: ${requestedTests}. Urgency: ${urgency}.`;
            if (typeof window.showDoctorMessage === "function") {
                await window.showDoctorMessage({
                    heading: "Test request submitted",
                    text: successMessage
                });
            } else {
                alert(`Test request submitted. ${successMessage}`);
            }

            window.location.assign(routeUrl(routes.requests));

        }
        catch (error) {

            console.error(
                "Create test request error:",
                error
            );


            alert(
                error.message
                ||
                "An error occurred while creating the test request."
            );

        }
        finally {
            submittingRequest = false;
            submitButton.disabled = false;
            submitButton.textContent = originalLabel;
        }

    }
