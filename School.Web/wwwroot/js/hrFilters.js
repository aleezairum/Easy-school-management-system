/* ============================================================================
   HR report filters and printing, shared by every HR report:
   Staff Attendance Report, Attendance Card, Salary Slips, Staff Statement.

   Campus, Designation and Employee, with the employee list narrowed to the
   campus and designation picked - so choosing "Girls Campus" offers only the
   Girls Campus staff. The selectors default to #fCampus, #fDesignation and
   #fEmployee; a page with other ids passes them to hrInitFilters.
   ============================================================================ */

// The school's full name, exactly as on the WhatsApp documents. Plain text:
// escape it (hrEsc) wherever it goes into HTML - it carries an "&".
const HR_SCHOOL_NAME = 'SIR SYED MODEL HIGH SCHOOL (BOYS & GIRLS)';
const HR_SCHOOL_ADDRESS = 'Chhonawala Road, Hasilpur, District Bahawalpur';

let _hrSel = { campus: '#fCampus', designation: '#fDesignation', employee: '#fEmployee' };
let _hrEmployees = [];

function hrEsc(v) {
    return String(v ?? '').replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
}

/** Rupees to the paisa, blank for nothing. */
function hrMoney(v, blankZero) {
    const n = Number(v) || 0;
    if (blankZero && !n) return '';
    return n.toLocaleString('en-PK', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}

/** Whole rupees, for salary slips and totals. */
function hrRupees(v) {
    return Math.round(Number(v) || 0).toLocaleString('en-PK');
}

function hrTime(t) { return t ? String(t).substring(0, 5) : ''; }

async function hrInitFilters(selectors) {
    _hrSel = Object.assign({}, _hrSel, selectors || {});
    const [campuses, designations, employees] = await Promise.all([
        fetch(`${API_BASE}/campus`).then(r => r.ok ? r.json() : []).catch(() => []),
        fetch(`${API_BASE}/Designation/dropdown`).then(r => r.ok ? r.json() : []).catch(() => []),
        fetch(`${API_BASE}/Employee/dropdown`).then(r => r.ok ? r.json() : []).catch(() => [])
    ]);
    campuses.forEach(c => $(_hrSel.campus).append(`<option value="${c.vid}">${hrEsc(c.vName)}</option>`));
    designations.forEach(d => $(_hrSel.designation).append(`<option value="${d.id}">${hrEsc(d.name)}</option>`));
    _hrEmployees = employees;
    hrFillEmployees();
    $(_hrSel.campus + ',' + _hrSel.designation).on('change', hrFillEmployees);
}

// The employee list follows the campus and designation picked, keeping the
// chosen employee when they are still in it.
function hrFillEmployees() {
    const $emp = $(_hrSel.employee);
    if (!$emp.length) return;
    const campus = parseInt($(_hrSel.campus).val()) || null;
    const desig = parseInt($(_hrSel.designation).val()) || null;
    const current = $emp.val();
    const list = _hrEmployees.filter(e => (!campus || e.campusId === campus) && (!desig || e.designationId === desig));
    $emp.find('option:not(:first)').remove();
    list.forEach(e => $emp.append(`<option value="${e.id}">${hrEsc(e.fullName)} (${hrEsc(e.employeeCode)})</option>`));
    $emp.val(list.some(e => String(e.id) === current) ? current : '');
}

/** "&campusId=..&designationId=..&employeeId=.." for whatever is picked. */
function hrFilterQuery() {
    let q = '';
    const c = $(_hrSel.campus).val(), d = $(_hrSel.designation).val(), e = $(_hrSel.employee).val();
    if (c) q += `&campusId=${c}`;
    if (d) q += `&designationId=${d}`;
    if (e) q += `&employeeId=${e}`;
    return q;
}

/** The filters in words, for a printed heading. Empty when nothing is picked. */
function hrFilterCaption() {
    const parts = [];
    const pick = (sel, label) => {
        if ($(sel).val()) parts.push(`${label}: ${$(sel).find('option:selected').text()}`);
    };
    pick(_hrSel.campus, 'Campus');
    pick(_hrSel.designation, 'Designation');
    pick(_hrSel.employee, 'Employee');
    return parts.join('  |  ');
}

function hrPeriodCaption(from, to) {
    if (!from && !to) return '';
    if (from === to) return fmtDate(from);
    return `${fmtDate(from)} to ${fmtDate(to)}`;
}

/**
 * Opens a print window: the school heading, a title band, a caption line,
 * then the body. `landscape` turns the page.
 */
function hrPrintWindow(title, caption, bodyHtml, opts) {
    opts = opts || {};
    const win = window.open('', '_blank', 'width=1250,height=850');
    if (!win) { toastError('Allow pop-ups for this site to print'); return; }
    win.document.write(`<!doctype html><html><head><meta charset="utf-8"><title>${hrEsc(title)}</title>
<style>
    @page { size: A4 ${opts.landscape ? 'landscape' : 'portrait'}; margin: 8mm; }
    * { box-sizing: border-box; }
    body { font-family: Arial, Helvetica, sans-serif; color: #000; margin: 0; font-size: 11px;
           -webkit-print-color-adjust: exact; print-color-adjust: exact; }
    .hr-head { text-align: center; margin-bottom: 6px; }
    .hr-head .n { font-size: 17px; font-weight: 700; }
    .hr-head .a { font-size: 11px; }
    .hr-head .t { margin-top: 5px; background: #000; color: #fff; font-weight: 700; font-size: 13px; padding: 4px; }
    .hr-head .c { margin-top: 4px; font-size: 11px; }
    table { width: 100%; border-collapse: collapse; }
    th, td { border: 1px solid #000; padding: 3px 4px; vertical-align: middle; }
    th { background: #e8e8e8; font-weight: 700; }
    tfoot td { font-weight: 700; background: #f2f2f2; }
    thead { display: table-header-group; }
    tr { page-break-inside: avoid; }
    .num { text-align: right; } .ctr { text-align: center; }
    .toolbar { text-align: center; margin: 8px 0 12px; }
    .toolbar button { padding: 7px 20px; border: 0; border-radius: 5px; color: #fff; background: #1a7a3e; font-weight: 600; cursor: pointer; }
    .toolbar button.close { background: #555; margin-left: 8px; }
    @media print { .toolbar { display: none; } }
    ${opts.css || ''}
</style></head><body>
<div class="toolbar"><button onclick="window.print()">Print</button><button class="close" onclick="window.close()">Close</button></div>
${opts.noHeading ? '' : hrHeadingHtml(title, caption)}
${bodyHtml}
</body></html>`);
    win.document.close();
    win.focus();
    setTimeout(() => { try { win.print(); } catch (e) { } }, 600);
}

function hrHeadingHtml(title, caption) {
    return `<div class="hr-head">
        <div class="n">${hrEsc(HR_SCHOOL_NAME)}</div>
        <div class="a">${HR_SCHOOL_ADDRESS}</div>
        <div class="t">${hrEsc(title)}</div>
        ${caption ? `<div class="c">${hrEsc(caption)}</div>` : ''}
    </div>`;
}
