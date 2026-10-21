<#
=============================================================================
 LIVE DATABASE UPDATE - 23 September 2026
=============================================================================

 WHY THIS EXISTS RIGHT NOW

 The web application was deployed to live ahead of the database, so the new
 screens call procedures the live database has not got:

     /api/ApplyFee/PendingStudents ... 500   SpFillCombo_PendingFeeStudents
     /api/fee/promise .............. 500   SMSFeePromise and friends

 A 500 is the giveaway. A missing ROUTE answers 405; a missing PROCEDURE gets
 as far as the database and throws. Applying the files below clears it.

 WHAT IT APPLIES

   SpGetPending_SMSStudentFee.sql
                        The Receive Payment balance. Takes @AsOn and marks each
                        unpaid row IsDue, so a fee raised for a month still to
                        come is no longer counted as arrears. This is what made
                        a September receipt announce October to December as
                        pending. Future rows are still returned, so a parent
                        paying months ahead can still do so.

   SpGet_SMSStudentFee.sql
                        The Apply Fee List screen. It shares its result shape
                        with the procedure above, so when that one gained IsDue
                        this one had to as well: EF Core refuses a result set
                        missing a mapped column, and the screen answered 500
                        until the column was added. MUST GO OUT WITH THE ONE
                        ABOVE - applying either alone leaves a screen broken.

   SpFillCombo_PendingFeeStudents.sql
                        NEW. The Find Student dropdowns on Receive Payment and
                        on Promise to Pay: the children of a class or section
                        who actually owe something as at a date, with the
                        amount. THIS IS THE ONE THE 500s ARE ABOUT.

   FeePromise.sql       The Promise to Pay screen: SMSFeePromise, the view
                        vwActiveFeePromise, and the Get / Save / Delete
                        procedures. A promise is recorded against one child and
                        covers every sibling of the same parent.
                        MUST RUN BEFORE the two files below, which read the
                        view it creates.

   SpRPT_SMSFeePromise.sql
                        NEW. The printed Promise to Pay sheet: one row per
                        STUDENT a promise covers, siblings included, so the
                        office does not chase a child whose father has already
                        given his word. Reads nothing that FeePromise.sql does
                        not create, but needs SMSFeePromise, so it runs after.

   SmsHoldAndFeeTillDate.sql
                        The fee reminder list, which now skips a family with a
                        promise in force. Reads vwActiveFeePromise.

   SpRPT_SMSStudentFee.sql
                        The fee report: campus filter, ReportType 3 for the
                        unpaid fee of students who have left, and the
                        @IncludePromised switch. Reads vwActiveFeePromise.
                        Re-applied here because it has changed since the
                        September runner.

   AddShortLeaveOuting.sql
                        Short leave: when a member of staff goes out during the
                        day and comes back. The minutes are counted and
                        deducted in payroll.

   AllowOvernightShift.sql
                        Lets a timing cross midnight, for the guard on 7:00 PM
                        to 7:00 AM. Without it his attendance cannot be marked.

   CompleteAdministrationForms.sql
                        The remaining Administration screens.

   SalarySheetColumns.sql
                        HrPayrollMonthlyAmount - the incentive, security, fine
                        and advance the office types per person per month - plus
                        ten columns on HrPayslip so a sheet reprinted later still
                        shows what was actually paid. Fill the amounts in from
                        Staff and Payroll, Monthly Amounts, BEFORE generating.

   EarlyAndDutyMinutes.sql
                        Charges EARLY LEAVING, which attendance had always
                        recorded and payroll had always ignored, at the same
                        per-minute rate as lateness. Adds HrAttendance
                        ShortMinutes and ExtraMinutes for the duty swap - when
                        one member of staff covers another - and the rule that
                        charges short less extra, so the covered one pays and
                        the one who covered is paid.
                        RE-RUN the payroll afterwards.

   StaffCategory.sql
                        The staff category list - the groups the office counts
                        and prints staff by, separate from the designation -
                        and Employees.StaffCategoryId pointing at it. Nothing is
                        seeded: the categories are the school's to name.

   LateMinuteDeduction.sql
                        The late-arrival and short-leave deduction rules. The
                        Payroll Review sheet showed late minutes and deducted
                        nothing, because payroll only deducts what the Pay
                        Components say and there was no late rule. One minute
                        costs Gross divided by the month working days, the
                        hours in the day and sixty. No grace. Short leave is
                        priced the same way and was switched on.
                        RE-RUN the payroll afterwards: payslips already
                        generated keep the figures they were generated with.

   ExtraDays.sql        Extra days for staff with no day off in the month:
                        the "working days more than" setting (20 to start
                        with), extra days per designation, what was given per
                        person per month, and two HrPayslip columns so the
                        salary sheet shows them. Nothing is given until the
                        office saves the Extra Days screen.

   PayrollRunCampus.sql HrPayrollRun.CampusId, so a month can be paid campus
                        by campus - a salary bill per campus - instead of in
                        one run. Existing runs stay "all campuses". The one-
                        run-a-month index becomes one per month per campus.

   PayslipMinutes.sql   HrPayslip.EarlyMinutes and OutingMinutes, so the
                        salary slip shows the minutes behind the early-leaving
                        and out-of-school deductions. Generate the payroll
                        again for existing payslips to fill them.

   DailyDiaryAndAnnouncements.sql
                        SMSDailyDiary and SMSDailyDiaryImage - the class and
                        section wise diary pictures parents get on WhatsApp
                        ("Today's Diary") - and SMSAnnouncement, the notices
                        on the WhatsApp menu ("Announcements").

   PayrollMinuteRate.sql
                        Puts the minute rules back where Payroll Setup had
                        turned them all into "Per Late Minute" (late minutes
                        charged several times, early and out-of-school not at
                        all), and adds the per-minute rate to HrPayslip for the
                        Payroll Review. RE-GENERATE the payroll afterwards.

   HikvisionAttendance.sql
                        Face-attendance machines: the machines, who is on
                        them, face photos, and the log of every punch. New
                        tables only. Nothing runs until a machine is added on
                        Staff & Payroll -> Setup -> Attendance Machines.

   ExamResultControls.sql
                        SMSExamFinalize - only a FINALISED exam's result goes
                        to WhatsApp (Exams screen -> Finalize). Until an exam
                        is finalised there, parents and teachers see no result
                        for it. SMSExamExclude - students left out of the
                        teachers' result (Examinations -> Result Exclude List).

   SpRPT_SMSExamTeacherWise.sql, SpRPT_SMSExamTeacherComparison.sql
                        The teachers' result, now leaving out the students on
                        the exam's exclude list.

   PayrollIncentivesAndAdvance.sql
                        Extra Duty, Academy Duty, Prize and Fine as their own
                        columns on Monthly Amounts and the payslip (the old
                        signed Prize/Fine is split once), and HrStaffAdvance -
                        advances entered by date (Staff & Payroll -> Staff
                        Advances). The advances typed on Monthly Amounts so
                        far are carried into it, dated the 1st of their month.
                        GENERATE the month's payroll again afterwards.

   LeaveDeduction.sql   The Leave Deduction rule (Per Leave Day): a day of
                        leave costs a day's pay, by its leave type's Paid /
                        Deduct Days; Free Units/Month = paid leave days. Also
                        the Absent Deduction rule when there is none yet.
                        GENERATE the month's payroll again afterwards.

   HikvisionBiometrics.sql
                        Faces and fingerprints read from a machine are kept
                        (HrBiometricFinger), to put on another machine; and
                        Employees.Photo widened so the employee form's picture
                        (or the face from the machine) can be saved.

   SpRPT_SMSExamResultAnnouncement.sql
                        Result Announcement report: the "Pass % Prev." column
                        - the previous term's pass % beside this term's, for
                        the whole school, each campus and each class.

   MobileApp.sql        The EasySchool mobile app: logins (App Logins page),
                        what each role may do (Mobile App Permissions), phones
                        signed in, and which announcements have been pushed.
                        Adds a "Teacher" role if there is none.

 THE PAYROLL CHECK
 -----------------
 The run ends by checking the four deduction rules (LATE, SHORTLEAVE, EARLY,
 DUTYSWAP) are in HrPayComponent. Payroll deducts only what those rules say:
 without them late minutes show on the sheet and cost nothing, which is
 exactly what happened before. If any is missing the run says so in red -
 DO NOT generate salaries until it is fixed.

 All of it is additive. Tables and columns are created only if absent and
 procedures are dropped and recreated, which is how every script in this folder
 installs one. Nothing here drops a table or a column, and nothing here touches
 Backup and Restore.

 ORDER MATTERS. FeePromise.sql creates the view that SmsHoldAndFeeTillDate.sql
 and SpRPT_SMSStudentFee.sql select from. The list below is already in that
 order; do not sort it.

 NOTE: this file is deliberately plain ASCII. Windows PowerShell 5.1 reads a
 .ps1 without a byte-order mark as ANSI, so a dash or a box-drawing character
 saved as UTF-8 arrives as mojibake and the script fails to parse.

 BEFORE YOU RUN
 --------------
   1. TAKE A FULL BACKUP. Non-negotiable, as with every live change.
   2. Confirm the live database NAME. appsettings.json says EasySchoolDB while
      an earlier deployment recorded a run against EasySchoolDB_Live, so the
      two disagree. This script prints what it found and asks before changing
      anything.

 RUN IT
 ------
   On the SERVER, in PowerShell:

     cd "D:\01 EasyPOS\WebApp\EasySchool\01 EasySchoolAPI\Scripts"
     .\Deploy_Live_2026-09-23.ps1 -Database EasySchoolDB -Password '<sa password>'

   Add -DryRun to see what it would do without changing anything.

 AFTER IT RUNS
 -------------
   Restart the API (recycle the app pool). The procedures are picked up
   without a restart, but a restart clears any cached failure.
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

