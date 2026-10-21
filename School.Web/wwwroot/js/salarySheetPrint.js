/* The school's own salary sheet, printed.
 *
 * Twenty columns, in the school's own order and wording:
 *
 *   Sr.No | Name | Designation | Basic Pay | Total Days | Daily Pay |
 *   Working Days | No. of Leaves | Extra Days | Extra Days Pay |
 *   Incentive | Security Return | Prize |          (green: added to the pay)
 *   Pay Deduction | Fine | Security Deduction | Net Pay |
 *   Advance | Take home salary | Signature
 *
 * A separate file rather than inline in the view: Razor eats a bare @ inside a
 * .cshtml script block, and this is mostly HTML in template strings.
 *
 * Every figure comes off the PAYSLIP, which stored them when the run was
 * generated. A sheet reprinted next March therefore shows what March was paid,
 * not what the amounts have been changed to since.
 */

const SS_MONTHS = ['January', 'February', 'March', 'April', 'May', 'June',
                   'July', 'August', 'September', 'October', 'November', 'December'];

function ssMoney(v) {
    const n = Number(v) || 0;
    return n.toLocaleString('en-PK', { maximumFractionDigits: 0 });
}

// Daily pay is the one column the sheet carries to two decimals.
function ssMoney2(v) {
    return (Number(v) || 0).toLocaleString('en-PK', {
        minimumFractionDigits: 2, maximumFractionDigits: 2
    });
}

function ssEsc(v) {
    return (v ?? '').toString()
        .replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
}

async function printSalarySheet() {
    try {
        const [slips, runs] = await Promise.all([
            fetch(`${API_BASE}/hr/payroll/runs/${runId}`).then(r => r.ok ? r.json() : []),
            fetch(`${API_BASE}/hr/payroll/runs`).then(r => r.ok ? r.json() : [])
        ]);

        if (!slips.length) { toastWarning('Nothing to print in this run'); return; }

        const run = runs.find(r => r.id === runId) || {};
        const win = window.open('', '_blank', 'width=1300,height=850');
        win.document.write(buildSalarySheetHTML(slips, run));
        win.document.close();
        win.focus();
        setTimeout(() => { win.print(); }, 600);
    } catch (e) {
        toastError('Error building the sheet: ' + e.message);
    }
}

