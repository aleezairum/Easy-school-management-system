<#
=============================================================================
 LIVE DATABASE UPDATE - 16 October 2026: Result Exclude List everywhere
=============================================================================

 Students ticked on Exam -> Result Exclude List were left out of the
 teachers' result only. The class result sheet, result cards, position and
 percentage lists, subject-wise, executive and result announcement reports
 still showed them (with "-" marks, 0% and Fail). This applies the seven
 report procedures that now skip them as well:

   SpRPT_SMSExamResultSheet.sql        SpRPT_SMSExamSubjectWise.sql
   SpRPT_SMSExamResultCard.sql         SpRPT_SMSExamExecutive.sql
   SpRPT_SMSExamStudentPercentage.sql  SpRPT_SMSExamResultAnnouncement.sql
   SpRPT_SMSExamStudentPosition.sql

 Updated 22 October 2026: the Position and Percentage reports also get a
 Prev.% column (the same student's percentage in the previous term). Running
 this file again applies that too.

 Updated 26 October 2026: first creates the academy attendance table
 (AcademyAttendance.sql, Students -> Mark Academy Attendance), and the result
 card shows the academy attendance of the students marked there. The card's
 school attendance no longer counts days whose attendance was deleted.

 Updated 30 October 2026: also creates the Academy Subject Allocation
 (AcademyStaffSubject.sql, Academics -> Academy Subject Allocation) and the
 Academy Teacher Wise result procedure. New table and procedures only.

 Nothing else changes: marks stay as entered, and with an empty exclude list
 every report returns exactly what it did before. The mobile app and the
 WhatsApp result skip excluded students too, through the API publish.

 Run it on the server (or any PC that reaches the live SQL Server):

   powershell -ExecutionPolicy Bypass -File Deploy_ResultExclude_2026-10-16.ps1 -Database EasySchoolDB -Password <sa password>

 Back up the database first. Safe to run more than once.
 This file is plain ASCII on purpose (Windows PowerShell 5.1).
=============================================================================
#>
param(
    [string]$Server = ".",

    [Parameter(Mandatory = $true)]
    [string]$Database,

    [string]$User = "sa",

    [Parameter(Mandatory = $true)]
    [string]$Password,

    # Skip the confirmation prompt. For a scripted run only.
    [switch]$Yes
)

$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot

function Invoke-Sql([string]$Query) {
    $out = sqlcmd -S $Server -d $Database -U $User -P $Password -b -h-1 -W -Q "SET NOCOUNT ON; SET QUOTED_IDENTIFIER ON; $Query"
    if ($LASTEXITCODE -ne 0) { throw "sqlcmd failed: $out" }
    return $out
}

$files = @(
    "SpRPT_SMSExamResultSheet.sql",
    "SpRPT_SMSExamResultCard.sql",
    "SpRPT_SMSExamStudentPercentage.sql",
    "SpRPT_SMSExamStudentPosition.sql",
    "SpRPT_SMSExamSubjectWise.sql",
    "SpRPT_SMSExamExecutive.sql",
    "SpRPT_SMSExamResultAnnouncement.sql"
)

Write-Host ""
Write-Host "Server $Server, database $Database" -ForegroundColor Cyan
$hasList = Invoke-Sql "SELECT CASE WHEN OBJECT_ID('dbo.SMSExamExclude', 'U') IS NULL THEN 'missing' ELSE 'ready' END"
if (($hasList -join "") -notmatch 'ready') {
    throw "dbo.SMSExamExclude is missing in $Database. Run ExamResultControls.sql first."
}
if (-not $Yes) {
    $answer = Read-Host "Create the academy tables (attendance, subject allocation) and apply the 7 result report procedures to $Database? (yes/no)"
    if ($answer -ne "yes") { Write-Host "Nothing applied."; return }
}

# The academy attendance table first: the result card procedure reads it.
Write-Host ""
Write-Host "=== Applying AcademyAttendance.sql ===" -ForegroundColor Cyan
sqlcmd -S $Server -d $Database -U $User -P $Password -b -i "AcademyAttendance.sql"
if ($LASTEXITCODE -ne 0) { throw "AcademyAttendance.sql failed - nothing else applied." }
Write-Host "  applied." -ForegroundColor Green

# 30 October 2026: the academy's own subject allocation and its teacher result.
Write-Host ""
Write-Host "=== Applying AcademyStaffSubject.sql ===" -ForegroundColor Cyan
sqlcmd -S $Server -d $Database -U $User -P $Password -b -i "AcademyStaffSubject.sql"
if ($LASTEXITCODE -ne 0) { throw "AcademyStaffSubject.sql failed - nothing else applied." }
Write-Host "  applied." -ForegroundColor Green

foreach ($file in $files) {
    Write-Host ""
    Write-Host "=== Applying $file ===" -ForegroundColor Cyan
    if (-not (Test-Path $file)) { throw "$file is not in $PSScriptRoot" }

    # Most of these files start with USE [EasySchoolDB]. Dropped here, so the
    # procedure lands in -Database whatever the database is called.
    $tmp = Join-Path $env:TEMP ("resultexclude-" + $file)
    $text = (Get-Content $file -Raw) -replace '(?im)^\s*USE\s+\[?[A-Za-z0-9_]+\]?\s*;?\s*$', ''
    [IO.File]::WriteAllText($tmp, $text, (New-Object Text.UTF8Encoding $false))
    try {
        # -b: a SQL error gives a non-zero exit code instead of a silent "success".
        sqlcmd -S $Server -d $Database -U $User -P $Password -b -i $tmp
        if ($LASTEXITCODE -ne 0) { throw "$file failed - stopping before the next file." }
    } finally {
        Remove-Item $tmp -ErrorAction SilentlyContinue
    }
    Write-Host "  applied." -ForegroundColor Green
}

Write-Host ""
Write-Host "=== Verify ===" -ForegroundColor Cyan
$ok = 0
foreach ($file in $files) {
    $name = [IO.Path]::GetFileNameWithoutExtension($file)
    $line = Invoke-Sql "SELECT CASE WHEN OBJECT_DEFINITION(OBJECT_ID('dbo.$name')) LIKE '%SMSExamExclude%' THEN 'OK' ELSE 'NOT UPDATED' END"
    $state = ($line -join "").Trim()
    if ($state -eq 'OK') { $ok++; Write-Host ("  OK    " + $name) -ForegroundColor Green }
    else { Write-Host ("  FAIL  " + $name + " does not read the exclude list") -ForegroundColor Red }
}

# 22 October 2026: the Position and Percentage reports gained a Prev.% column.
foreach ($name in @("SpRPT_SMSExamStudentPosition", "SpRPT_SMSExamStudentPercentage")) {
    $line = Invoke-Sql "SELECT CASE WHEN OBJECT_DEFINITION(OBJECT_ID('dbo.$name')) LIKE '%PrevPct%' THEN 'OK' ELSE 'NO' END"
    if ((($line -join "").Trim()) -eq 'OK') { Write-Host ("  OK    " + $name + " has Prev.%") -ForegroundColor Green }
    else { $ok--; Write-Host ("  FAIL  " + $name + " has no Prev.% column") -ForegroundColor Red }
}

# 26 October 2026: academy attendance on the result card.
$line = Invoke-Sql "SELECT CASE WHEN OBJECT_ID('dbo.SMSAcademyAttendance', 'U') IS NOT NULL AND OBJECT_DEFINITION(OBJECT_ID('dbo.SpRPT_SMSExamResultCard')) LIKE '%SMSAcademyAttendance%' THEN 'OK' ELSE 'NO' END"
if ((($line -join "").Trim()) -eq 'OK') { Write-Host "  OK    academy attendance table and result card" -ForegroundColor Green }
else { $ok--; Write-Host "  FAIL  academy attendance table or result card not updated" -ForegroundColor Red }

# 30 October 2026: Academy Subject Allocation and the Academy Teacher Wise result.
$line = Invoke-Sql "SELECT CASE WHEN OBJECT_ID('dbo.SMSAcademyStaffSubject', 'U') IS NOT NULL AND OBJECT_ID('dbo.SpRPT_SMSExamAcademyTeacherWise', 'P') IS NOT NULL THEN 'OK' ELSE 'NO' END"
if ((($line -join "").Trim()) -eq 'OK') { Write-Host "  OK    academy subject allocation and academy teacher result" -ForegroundColor Green }
else { $ok--; Write-Host "  FAIL  academy subject allocation not created" -ForegroundColor Red }

# The result sheet builds its query as text at run time, so a mistake in it
# only shows when it runs. Run it once on the latest exam that has marks.
Write-Host ""
Write-Host "=== Result sheet runs ===" -ForegroundColor Cyan
$pick = Invoke-Sql "SELECT TOP 1 CONCAT(ExamID, ',', ClassID, ',', SectionID) FROM SMSExamResult WHERE IsActive = 1 ORDER BY ExamID DESC, VDate DESC"
$p = (($pick -join "").Trim()) -split ','
if ($p.Count -eq 3) {
    $null = Invoke-Sql "EXEC dbo.SpRPT_SMSExamResultSheet @ExamID = $($p[0]), @ClassID = $($p[1]), @SectionID = $($p[2]), @PrevExamID = NULL"
    Write-Host "  OK    exam $($p[0]), class $($p[1]), section $($p[2])" -ForegroundColor Green
} else {
    Write-Host "  (no marks yet - nothing to run)"
}

Write-Host ""
if ($ok -eq $files.Count) {
    Write-Host "All $ok procedures updated. Excluded students are now left out of every result report." -ForegroundColor Green
} else {
    Write-Host "Only $ok of $($files.Count) procedures updated - see above." -ForegroundColor Red
}
