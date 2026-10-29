using System.Globalization;
using System.Text;
using School.API.DTOs.Academic;

namespace School.API.Services.Implementations.Academic
{
    // Builds a compact, WhatsApp-friendly TEXT result summary for one student
    // from the same data as the result card (SpRPT_SMSExamResultCard). Used when
    // sending results as text (no PDF attachment required).
    public static class ResultTextBuilder
    {
        private const int MaxLen = 1000; // matches SmsMessage.MessageText column

        public static string Build(ResultCardStudentDto s, string? examName, string? customPrefix)
        {
            var sb = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(customPrefix))
                sb.AppendLine(customPrefix!.Trim()).AppendLine();

            sb.AppendLine("Sir Syed Model High School (Boys & Girls)");
            sb.AppendLine("Chhonawala Road, Hasilpur");
            if (!string.IsNullOrWhiteSpace(examName))
                sb.AppendLine($"Result: {examName}");

            var roll = string.IsNullOrWhiteSpace(s.RollNo) ? "" : $" (Roll {s.RollNo})";
            sb.AppendLine($"Student: {s.StudentName}{roll}");

            var classLine = s.ClassName ?? "";
            if (!string.IsNullOrWhiteSpace(s.SectionName)) classLine += $" ({s.SectionName})";
            if (!string.IsNullOrWhiteSpace(classLine)) sb.AppendLine($"Class: {classLine.Trim()}");

            sb.AppendLine("-----");
            foreach (var sub in s.Subjects)
                sb.AppendLine($"{sub.SubjectName}: {Marks(sub.MarksObtained)}/{Marks(sub.TotalMarks)} ({SubStatus(sub)})");
            sb.AppendLine("-----");

            sb.AppendLine($"Total: {Marks(s.GrandObt)}/{Marks(s.GrandTotal)} ({Pct(s.CurrentPct)})");
            sb.AppendLine($"Total Students: {s.TotalStudents}");
            sb.AppendLine($"Position in Class: {(s.Position.HasValue ? s.Position.Value.ToString() : "-")}");
            sb.AppendLine($"Attendance: {s.PresentDays}/{s.SchoolDays} ({Pct(s.AttendancePct)})");
            if (s.AcademyDays > 0)
                sb.AppendLine($"Academy Attendance: {s.AcademyPresentDays}/{s.AcademyDays} ({Pct(s.AcademyPct)})");
            sb.AppendLine($"Result: {s.GrandStatus}");

            var text = sb.ToString().TrimEnd();
            return text.Length > MaxLen ? text.Substring(0, MaxLen) : text;
        }

        // Per-subject Pass/Fail: prefer the SP-provided Status, else derive from marks vs pass marks.
        private static string SubStatus(ResultCardSubjectDto sub)
        {
            if (!string.IsNullOrWhiteSpace(sub.Status))
                return sub.Status!.Trim();
            if (sub.MarksObtained.HasValue && sub.PassMarks.HasValue)
                return sub.MarksObtained.Value >= sub.PassMarks.Value ? "Pass" : "Fail";
            return "-";
        }

        private static string Marks(decimal? v)
            => v.HasValue
                ? (v.Value == Math.Floor(v.Value)
                    ? ((long)v.Value).ToString()
                    : v.Value.ToString("0.##", CultureInfo.InvariantCulture))
                : "-";

        private static string Pct(decimal? v)
            => v.HasValue ? v.Value.ToString("0.00", CultureInfo.InvariantCulture) + "%" : "-";
    }
}