function buildSalarySheetHTML(slips, run) {
    const month = run.payMonth ? SS_MONTHS[run.payMonth - 1] : '';
    const year = run.payYear || '';

    const t = {
        basic: 0, dailyPay: 0, leaves: 0, extraDays: 0, extraDaysPay: 0, incentive: 0, securityReturn: 0,
        payDeduction: 0, prize: 0, fine: 0, securityDeduction: 0,
        netPay: 0, advance: 0, takeHome: 0
    };

    const cell = 'border:1px solid #000;padding:3px 5px;';
    // The columns that add to the pay are shaded green.
    const inc = 'background:#eaf6ee;';
    // Extra duty and academy duty (and any old-style incentive) in one column.
    const incentiveOf = p => (p.extraDuty || 0) + (p.academyDuty || 0) + (p.incentive || 0);
    let rows = '';

    slips.forEach((p, i) => {
        t.basic += p.basic || 0;
        t.dailyPay += p.dailyPay || 0;
        t.leaves += p.leaveDays || 0;
        t.extraDays += p.extraDays || 0;
        t.extraDaysPay += p.extraDaysPay || 0;
        t.incentive += incentiveOf(p);
        t.securityReturn += p.securityReturn || 0;
        t.payDeduction += p.payDeduction || 0;
        t.prize += p.prize || 0;
        t.fine += p.fine || 0;
        t.securityDeduction += p.securityDeduction || 0;
        t.netPay += p.netPay || 0;
        t.advance += p.advance || 0;
        t.takeHome += p.takeHome || 0;

        rows += `<tr>
            <td style="${cell}text-align:center;">${i + 1}</td>
            <td style="${cell}font-weight:600;">${ssEsc(p.employeeName)}</td>
            <td style="${cell}text-align:center;">${ssEsc(p.designationName || '-')}</td>
            <td style="${cell}text-align:right;">${ssMoney(p.basic)}</td>
            <td style="${cell}text-align:center;">${p.totalDays || 0}</td>
            <td style="${cell}text-align:right;">${ssMoney2(p.dailyPay)}</td>
            <td style="${cell}text-align:center;">${p.workingDays || 0}</td>
            <td style="${cell}text-align:center;">${p.leaveDays || 0}</td>
            <td style="${cell}text-align:center;">${p.extraDays || 0}</td>
            <td style="${cell}${inc}text-align:right;">${ssMoney(p.extraDaysPay)}</td>
            <td style="${cell}${inc}text-align:right;">${ssMoney(incentiveOf(p))}</td>
            <td style="${cell}${inc}text-align:right;">${ssMoney(p.securityReturn)}</td>
            <td style="${cell}${inc}text-align:right;">${ssMoney(p.prize)}</td>
            <td style="${cell}text-align:right;">${ssMoney(p.payDeduction)}</td>
            <td style="${cell}text-align:right;${(p.fine || 0) > 0 ? 'color:#b00;' : ''}">${ssMoney(p.fine)}</td>
            <td style="${cell}text-align:right;">${ssMoney(p.securityDeduction)}</td>
            <td style="${cell}text-align:right;font-weight:700;">${ssMoney(p.netPay)}</td>
            <td style="${cell}text-align:right;">${ssMoney(p.advance)}</td>
            <td style="${cell}text-align:right;font-weight:700;">${ssMoney(p.takeHome)}</td>
            <td style="${cell}"></td>
        </tr>`;
    });

    const tot = 'border:1px solid #000;padding:4px 5px;background:#f0f4f8;font-weight:700;';

    return `<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <title>Salary Sheet</title>
    <style>
        * { box-sizing: border-box; margin: 0; padding: 0; }
        body { font-family: Arial, sans-serif; font-size: 11px; color: #000; background: #fff; padding: 10px 14px; }
        @page { size: A4 landscape; margin: 6mm 8mm; }

        /* Chrome and Edge drop background colours when printing unless this
           says otherwise, and the header band would come out blank. */
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
            thead { display: table-header-group; }   /* headings repeat per page */
        }

        /* Percentages and a fixed layout, never pixels: twenty columns of
           pixel widths add up to more than the paper and the last one falls off
           the edge, taking the right-hand border with it. */
        table { border-collapse: collapse; width: 100%; table-layout: fixed; }
        th, td { font-size: 11px; word-wrap: break-word; overflow-wrap: break-word; }
        th { border: 1px solid #000; padding: 4px 2px; text-align: center; vertical-align: middle; font-size: 9.5px; word-break: normal; overflow-wrap: normal; hyphens: auto; }

        col.c-sr    { width: 3.0%; }
        col.c-name  { width: 9.0%; }
        col.c-desig { width: 6.0%; }
        col.c-basic { width: 5.5%; }
        col.c-days  { width: 4.5%; }
        col.c-daily { width: 5.0%; }
        col.c-wdays { width: 5.0%; }
        col.c-leave { width: 4.5%; }
        col.c-xd    { width: 4.0%; }
        col.c-xdpay { width: 5.0%; }
        col.c-inc   { width: 5.0%; }
        col.c-sret  { width: 5.0%; }
        col.c-pded  { width: 5.0%; }
        col.c-prize { width: 4.5%; }
        col.c-fine  { width: 4.5%; }
        col.c-sded  { width: 5.0%; }
        col.c-net   { width: 5.5%; }
        col.c-adv   { width: 5.0%; }
        col.c-take  { width: 5.5%; }
        col.c-sign  { width: 3.5%; }
    </style>
</head>
<body>
    <div class="no-print" style="text-align:right;padding:8px 0 12px;">
        <button onclick="window.print()"
                style="background:#1a7a3e;color:#fff;border:none;padding:8px 22px;border-radius:6px;font-size:13px;font-weight:600;cursor:pointer;">
            Print
        </button>
        <button onclick="window.close()"
                style="background:#555;color:#fff;border:none;padding:8px 16px;border-radius:6px;font-size:13px;cursor:pointer;margin-left:8px;">
            Close
        </button>
    </div>

    <div style="text-align:center;font-size:15px;font-weight:700;margin-bottom:4px;">
        SIR SYED MODEL HIGH SCHOOL (BOYS &amp; GIRLS) CHHONAWALA ROAD HASILPUR DISTRICT BAHAWALPUR.
    </div>
    <div style="display:flex;justify-content:space-between;font-size:12px;font-weight:600;margin-bottom:6px;">
        <span>SALARY OF THE STAFF FOR THE MONTH OF
              <u>&nbsp;${ssEsc(month)}&nbsp;</u> &nbsp; <u>&nbsp;${ssEsc(String(year))}&nbsp;</u>
              ${run.campusName ? `&nbsp; CAMPUS: <u>&nbsp;${ssEsc(run.campusName)}&nbsp;</u>` : ''}</span>
        <span>DATE: <u>&nbsp;${fmtDate(new Date())}&nbsp;</u></span>
    </div>

    <table>
        <colgroup>
            <col class="c-sr"><col class="c-name"><col class="c-desig"><col class="c-basic">
            <col class="c-days"><col class="c-daily"><col class="c-wdays"><col class="c-leave">
            <col class="c-xd"><col class="c-xdpay">
            <col class="c-inc"><col class="c-sret"><col class="c-prize"><col class="c-pded"><col class="c-fine">
            <col class="c-sded"><col class="c-net"><col class="c-adv"><col class="c-take">
            <col class="c-sign">
        </colgroup>
        <thead>
            <tr style="background:#f0f4f8;">
                <th>Sr.No.</th>
                <th>Name</th>
                <th>Designation</th>
                <th>Basic Pay (Rs.)</th>
                <th>Total Days</th>
                <th>Daily Pay (Rs.)</th>
                <th>Working Days</th>
                <th>No. of Leaves</th>
                <th>Extra Days</th>
                <th style="background:#cdebd6;">Extra Days Pay (Rs.)</th>
                <th style="background:#cdebd6;">Incentive (Rs.)<div style="font-weight:400;font-size:9px;">Extra + Academy Duty</div></th>
                <th style="background:#cdebd6;">Security Return</th>
                <th style="background:#cdebd6;">Prize (Rs.)</th>
                <th>Pay Deduction</th>
                <th>Fine (Rs.)</th>
                <th>Security Deduction (Rs.)</th>
                <th>Net Pay (Rs.)</th>
                <th>Advance (Rs.)</th>
                <th>Take home salary (Rs.)</th>
                <th>SIGN</th>
            </tr>
        </thead>
        <tbody>
            ${rows}
            <tr>
                <td colspan="3" style="${tot}text-align:center;">Total</td>
                <td style="${tot}text-align:right;">${ssMoney(t.basic)}</td>
                <td style="${tot}"></td>
                <td style="${tot}text-align:right;">${ssMoney(t.dailyPay)}</td>
                <td style="${tot}"></td>
                <td style="${tot}text-align:center;">${t.leaves}</td>
                <td style="${tot}text-align:center;">${t.extraDays}</td>
                <td style="${tot}background:#cdebd6;text-align:right;">${ssMoney(t.extraDaysPay)}</td>
                <td style="${tot}background:#cdebd6;text-align:right;">${ssMoney(t.incentive)}</td>
                <td style="${tot}background:#cdebd6;text-align:right;">${ssMoney(t.securityReturn)}</td>
                <td style="${tot}background:#cdebd6;text-align:right;">${ssMoney(t.prize)}</td>
                <td style="${tot}text-align:right;">${ssMoney(t.payDeduction)}</td>
                <td style="${tot}text-align:right;">${ssMoney(t.fine)}</td>
                <td style="${tot}text-align:right;">${ssMoney(t.securityDeduction)}</td>
                <td style="${tot}text-align:right;">${ssMoney(t.netPay)}</td>
                <td style="${tot}text-align:right;">${ssMoney(t.advance)}</td>
                <td style="${tot}text-align:right;">${ssMoney(t.takeHome)}</td>
                <td style="${tot}"></td>
            </tr>
        </tbody>
    </table>

    <div style="display:flex;justify-content:space-between;margin-top:26px;font-size:12px;">
        <span>Prepared By:________________</span>
        <span>Rechecked By:________________</span>
        <span>Head of Institution:________________</span>
    </div>
</body>
</html>`;
}
