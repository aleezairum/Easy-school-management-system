/* ============================================================================
   Staff salary slip - two to an A4 page, cut along the dashed line.

   Built from a stored payslip, so a slip reprinted later shows what was
   actually paid. Earnings and deductions add up to the payslip's own Gross
   and Total Deduction:
     gross      = basic + earning lines + the incentives (extra daily,
                  security return, extra duty, academy duty, prize)
     deductions = rule lines (late, early, out of school, duty cover...)
                  + pay deduction + fine + security deduction
   A duty-cover line can be negative - the person covered more than was
   covered for them - and is shown as such, a deduction that pays.
   Needs hrFilters.js.
   ============================================================================ */

const SLIP_MONTHS = ['January', 'February', 'March', 'April', 'May', 'June', 'July',
    'August', 'September', 'October', 'November', 'December'];

const SLIP_CSS = `
    @page { size: A4 portrait; margin: 7mm; }
    .slip { position: relative; height: 136mm; overflow: hidden; padding: 3mm 2mm; page-break-inside: avoid; break-inside: avoid; }
    /* A payroll not finalised (locked) yet: a tag in the corner and the words
       across the slip. Neither takes room, so the slip keeps its height. */
    .slip .pr-tag { position: absolute; top: 3mm; right: 2mm; border: 1.5px solid #c00; color: #c00; font-weight: 700;
                    font-size: 10px; padding: 1px 6px; background: #fff; }
    .slip .pr-mark { position: absolute; top: 45%; left: 0; right: 0; text-align: center; transform: rotate(-22deg);
                     font-size: 52px; font-weight: 700; color: rgba(204, 0, 0, .13); pointer-events: none; z-index: 5; }
    /* The second slip on a page sits under the cutting line, then a new page. */
    .slip:nth-of-type(2n) { border-top: 1.5px dashed #555; page-break-after: always; break-after: page; }
    .slip:last-of-type { page-break-after: auto; break-after: auto; }
    .sl-head { text-align: center; }
    .sl-head .n { font-size: 15px; font-weight: 700; }
    .sl-head .a { font-size: 10px; }
    .sl-head .t { margin: 4px auto 6px; display: inline-block; background: #000; color: #fff; font-weight: 700;
                  font-size: 12px; padding: 3px 18px; }
    .sl-info td { border: 1px solid #000; padding: 2px 5px; font-size: 10.5px; }
    .sl-info td.k { background: #f0f0f0; font-weight: 700; white-space: nowrap; }
    .sl-cols { display: flex; gap: 8px; margin-top: 6px; align-items: flex-start; }
    .sl-cols > table { flex: 1; }
    .sl-cols th, .sl-cols td { border: 1px solid #000; padding: 2px 5px; font-size: 10.5px; }
    .sl-cols th { background: #e8e8e8; }
    .sl-cols td.amt { text-align: right; white-space: nowrap; width: 32%; }
    .sl-cols tr.tot td { font-weight: 700; background: #f2f2f2; }
    .sl-cols td.sub { font-weight: 700; color: #11632f; background: #eaf6ee; font-size: 10px; padding: 1px 5px; }
    .sl-net { width: 100%; margin-top: 6px; border-collapse: collapse; }
    .sl-net td { border: 1px solid #000; padding: 3px 6px; font-size: 11px; }
    .sl-net td.k { background: #f0f0f0; font-weight: 700; }
    .sl-net td.v { text-align: right; font-weight: 700; }
    .sl-words { font-size: 10.5px; margin-top: 4px; }
    .sl-sign { display: flex; justify-content: space-between; margin-top: 26px; font-size: 10.5px; }
    .sl-sign div { border-top: 1px solid #000; width: 27%; text-align: center; padding-top: 2px; }
`;

function slipLine(name, amount, note) {
    return `<tr><td>${hrEsc(name)}${note ? ` <span style="font-size:9.5px;">(${note})</span>` : ''}</td>
                <td class="amt">${hrMoney(amount)}</td></tr>`;
}

