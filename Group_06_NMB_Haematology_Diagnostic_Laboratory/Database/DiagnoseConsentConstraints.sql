-- Read-only: run in GRP-04-06. Does not grant or revoke access.
SET NOCOUNT ON;

-- A unique key on PatientID + DoctorID prevents separate per-test grants.
SELECT i.name AS IndexName, i.is_unique AS IsUnique,
       i.is_primary_key AS IsPrimaryKey, i.filter_definition AS FilterDefinition,
       c.name AS KeyColumn, ic.key_ordinal AS KeyPosition
FROM sys.indexes i
JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
WHERE i.object_id = OBJECT_ID(N'dbo.PatientConsent') AND ic.key_ordinal > 0
ORDER BY i.name, ic.key_ordinal;

SELECT c.name AS ColumnName, c.is_identity AS IsIdentity,
       c.is_nullable AS IsNullable, dc.definition AS DefaultDefinition
FROM sys.columns c
LEFT JOIN sys.default_constraints dc ON dc.object_id = c.default_object_id
WHERE c.object_id = OBJECT_ID(N'dbo.PatientConsent');

SELECT fk.name AS ForeignKeyName, pc.name AS ConsentColumn,
       OBJECT_SCHEMA_NAME(fk.referenced_object_id) AS ReferencedSchema,
       OBJECT_NAME(fk.referenced_object_id) AS ReferencedTable,
       rc.name AS ReferencedColumn
FROM sys.foreign_keys fk
JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
JOIN sys.columns pc ON pc.object_id = fkc.parent_object_id AND pc.column_id = fkc.parent_column_id
JOIN sys.columns rc ON rc.object_id = fkc.referenced_object_id AND rc.column_id = fkc.referenced_column_id
WHERE fk.parent_object_id = OBJECT_ID(N'dbo.PatientConsent');

SELECT name AS CheckConstraint, definition
FROM sys.check_constraints WHERE parent_object_id = OBJECT_ID(N'dbo.PatientConsent');

SELECT name AS TriggerName, is_disabled AS IsDisabled,
       OBJECT_DEFINITION(object_id) AS TriggerDefinition
FROM sys.triggers WHERE parent_id = OBJECT_ID(N'dbo.PatientConsent');
