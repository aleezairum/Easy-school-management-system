/* Students' monthly attendance register (Views/Attendance/Register.cshtml).
 *
 * A row per student, a column per day: P present, T late, A absent, L leave,
 * H holiday, S Sunday; a day the section's register was not taken is grey.
 * Totals: Present (late included), Absent, Leave, Late and the percentage of
 * the student's marked days they were present.
 *
 * A separate file rather than inline in the view: Razor eats a bare @ inside a
 * .cshtml script block, and this is mostly HTML in template strings.
 */

const REG_MONTHS = ['January', 'February', 'March', 'April', 'May', 'June',
                    'July', 'August', 'September', 'October', 'November', 'December'];
const REG_SCHOOL = 'SIR SYED MODEL HIGH SCHOOL (Boys &amp; Girls)';
const REG_ADDRESS = 'Chhonawala Road, Hasilpur';
let _registers = [];

function regEsc(v) {
    return (v ?? '').toString().replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
}

$(async function () {
    const now = new Date();
    $('#rMonth').val(`${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}`);
    const classes = await fetch(`${API_BASE}/class`).then(r => r.ok ? r.json() : []).catch(() => []);
    classes.forEach(c => $('#rClass').append(`<option value="${c.vid}">${regEsc(c.vName)}</option>`));
});

async function regLoadSections() {
    const classId = $('#rClass').val();
    $('#rSection').find('option:not(:first)').remove();
    if (!classId) return;
    const list = await fetch(`${API_BASE}/section/byClass/${classId}`).then(r => r.ok ? r.json() : []).catch(() => []);
    list.forEach(s => $('#rSection').append(`<option value="${s.vid}">${regEsc(s.vName)}</option>`));
}

async function regLoad() {
    const classId = $('#rClass').val(), month = $('#rMonth').val();
    if (!classId) { toastWarning('Choose a class'); return false; }
    if (!month) { toastWarning('Choose a month'); return false; }
    let url = `${API_BASE}/StdAttendence/register?classId=${classId}&month=${month}`;
    if ($('#rSection').val()) url += `&sectionId=${$('#rSection').val()}`;
    $('#regArea').html('<div class="text-center text-muted py-4"><span class="spinner-border spinner-border-sm"></span> Loading...</div>');
    const res = await fetch(url);
    const body = await res.json().catch(() => null);
    if (!res.ok) { toastError((body && body.message) || 'Could not load the register'); $('#regArea').empty(); return false; }
    _registers = body || [];
    $('#regArea').html(_registers.length ? _registers.map(r => regTable(r, false)).join('')
        : '<div class="text-center text-muted py-4">No sections for this class.</div>');
    return true;
}

function regMonthText(r) { return `${REG_MONTHS[r.month - 1]} ${r.year}`; }

// The day header and the cell for a day that is not a school day.
function regDayHead(d) {
    const cls = d.kind === 'school' ? '' : d.kind === 'holiday' ? ' hol' : ' off';
    return `<th class="d${cls}" title="${regEsc(d.holiday || d.weekday)}">${d.day}<small>${d.weekday.slice(0, 2)}</small></th>`;
}
function regCell(d, mark) {
    if (mark) return `<td class="d ${mark}">${mark}</td>`;
    if (d.kind === 'holiday') return '<td class="d hol">H</td>';
    if (d.kind === 'sunday') return '<td class="d off">S</td>';
    return `<td class="d${d.kind === 'school' ? '' : ' off'}"></td>`;
}

function regTable(r, forPrint) {
    const head = `<tr><th>Sr</th><th>Roll No</th><th class="l">Name</th><th class="l">Father Name</th>
        ${r.days.map(regDayHead).join('')}
        <th>P</th><th>A</th><th>L</th><th>T</th><th>%</th></tr>`;
    const rows = r.students.length ? r.students.map((s, i) => `<tr class="${s.left ? 'left' : ''}">
        <td>${i + 1}</td><td>${regEsc(s.rollNo)}</td>
        <td class="l name">${regEsc(s.name)}${s.left ? ' <span class="hint">(Left)</span>' : ''}</td>
        <td class="l">${regEsc(s.fatherName)}</td>
        ${r.days.map((d, k) => regCell(d, s.marks[k])).join('')}
        <td class="tot">${s.present}</td><td class="tot">${s.absent}</td><td class="tot">${s.leave}</td><td class="tot">${s.late}</td>
        <td class="tot">${s.percent == null ? '-' : Number(s.percent).toFixed(1)}</td></tr>`).join('')
        : `<tr><td colspan="${r.days.length + 9}" class="text-muted py-3">No students in this section.</td></tr>`;
    const title = `Class ${regEsc(r.className)} - ${regEsc(r.sectionName)}`;
    const info = `${regMonthText(r)} &nbsp;|&nbsp; ${r.students.length} students &nbsp;|&nbsp; register taken on ${r.schoolDays} day(s)`;
    if (forPrint) return { title, info, table: `<table class="reg"><thead>${head}</thead><tbody>${rows}</tbody></table>` };
    return `<div class="reg-block"><div class="reg-title">${title}<span class="hint">${info}</span></div>
        <div class="reg-wrap"><table class="reg"><thead>${head}</thead><tbody>${rows}</tbody></table></div></div>`;
}

