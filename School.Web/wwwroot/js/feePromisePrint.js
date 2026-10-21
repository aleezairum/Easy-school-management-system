/* Promise to Pay - the printed sheet.
 *
 * A separate file rather than inline in the view on purpose: a print routine is
 * mostly HTML built in template strings, and Razor eats a bare @ inside a .cshtml
 * script block. One missed @@ takes the whole page's script down, which is how
 * the staff-subjects print broke.
 *
 * WHAT IT PRINTS, AND WHY IT IS NOT THE GRID
 * The grid on screen shows one row per PROMISE, because each row has an Edit and
 * a Delete button and those belong to the promise. This sheet shows one row per
 * STUDENT the promise covers. A promise is taken against one child and holds for
 * every sibling of the same parent; a sheet listing only the child who was
 * written down would send the office chasing the other three. The API answers
 * that shape at /fee/promise/report - see Scripts/SpRPT_SMSFeePromise.sql.
 */

const PROMISE_STATUS_LABEL = {
    1: 'In force',
    2: 'Lapsed - still owing',
    3: 'Settled'
};

function promiseMoney(v) {
    return (Number(v) || 0).toLocaleString('en-PK', {
        minimumFractionDigits: 0,
        maximumFractionDigits: 0
    });
}

function promiseEsc(v) {
    return (v ?? '').toString()
        .replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
}

async function printPromises() {
    const status = $('#filterStatus').val() || 0;
    const campus = $('#filterCampus').val() || '';
    const cls = $('#filterClass').val() || '';

    let url = `${API_BASE}/fee/promise/report?status=${status}`;
    if (campus) url += `&campusIds=${campus}`;
    if (cls) url += `&classIds=${cls}`;

    try {
        const res = await fetch(url);
        if (!res.ok) { toastError('Could not build the report'); return; }

        const rows = await res.json();
        if (!rows.length) { toastWarning('Nothing to print for this filter'); return; }

        const html = buildPromiseReportHTML(rows, {
            status: $('#filterStatus option:selected').text(),
            campus: campus ? $('#filterCampus option:selected').text() : 'All campuses',
            cls: cls ? $('#filterClass option:selected').text() : 'All classes'
        });

        const win = window.open('', '_blank', 'width=1100,height=800');
        win.document.write(html);
        win.document.close();
        win.focus();
        setTimeout(() => { win.print(); }, 600);
    } catch (e) {
        toastError('Error building the report: ' + e.message);
    }
}