# What must exist when this is done. Counted at the end.
$checkQuery = @"
SELECT 'PROC  ' + name FROM sys.procedures
 WHERE name IN ('SpFillCombo_PendingFeeStudents','SpGetPending_SMSStudentFee',
                'SpSave_SMSFeePromise','SpDelete_SMSFeePromise','SpGet_SMSFeePromise','SpRPT_SMSFeePromise',
                'SpRPT_SMSStudentFee')
 ORDER BY name;
SELECT 'TABLE SMSFeePromise' WHERE OBJECT_ID('dbo.SMSFeePromise') IS NOT NULL;
SELECT 'VIEW  vwActiveFeePromise' WHERE OBJECT_ID('dbo.vwActiveFeePromise') IS NOT NULL;
SELECT 'ASON  SpGetPending takes a date'
 WHERE OBJECT_DEFINITION(OBJECT_ID('dbo.SpGetPending_SMSStudentFee')) LIKE '%@AsOn%';
SELECT 'ISDUE SpGet_SMSStudentFee returns it'
 WHERE OBJECT_DEFINITION(OBJECT_ID('dbo.SpGet_SMSStudentFee')) LIKE '%IsDue%';
SELECT 'PROMISED  the fee report can include them'
 WHERE OBJECT_DEFINITION(OBJECT_ID('dbo.SpRPT_SMSStudentFee')) LIKE '%IncludePromised%';
