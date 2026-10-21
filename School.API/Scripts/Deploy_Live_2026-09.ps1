<#
=============================================================================
 LIVE DATABASE UPDATE - September 2026
=============================================================================

 Applies the script files the live database is missing:

   SMSClassTiming.sql   Class-wise student timings: the SMSClassTiming table
                        and its Get / Save / Delete procedures. Needed by the
                        Class Timings screen and by the WhatsApp auto-reply,
                        which answers "School Timings" from this table. Also
                        carries the academy times used by the printed notice.

   AddUrduNameToClass.sql
                        SMSClass.VName_Urdu (nullable) plus its Get / Save
                        procedures, so the Class form can carry an Urdu name
                        for the parents' Urdu timing notice. Optional per
                        class: untranslated classes fall back to English.

   SpRPT_Ledger.sql     Student Ledger, Family Ledger and the family lookup.
                        Built earlier but never applied to live, so the Ledger
                        page and the ledger PDF the bot sends both fail there.

   AddCampusToEmployees.sql
                        Employees.CampusID (nullable) + FK + index, so staff can
                        be added campus-wise and the Staff Statement can print
                        one campus. Existing staff keep NULL until the office
                        fills each one in.

   ReplaceShiftWithDesignationTiming.sql
                        Retires HrShift and adds DesignationId to the STAFF school
                        timing, because a Principal, a Head Teacher and a Teacher
                        keep different hours. Grace, half-day, full-day and work
                        hours move onto the timing so attendance and payroll keep
                        behaving as they did. DESTRUCTIVE: drops HrShift,
                        Employees.ShiftId and HrAttendance.ShiftId; the shift rows
                        are kept in HrShift_Backup_2026_09.

   SpRPT_SMSExamResultCard.sql
                        FIX: position was ranked within the SECTION whenever a
                        section was passed, while the card prints "Position in
                        Class". Affected the exam screen, the result-card SMS
                        campaign and the WhatsApp auto-reply. Result cards
                        already sent carry the wrong position.

   SpRPT_SMSStudentFee.sql
                        Rewrites the fee report procedure: adds a Campus filter
                        to every report type, returns CampusName and StatusName,
                        and adds ReportType 3 - the unpaid fee of students who
                        have LEFT. That money is invisible on the other three
                        types, which all filter to StatusID = 1. ReportType 3
                        ignores the date range on purpose: a leaver's arrears
                        are whatever is still outstanding today.

 Everything above is additive EXCEPT ReplaceShiftWithDesignationTiming.sql. Tables and columns are
 created only if absent, and procedures are dropped and recreated, which is how
 every other script in this folder installs a procedure.

 ReplaceShiftWithDesignationTiming.sql is the exception and it DELETES:

     HrShift               the table, after copying it to HrShift_Backup_2026_09
     Employees.ShiftId     the column, and the shift each employee was assigned
     HrAttendance.ShiftId  the column, on every historical attendance row

 The shift a member of staff was on is therefore recoverable only through the
 backup table, and which employee was on which shift is NOT -- that link is in
 the dropped column. If anyone still needs it, export
 "SELECT Id, EmployeeCode, ShiftId FROM Employees" BEFORE running this.

 NOTE: this file is deliberately plain ASCII. Windows PowerShell 5.1 reads a
 .ps1 without a byte-order mark as ANSI, so a dash or a box-drawing character
 saved as UTF-8 arrives as mojibake and the script fails to parse.

 BEFORE YOU RUN
 --------------
   1. TAKE A FULL BACKUP. Non-negotiable, as with every live change.
   2. Know the live database NAME. appsettings.json says EasySchoolDB, while
      Deploy_HR_To_Live.sql records a run against EasySchoolDB_Live - so the
      two disagree and the name must be confirmed, not assumed. This script
      prints what it found and asks before changing anything.

 RUN IT
 ------
   On the SERVER, in PowerShell:

     cd "D:\01 EasyPOS\WebApp\EasySchool\01 EasySchoolAPI\Scripts"
     .\Deploy_Live_2026-09.ps1 -Database EasySchoolDB -Password '<sa password>'

   Add -DryRun to see what it would do without changing anything.