function slipHtml(p) {
    const month = p.payMonth ? `${SLIP_MONTHS[p.payMonth - 1]} ${p.payYear}` : '';
    const lines = p.breakdown || [];

    // Earnings
    let earn = slipLine('Basic Salary', p.basic,
        Number(p.workingDays) && Number(p.workingDays) < Number(p.totalDays) ? `${p.workingDays} of ${p.totalDays} days` : '');
    lines.filter(b => b.kind === 1 && Number(b.amount)).forEach(b => earn += slipLine(b.name, b.amount));
    // The incentives, every one listed so the slip shows what was and was not
    // paid (StaffDocumentsHtmlBuilder.SalarySlip does the same for WhatsApp).
    earn += '<tr><td colspan="2" class="sub">Incentives</td></tr>';
    earn += slipLine('Extra Daily', p.extraDaysPay, Number(p.extraDays) ? `${p.extraDays} day(s) x ${hrMoney(p.dailyPay)}` : '');
    earn += slipLine('Security Return', p.securityReturn);
    earn += slipLine('Extra Duty', p.extraDuty);
    earn += slipLine('Academy Duty', p.academyDuty);
    earn += slipLine('Prize', p.prize);
    if (Number(p.incentive)) earn += slipLine('Incentive', p.incentive);

    // Deductions - the minutes behind each attendance charge are named.
    const minutesFor = name => /late/i.test(name) ? p.lateMinutes
        : /early/i.test(name) ? p.earlyMinutes
        : /short leave|out of school/i.test(name) ? p.outingMinutes : 0;
    let ded = '';
    lines.filter(b => b.kind === 2 && Number(b.amount)).forEach(b => {
        const m = minutesFor(b.name);
        ded += slipLine(b.name, b.amount, Number(b.amount) < 0 ? 'bonus' : m ? `${m} min` : '');
    });
    if (Number(p.payDeduction)) ded += slipLine('Pay Deduction', p.payDeduction);
    ded += slipLine('Fine', p.fine);
    if (Number(p.securityDeduction)) ded += slipLine('Security Deduction', p.securityDeduction);

    // RunStatus 1 = the payroll is still a Draft (see StaffDocumentsHtmlBuilder).
    const proof = Number(p.runStatus) === 1
        ? '<div class="pr-tag">PROOF READ ONLY &mdash; not final</div><div class="pr-mark">PROOF READ ONLY</div>' : '';

    return `<div class="slip">${proof}
        <div class="sl-head">
            <div class="n">${hrEsc(HR_SCHOOL_NAME)}</div>
            <div class="a">${HR_SCHOOL_ADDRESS}${p.campusName ? ' &nbsp;|&nbsp; ' + hrEsc(p.campusName) : ''}</div>
            <div class="t">SALARY SLIP &mdash; ${hrEsc(month)}</div>
        </div>
        <table class="sl-info" style="width:100%;border-collapse:collapse;">
            <tr><td class="k">Name</td><td>${hrEsc(p.employeeName)}</td>
                <td class="k">Code</td><td>${hrEsc(p.employeeCode)}</td>
                <td class="k">Designation</td><td>${hrEsc(p.designationName || '-')}</td></tr>
            <tr><td class="k">Days in Month</td><td>${p.totalDays || '-'}</td>
                <td class="k">Present</td><td>${p.presentDays}</td>
                <td class="k">Absent / Leave</td><td>${p.absentDays} / ${p.leaveDays}</td></tr>
            <tr><td class="k">Late (min)</td><td>${p.lateMinutes || 0}</td>
                <td class="k">Early (min)</td><td>${p.earlyMinutes || 0}</td>
                <td class="k">Out of School (min)</td><td>${p.outingMinutes || 0}</td></tr>
        </table>
        <div class="sl-cols">
            <table style="border-collapse:collapse;">
                <tr><th colspan="2">Earnings (Rs)</th></tr>${earn}
                <tr class="tot"><td>Gross Salary</td><td class="amt">${hrMoney(p.grossEarning)}</td></tr>
            </table>
            <table style="border-collapse:collapse;">
                <tr><th colspan="2">Deductions (Rs)</th></tr>${ded}
                <tr class="tot"><td>Total Deduction</td><td class="amt">${hrMoney(p.totalDeduction)}</td></tr>
            </table>
        </div>
        <table class="sl-net">
            <tr><td class="k">Net Salary</td><td class="v">${hrMoney(p.netPay)}</td>
                <td class="k">Advance</td><td class="v">${hrMoney(p.advance)}</td>
                <td class="k">Take Home</td><td class="v" style="font-size:12.5px;">Rs ${hrRupees(p.takeHome)}</td></tr>
        </table>
        <div class="sl-words"><strong>In words:</strong> ${slipWords(Math.round(Number(p.takeHome) || 0))} Rupees Only.
            ${p.paidDate ? `&nbsp; <strong>Paid:</strong> ${fmtDate(p.paidDate)}${p.paymentMethod ? ' (' + hrEsc(p.paymentMethod) + ')' : ''}` : ''}</div>
        <div class="sl-sign"><div>Accountant</div><div>Principal</div><div>Received by</div></div>
    </div>`;
}

/** Prints the payslips given, two to a page. */
function printSalarySlips(slips) {
    if (!slips || !slips.length) { toastWarning('No salary slip to print'); return; }
    hrPrintWindow('Salary Slips', '', '<div class="slips">' + slips.map(slipHtml).join('') + '</div>', { noHeading: true, css: SLIP_CSS });
}

// Whole rupees in words, the Pakistani way: thousand, lakh, crore.
function slipWords(n) {
    if (!n) return 'Zero';
    const ones = ['', 'One', 'Two', 'Three', 'Four', 'Five', 'Six', 'Seven', 'Eight', 'Nine', 'Ten', 'Eleven',
        'Twelve', 'Thirteen', 'Fourteen', 'Fifteen', 'Sixteen', 'Seventeen', 'Eighteen', 'Nineteen'];
    const tens = ['', '', 'Twenty', 'Thirty', 'Forty', 'Fifty', 'Sixty', 'Seventy', 'Eighty', 'Ninety'];
    const two = x => x < 20 ? ones[x] : tens[Math.floor(x / 10)] + (x % 10 ? ' ' + ones[x % 10] : '');
    const three = x => (x >= 100 ? ones[Math.floor(x / 100)] + ' Hundred' + (x % 100 ? ' ' : '') : '') + (x % 100 ? two(x % 100) : '');
    const parts = [];
    const crore = Math.floor(n / 10000000); n %= 10000000;
    const lakh = Math.floor(n / 100000); n %= 100000;
    const thousand = Math.floor(n / 1000); n %= 1000;
    if (crore) parts.push(three(crore) + ' Crore');
    if (lakh) parts.push(two(lakh) + ' Lakh');
    if (thousand) parts.push(two(thousand) + ' Thousand');
    if (n) parts.push(three(n));
    return parts.join(' ');
}
