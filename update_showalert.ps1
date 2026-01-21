$newShowAlert = @'
        function showAlert(message, type) {
            if (type === 'success') {
                toastSuccess(message);
            } else if (type === 'danger' || type === 'error') {
                toastError(message);
            } else if (type === 'warning') {
                toastWarning(message);
            } else {
                toastInfo(message);
            }
        }
'@

$files = @(
    'E:\20-01-2026\Easy-school-management-system-main\School.Web\Views\Salary\Index.cshtml',
    'E:\20-01-2026\Easy-school-management-system-main\School.Web\Views\Exam\Index.cshtml',
    'E:\20-01-2026\Easy-school-management-system-main\School.Web\Views\Attendance\Index.cshtml',
    'E:\20-01-2026\Easy-school-management-system-main\School.Web\Views\TimeTable\Index.cshtml',
    'E:\20-01-2026\Easy-school-management-system-main\School.Web\Views\Admission\Form.cshtml',
    'E:\20-01-2026\Easy-school-management-system-main\School.Web\Views\Designation\Index.cshtml',
    'E:\20-01-2026\Easy-school-management-system-main\School.Web\Views\Employee\Form.cshtml',
    'E:\20-01-2026\Easy-school-management-system-main\School.Web\Views\Employee\Index.cshtml',
    'E:\20-01-2026\Easy-school-management-system-main\School.Web\Views\AcademicGrade\Index.cshtml',
    'E:\20-01-2026\Easy-school-management-system-main\School.Web\Views\HRGrade\Index.cshtml',
    'E:\20-01-2026\Easy-school-management-system-main\School.Web\Views\Student\Form.cshtml',
    'E:\20-01-2026\Easy-school-management-system-main\School.Web\Views\Classes\Index.cshtml',
    'E:\20-01-2026\Easy-school-management-system-main\School.Web\Views\Roles\Index.cshtml',
    'E:\20-01-2026\Easy-school-management-system-main\School.Web\Views\Sections\Index.cshtml',
    'E:\20-01-2026\Easy-school-management-system-main\School.Web\Views\Sessions\Index.cshtml',
    'E:\20-01-2026\Easy-school-management-system-main\School.Web\Views\Subjects\Index.cshtml',
    'E:\20-01-2026\Easy-school-management-system-main\School.Web\Views\Users\Index.cshtml'
)

foreach ($file in $files) {
    if (Test-Path $file) {
        $content = Get-Content $file -Raw
        $pattern = "(?s)        function showAlert\(message, type\) \{[^}]+\`$\('#alertContainer'\)\.html\(alert\);[^}]+setTimeout\(\(\) => \{[^}]+\}, 5000\);[^}]+\}"
        
        if ($content -match $pattern) {
            $content = $content -replace $pattern, $newShowAlert
            Set-Content $file $content -NoNewline
            Write-Host "Updated: $file"
        } else {
            Write-Host "Pattern not found in: $file"
        }
    } else {
        Write-Host "File not found: $file"
    }
}
