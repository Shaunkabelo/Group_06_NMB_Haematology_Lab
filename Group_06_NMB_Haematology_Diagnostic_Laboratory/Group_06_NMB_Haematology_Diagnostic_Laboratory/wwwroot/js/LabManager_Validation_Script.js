function validateSouthAfricanId(idInputId, dobInputId) {
    const id = document.getElementById(idInputId).value;

    const dob = document.getElementById(dobInputId).value;

    if (id.length !== 13 || dob === "")
        return true;

    const parts = dob.split("-");

    const yy = parts[0].substring(2);

    const mm = parts[1];

    const dd = parts[2];

    const expected = yy + mm + dd;

    if (id.substring(0, 6) !== expected) {
        alert("The SA ID Number does not match the Date of Birth.");

        return false;
    }

    return true;
}