// Services/Implementations/FeeManagement/FeeStructureHtmlBuilder.cs
using System.Globalization;
using System.Net;
using System.Text;
using School.API.DTOs.SMS;

namespace School.API.Services.Implementations.Accounts
{
    /// <summary>
    /// Renders the school's "Dues Detail" sheet — the printed page handed out at
    /// admission time — from the fee structure actually held in the system.
    /// </summary>
    /// <remarks>
    /// This is laid out to match the sheet the school prints for its prospectus:
    /// a schedule for the one-off admission fee, then the grade-wise monthly
    /// schedule with a column per class group and a row per grade band. Nothing
    /// here is typed in — the amounts, the classes, the grade bands and the
    /// session name all come out of SMSFeeStructure, AcademicGrades and
    /// AcademicSessionYear, so the sheet a parent receives on WhatsApp is the
    /// same figure the office will charge them, and it changes by itself when
    /// next year's fees are entered.
    ///
    /// Neighbouring classes charging exactly the same are merged into one column
    /// ("Nursery to 3"), which is what makes it read like the printed sheet
    /// rather than a database dump. The merge is by value, not by a hard-coded
    /// list, so a class whose fee is changed on its own simply splits out.
    ///
    /// Inline CSS only, no images and no web fonts: PuppeteerPdfRenderer renders
    /// offline and anything it had to fetch would stall or silently vanish.
    /// See LedgerHtmlBuilder for the same constraint.
    /// </remarks>
    public static class FeeStructureHtmlBuilder
    {
        private static string E(string? s) => WebUtility.HtmlEncode(s ?? string.Empty);

        private static string Money(decimal v) =>
            v.ToString("N0", CultureInfo.InvariantCulture);

        // One colour per class-group column, cycled. The printed sheet uses a
        // different shade for each class band and parents read down the colour.
        private static readonly string[] ColumnColours =
            { "#4a7fb5", "#5aa0a0", "#7b6bb0", "#c06c84", "#b5763a", "#5d8c5a", "#8a6d3b", "#4f6d9c" };