=============================================================================
#>

param(
    [string]$Server = ".",

    [Parameter(Mandatory = $true)]
    [string]$Database,

    [string]$User = "sa",

    [Parameter(Mandatory = $true)]
    [string]$Password,

    # Skip the confirmation prompt. For a scripted run only - the prompt is
    # what stops this being applied to the development database by mistake.
    [switch]$Force,

    [switch]$DryRun
)

$ErrorActionPreference = "Stop"

# sqlcmd refuses an absolute -i path that carries a drive letter, so every
# call below uses a bare file name and runs from this folder.
Set-Location $PSScriptRoot

function Invoke-Sql([string]$Query) {
    $out = sqlcmd -S $Server -d $Database -U $User -P $Password -h-1 -W -Q "SET NOCOUNT ON; $Query"
    if ($LASTEXITCODE -ne 0) { throw "sqlcmd failed: $out" }
    return $out
}

function Write-Lines($lines) {
    foreach ($line in $lines) {
        if ($line -ne "") { Write-Host ("  " + $line) }
    }
}

Write-Host ""
Write-Host "=== Target ===" -ForegroundColor Cyan

# Identify the database before touching it. A wrong name here is the single
# most expensive mistake available, and it is completely silent otherwise.
$identity = Invoke-Sql "SELECT 'Server    : ' + @@SERVERNAME; SELECT 'Database  : ' + DB_NAME(); SELECT 'Students  : ' + CAST(COUNT(*) AS VARCHAR) FROM SMSStudent; SELECT 'Employees : ' + CAST(COUNT(*) AS VARCHAR) FROM Employees; SELECT 'Vouchers  : ' + CAST(COUNT(*) AS VARCHAR) FROM FeeVoucherMain;"
Write-Lines $identity

Write-Host ""
Write-Host "=== Objects already present ===" -ForegroundColor Cyan
$before = Invoke-Sql "SELECT 'TABLE ' + name FROM sys.tables WHERE name = 'SMSClassTiming'; SELECT 'COLUMN Employees.CampusID' FROM sys.columns WHERE object_id = OBJECT_ID('Employees') AND name = 'CampusID'; SELECT 'COLUMN SMSClass.VName_Urdu' FROM sys.columns WHERE object_id = OBJECT_ID('SMSClass') AND name = 'VName_Urdu'; SELECT 'COLUMN SMSClassTiming.AcademyTimeIn' FROM sys.columns WHERE object_id = OBJECT_ID('SMSClassTiming') AND name = 'AcademyTimeIn'; SELECT 'COLUMN HrSchoolTiming.DesignationId' FROM sys.columns WHERE object_id = OBJECT_ID('HrSchoolTiming') AND name = 'DesignationId'; SELECT 'GONE  HrShift table removed' WHERE NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'HrShift'); SELECT 'PROC  ' + name FROM sys.procedures WHERE name IN ('SpGet_SMSClassTiming','SpSave_SMSClassTiming','SpDelete_SMSClassTiming','SpRPT_StudentLedger','SpRPT_FamilyLedger','SpGet_FamilyLookup','SpRPT_SMSExamResultCard','SpRPT_SMSStudentFee') ORDER BY name;"

$existing = @($before | Where-Object { $_ -match '^(TABLE|PROC|COLUMN|GONE)' })
if ($existing.Count -eq 0) {
    Write-Host "  (none - this is a first install)"
} else {
    Write-Lines $existing
}

if ($DryRun) {
    Write-Host ""
    Write-Host "DryRun: nothing was changed." -ForegroundColor Yellow
    return
}

if (-not $Force) {
    Write-Host ""
    Write-Host "The row counts above must look like LIVE, not development." -ForegroundColor Yellow
    $answer = Read-Host "Type the database name again to apply the update"
    if ($answer -ne $Database) {
        Write-Host "Cancelled - nothing was changed." -ForegroundColor Yellow
        return
    }
}

