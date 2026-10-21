using System.Globalization;
using System.Net;
using System.Text;
using School.API.DTOs.Academic;

namespace School.API.Services.Implementations.Academic
{
    // Builds a single-student Result Card as a self-contained HTML document
    // (inline CSS) for PDF rendering. Mirrors the design of the printable
    // School.Web/Views/Exam/ResultCard.cshtml view, one card per page.
    public static class ResultCardHtmlBuilder
    {
        public static string Build(ResultCardStudentDto s, string? examName)
        {
            var issueDate = DateTime.Now.ToString("dd-MMM-yyyy");

            var rows = new StringBuilder();
            foreach (var sub in s.Subjects)
            {
                var statusClass = sub.Status == "Pass" ? "pass" : "fail";
                rows.Append($@"
                    <tr>
                        <td class=""subject-name"">{Enc(sub.SubjectName)}</td>
                        <td>{(sub.VDate.HasValue ? sub.VDate.Value.ToString("dd-MMM-yyyy") : "-")}</td>
                        <td>{Marks(sub.TotalMarks)}</td>
                        <td>{Marks(sub.PassMarks)}</td>
                        <td>{PctOf(sub.PassMarks, sub.TotalMarks)}</td>
                        <td>{Marks(sub.MarksObtained)}</td>
                        <td>{PctOf(sub.MarksObtained, sub.TotalMarks)}</td>
                        <td class=""{statusClass}"">{Enc(sub.Status)}</td>
                    </tr>");
            }

            var grandStatusClass = s.GrandStatus == "Pass" ? "pass" : "fail";
            var classLine = string.IsNullOrWhiteSpace(s.ClassName) ? "-" : Enc(s.ClassName);
            if (!string.IsNullOrWhiteSpace(s.SectionName))
                classLine += " (" + Enc(s.SectionName) + ")";

            return $@"<!DOCTYPE html>
<html lang=""en"">
<head>
<meta charset=""UTF-8"" />
<style>
    * {{ margin: 0; padding: 0; box-sizing: border-box; }}
    html, body, table, th, td, tr {{
        -webkit-print-color-adjust: exact !important;
        print-color-adjust: exact !important;
    }}
    body {{ font-family: Arial, sans-serif; font-size: 12px; background: #fff; color: #000; }}
    .card-wrapper {{ border: 2px solid #555; max-width: 780px; margin: 0 auto; }}
    .school-header {{ text-align: center; border-bottom: 2px solid #555; padding: 8px 4px 6px; }}
    .school-name {{ font-size: 19px; font-weight: bold; }}
    .school-address {{ font-size: 15px; font-weight: bold; margin-top: 2px; }}
    .card-title {{ text-align: center; font-size: 14px; font-weight: bold; text-decoration: underline; padding: 5px 0 4px; border-bottom: 1px solid #555; }}
    .info-section {{ display: flex; border-bottom: 1px solid #555; }}
    .info-left {{ flex: 1; padding: 6px 8px; border-right: 1px solid #555; }}
    .info-right {{ flex: 1; padding: 6px 8px; }}
    .info-photo {{ width: 90px; border-left: 1px solid #555; display: flex; align-items: center; justify-content: center; padding: 4px; }}
    .photo-placeholder {{ width: 80px; height: 90px; border: 1px solid #aaa; display: flex; align-items: center; justify-content: center; background: #e8e8e8; color: #888; font-size: 10px; text-align: center; }}
    .info-row {{ margin-bottom: 4px; line-height: 1.5; }}
    table {{ width: 100%; border-collapse: collapse; font-size: 11.5px; }}
    table th, table td {{ border: 1px solid #555; padding: 4px 6px; text-align: center; vertical-align: middle; }}
    table th {{ background-color: #d6d6d6; font-weight: bold; }}
    td.subject-name {{ text-align: left; padding-left: 8px; }}
    tr.total-row td {{ font-weight: bold; background-color: #efefef; }}
    .pass {{ color: #1a6b1a; font-weight: bold; }}
    .fail {{ color: #c00000; font-weight: bold; }}
    .signature-row {{ display: flex; justify-content: space-between; padding: 16px 24px 10px; border-top: 1px solid #555; }}
    .sig-block {{ text-align: center; flex: 1; }}
    .sig-line {{ border-top: 1.5px solid #000; margin: 0 auto 4px; width: 140px; }}
    .sig-label {{ font-size: 12px; font-weight: bold; }}
    @page {{ size: A4 portrait; margin: 0; }}
</style>
</head>
<body>
    <div class=""card-wrapper"">
        <div class=""school-header"">
            <div class=""school-name"">SIR SYED MODEL HIGH SCHOOL (Boys&amp;Girls)</div>
            <div class=""school-address"">Chhonawala Road, Hasilpur &nbsp; 0333-6335634 , 0301-7330101</div>
        </div>
        <div class=""card-title"">Result Card</div>
        <div class=""info-section"">
            <div class=""info-left"">
                <div class=""info-row""><strong>Roll No.&nbsp;&nbsp;:</strong> {Enc(s.RollNo)} &nbsp;&nbsp;&nbsp;&nbsp; <strong>Issue Date :</strong> {issueDate}</div>
                <div class=""info-row""><strong>Student's Name :</strong> {Enc(s.StudentName)}</div>
                <div class=""info-row""><strong>Father's Name :</strong> {Enc(s.FatherName)}</div>
                <div class=""info-row""><strong>Class &nbsp;&nbsp;&nbsp;&nbsp; :</strong> {classLine}</div>
                <div class=""info-row""><strong>Exam &nbsp;&nbsp;&nbsp;&nbsp; :</strong> {(string.IsNullOrWhiteSpace(examName) ? "-" : Enc(examName))}</div>
            </div>
            <div class=""info-right"">
                <div class=""info-row""><strong>Highest Obtained Marks in Class :</strong> {Marks(s.HighestMarks)}</div>
                <div class=""info-row""><strong>Total Students:</strong> {s.TotalStudents}</div>
                <div class=""info-row""><strong>Position in Class :</strong> {(s.Position.HasValue ? s.Position.Value.ToString() : "-")}</div>
                <div class=""info-row""><strong>Previous Percentage :</strong> {Pct(s.PrevPct)}</div>
                <div class=""info-row""><strong>Attendance :</strong> {s.PresentDays}/{s.SchoolDays} ({Pct(s.AttendancePct)})</div>
            </div>
            <div class=""info-photo""><div class=""photo-placeholder"">Photo</div></div>
        </div>
        <table>
            <thead>
                <tr>
                    <th style=""text-align:left; padding-left:8px;"">Subject</th>
                    <th>Date</th>
                    <th>Total Marks</th>
                    <th colspan=""2"">Pass M &amp; %</th>
                    <th>Obt. Marks</th>
                    <th>Obt. %</th>
                    <th>Status</th>
                </tr>
            </thead>
            <tbody>
                {rows}
                <tr class=""total-row"">
                    <td class=""subject-name"" colspan=""2""><strong>Total:</strong></td>
                    <td>{Marks(s.GrandTotal)}</td>
                    <td colspan=""2"">{Marks(s.GrandPass)}</td>
                    <td>{Marks(s.GrandObt)}</td>
                    <td>{Pct(s.CurrentPct)}</td>
                    <td class=""{grandStatusClass}"">{Enc(s.GrandStatus)}</td>
                </tr>
            </tbody>
        </table>
        <div class=""signature-row"">
            <div class=""sig-block""><div class=""sig-line""></div><div class=""sig-label"">Parent's Signature</div></div>
            <div class=""sig-block""><div class=""sig-line""></div><div class=""sig-label"">Exam Controller</div></div>
            <div class=""sig-block""><div class=""sig-line""></div><div class=""sig-label"">Principal</div></div>
        </div>
    </div>
</body>
</html>";
        }

        private static string Enc(string? v) => WebUtility.HtmlEncode(v ?? string.Empty);

        private static string Marks(decimal? v)
            => v.HasValue
                ? (v.Value == Math.Floor(v.Value)
                    ? ((long)v.Value).ToString()
                    : v.Value.ToString("0.##", CultureInfo.InvariantCulture))
                : "-";

        private static string Pct(decimal? v)
            => v.HasValue ? v.Value.ToString("0.00", CultureInfo.InvariantCulture) + "%" : "-";

        private static string PctOf(decimal? part, decimal? whole)
            => (part.HasValue && whole.HasValue && whole.Value != 0)
                ? (part.Value * 100m / whole.Value).ToString("0.##", CultureInfo.InvariantCulture) + "%"
                : "-";
    }
}
