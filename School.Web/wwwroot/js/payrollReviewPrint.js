/* Payroll Review - the printed sheet.
 *
 * A separate file rather than inline in the view, for the same reason as every
 * other print routine here: it is mostly HTML built in template strings, and
 * Razor eats a bare @ inside a .cshtml script block.
 *
 * Prints exactly what is on screen for the run being reviewed, plus the totals
 * and a column to sign against - a salary sheet is signed for when the money
 * changes hands.
 */

const PAYROLL_RUN_STATUS = { 1: 'Draft', 2: 'Locked', 3: 'Paid' };
const PAYROLL_MONTHS = ['January', 'February', 'March', 'April', 'May', 'June',
                        'July', 'August', 'September', 'October', 'November', 'December'];

function payrollMoney(v) {
    return (Number(v) || 0).toLocaleString('en-PK', {
        minimumFractionDigits: 0,
        maximumFractionDigits: 0
    });
}

// Everything added to the pay on top of the salary: extra daily, security
// return, extra duty, academy duty, prize (and any old-style incentive).
function payrollIncentive(p) {
    return ['extraDaysPay', 'securityReturn', 'extraDuty', 'academyDuty', 'prize', 'incentive']
        .reduce((t, k) => t + (Number(p[k]) || 0), 0);
}

function payrollEsc(v) {
    return (v ?? '').toString()
        .replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
}

async function printPayrollReview() {
    try {
        const [slips, runs] = await Promise.all([
            fetch(`${API_BASE}/hr/payroll/runs/${runId}`).then(r => r.ok ? r.json() : []),
            fetch(`${API_BASE}/hr/payroll/runs`).then(r => r.ok ? r.json() : [])
        ]);

        if (!slips.length) { toastWarning('Nothing to print in this run'); return; }

        const run = runs.find(r => r.id === runId) || {};
        const html = buildPayrollReviewHTML(slips, run);

        const win = window.open('', '_blank', 'width=1200,height=800');
        win.document.write(html);
        win.document.close();
        win.focus();
        setTimeout(() => { win.print(); }, 600);
    } catch (e) {
        toastError('Error building the sheet: ' + e.message);
    }
}

