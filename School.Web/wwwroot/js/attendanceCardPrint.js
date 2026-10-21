/* ============================================================================
   Staff monthly attendance card - one employee per A4 page.

   Every day of the period is on the card, marked or not: holidays, weekly
   offs and days nobody marked are shown as such, so a gap in the register is
   visible on paper. The deductions are what payroll charges for the late,
   early and out-of-school minutes (see HrReportsService on the API).
   Needs hrFilters.js (heading, money and escaping helpers).
   ============================================================================ */

const AC_DAYS = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];

const AC_CSS = `
    .ac-card { page-break-after: always; break-after: page; }
    .ac-card:last-child { page-break-after: auto; break-after: auto; }
    .ac-info { width: 100%; border-collapse: collapse; margin-bottom: 6px; }
    .ac-info td { border: 1px solid #000; padding: 3px 6px; font-size: 11px; }
    .ac-info td.k { background: #f0f0f0; font-weight: 700; width: 14%; }
    .ac-days th, .ac-days td { font-size: 10px; padding: 2px 4px; text-align: center; border: 1px solid #000; }
    .ac-days td.rem { text-align: left; }
    .ac-days tr.off td { background: #eeeeee; color: #444; }
    .ac-days tr.abs td.st { color: #b00000; font-weight: 700; }
    .ac-days tr.nm td.st { color: #b35c00; font-weight: 700; }
    .ac-days td.ded { text-align: right; }
    .ac-sum { width: 100%; border-collapse: collapse; margin-top: 6px; }
    .ac-sum td, .ac-sum th { border: 1px solid #000; padding: 3px 5px; font-size: 10.5px; text-align: center; }
    .ac-sum th { background: #e8e8e8; }
    .ac-sign { display: flex; justify-content: space-between; margin-top: 34px; font-size: 11px; }
    .ac-sign div { border-top: 1px solid #000; width: 28%; text-align: center; padding-top: 3px; }
`;

function acCardHtml(card, withHeading) {
    const period = card.from.substring(0, 10) === card.to.substring(0, 10)
        ? fmtDate(card.from) : `${fmtDate(card.from)} to ${fmtDate(card.to)}`;
    const t = card.totals || {};

    const rows = card.days.map(d => {
        const dt = new Date(d.date.substring(0, 10) + 'T00:00:00');
        const off = !d.status && (d.label === 'Holiday' || d.label === 'Weekly Off' || d.label === 'Not on staff');
        const cls = off ? 'off' : d.status === 2 ? 'abs' : d.label === 'Not marked' ? 'nm' : '';
        const label = d.label === 'Holiday' && d.holidayName ? `Holiday - ${hrEsc(d.holidayName)}` : hrEsc(d.label);
        const duty = d.dutyIn && !off ? `${hrTime(d.dutyIn)}-${hrTime(d.dutyOut)}` : '';
        const late = d.lateMinutes ? d.lateMinutes + (d.ignoreLate ? '*' : '') : '';
        return `<tr class="${cls}">
            <td>${String(dt.getDate()).padStart(2, '0')}</td>
            <td>${AC_DAYS[dt.getDay()]}</td>
            <td class="st">${label}</td>
            <td>${duty}</td>
            <td>${hrTime(d.timeIn)}</td>
            <td>${hrTime(d.timeOut)}</td>
            <td>${late}</td>
            <td>${d.earlyMinutes || ''}</td>
            <td>${d.outingMinutes || ''}</td>
            <td class="ded">${hrMoney(d.totalDeduction, true)}</td>
            <td class="rem">${hrEsc(d.remarks || '')}</td>
        </tr>`;
    }).join('');

    const excused = card.days.some(d => d.ignoreLate && d.lateMinutes);

    return `<div class="ac-card">
        ${withHeading ? hrHeadingHtml('Staff Attendance Card', period) : ''}
        <table class="ac-info">
            <tr><td class="k">Name</td><td>${hrEsc(card.employeeName)}</td>
                <td class="k">Code</td><td>${hrEsc(card.employeeCode)}</td></tr>
            <tr><td class="k">Designation</td><td>${hrEsc(card.designationName || '-')}</td>
                <td class="k">Campus</td><td>${hrEsc(card.campusName || '-')}</td></tr>
            <tr><td class="k">Period</td><td>${period}</td>
                <td class="k">Joining Date</td><td>${card.joiningDate ? fmtDate(card.joiningDate) : '-'}</td></tr>
        </table>
        <table class="ac-days" style="width:100%;border-collapse:collapse;">
            <thead><tr>
                <th style="width:5%">Date</th><th style="width:5%">Day</th><th style="width:20%">Status</th>
                <th style="width:12%">Duty Time</th><th style="width:7%">In</th><th style="width:7%">Out</th>
                <th style="width:7%">Late (min)</th><th style="width:7%">Early (min)</th><th style="width:7%">Out of School (min)</th>
                <th style="width:9%">Deduction (Rs)</th><th>Remarks</th>
            </tr></thead>
            <tbody>${rows}</tbody>
        </table>
        ${excused ? '<div style="font-size:9.5px;margin-top:2px;">* late arrival excused by the office - not deducted.</div>' : ''}
        ${t.noSalaryRecord ? '<div style="font-size:10px;margin-top:2px;color:#b00000;"><b>No salary record</b> - deductions cannot be worked out (payroll skips this employee). Add a salary structure on the Employee form.</div>' : ''}
        <table class="ac-sum">
            <tr><th>Present</th><th>Absent</th><th>Leave</th><th>Half Day</th><th>Short Leave</th><th>Exempt</th>
                <th>Holidays</th><th>Weekly Off</th><th>Not Marked</th></tr>
            <tr><td>${t.presentDays || 0}</td><td>${t.absentDays || 0}</td><td>${t.leaveDays || 0}</td><td>${t.halfDays || 0}</td>
                <td>${t.shortLeaveDays || 0}</td><td>${t.exemptDays || 0}</td><td>${card.holidayDays || 0}</td>
                <td>${card.offDays || 0}</td><td>${card.notMarkedDays || 0}</td></tr>
        </table>
        <table class="ac-sum">
            <tr><th></th><th>Days</th><th>Minutes</th><th>Deduction (Rs)</th></tr>
            <tr><th>Late Arrival</th><td>${t.lateCount || 0}</td><td>${t.lateMinutes || 0}</td><td>${hrMoney(t.lateDeduction)}</td></tr>
            <tr><th>Early Leaving</th><td>${t.earlyCount || 0}</td><td>${t.earlyMinutes || 0}</td><td>${hrMoney(t.earlyDeduction)}</td></tr>
            <tr><th>Out of School</th><td>${t.outingCount || 0}</td><td>${t.outingMinutes || 0}</td><td>${hrMoney(t.outingDeduction)}</td></tr>
            <tr><th>Total</th><td></td><td>${(t.lateMinutes || 0) + (t.earlyMinutes || 0) + (t.outingMinutes || 0)}</td>
                <td><strong>${hrMoney(t.totalDeduction)}</strong></td></tr>
        </table>
        <div class="ac-sign"><div>Employee</div><div>Incharge / Clerk</div><div>Principal</div></div>
    </div>`;
}

/** Prints the cards given - one per page. */
function printAttendanceCards(cards) {
    if (!cards || !cards.length) { toastWarning('No attendance card to print'); return; }
    hrPrintWindow('Staff Attendance Card', '', cards.map(c => acCardHtml(c, true)).join(''),
        { noHeading: true, css: AC_CSS + ' body{font-size:10.5px}' });
}