SELECT 'TABLE HrPayrollMonthlyAmount' WHERE OBJECT_ID('dbo.HrPayrollMonthlyAmount') IS NOT NULL;
SELECT 'TABLE HrStaffCategory' WHERE OBJECT_ID('dbo.HrStaffCategory') IS NOT NULL;
SELECT 'TABLE HrExtraDaysSetting' WHERE OBJECT_ID('dbo.HrExtraDaysSetting') IS NOT NULL;
SELECT 'TABLE HrExtraDaysRule' WHERE OBJECT_ID('dbo.HrExtraDaysRule') IS NOT NULL;
SELECT 'TABLE HrPayrollExtraDays' WHERE OBJECT_ID('dbo.HrPayrollExtraDays') IS NOT NULL;
SELECT 'COLUMN HrPayslip.ExtraDays' WHERE COL_LENGTH('dbo.HrPayslip', 'ExtraDays') IS NOT NULL;
SELECT 'COLUMN HrPayrollRun.CampusId' WHERE COL_LENGTH('dbo.HrPayrollRun', 'CampusId') IS NOT NULL;
SELECT 'COLUMN HrPayslip.EarlyMinutes' WHERE COL_LENGTH('dbo.HrPayslip', 'EarlyMinutes') IS NOT NULL;
SELECT 'TABLE SMSDailyDiaryImage' WHERE OBJECT_ID('dbo.SMSDailyDiaryImage') IS NOT NULL;
SELECT 'TABLE SMSAnnouncement' WHERE OBJECT_ID('dbo.SMSAnnouncement') IS NOT NULL;
SELECT 'COLUMN HrPayslip.PerMinuteRate' WHERE COL_LENGTH('dbo.HrPayslip', 'PerMinuteRate') IS NOT NULL;
SELECT 'TABLE HrDeviceEvent' WHERE OBJECT_ID('dbo.HrDeviceEvent') IS NOT NULL;
SELECT 'TABLE HrBiometricFinger' WHERE OBJECT_ID('dbo.HrBiometricFinger') IS NOT NULL;
SELECT 'COLUMN HrDevicePerson.FingersOnDevice' WHERE COL_LENGTH('dbo.HrDevicePerson', 'FingersOnDevice') IS NOT NULL;
SELECT 'TABLE SMSExamFinalize' WHERE OBJECT_ID('dbo.SMSExamFinalize') IS NOT NULL;
SELECT 'TABLE SMSExamExclude' WHERE OBJECT_ID('dbo.SMSExamExclude') IS NOT NULL;
SELECT 'TABLE HrStaffAdvance' WHERE OBJECT_ID('dbo.HrStaffAdvance') IS NOT NULL;
SELECT 'COLUMN HrPayslip.Fine' WHERE COL_LENGTH('dbo.HrPayslip', 'Fine') IS NOT NULL;
SELECT 'PROC  SpRPT_SMSExamTeacherWise skips the exclude list'
 WHERE OBJECT_DEFINITION(OBJECT_ID('dbo.SpRPT_SMSExamTeacherWise')) LIKE '%SMSExamExclude%';
