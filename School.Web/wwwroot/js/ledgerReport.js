/* ---------------------------------------------------------------------------
   Account Ledger rendering — Student Ledger and Family Ledger.

   The API returns a signed running balance: positive means the account owes
   the school (Dr), negative means it has paid ahead (Cr). Everything here
   reads that one number, so the on-screen preview and the printed page can
   never disagree about which way a balance leans.

   The printed page deliberately mirrors the Paid / Unpaid fee reports
   (applyFeeReport.js -> buildReportHTML): same grey title block, black
   account bar, #999/#ccc table borders, #e8ecf0 headings, #f0f4f8 totals and
   the boxed grand total, on A4 landscape. Change one and change the other.
   --------------------------------------------------------------------------- */

// Hardcoded the same way the fee voucher prints are (Views/FeeVoucher/Print11
// and Reports/*.rdlc) — the Institutes table is empty and its API is commented
// out, so there is nothing to read this from yet. Change it here and in those
// files together.
const LEDGER_SCHOOL = {
    name: 'SIR SYED MODEL HIGH SCHOOL (Boys & Girls)',
    address: 'Chhonawala Road, Hasilpur  |  0333-6335634  |  0301-7330101'
};

// Product credit, printed centred at the foot of every sheet — the slot the
// school's previous ledger used for its vendor line.
const LEDGER_BRAND = 'EasySchool 0304-1003238';

// ── Shared cell styling, lifted from the Paid/Unpaid report ────────────────
const LG_TH    = 'border:1px solid #999;padding:4px 6px;text-align:center;background:#e8ecf0;vertical-align:middle;';
const LG_TD    = 'border:1px solid #ccc;padding:3px 6px;';
const LG_NUM   = 'border:1px solid #ccc;padding:3px 6px;text-align:right;white-space:nowrap;';
const LG_TOTAL = 'border:1px solid #ccc;padding:3px 6px;text-align:right;font-weight:700;background:#f0f4f8;';

function ledgerAmount(v) {
    const n = Number(v || 0);
    return n.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}

const LG_MONTHS = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun',
                   'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];

// dd-MMM-yyyy — built from the date's own parts so a calendar date can never
// slide a day in a timezone west of UTC.
function ledgerDate(value) {
    const d = parseCalendarDate(value);
    if (!d) return '';
    return `${String(d.getDate()).padStart(2, '0')}-${LG_MONTHS[d.getMonth()]}-${d.getFullYear()}`;
}

// hh:mm tt, 12-hour with a leading zero.
function ledgerClock(value) {
    const d = value instanceof Date ? value : new Date(value);
    if (isNaN(d)) return '';
    let h = d.getHours();
    const ampm = h < 12 ? 'AM' : 'PM';
    h = h % 12 || 12;
    return `${String(h).padStart(2, '0')}:${String(d.getMinutes()).padStart(2, '0')} ${ampm}`;
}

// dd-MMM-yyyy hh:mm tt for the ledger's Date column.
//
// The accounting date (SMSStudentFee.VDate / FeeVoucherMain.VDate) is a SQL
// `date` and carries no time of day, so the time comes from when the row was
// entered — and the procedure supplies it ONLY when that entry happened on the
// line's own date, which is the only case where it describes this line. Fee
// postings are generated in bulk (a year of 1st-of-month rows inserted in one
// run), so most correctly show the date alone rather than a fake 12:00 AM.
function ledgerDateTime(lineDate, entryTime) {
    const day = ledgerDate(lineDate);
    if (!day) return '';
    return entryTime ? `${day} ${entryTime}` : day;
}

function ledgerLongDate(value) {
    return ledgerDate(value);
}

// "04-Sep-2026 02:45 AM" for the printed-on stamp, which has a real time.
function ledgerStamp(d) {
    return `${ledgerDate(d)} ${ledgerClock(d)}`;
}

// A balance of exactly zero carries no side — matching the source ledger,
// which prints a bare "0.00" rather than "0.00Dr".
function ledgerBalance(v) {
    const n = Number(v || 0);
    if (Math.abs(n) < 0.005) return '0.00';
    return ledgerAmount(Math.abs(n)) + (n > 0 ? 'Dr' : 'Cr');
}