function buildPromiseReportHTML(rows, filters) {
    // A block per promise, the children it covers inside it.
    const blocks = new Map();
    rows.forEach(r => {
        if (!blocks.has(r.promiseID)) blocks.set(r.promiseID, []);
        blocks.get(r.promiseID).push(r);
    });

    let body = '';
    let grandTotal = 0;
    let childCount = 0;

    blocks.forEach(children => {
        const p = children[0];
        const label = PROMISE_STATUS_LABEL[p.promiseStatus] || '-';

        // The family's dues count ONCE per promise, however many children the
        // promise lists - the money is owed by the family, not by each child.
        grandTotal += (p.familyDues || 0);
        childCount += children.length;

        const when = fmtDate(p.promiseDate);
        const left = p.promiseStatus === 1
            ? (p.daysLeft === 0 ? 'due today' : p.daysLeft + ' day(s) left')
            : (p.promiseStatus === 2 ? Math.abs(p.daysLeft) + ' day(s) overdue' : '');

        let studentRows = '';
        children.forEach((c, i) => {
            studentRows += `<tr>
                <td style="border:1px solid #ccc;padding:3px 6px;text-align:center;">${i + 1}</td>
                <td style="border:1px solid #ccc;padding:3px 6px;text-align:center;">${promiseEsc(c.classRollNo || c.rollNo || '-')}</td>
                <td style="border:1px solid #ccc;padding:3px 6px;font-weight:600;">${promiseEsc(c.studentName)}${
                    c.studentName === p.promisedForName
                        ? ' <span style="font-weight:400;font-size:9px;">(promise recorded here)</span>'
                        : ''}</td>
                <td style="border:1px solid #ccc;padding:3px 6px;text-align:center;">${promiseEsc(c.className || '-')} ${promiseEsc(c.sectionName || '')}</td>
                <td style="border:1px solid #ccc;padding:3px 6px;">${promiseEsc(c.campusName || '-')}</td>
                <td style="border:1px solid #ccc;padding:3px 6px;">${promiseEsc(c.fatherName || '-')}</td>
                <td style="border:1px solid #ccc;padding:3px 6px;">${promiseEsc(c.contactNo || '-')}</td>
                <td style="border:1px solid #ccc;padding:3px 6px;text-align:right;">${promiseMoney(c.ownDues)}</td>
                <td style="border:1px solid #ccc;padding:3px 6px;"></td>
            </tr>`;
        });

        body += `
        <div class="promise-block" style="margin-bottom:18px;">
            <div style="background:#000;color:#fff;padding:5px 10px;font-size:12px;font-weight:700;display:flex;justify-content:space-between;-webkit-print-color-adjust:exact;print-color-adjust:exact;">
                <span>Promised by ${promiseEsc(p.promisedBy || 'the parent')} &nbsp;|&nbsp; by ${when}${left ? ' (' + left + ')' : ''}</span>
                <span>${label} &nbsp;|&nbsp; Family dues Rs.${promiseMoney(p.familyDues)}</span>
            </div>
            <div style="background:#efefef;border:1px solid #aaa;border-top:none;padding:4px 10px;font-size:10px;">
                Covers ${children.length} child(ren) of ${promiseEsc(p.fatherName || 'this family')}.
                Recorded against ${promiseEsc(p.promisedForName)}.
                ${p.promisedAmount ? 'Amount promised Rs.' + promiseMoney(p.promisedAmount) + '.' : 'Whole amount promised.'}
                ${p.remarks ? promiseEsc(p.remarks) : ''}
            </div>
            <table>
                <colgroup>
                    <col class="c-sr"><col class="c-roll"><col class="c-student">
                    <col class="c-class"><col class="c-campus"><col class="c-father">
                    <col class="c-contact"><col class="c-dues"><col class="c-sign">
                </colgroup>
                <thead>
                    <tr style="background:#f0f4f8;">
                        <th style="border:1px solid #ccc;padding:4px 6px;">Sr</th>
                        <th style="border:1px solid #ccc;padding:4px 6px;">Roll</th>
                        <th style="border:1px solid #ccc;padding:4px 6px;text-align:left;">Student</th>
                        <th style="border:1px solid #ccc;padding:4px 6px;">Class</th>
                        <th style="border:1px solid #ccc;padding:4px 6px;text-align:left;">Campus</th>
                        <th style="border:1px solid #ccc;padding:4px 6px;text-align:left;">Father</th>
                        <th style="border:1px solid #ccc;padding:4px 6px;">Contact</th>
                        <th style="border:1px solid #ccc;padding:4px 6px;">Own Dues</th>
                        <th style="border:1px solid #ccc;padding:4px 6px;">Received / Sign</th>
                    </tr>
                </thead>
                <tbody>
                    ${studentRows}
                    <tr>
                        <td colspan="7" style="border:1px solid #ccc;padding:3px 6px;text-align:right;font-weight:700;background:#f0f4f8;">Family total</td>
                        <td style="border:1px solid #ccc;padding:3px 6px;text-align:right;font-weight:700;background:#f0f4f8;">${promiseMoney(p.familyDues)}</td>
                        <td style="border:1px solid #ccc;background:#f0f4f8;"></td>
                    </tr>
                </tbody>
            </table>
        </div>`;
    });

    return `<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <title>Promise to Pay</title>
    <style>
        * { box-sizing: border-box; margin: 0; padding: 0; }
        body { font-family: Arial, sans-serif; font-size: 11px; color: #000; background: #fff; padding: 10px 14px; }
        @page { size: A4 portrait; margin: 8mm 10mm; }

        /* Chrome and Edge throw away background colours when they print,
           unless this says otherwise. Without it the black band came out as
           white text on white paper - the heading simply vanished, while the
           screen looked right. It has to sit outside @media print as well:
           the preview paints from these rules too. */
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
            /* Only a promise block is kept whole. This used to say "div", which
               told the browser not to break ANY box - including the one holding
               every block - and a long list then paginated badly. */
            .promise-block { page-break-inside: avoid; }
        }
        /* table-layout: fixed and percentage widths, NOT pixels.

           With pixel widths the seven fixed columns came to 580px and left the
           Student and Father columns to fight over what was left of an A4
           page. They lost: both wrapped onto two lines and the table ran off
           the right-hand edge, taking its right border with it. Percentages
           always add up to the paper, whatever the paper is. */
        table { border-collapse: collapse; width: 100%; table-layout: fixed; }
        th, td { font-size: 11px; word-wrap: break-word; overflow-wrap: break-word; }
        col.c-sr      { width: 4%; }
        col.c-roll    { width: 7%; }
        col.c-student { width: 21%; }
        col.c-class   { width: 10%; }
        col.c-campus  { width: 11%; }
        col.c-father  { width: 15%; }
        col.c-contact { width: 12%; }
        col.c-dues    { width: 9%; }
        col.c-sign    { width: 11%; }
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
        <div style="background:#d8d8d8;border:1px solid #aaa;padding:6px;font-size:14px;font-weight:700;letter-spacing:.5px;">
            Promise to Pay
        </div>
        <div style="background:#e4e4e4;border:1px solid #aaa;border-top:none;padding:5px;font-size:12px;">
            ${promiseEsc(filters.status)} &nbsp;|&nbsp; ${promiseEsc(filters.campus)} &nbsp;|&nbsp; ${promiseEsc(filters.cls)}
            &nbsp;|&nbsp; As at ${fmtDate(new Date())}
        </div>
        <div style="border:1px solid #aaa;border-top:none;padding:4px;font-size:10px;">
            A promise covers every child of that parent. Each one is listed below, so nobody is chased by mistake.
        </div>
    </div>

    ${body}

    <div style="text-align:right;margin-top:6px;">
        <span style="font-size:11px;margin-right:12px;">${blocks.size} promise(s), ${childCount} child(ren)</span>
        <span style="font-size:12px;font-weight:600;margin-right:8px;">Total owed</span>
        <span style="border:2px solid #000;padding:4px 18px;font-size:13px;font-weight:700;">Rs.${promiseMoney(grandTotal)}</span>
    </div>
</body>
</html>`;
}