        public static string Build(
            BotFeeStructureDto data,
            string schoolName,
            string schoolAddress,
            IReadOnlyList<string> notes)
        {
            var sb = new StringBuilder();

            sb.Append("<!DOCTYPE html><html><head><meta charset=\"utf-8\"><title>")
              .Append("Dues Detail ").Append(E(data.SessionName))
              .Append("</title><style>")
              .Append(@"
    * { box-sizing: border-box; margin: 0; padding: 0;
        -webkit-print-color-adjust: exact; print-color-adjust: exact; }
    body { font-family: Arial, Helvetica, sans-serif; font-size: 11px; color: #1a1a1a; }
    @page { size: A4 portrait; margin: 10mm 9mm; }
    table { border-collapse: collapse; width: 100%; margin-bottom: 4px; }
    thead { display: table-header-group; }
    tr { page-break-inside: avoid; }
    .campus { page-break-after: always; }
    .campus:last-child { page-break-after: auto; }
    .th { color: #fff; font-weight: 700; text-align: center; padding: 5px 4px;
          border: 1px solid #fff; font-size: 10px; line-height: 1.25; }
    .td { border: 1px solid #c9d2da; padding: 4px 6px; }
    .sr { border: 1px solid #c9d2da; padding: 4px; text-align: center; width: 34px; }
    .amt { border: 1px solid #c9d2da; padding: 4px 6px; text-align: center;
           white-space: nowrap; font-weight: 600; }
    .band { border: 1px solid #c9d2da; padding: 4px 6px; }
    .grade { border: 1px solid #c9d2da; padding: 4px; text-align: center;
             font-weight: 700; width: 46px; }
    .section { text-align: center; font-size: 13px; font-weight: 700;
               letter-spacing: .4px; color: #b03a2e; margin: 12px 0 5px; }
    .note { font-size: 9.5px; line-height: 1.5; margin-top: 6px; }
    .foot { margin-top: 10px; padding-top: 4px; border-top: 1px solid #999;
            display: flex; justify-content: space-between; font-size: 9px; color: #444; }")
              .Append("</style></head><body>");

            foreach (var campus in data.Campuses)
            {
                if (campus.Schedules.Count == 0) continue;
                AppendCampus(sb, data, campus, schoolName, schoolAddress, notes);
            }

            return sb.Append("</body></html>").ToString();
        }

        private static void AppendCampus(
            StringBuilder sb,
            BotFeeStructureDto data,
            BotFeeCampusDto campus,
            string schoolName,
            string schoolAddress,
            IReadOnlyList<string> notes)
        {
            sb.Append("<div class=\"campus\">");

            // ── Masthead ─────────────────────────────────────────────────
            sb.Append("<div style=\"text-align:center;margin-bottom:2px;\">")
              .Append("<div style=\"font-size:17px;font-weight:700;\">").Append(E(schoolName)).Append("</div>");
            if (!string.IsNullOrWhiteSpace(schoolAddress))
                sb.Append("<div style=\"font-size:10px;color:#444;\">").Append(E(schoolAddress)).Append("</div>");
            sb.Append("</div>");

            sb.Append("<div style=\"text-align:center;margin:8px 0 2px;\">")
              .Append("<div style=\"font-size:16px;font-weight:700;color:#1f3864;letter-spacing:.3px;\">")
              .Append("DUES DETAIL FOR THE ACADEMIC YEAR ").Append(E(data.SessionName.Replace("Year ", "")))
              .Append("</div>");

            // "For Girls Campus Class Play Group to 10" — the class range is the
            // widest any schedule on this campus covers, not a fixed string.
            var range = ClassRange(campus);
            sb.Append("<div style=\"font-size:12px;font-weight:700;color:#b03a2e;\">For ")
              .Append(E(campus.Name));
            if (range is not null) sb.Append(" &nbsp;&#8212;&nbsp; Classes ").Append(E(range));
            sb.Append("</div></div>");

            // One-off charges head the sheet, then the grade-wise schedule —
            // that is the headline monthly fee and the reason the parent asked.
            // Flat add-ons (the academy fee) follow it rather than pushing it
            // down the page just because they carry a lower fee-type id.
            var schedules = campus.Schedules
                .OrderBy(s => string.Equals(s.Frequency, "Once", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                .ThenByDescending(s => s.IsGradeWise)
                .ThenBy(s => s.FeeTypeId)
                .ToList();

            foreach (var schedule in schedules)
            {
                sb.Append("<div class=\"section\">SCHEDULE FOR ")
                  .Append(E(schedule.FeeTypeName.ToUpperInvariant())).Append("</div>");

                if (schedule.IsGradeWise) AppendGradeTable(sb, data, schedule);
                else AppendFlatTable(sb, schedule);
            }

            if (notes.Count > 0)
            {
                sb.Append("<div class=\"note\"><b style=\"color:#b03a2e;\">Note:</b><br>");
                for (var i = 0; i < notes.Count; i++)
                    sb.Append("&nbsp;&nbsp;(").Append(i + 1).Append(")&nbsp; ").Append(E(notes[i])).Append("<br>");
                sb.Append("</div>");
            }

            sb.Append("<div class=\"foot\"><span>Printed On: ")
              .Append(DateTime.Now.ToString("dd-MMM-yyyy hh:mm tt"))
              .Append("</span><span>EasySchool 0304-1003238</span></div>");

            sb.Append("</div>");
        }

        // ── One amount per class: admission fee, academy fee ─────────────
        private static void AppendFlatTable(StringBuilder sb, BotFeeScheduleDto schedule)
        {
            var groups = Merge(schedule.Classes);

            // "Once" means it is charged at admission and never again — worth
            // spelling out, because a parent reading a bare figure assumes monthly.
            var qualifier = string.Equals(schedule.Frequency, "Once", StringComparison.OrdinalIgnoreCase)
                ? "Only at the time of admission"
                : "Per month";

            sb.Append("<table><thead><tr>")
              .Append("<th class=\"th\" style=\"background:#7b8a99;width:38px;\">Sr.<br>No.</th>")
              .Append("<th class=\"th\" style=\"background:#4a7fb5;\">Classes</th>")
              .Append("<th class=\"th\" style=\"background:#c06c84;width:190px;\">")
              .Append(E(schedule.FeeTypeName)).Append(" <span style=\"font-weight:400;font-size:8.5px;\">(")
              .Append(E(qualifier)).Append(")</span></th>")
              .Append("</tr></thead><tbody>");

            var sr = 0;
            foreach (var g in groups)
            {
                sr++;
                sb.Append("<tr><td class=\"sr\">").Append(sr).Append("</td>")
                  .Append("<td class=\"td\">").Append(E(g.Label)).Append("</td>")
                  .Append("<td class=\"amt\"><span style=\"font-size:8.5px;font-weight:400;\">Rs.</span> ")
                  .Append(Money(g.Amounts.Values.FirstOrDefault())).Append("</td></tr>");
            }

            sb.Append("</tbody></table>");
        }

        // ── Grade x class matrix: the monthly fee ────────────────────────
        private static void AppendGradeTable(
            StringBuilder sb, BotFeeStructureDto data, BotFeeScheduleDto schedule)
        {
            var groups = Merge(schedule.Classes);

            sb.Append("<table><thead><tr>")
              .Append("<th class=\"th\" style=\"background:#7b8a99;width:34px;\">Sr.<br>No.</th>")
              .Append("<th class=\"th\" style=\"background:#4a7fb5;text-align:left;\">")
              .Append("Average Obtained Marks in all<br>internal Terms / Admission Test</th>")
              .Append("<th class=\"th\" style=\"background:#5aa0a0;width:46px;\">Grade</th>");

            for (var i = 0; i < groups.Count; i++)
                sb.Append("<th class=\"th\" style=\"background:").Append(ColumnColours[i % ColumnColours.Length])
                  .Append(";\">").Append(E(groups[i].Label))
                  .Append("<br><span style=\"font-weight:400;font-size:8.5px;\">Rs.</span></th>");

            sb.Append("</tr></thead><tbody>");

            var sr = 0;
            foreach (var grade in data.Grades)
            {
                // A grade nobody has priced is not part of this schedule.
                if (!groups.Any(g => g.Amounts.ContainsKey(grade.GradeId))) continue;

                sr++;
                sb.Append("<tr><td class=\"sr\">").Append(sr).Append("</td>")
                  .Append("<td class=\"band\">").Append(E(grade.Band)).Append("</td>")
                  .Append("<td class=\"grade\">").Append(E(grade.Name)).Append("</td>");

                foreach (var g in groups)
                    sb.Append("<td class=\"amt\">")
                      .Append(g.Amounts.TryGetValue(grade.GradeId, out var amount)
                          // A class group priced flat still belongs in the
                          // matrix — it just repeats down every grade row, which
                          // is exactly how the printed sheet shows Play Group.
                          ? Money(amount)
                          : g.Amounts.TryGetValue(0, out var flat) ? Money(flat) : "&ndash;")
                      .Append("</td>");

                sb.Append("</tr>");
            }

            sb.Append("</tbody></table>");
        }

        private sealed class ClassGroup
        {
            public string Label { get; set; } = string.Empty;
            public Dictionary<int, decimal> Amounts { get; set; } = new();
        }

        /// <summary>
        /// Collapses neighbouring classes that charge identically into a single
        /// row or column, the way the printed sheet writes "Nursery to 3".
        /// </summary>
        /// <remarks>
        /// Only NEIGHBOURING classes merge, and the list arrives in the school's
        /// own class order. Merging any two equal classes regardless of position
        /// would produce a label like "Play Group to 10" that skips the classes
        /// in between — true of the fee, but a lie about the range.
        /// </remarks>
        private static List<ClassGroup> Merge(List<BotFeeClassRowDto> classes)
        {
            var groups = new List<ClassGroup>();
            var run = new List<BotFeeClassRowDto>();

            void Flush()
            {
                if (run.Count == 0) return;
                groups.Add(new ClassGroup { Label = Label(run), Amounts = run[0].Amounts });
                run = new List<BotFeeClassRowDto>();
            }

            foreach (var c in classes)
            {
                if (run.Count > 0 && run[0].Signature != c.Signature) Flush();
                run.Add(c);
            }
            Flush();

            return groups;
        }

        private static string Label(List<BotFeeClassRowDto> run) => run.Count switch
        {
            1 => ClassLabel(run[0].ClassName),
            2 => $"{ClassLabel(run[0].ClassName)}, {ClassLabel(run[1].ClassName)}",
            _ => $"{ClassLabel(run[0].ClassName)} to {ClassLabel(run[^1].ClassName)}"
        };

        /// <summary>
        /// "4" reads as "4th" on the printed sheet.
        /// </summary>
        /// <remarks>
        /// Only a name that is a bare number is touched. "Montessori", "Prep"
        /// and "9 Junior" are the school's own wording and go through unchanged
        /// — guessing at those is how a fee sheet ends up saying "9 Juniorth".
        /// </remarks>
        private static string ClassLabel(string? name)
        {
            var text = (name ?? string.Empty).Trim();
            if (!int.TryParse(text, out var n) || n <= 0) return text;

            var suffix = n % 100 is >= 11 and <= 13
                ? "th"
                : (n % 10) switch { 1 => "st", 2 => "nd", 3 => "rd", _ => "th" };

            return text + suffix;
        }

        /// <summary>The widest class range priced on this campus, e.g. "Montessori to 10".</summary>
        private static string? ClassRange(BotFeeCampusDto campus)
        {
            var classes = campus.Schedules
                .SelectMany(s => s.Classes)
                .GroupBy(c => c.ClassId)
                .OrderBy(g => g.Key)
                .Select(g => ClassLabel(g.First().ClassName))
                .ToList();

            return classes.Count switch
            {
                0 => null,
                1 => classes[0],
                _ => $"{classes[0]} to {classes[^1]}"
            };
        }
    }
}