function buildPayrollReviewHTML(slips, run) {
    const period = run.payMonth
        ? `${PAYROLL_MONTHS[run.payMonth - 1]} ${run.payYear}`
        : 'This run';
    const status = PAYROLL_RUN_STATUS[run.status] || '';

    let rows = '';
    let tBasic = 0, tIncentive = 0, tGross = 0, tFine = 0, tDeduction = 0, tNet = 0, tLate = 0, tEarly = 0, tOut = 0;

    slips.forEach((p, i) => {
        const incentive = payrollIncentive(p);
        tBasic += Number(p.basic) || 0;
        tIncentive += incentive;
        tGross += Number(p.grossEarning) || 0;
        tFine += Number(p.fine) || 0;
        tDeduction += Number(p.totalDeduction) || 0;
        tNet += Number(p.netPay) || 0;
        tLate += Number(p.lateMinutes) || 0;
        tEarly += Number(p.earlyMinutes) || 0;
        tOut += Number(p.outingMinutes) || 0;

        const cell = 'border:1px solid #ccc;padding:3px 5px;';
        rows += `<tr>
            <td style="${cell}text-align:center;">${i + 1}</td>
            <td style="${cell}text-align:center;">${payrollEsc(p.employeeCode)}</td>
            <td style="${cell}font-weight:600;">${payrollEsc(p.employeeName)}</td>
            <td style="${cell}">${payrollEsc(p.designationName || '-')}</td>
            <td style="${cell}text-align:right;">${payrollMoney(p.basic)}</td>
            <td style="${cell}text-align:right;background:#eaf6ee;${incentive ? 'color:#11632f;font-weight:600;' : ''}">${payrollMoney(incentive)}</td>
            <td style="${cell}text-align:right;">${payrollMoney(p.grossEarning)}</td>
            <td style="${cell}text-align:right;${Number(p.fine) > 0 ? 'color:#b00;' : ''}">${payrollMoney(p.fine)}</td>
            <td style="${cell}text-align:right;${Number(p.totalDeduction) > 0 ? 'color:#b00;' : ''}">${payrollMoney(p.totalDeduction)}</td>
            <td style="${cell}text-align:right;font-weight:700;">${payrollMoney(p.netPay)}</td>
            <td style="${cell}text-align:center;">${p.presentDays}</td>
            <td style="${cell}text-align:center;">${p.absentDays}</td>
            <td style="${cell}text-align:center;">${p.lateMinutes || 0}</td>
            <td style="${cell}text-align:center;">${p.earlyMinutes || 0}</td>
            <td style="${cell}text-align:center;">${p.outingMinutes || 0}</td>
            <td style="${cell}"></td>
        </tr>`;
    });

    const tot = 'border:1px solid #ccc;padding:4px 5px;background:#f0f4f8;font-weight:700;';

    return `<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <title>Payroll Review</title>
    <style>
        * { box-sizing: border-box; margin: 0; padding: 0; }
        body { font-family: Arial, sans-serif; font-size: 11px; color: #000; background: #fff; padding: 10px 14px; }
        @page { size: A4 landscape; margin: 8mm 10mm; }

        /* Chrome and Edge drop background colours when printing unless this
           says otherwise, and the heading band would come out as white text
           on white paper. Outside @media print as well, because the preview
           paints from these rules too. */
        body, body * {
            -webkit-print-color-adjust: exact;
            print-color-adjust: exact;
        }

        @media print {
            body { padding: 0; }
            .no-print { display: none !important; }
            body, body * {
                -webkit-print-color-adjust: exact !important;
                print-color-adjust: exact !important;
            }
            thead { display: table-header-group; }   /* repeat the header on every page */
        }

        /* Percentages and a fixed layout, never pixel widths: pixel widths add
           up to more than the paper and the last column falls off the edge,
           taking the right-hand border with it. */
        table { border-collapse: collapse; width: 100%; table-layout: fixed; }
        th, td { font-size: 11px; word-wrap: break-word; overflow-wrap: break-word; }
        col.c-sr     { width: 3%; }
        col.c-code   { width: 6%; }
        col.c-name   { width: 12%; }
        col.c-desig  { width: 9%; }
        col.c-basic  { width: 6.5%; }
        col.c-inc    { width: 6.5%; }
        col.c-gross  { width: 7%; }
        col.c-fine   { width: 5.5%; }
        col.c-ded    { width: 7%; }
        col.c-net    { width: 7.5%; }
        col.c-pres   { width: 5%; }
        col.c-abs    { width: 5%; }
        col.c-late   { width: 5%; }
        col.c-sign   { width: 6%; }
    </style>
</head>
<body>
    <div class="no-print" style="text-align:right;padding:8px 0 14px;">
        <button onclick="window.print()"
                style="background:#1a7a3e;color:#fff;border:none;padding:8px 22px;border-radius:6px;font-size:13px;font-weight:600;cursor:pointer;">
            Print
        </button>
        <button onclick="window.close()"
                style="background:#555;color:#fff;border:none;padding:8px 16px;border-radius:6px;font-size:13px;cursor:pointer;margin-left:8px;">
            Close
        </button>
    </div>

    <div style="text-align:center;margin-bottom:10px;">
        <div style="font-size:16px;font-weight:700;letter-spacing:.3px;">${payrollEsc(typeof HR_SCHOOL_NAME !== 'undefined' ? HR_SCHOOL_NAME : 'SIR SYED MODEL HIGH SCHOOL (BOYS & GIRLS)')}</div>
        <div style="font-size:11px;margin-bottom:6px;">${payrollEsc(typeof HR_SCHOOL_ADDRESS !== 'undefined' ? HR_SCHOOL_ADDRESS : '')}</div>
        <div style="background:#d8d8d8;border:1px solid #aaa;padding:6px;font-size:14px;font-weight:700;letter-spacing:.5px;">
            Salary Sheet &mdash; ${payrollEsc(period)}${run.campusName ? ' &mdash; ' + payrollEsc(run.campusName) : ''}
        </div>
        <div style="background:#e4e4e4;border:1px solid #aaa;border-top:none;padding:5px;font-size:12px;">
            ${slips.length} employee(s)${status ? ' &nbsp;|&nbsp; ' + status : ''}
            &nbsp;|&nbsp; Printed ${fmtDate(new Date())}
        </div>
    </div>

    <table>
        <colgroup>
            <col class="c-sr"><col class="c-code"><col class="c-name"><col class="c-desig">
            <col class="c-basic"><col class="c-inc"><col class="c-gross"><col class="c-fine"><col class="c-ded"><col class="c-net">
            <col class="c-pres"><col class="c-abs"><col class="c-late"><col class="c-late"><col class="c-late"><col class="c-sign">
        </colgroup>
        <thead>
            <tr style="background:#f0f4f8;">
                <th style="border:1px solid #ccc;padding:4px 5px;">Sr</th>
                <th style="border:1px solid #ccc;padding:4px 5px;">Code</th>
                <th style="border:1px solid #ccc;padding:4px 5px;text-align:left;">Employee</th>
                <th style="border:1px solid #ccc;padding:4px 5px;text-align:left;">Designation</th>
                <th style="border:1px solid #ccc;padding:4px 5px;">Basic</th>
                <th style="border:1px solid #ccc;padding:4px 5px;background:#cdebd6;color:#11632f;">Incentive</th>
                <th style="border:1px solid #ccc;padding:4px 5px;">Gross</th>
                <th style="border:1px solid #ccc;padding:4px 5px;">Fine</th>
                <th style="border:1px solid #ccc;padding:4px 5px;">Deduction</th>
                <th style="border:1px solid #ccc;padding:4px 5px;">Net Pay</th>
                <th style="border:1px solid #ccc;padding:4px 5px;">Present</th>
                <th style="border:1px solid #ccc;padding:4px 5px;">Absent</th>
                <th style="border:1px solid #ccc;padding:4px 5px;">Late (min)</th>
                <th style="border:1px solid #ccc;padding:4px 5px;">Early (min)</th>
                <th style="border:1px solid #ccc;padding:4px 5px;">Out (min)</th>
                <th style="border:1px solid #ccc;padding:4px 5px;">Signature</th>
            </tr>
        </thead>
        <tbody>
            ${rows}
            <tr>
                <td colspan="4" style="${tot}text-align:right;">Total</td>
                <td style="${tot}text-align:right;">${payrollMoney(tBasic)}</td>
                <td style="${tot}text-align:right;background:#cdebd6;color:#11632f;">${payrollMoney(tIncentive)}</td>
                <td style="${tot}text-align:right;">${payrollMoney(tGross)}</td>
                <td style="${tot}text-align:right;">${payrollMoney(tFine)}</td>
                <td style="${tot}text-align:right;">${payrollMoney(tDeduction)}</td>
                <td style="${tot}text-align:right;">${payrollMoney(tNet)}</td>
                <td style="${tot}"></td>
                <td style="${tot}"></td>
                <td style="${tot}text-align:center;">${tLate}</td>
                <td style="${tot}text-align:center;">${tEarly}</td>
                <td style="${tot}text-align:center;">${tOut}</td>
                <td style="${tot}"></td>
            </tr>
        </tbody>
    </table>

    <div style="display:flex;justify-content:space-between;margin-top:14px;">
        <div style="font-size:11px;">
            <div style="margin-top:26px;">Prepared by ______________________</div>
        </div>
        <div style="font-size:11px;">
            <div style="margin-top:26px;">Checked by ______________________</div>
        </div>
        <div style="font-size:11px;">
            <div style="margin-top:26px;">Principal ______________________</div>
        </div>
        <div style="text-align:right;">
            <span style="font-size:12px;font-weight:600;margin-right:8px;">Total Net Pay</span>
            <span style="border:2px solid #000;padding:4px 18px;font-size:13px;font-weight:700;">Rs.${payrollMoney(tNet)}</span>
        </div>
    </div>
</body>
</html>`;
}