SELECT 'PROC  SpRPT_SMSExamTeacherComparison skips the exclude list'
 WHERE OBJECT_DEFINITION(OBJECT_ID('dbo.SpRPT_SMSExamTeacherComparison')) LIKE '%SMSExamExclude%';
SELECT 'PROC  SpRPT_SMSExamResultAnnouncement has Pass % Prev.'
 WHERE OBJECT_DEFINITION(OBJECT_ID('dbo.SpRPT_SMSExamResultAnnouncement')) LIKE '%PrevExamID%';
SELECT 'TABLE AppAccount' WHERE OBJECT_ID('dbo.AppAccount') IS NOT NULL;
SELECT 'TABLE AppRoleFeature' WHERE OBJECT_ID('dbo.AppRoleFeature') IS NOT NULL;
SELECT 'TABLE AppDevice' WHERE OBJECT_ID('dbo.AppDevice') IS NOT NULL;
SELECT 'TABLE AppPushLog' WHERE OBJECT_ID('dbo.AppPushLog') IS NOT NULL;
"@

# The deduction rules payroll needs. Checked on their own because a missing
# one does not break a screen - it silently pays too much.
$rulesQuery = @"
IF OBJECT_ID('dbo.HrPayComponent') IS NOT NULL
    SELECT 'RULE  ' + Code + ' - ' + Name FROM HrPayComponent
     WHERE (Code = 'LATE' AND CalcType = 3) OR (Code = 'SHORTLEAVE' AND CalcType = 8)
        OR (Code = 'EARLY' AND CalcType = 9) OR (Code = 'DUTYSWAP' AND CalcType = 10)
     ORDER BY Code;
