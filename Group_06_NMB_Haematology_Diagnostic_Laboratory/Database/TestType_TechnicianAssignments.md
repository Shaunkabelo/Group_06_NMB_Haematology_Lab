# Test type technician assignments

Apply TestType_TechnicianAssignments.sql in the application database.
It alters the existing sp_LabTechnicianTestType_GetByTechnician and
sp_LabTechnicianTestType_Replace procedures; it does not create another assignment table.
The updated procedures are already installed in local GRP_04_06_db.
Teammates and hosted deployments must apply this script as well.

Existing calls with LabTechnicianID and TestTypeIDs remain supported.
To edit one test type, pass TestTypeID, TechnicianIDs and ExpectedTechnicianIDs
(the active assignments shown when the page loaded).
GetByTechnician also supports optional TestTypeID and TestCategoryID filters.
Without filters, it returns all assignment links with test/category information.

The count and Assign Technicians button open the same panel. No table row expands.
The panel identifies the test/category, lists assigned technicians and provides
a searchable active-technician checklist. Inactive links remain visible and are
preserved. Inactive test types are view-only.
The test table and technician-page test choices are grouped by category.

Both assignment directions share a transaction-owned application lock.
Duplicate additions are prevented; changing one test leaves other test assignments
intact. Removal cannot leave an active technician without an active test type.
The technician-page procedure also rejects an empty active test selection.
Stale test-page submissions are rejected and the refreshed page shows current values.

Verification: Razor build passed; SQL tests exercised both directions, duplicates,
empty lists, other-test preservation, last-assignment protection and stale saves.
SQL fixture changes were rolled back.