$files = @(
    "SMSClassTiming.sql",
    "SpRPT_Ledger.sql",
    "AddCampusToEmployees.sql",
    "SpRPT_SMSExamResultCard.sql",
    "SpRPT_SMSStudentFee.sql",
    # Must run BEFORE SMSClassTiming.sql is of any use for the notice: that
    # procedure now selects SMSClass.VName_Urdu, which this script adds. The
    # order here re-runs SMSClassTiming.sql afterwards for exactly that reason.
    "AddUrduNameToClass.sql",
    "SMSClassTiming.sql",
    # Retires HrShift and makes the STAFF timing designation-aware. Destructive:
    # it drops HrShift, Employees.ShiftId and HrAttendance.ShiftId, keeping the
    # shift rows in HrShift_Backup_2026_09.
    "ReplaceShiftWithDesignationTiming.sql"
)

foreach ($file in $files) {
    Write-Host ""
    Write-Host "=== Applying $file ===" -ForegroundColor Cyan

    if (-not (Test-Path $file)) { throw "$file is not in $PSScriptRoot" }

    # -b makes sqlcmd return a non-zero exit code on a SQL error; without it a
    # failed batch prints its message and the run reports success anyway.
    sqlcmd -S $Server -d $Database -U $User -P $Password -b -i $file
    if ($LASTEXITCODE -ne 0) { throw "$file failed - stopping before the next file." }

    Write-Host "  applied." -ForegroundColor Green
}

Write-Host ""
Write-Host "=== Verify ===" -ForegroundColor Cyan
$after = Invoke-Sql "SELECT 'TABLE ' + name FROM sys.tables WHERE name = 'SMSClassTiming'; SELECT 'COLUMN Employees.CampusID' FROM sys.columns WHERE object_id = OBJECT_ID('Employees') AND name = 'CampusID'; SELECT 'COLUMN SMSClass.VName_Urdu' FROM sys.columns WHERE object_id = OBJECT_ID('SMSClass') AND name = 'VName_Urdu'; SELECT 'COLUMN SMSClassTiming.AcademyTimeIn' FROM sys.columns WHERE object_id = OBJECT_ID('SMSClassTiming') AND name = 'AcademyTimeIn'; SELECT 'COLUMN HrSchoolTiming.DesignationId' FROM sys.columns WHERE object_id = OBJECT_ID('HrSchoolTiming') AND name = 'DesignationId'; SELECT 'GONE  HrShift table removed' WHERE NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'HrShift'); SELECT 'PROC  ' + name FROM sys.procedures WHERE name IN ('SpGet_SMSClassTiming','SpSave_SMSClassTiming','SpDelete_SMSClassTiming','SpRPT_StudentLedger','SpRPT_FamilyLedger','SpGet_FamilyLookup','SpRPT_SMSExamResultCard','SpRPT_SMSStudentFee') ORDER BY name; SELECT 'Class timings entered: ' + CAST(COUNT(*) AS VARCHAR) FROM SMSClassTiming; SELECT 'Staff with a campus: ' + CAST(COUNT(*) AS VARCHAR) + ' of ' + CAST((SELECT COUNT(*) FROM Employees) AS VARCHAR) FROM Employees WHERE CampusID IS NOT NULL;"
Write-Lines $after

$found = @($after | Where-Object { $_ -match '^(TABLE|PROC|COLUMN|GONE)' }).Count
Write-Host ""
if ($found -eq 14) {
    Write-Host "All 14 objects are in place." -ForegroundColor Green
    Write-Host "Next:"
    Write-Host "  1. Class Timings screen - enter the real times. Until then the"
    Write-Host "     auto-reply tells parents the timing has not been entered yet."
    Write-Host "  2. Employee form - set each member of staff's campus. Until then"
    Write-Host "     a campus-specific Staff Statement comes back empty."
} else {
    Write-Host "Expected 14 objects, found $found. Check the output above." -ForegroundColor Red
}