IF OBJECT_ID('dbo.HrPayComponent') IS NOT NULL
    SELECT 'DAYRULE ' + Code + ' - ' + Name + CASE WHEN IsActive = 1 THEN '' ELSE ' (OFF)' END FROM HrPayComponent
     WHERE CalcType IN (4, 11) ORDER BY CalcType;
"@

Write-Host ""
Write-Host "=== Target ===" -ForegroundColor Cyan

# Identify the database before touching it. A wrong name here is the single
# most expensive mistake available, and it is completely silent otherwise.
$identity = Invoke-Sql "SELECT 'Server    : ' + @@SERVERNAME; SELECT 'Database  : ' + DB_NAME(); SELECT 'Students  : ' + CAST(COUNT(*) AS VARCHAR) FROM SMSStudent; SELECT 'Employees : ' + CAST(COUNT(*) AS VARCHAR) FROM Employees; SELECT 'Vouchers  : ' + CAST(COUNT(*) AS VARCHAR) FROM FeeVoucherMain;"
Write-Lines $identity

Write-Host ""
Write-Host "=== Objects already present ===" -ForegroundColor Cyan
$before = Invoke-Sql $checkQuery
$existing = @($before | Where-Object { $_ -match '^(TABLE|PROC|VIEW|ASON|ISDUE|PROMISED|COLUMN)' })
if ($existing.Count -eq 0) {
    Write-Host "  (none of them - which is exactly why the screens answer 500)"
} else {
    Write-Lines $existing
}