function ledgerEscape(s) {
    return String(s ?? '').replace(/[&<>"']/g, c =>
        ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
}

/* ── On-screen preview ──────────────────────────────────────────────────── */
function renderLedgerPreview(containerId, data, opts) {
    opts = opts || {};
    const showStudent = !!opts.showStudent;
    const el = document.getElementById(containerId);

    if (!data || !data.rows || data.rows.length === 0) {
        el.innerHTML = `<div class="text-center text-muted py-5">
            <i class="bi bi-journal-x" style="font-size:28px;"></i>
            <div class="mt-2" style="font-size:13px;">No entries for this account in the selected period.</div>
            ${data && Math.abs(Number(data.openingBalance || 0)) > 0.005
                ? `<div class="mt-1" style="font-size:12px;">Opening balance: <b>${ledgerBalance(data.openingBalance)}</b></div>` : ''}
        </div>`;
        return;
    }

    let kids = '';
    if (showStudent && data.students && data.students.length > 1) {
        kids = data.students.map(s => `
            <div class="col-md-4 col-sm-6">
                <div style="border:1px solid #e2e8f0;border-radius:8px;padding:8px 10px;background:#fff;">
                    <div style="font-weight:600;font-size:12px;">${ledgerEscape(s.studentName)}</div>
                    <div style="font-size:11px;color:#64748b;">
                        Dr ${ledgerAmount(s.debit)} &nbsp;|&nbsp; Cr ${ledgerAmount(s.credit)}
                        &nbsp;|&nbsp; <b>${ledgerBalance(s.balance)}</b>
                    </div>
                </div>
            </div>`).join('');
        kids = `<div class="row g-2 mb-3">${kids}</div>`;
    }

    const rows = data.rows.map(r => `
        <tr>
            <td class="text-nowrap">${ledgerDateTime(r.lineDate, r.entryTime)}</td>
            <td>${ledgerEscape(r.voucherType)}</td>
            <td class="text-center">${ledgerEscape(r.vNo)}</td>
            ${showStudent ? `<td>${ledgerEscape(r.studentName)}</td>` : ''}
            <td>${ledgerEscape(r.description)}</td>
            <td class="text-end">${ledgerAmount(r.debit)}</td>
            <td class="text-end">${ledgerAmount(r.credit)}</td>
            <td class="text-end fw-semibold">${ledgerBalance(r.balance)}</td>
        </tr>`).join('');

    const span = showStudent ? 5 : 4;

    el.innerHTML = `
        <div class="d-flex justify-content-between align-items-start flex-wrap gap-2 mb-2">
            <div>
                <div style="font-size:15px;font-weight:700;">${ledgerEscape(data.accountTitle)}
                    <span class="text-muted" style="font-weight:500;">${ledgerEscape(data.accountCode)}</span></div>
                <div class="text-muted" style="font-size:12px;">${ledgerEscape(data.accountInfo)}</div>
            </div>
            <div class="text-end" style="font-size:12px;">
                <div>Opening Balance: <b>${ledgerBalance(data.openingBalance)}</b></div>
                <div>Closing Balance: <b>${ledgerBalance(data.closingBalance)}</b></div>
            </div>
        </div>
        ${kids}
        <div class="table-responsive">
            <table class="table table-sm table-bordered align-middle mb-0" style="font-size:12px;">
                <thead class="table-light">
                    <tr>
                        <th style="width:150px;">Date</th>
                        <th style="width:120px;">Voucher Type</th>
                        <th style="width:100px;" class="text-center">Voucher No.</th>
                        ${showStudent ? '<th style="width:170px;">Student</th>' : ''}
                        <th>Description</th>
                        <th style="width:110px;" class="text-end">Debit</th>
                        <th style="width:110px;" class="text-end">Credit</th>
                        <th style="width:130px;" class="text-end">Balance</th>
                    </tr>
                </thead>
                <tbody>
                    <tr class="table-light">
                        <td colspan="${span}" class="fw-semibold">Opening Balance</td>
                        <td class="text-end">0.00</td>
                        <td class="text-end">0.00</td>
                        <td class="text-end fw-bold">${ledgerBalance(data.openingBalance)}</td>
                    </tr>
                    ${rows}
                </tbody>
                <tfoot class="table-light">
                    <tr>
                        <td colspan="${span}" class="text-end fw-bold">Total</td>
                        <td class="text-end fw-bold">${ledgerAmount(data.totalDebit)}</td>
                        <td class="text-end fw-bold">${ledgerAmount(data.totalCredit)}</td>
                        <td class="text-end fw-bold">${ledgerBalance(data.closingBalance)}</td>
                    </tr>
                </tfoot>
            </table>
        </div>`;
}

/* ── Printable page — same visual language as the Paid/Unpaid report ──────
   The sheet is paginated here rather than left to the browser, because "Page
   1 of 2" cannot be produced any other way: Chrome ignores @page margin boxes
   and CSS page counters, and its own header/footer option is off by default
   and not controllable from the page. So the rows are measured into fixed
   A4-landscape sheets and each one gets its own numbered footer.

   The unpaginated report is emitted first as #ledger-src and used as the
   source; it stays put if the paginator ever throws, so a layout bug can
   never lose the report.
   ------------------------------------------------------------------------ */
function buildLedgerPrintHTML(data, opts) {
    opts = opts || {};
    const showStudent = !!opts.showStudent;
    const title = opts.title || 'Account Ledger';
    const span = showStudent ? 5 : 4;

    // Kept as an array so the paginator can place rows one at a time.
    const rowsHtml = (data.rows || []).map(r => `
        <tr>
            <td style="${LG_TD}white-space:nowrap;">${ledgerDateTime(r.lineDate, r.entryTime)}</td>
            <td style="${LG_TD}">${ledgerEscape(r.voucherType)}</td>
            <td style="${LG_TD}text-align:center;">${ledgerEscape(r.vNo)}</td>
            ${showStudent ? `<td style="${LG_TD}">${ledgerEscape(r.studentName)}</td>` : ''}
            <td style="${LG_TD}">${ledgerEscape(r.description)}</td>
            <td style="${LG_NUM}">${ledgerAmount(r.debit)}</td>
            <td style="${LG_NUM}">${ledgerAmount(r.credit)}</td>
            <td style="${LG_NUM}font-weight:600;">${ledgerBalance(r.balance)}</td>
        </tr>`);

    const openingRow = `
        <tr>
            <td colspan="${span}" style="${LG_TD}font-weight:700;background:#f0f4f8;">Opening Balance</td>
            <td style="${LG_TOTAL}">0.00</td>
            <td style="${LG_TOTAL}">0.00</td>
            <td style="${LG_TOTAL}">${ledgerBalance(data.openingBalance)}</td>
        </tr>`;

    const totalRow = `
        <tr>
            <td colspan="${span}" style="${LG_TOTAL}">Total</td>
            <td style="${LG_TOTAL}">${ledgerAmount(data.totalDebit)}</td>
            <td style="${LG_TOTAL}">${ledgerAmount(data.totalCredit)}</td>
            <td style="${LG_TOTAL}">${ledgerBalance(data.closingBalance)}</td>
        </tr>`;

    const allRows = [openingRow].concat(rowsHtml).concat([totalRow]);

    const theadHtml = `
        <tr>
            <th style="${LG_TH}width:135px;">Date</th>
            <th style="${LG_TH}width:110px;">Voucher Type</th>
            <th style="${LG_TH}width:90px;">Voucher No.</th>
            ${showStudent ? `<th style="${LG_TH}width:160px;">Student</th>` : ''}
            <th style="${LG_TH}">Description</th>
            <th style="${LG_TH}width:110px;">Debit</th>
            <th style="${LG_TH}width:110px;">Credit</th>
            <th style="${LG_TH}width:130px;">Balance</th>
        </tr>`;

    const accountLine = `${ledgerEscape(data.accountTitle)}`
        + (data.accountCode ? `&nbsp; &nbsp;${ledgerEscape(data.accountCode)}` : '');

    // Repeated at the top of every sheet, the way the school's own ledger does.
    const headerHtml = `
    <div style="text-align:center;margin-bottom:6px;">
        <div style="font-size:15px;font-weight:700;">${ledgerEscape(LEDGER_SCHOOL.name)}</div>
        <div style="font-size:10px;">${ledgerEscape(LEDGER_SCHOOL.address)}</div>
    </div>

    <div style="text-align:center;margin-bottom:10px;">
        <div style="background:#d8d8d8;border:1px solid #aaa;padding:6px;font-size:14px;font-weight:700;letter-spacing:.5px;">
            ${ledgerEscape(title)}
        </div>
        <div style="background:#e4e4e4;border:1px solid #aaa;border-top:none;padding:5px;font-size:12px;">
            Account Ledger From ( ${ledgerLongDate(data.dateFrom)} ) To ( ${ledgerLongDate(data.dateTo)} )
        </div>
    </div>

    <div style="background:#000;color:#fff;padding:5px 10px;font-size:13px;font-weight:700;text-align:center;">
        ${showStudent ? 'Family' : 'Student'} : ${accountLine}
    </div>
    ${data.accountInfo
        ? `<div style="border:1px solid #ccc;border-top:none;padding:3px 10px;font-size:10px;text-align:center;background:#fafafa;">${ledgerEscape(data.accountInfo)}</div>`
        : ''}`;

    // Per-child subtotals only earn their space when there is more than one.
    let kidsBlock = '';
    if (showStudent && data.students && data.students.length > 1) {
        kidsBlock = `
        <div style="margin-bottom:14px;">
            <div style="background:#000;color:#fff;padding:5px 10px;font-size:13px;font-weight:700;text-align:center;">
                Students in this Family
            </div>
            <table style="width:100%;border-collapse:collapse;font-size:11px;">
                <thead>
                    <tr>
                        <th style="${LG_TH}text-align:left;">Student</th>
                        <th style="${LG_TH}width:130px;">Debit</th>
                        <th style="${LG_TH}width:130px;">Credit</th>
                        <th style="${LG_TH}width:150px;">Balance</th>
                    </tr>
                </thead>
                <tbody>
                    ${data.students.map(s => `
                    <tr>
                        <td style="${LG_TD}font-weight:600;">${ledgerEscape(s.studentName)}</td>
                        <td style="${LG_NUM}">${ledgerAmount(s.debit)}</td>
                        <td style="${LG_NUM}">${ledgerAmount(s.credit)}</td>
                        <td style="${LG_NUM}font-weight:600;">${ledgerBalance(s.balance)}</td>
                    </tr>`).join('')}
                </tbody>
            </table>
        </div>`;
    }

    const tailHtml = `
    ${kidsBlock}
    <div style="text-align:right;margin-top:6px;">
        <span style="font-size:12px;font-weight:600;margin-right:8px;">Closing Balance</span>
        <span style="border:2px solid #000;padding:4px 18px;font-size:13px;font-weight:700;">${ledgerBalance(data.closingBalance)}</span>
    </div>`;

    const printedOn = `Printed On: ${ledgerStamp(new Date())}`;

    const tableHtml = rows => `
        <div style="margin-bottom:14px;">
            <table><thead>${theadHtml}</thead><tbody>${rows}</tbody></table>
        </div>`;

    // The flat report: the paginator's source, and the fallback if it fails.
    const flatHtml = headerHtml + tableHtml(allRows.join('')) + tailHtml;

    // Escaping "<" keeps a stray "</script>" inside the data from closing the
    // block early; JSON is still valid with the escape.
    const payload = JSON.stringify({
        header: headerHtml,
        thead: theadHtml,
        rows: allRows,
        tail: tailHtml,
        printedOn: printedOn,
        brand: LEDGER_BRAND
    }).replace(/</g, '\\u003c');

    return `<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="UTF-8">
<title>${ledgerEscape(title)} - ${ledgerEscape(data.accountTitle)}</title>
<style>
    /* Print the fills instead of dropping them. Chrome paints background
       colours on paper only when the user ticks "Background graphics", so
       without this the grey title bands and the totals row come out blank and
       the white-on-black account bar prints white on white — invisible. */
    * {
        box-sizing: border-box;
        margin: 0;
        padding: 0;
        -webkit-print-color-adjust: exact;
        print-color-adjust: exact;
    }
    body {
        font-family: Arial, sans-serif;
        font-size: 11px;
        color: #000;
        background: #fff;
    }
    @page {
        size: A4 landscape;
        margin: 8mm 10mm;
    }
    table { border-collapse: collapse; width: 100%; }
    th, td { font-size: 11px; }

    /* One .sheet is one printed page. Fixed height is what lets the rows be
       measured into pages, which is what makes "Page 1 of 2" possible.
       277mm x 194mm is A4 landscape less the @page margins. */
    .sheet {
        width: 277mm;
        height: 194mm;
        display: flex;
        flex-direction: column;
        overflow: hidden;
        page-break-after: always;
        break-after: page;
    }
    .sheet:last-of-type { page-break-after: auto; break-after: auto; }
    /* min-height:0 is load-bearing. A flex item defaults to min-height:auto,
       which lets it grow past the sheet instead of being capped by it — and a
       body that grows never reports scrollHeight > clientHeight, so the
       paginator would see no overflow and pile every row onto page one. */
    .sheet-body { flex: 1 1 auto; min-height: 0; overflow: hidden; }
    .sheet-foot { flex: 0 0 auto; padding-top: 6px; }

    .foot-rule {
        display: flex;
        justify-content: space-between;
        border-top: 1px solid #999;
        padding-top: 4px;
        font-size: 10px;
    }
    .brand { text-align: center; font-size: 9px; padding-top: 3px; }

    @media screen {
        body { background: #eceff1; padding: 10px 14px; }
        .sheet { background: #fff; margin: 0 auto 14px; box-shadow: 0 0 6px rgba(0,0,0,.28); }
        #ledger-src { background: #fff; padding: 10px 14px; }
    }
    @media print {
        .no-print { display: none !important; }
        thead { display: table-header-group; }
        tr { page-break-inside: avoid; }
    }
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

    <div id="ledger-pages"></div>
    <div id="ledger-src">${flatHtml}</div>

<script id="ledger-data" type="application/json">${payload}</script>
<script>
function __paginateLedger() {
    var host = document.getElementById('ledger-pages');
    var src  = document.getElementById('ledger-src');
    try {
        var D = JSON.parse(document.getElementById('ledger-data').textContent);
        var sheets = [];

        // The footer is built with the sheet, not stamped on at the end: an
        // empty .sheet-foot leaves the body ~29px taller than it will finally
        // be, so rows would be packed to fill space the footer then takes back,
        // clipping the last row of every page. Only the page number is filled
        // in later, and that does not change the footer's height.
        function addSheet(withTable) {
            var s = document.createElement('div');
            s.className = 'sheet';
            s.innerHTML =
                '<div class="sheet-body">' + D.header +
                (withTable
                    ? '<div style="margin-bottom:14px;"><table><thead>' + D.thead +
                      '</thead><tbody></tbody></table></div>'
                    : '') +
                '</div>' +
                '<div class="sheet-foot">' +
                    '<div class="foot-rule">' +
                        '<span>' + D.printedOn + '</span>' +
                        '<span class="pageno">Page</span>' +
                    '</div>' +
                    '<div class="brand">' + D.brand + '</div>' +
                '</div>';
            host.appendChild(s);
            sheets.push(s);
            return s;
        }

        var sheet = addSheet(true);
        var body  = sheet.querySelector('.sheet-body');
        var tbody = sheet.querySelector('tbody');
        var overflows = function () { return body.scrollHeight > body.clientHeight; };

        D.rows.forEach(function (rowHtml) {
            tbody.insertAdjacentHTML('beforeend', rowHtml);
            if (overflows()) {
                var last = tbody.lastElementChild;
                // A single row taller than a whole page has nowhere better to
                // go; leave it rather than loop forever.
                if (tbody.children.length === 1) return;
                tbody.removeChild(last);
                sheet = addSheet(true);
                body  = sheet.querySelector('.sheet-body');
                tbody = sheet.querySelector('tbody');
                tbody.appendChild(last);
            }
        });

        var tail = document.createElement('div');
        tail.innerHTML = D.tail;
        body.appendChild(tail);
        if (overflows()) {
            body.removeChild(tail);
            sheet = addSheet(false);
            body  = sheet.querySelector('.sheet-body');
            body.appendChild(tail);
        }

        sheets.forEach(function (s, i) {
            s.querySelector('.pageno').textContent = 'Page ' + (i + 1) + ' of ' + sheets.length;
        });

        src.parentNode.removeChild(src);
    } catch (e) {
        // Never lose the report to a layout bug: keep the flat version and
        // give it the same footer, minus the page numbers it cannot know.
        if (host) host.innerHTML = '';
        if (src) {
            var f = document.createElement('div');
            f.className = 'sheet-foot';
            f.innerHTML = '<div class="foot-rule"><span>Printed On</span><span></span></div>' +
                          '<div class="brand">' + ${JSON.stringify(LEDGER_BRAND)} + '</div>';
            src.appendChild(f);
        }
    }
    window.__ledgerPaginated = true;
}

// Run once layout has settled. Measuring during parse gave row heights that
// were a few pixels short, so a page that actually overflowed by 12px looked
// like it fitted and every row landed on sheet one.
if (document.readyState === 'complete') __paginateLedger();
else window.addEventListener('load', __paginateLedger);
</script>
</body>
</html>`;
}

function openLedgerPrint(data, opts) {
    const win = window.open('', '_blank', 'width=1200,height=800');
    if (!win) { toastError('Allow pop-ups for this site to print the ledger.'); return; }
    win.document.write(buildLedgerPrintHTML(data, opts));
    win.document.close();
    win.focus();

    // Wait for the sheet to paginate before opening the print dialog —
    // printing mid-pagination would show unnumbered or half-built pages.
    // The cap means a failure still reaches the dialog rather than hanging.
    const deadline = Date.now() + 5000;
    (function whenReady() {
        if (win.closed) return;
        if (win.__ledgerPaginated || Date.now() > deadline) { win.print(); return; }
        setTimeout(whenReady, 50);
    })();
}
