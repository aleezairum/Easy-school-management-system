/* ============================================================================
   Employees.CampusID — staff are recruited and posted per campus
   ============================================================================

   The school runs a Boys campus and a Girls campus, and a member of staff
   belongs to one of them. Until now Employees carried no campus at all, which
   is why the Staff Statement could only put the campus name in its heading and
   still list everybody underneath it — the sheet said "Girls High School" over
   a list that included the Boys campus.

   NULLABLE, deliberately. The table already holds staff entered before this
   column existed, and there is no honest way to guess which campus each of
   them belongs to. A NOT NULL column would have to invent a default, and every
   one of those rows would then be a confident lie. NULL reads as "not recorded
   yet", the office fills it in from the Employee form, and the campus filter
   simply does not match a row until someone has said which campus it is.

   Idempotent: safe to run more than once.

   Run with:
     cd School.API\Scripts
     sqlcmd -S . -U sa -P <pwd> -d EasySchoolDB -i AddCampusToEmployees.sql
   ============================================================================ */

-- 1. Column -------------------------------------------------------------------
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('Employees') AND name = 'CampusID')
BEGIN
    ALTER TABLE Employees ADD CampusID INT NULL;
    PRINT 'Employees.CampusID added.';
END
ELSE
    PRINT 'Employees.CampusID already present.';
GO

-- 2. Foreign key --------------------------------------------------------------
IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Employees_Campus')
   AND EXISTS (SELECT 1 FROM sys.tables WHERE name = 'SMSCampus')
BEGIN
    ALTER TABLE Employees
        ADD CONSTRAINT FK_Employees_Campus
        FOREIGN KEY (CampusID) REFERENCES SMSCampus(VID);
    PRINT 'FK_Employees_Campus added.';
END
ELSE
    PRINT 'FK_Employees_Campus already present (or SMSCampus is missing).';
GO

-- 3. Index --------------------------------------------------------------------
-- Every campus-wise screen and report filters on this, and it is a two-value
-- column on a small table, so a filtered scan is cheap either way — the index
-- is here so the Staff Statement and the custom report do not scan Employees
-- once per campus for no reason.
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_Employees_CampusID' AND object_id = OBJECT_ID('Employees'))
BEGIN
    CREATE INDEX IX_Employees_CampusID ON Employees(CampusID);
    PRINT 'IX_Employees_CampusID created.';
END
ELSE
    PRINT 'IX_Employees_CampusID already present.';
GO

-- 4. Report -------------------------------------------------------------------
SELECT
    CAST(COUNT(*) AS VARCHAR) + ' employees, ' +
    CAST(SUM(CASE WHEN CampusID IS NULL THEN 1 ELSE 0 END) AS VARCHAR) +
    ' with no campus recorded yet' AS Summary
FROM Employees;
GO