Write-Host ""
Write-Host "=== Payroll deduction rules already present ===" -ForegroundColor Cyan
$rulesBefore = @(Invoke-Sql $rulesQuery | Where-Object { $_ -match '^RULE' })
if ($rulesBefore.Count -eq 0) {
    Write-Host "  (none - late minutes are shown and NOT deducted)" -ForegroundColor Yellow
} else {
    Write-Lines $rulesBefore
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

# Order matters: FeePromise.sql creates the view the two after it read.
$files = @(
    "SpGetPending_SMSStudentFee.sql",
    # Shares its result shape with the file above, so the two go out together.
    "SpGet_SMSStudentFee.sql",
    "SpFillCombo_PendingFeeStudents.sql",
    "FeePromise.sql",
    # Needs SMSFeePromise, so it follows the file above.
    "SpRPT_SMSFeePromise.sql",
    "SmsHoldAndFeeTillDate.sql",
    "SpRPT_SMSStudentFee.sql",
    "AddShortLeaveOuting.sql",
    "AllowOvernightShift.sql",
    "CompleteAdministrationForms.sql",
    # Sets up the late-minute and short-leave deduction rules. Payroll deducts
    # only what the Pay Components tell it to, and there was no late rule at
    # all, so late minutes were shown and never charged.
    "LateMinuteDeduction.sql",
    # Charges early leaving, and adds the duty-swap columns and their rule.
    "EarlyAndDutyMinutes.sql",
    # The salary sheet the school has always used: the month's hand-entered
    # amounts, and the ten columns a payslip now remembers.
    "SalarySheetColumns.sql",
    # The staff category list and the Employees column that points at it.
    "StaffCategory.sql",
    # Extra days for a month with no day off. Adds to HrPayslip, so it follows
    # SalarySheetColumns.sql like the other payslip columns.
    "ExtraDays.sql",
    # A payroll run per campus.
    "PayrollRunCampus.sql",
    # Early and out-of-school minutes kept on the payslip.
    "PayslipMinutes.sql",
    # The class diary pictures and the school announcements, for WhatsApp.
    "DailyDiaryAndAnnouncements.sql",
    # Minute rules put right, and the per-minute rate on the payslip.
    "PayrollMinuteRate.sql",
    # Face-attendance machines (Hikvision), then faces and fingerprints kept.
    "HikvisionAttendance.sql",
    "HikvisionBiometrics.sql",
    # Result finalise and the teachers' result exclude list, then the two
    # teacher reports that read the list.
    "ExamResultControls.sql",
    "SpRPT_SMSExamTeacherWise.sql",
    "SpRPT_SMSExamTeacherComparison.sql",
    # Result Announcement with the previous term's pass %.
    "SpRPT_SMSExamResultAnnouncement.sql",
    # Incentives itemised, Prize and Fine apart, advances by date.
    "PayrollIncentivesAndAdvance.sql",
    # Leave (and absent) charged by the day.
    "LeaveDeduction.sql",
    # The mobile app: logins, role features, phones, push log.
    "MobileApp.sql"
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
$after = Invoke-Sql $checkQuery
Write-Lines $after

# The same call the browser makes, answered here instead, so the run does not
# end on a promise that it worked.
Write-Host ""
Write-Host "=== The call that was failing ===" -ForegroundColor Cyan
$smoke = Invoke-Sql "CREATE TABLE #p (VID INT, RollNo NVARCHAR(100), Name NVARCHAR(300), Name_Urdu NVARCHAR(300), ClassRollNo NVARCHAR(100), ParentID INT, FatherName NVARCHAR(200), FatherCNIC NVARCHAR(50), PendingAmount DECIMAL(18,2), MonthsDue INT); INSERT #p EXEC SpFillCombo_PendingFeeStudents @ClassID = 0, @SectionID = 0; SELECT 'PendingStudents returns ' + CAST(COUNT(*) AS VARCHAR) + ' children owing ' + CAST(CAST(ISNULL(SUM(PendingAmount),0) AS DECIMAL(18,0)) AS VARCHAR) FROM #p; DROP TABLE #p;"
Write-Lines $smoke

Write-Host ""
Write-Host "=== Payroll deduction rules ===" -ForegroundColor Cyan
$ruleLines = @(Invoke-Sql $rulesQuery)
$rules = @($ruleLines | Where-Object { $_ -match '^RULE' })
$dayRules = @($ruleLines | Where-Object { $_ -match '^DAYRULE' })
Write-Lines $rules
Write-Lines $dayRules

$found = @($after | Where-Object { $_ -match '^(TABLE|PROC|VIEW|ASON|ISDUE|PROMISED|COLUMN)' }).Count
Write-Host ""
if ($rules.Count -ne 4) {
    Write-Host "ONLY $($rules.Count) OF THE 4 DEDUCTION RULES ARE THERE WITH THE RIGHT CALC TYPE." -ForegroundColor Red
    Write-Host "DO NOT GENERATE SALARIES: late or early minutes would be charged wrongly." -ForegroundColor Red
    Write-Host "Re-run LateMinuteDeduction.sql, EarlyAndDutyMinutes.sql and PayrollMinuteRate.sql and check again." -ForegroundColor Red
}
if (-not ($dayRules -match 'LEAVE')) {
    Write-Host 'No Leave Deduction rule: leave will cost nothing. Re-run LeaveDeduction.sql.' -ForegroundColor Yellow
}
if ($found -eq 37) {
    Write-Host "All 37 objects are in place." -ForegroundColor Green
    Write-Host "Next:"
    Write-Host "  1. Recycle the API app pool."
    Write-Host "  2. Open Receive Payment. The Student list should fill and say"
    Write-Host "     how many children owe and how much between them."
    Write-Host "  3. Open Promise to Pay. Same finder, and saving a promise"
    Write-Host "     should cover every sibling of that parent."
    Write-Host "  4. For September salaries: Attendance > Recalculate September,"
    Write-Host "     complete any unmarked days, then Extra Days, Monthly Amounts,"
    Write-Host "     and Generate Payroll LAST."
} else {
    Write-Host "Expected 37 objects, found $found. Check the output above." -ForegroundColor Red
}