// One A4 landscape page per section, in the school's register layout.
async function regPrint() {
    // Always what the filters say now, not what was loaded before they changed.
    if (!(await regLoad())) return;
    if (!_registers.length) { toastWarning('Nothing to print'); return; }
    const pages = _registers.map(r => {
        const t = regTable(r, true);
        return `<section class="page">
            <div class="school">${REG_SCHOOL}</div><div class="addr">${REG_ADDRESS}</div>
            <div class="title">MONTHLY ATTENDANCE REGISTER</div>
            <div class="meta"><b>${t.title}</b><span>${t.info}</span></div>
            ${t.table}
            <div class="foot"><span>P Present &nbsp; A Absent &nbsp; L Leave &nbsp; T Late &nbsp; H Holiday &nbsp; S Sunday &nbsp; % = present of the days marked</span>
                <span class="sign">Class Teacher</span><span class="sign">Principal</span></div>
        </section>`;
    }).join('');
    const win = window.open('', '_blank', 'width=1300,height=850');
    if (!win) { toastError('Allow pop-ups to print'); return; }
    win.document.write(`<!doctype html><html><head><meta charset="utf-8"><title>Monthly Attendance Register</title><style>
        @page { size: A4 landscape; margin: 6mm 7mm; }
        * { box-sizing: border-box; }
        body { font-family: Arial, sans-serif; margin: 0; color: #000; -webkit-print-color-adjust: exact; print-color-adjust: exact; }
        .page { page-break-after: always; }
        .page:last-child { page-break-after: auto; }
        .school { text-align: center; font-size: 17px; font-weight: 700; }
        .addr { text-align: center; font-size: 11px; margin-bottom: 3px; }
        .title { text-align: center; background: #000; color: #fff; font-weight: 700; font-size: 12px; padding: 3px; letter-spacing: .08em; }
        .meta { display: flex; justify-content: space-between; font-size: 11px; margin: 4px 0; }
        table.reg { width: 100%; border-collapse: collapse; table-layout: fixed; font-size: 8.5px; }
        table.reg th, table.reg td { border: 1px solid #555; padding: 1.5px 1px; text-align: center; overflow: hidden; white-space: nowrap; }
        table.reg thead th { background: #e5e5e5; font-size: 8px; }
        table.reg th.d small { display: block; font-weight: 400; font-size: 6.5px; }
        table.reg .l { text-align: left; padding-left: 3px; text-overflow: ellipsis; }
        table.reg .off { background: #ececec; color: #777; }
        table.reg .hol { background: #fdf0c8; }
        table.reg td.A { font-weight: 700; color: #c00; }
        table.reg td.L { font-weight: 700; color: #1d4ed8; }
        table.reg td.T { font-weight: 700; color: #b45309; }
        table.reg td.tot { font-weight: 700; background: #f3f3f3; }
        table.reg tr.left td { color: #888; }
        table.reg tr { page-break-inside: avoid; }
        .hint { font-size: 7px; }
        .foot { display: flex; justify-content: space-between; align-items: flex-end; font-size: 9px; margin-top: 18px; }
        .sign { border-top: 1px solid #000; padding-top: 2px; width: 150px; text-align: center; }
        .no-print { text-align: right; padding: 6px; }
        @media print { .no-print { display: none; } }
        col.c-sr { width: 2.2%; } col.c-roll { width: 3.6%; } col.c-name { width: 11%; } col.c-father { width: 9.5%; }
        col.c-tot { width: 2.3%; } col.c-pct { width: 3%; }
    </style></head><body><div class="no-print"><button onclick="window.print()">Print</button></div>${pages}</body></html>`);
    // fixed column widths: the names get the room, the days share the rest
    win.document.querySelectorAll('table.reg').forEach(t => {
        const days = t.querySelectorAll('thead th.d').length;
        const cg = win.document.createElement('colgroup');
        cg.innerHTML = '<col class="c-sr"><col class="c-roll"><col class="c-name"><col class="c-father">'
            + '<col>'.repeat(days) + '<col class="c-tot"><col class="c-tot"><col class="c-tot"><col class="c-tot"><col class="c-pct">';
        t.prepend(cg);
    });
    win.document.close();
    setTimeout(() => win.print(), 500);
}
