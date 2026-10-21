// Printing the time table from the Staff Subjects screen.
//
// Deliberately a plain .js file rather than script inside the Razor view: the
// sheet is built from a long HTML template, and in a .cshtml every '@' in that
// template (@page, @media) has to be written '@@' for Razor. One missed escape
// breaks the whole page script, which is exactly what happened. Here the text
// is the text.
//
// Called from Views/StaffSubjects/Index.cshtml, and uses these from it:
//   allRows  - the assignments already loaded by the page
//   val(), esc(), rid(), showToast()  - the page's own helpers
//   fmtDate() - from common.js

// ══ Printed time table ═════════════════════════════════════════════════
//
// Periods down the page, one column per class-section, each cell the
// subject and who takes it — the sheet the school already works from.
// Built from the assignments already loaded into `allRows`, so printing
// costs no extra round trip.

const PRINT_SCHOOL = 'SIR SYED MODEL HIGH SCHOOL (BOYS & GIRLS)';
const MAX_COLS_PER_PAGE = 8;   // beyond this a landscape page is unreadable

// Every class-section that has assignments in one exam, in the school's own
// class order (the id), then section.
function columnsFor(examId) {
    const map = new Map();
    allRows
        .filter(r => String(val(r, 'examId', 'ExamId') ?? '') === String(examId))
        .forEach(r => {
            const cId = Number(val(r, 'classId', 'ClassId')) || 0;
            const sId = Number(val(r, 'sectionId', 'SectionId')) || 0;
            const key = cId + '-' + sId;
            if (!map.has(key)) {
                map.set(key, {
                    key, classId: cId, sectionId: sId,
                    label: [val(r, 'className', 'ClassName'), val(r, 'sectionName', 'SectionName')]
                        .filter(Boolean).join('-')
                });
            }
        });

    return [...map.values()].sort((a, b) => a.classId - b.classId || a.sectionId - b.sectionId);
}

function fillPrintColumns() {
    const sel = $('#printClasses').empty();
    columnsFor($('#printExam').val()).forEach(c =>
        sel.append(`<option value="${c.key}">${esc(c.label)}</option>`));
}

function printSelectAll(select) {
    $('#printClasses option').prop('selected', !!select);
}

function printTimeTable() {
    const examId = String($('#printExam').val() || '');
    if (!examId) { showToast('Choose an exam / term to print', 'warning'); return; }

    const all = columnsFor(examId);
    const picked = ($('#printClasses').val() || []).map(String);
    const columns = picked.length ? all.filter(c => picked.includes(c.key)) : all;

    const columnKeys = new Set(columns.map(c => c.key));
    const rows = allRows.filter(r =>
        String(val(r, 'examId', 'ExamId') ?? '') === examId &&
        columnKeys.has((Number(val(r, 'classId', 'ClassId')) || 0) + '-' + (Number(val(r, 'sectionId', 'SectionId')) || 0)));

    if (rows.length === 0) {
        showToast('No assignments for that exam and those columns', 'warning');
        return;
    }

    // What is taught in each period, per column.
    //
    // A LIST per cell, not one entry: a period can legitimately carry two
    // (one subject split between two teachers), and it can carry two by
    // mistake (two different subjects booked at once). Keeping only the
    // last would hide both cases — the second is a clash the office needs
    // to see on the sheet, not a row quietly dropped.
    const cell = {};
    rows.forEach(r => {
        const lec = Number(val(r, 'lectureNo', 'LectureNo')) || 0;
        if (!lec) return;
        const key = (Number(val(r, 'classId', 'ClassId')) || 0) + '-' + (Number(val(r, 'sectionId', 'SectionId')) || 0);
        cell[lec] = cell[lec] || {};
        (cell[lec][key] = cell[lec][key] || []).push({
            subject: val(r, 'subjectName', 'SubjectName') || '',
            teacher: val(r, 'teacherName', 'TeacherName') || ''
        });
    });

    const lectures = [...new Set(rows.map(r => Number(val(r, 'lectureNo', 'LectureNo')) || 0))]
        .filter(n => n > 0).sort((a, b) => a - b);

    // Who is free in each period.
    //
    // Two different sets are at work here, and mixing them up makes the
    // column useless:
    //
    //   the POOL is the staff on THIS sheet — the teachers who take these
    //   classes. Listing all 67 employees would fill the column with people
    //   who never come to this building.
    //
    //   BUSY is measured across every class in the term, not just these
    //   columns. A teacher taking a class on another sheet is not spare,
    //   and calling her spare is how two classes are sent the same person.
    const pool = new Map();
    rows.forEach(r => {
        const id = String(val(r, 'teacherId', 'TeacherId') ?? '');
        const name = val(r, 'teacherName', 'TeacherName') || '';
        if (id && name) pool.set(id, name);
    });

    const busy = {};
    allRows.filter(r => String(val(r, 'examId', 'ExamId') ?? '') === examId).forEach(r => {
        const lec = Number(val(r, 'lectureNo', 'LectureNo')) || 0;
        if (!lec) return;
        (busy[lec] = busy[lec] || new Set()).add(String(val(r, 'teacherId', 'TeacherId') ?? ''));
    });

    const spareFor = lec => [...pool.entries()]
        .filter(([id]) => !(busy[lec] || new Set()).has(id))
        .map(([, name]) => name)
        .sort((a, b) => a.localeCompare(b));

    const heading = ($('#printHeading').val() || '').trim();
    const fromDate = $('#printFrom').val();
    const examName = $('#printExam option:selected').text().trim();

    const win = window.open('', '_blank', 'width=1200,height=800');
    win.document.write(buildTimeTableHtml({
        columns, lectures, cell, spareFor, heading, fromDate, examName
    }));
    win.document.close();
    win.focus();
}

function buildTimeTableHtml({ columns, lectures, cell, spareFor, heading, fromDate, examName }) {
    // Split into page-widths so a school with many sections still gets a
    // readable sheet; each page repeats the period numbers.
    const pages = [];
    for (let i = 0; i < columns.length; i += MAX_COLS_PER_PAGE)
        pages.push(columns.slice(i, i + MAX_COLS_PER_PAGE));

    const body = pages.map((cols, pageIndex) => {
        const head = cols.map(c => `<th>${esc(c.label)}</th>`).join('');

        const lines = lectures.map(lec => {
            const cells = cols.map(c => {
                const list = (cell[lec] || {})[c.key] || [];
                if (list.length === 0) return '<td></td>';

                // Two different subjects in one period is a clash, not a
                // split class — marked so it is seen rather than buried.
                const subjects = new Set(list.map(v => v.subject));
                const clash = list.length > 1 && subjects.size > 1;

                const inner = list.map(v =>
                    `<div class="subj">${esc(v.subject)}</div><div class="tchr">${esc(v.teacher)}</div>`).join('');

                return `<td class="${clash ? 'clash' : ''}">${inner}</td>`;
            }).join('');

            // Special Duty is left blank on purpose: it is written in by hand
            // on the day, which is how the school's own sheet works.
            const spare = pageIndex === 0 ? spareFor(lec) : [];
            return `<tr>
                        <td class="lec">${lec}</td>
                        ${cells}
                        <td class="duty"></td>
                        <td class="spare">${spare.map(esc).join('<br>')}</td>
                    </tr>`;
        }).join('');

        return `<div class="sheet">
                    <div class="hdr">
                        <div class="ttl">Time Table${heading ? ' ' + esc(heading) : ''}</div>
                        <div class="school">${esc(PRINT_SCHOOL)}</div>
                        <div class="meta">
                            ${examName ? esc(examName) + ' &nbsp;&nbsp;|&nbsp;&nbsp; ' : ''}
                            From: ${fromDate ? esc(fmtDate(fromDate)) : '____________'}
                            &nbsp; to &nbsp; ____________
                        </div>
                    </div>
                    <table>
                        <thead>
                            <tr><th class="lec">No.</th>${head}<th class="duty">Special Duty</th><th class="spare">Spare Teachers</th></tr>
                        </thead>
                        <tbody>${lines}</tbody>
                    </table>
                </div>`;
    }).join('');

    return `<!DOCTYPE html><html><head><meta charset="utf-8"><title>Time Table</title>
<style>
* { box-sizing: border-box; margin: 0; padding: 0; -webkit-print-color-adjust: exact !important; print-color-adjust: exact !important; }
body { font-family: Arial, sans-serif; font-size: 11px; color: #000; padding: 10px 14px; }
@page { size: A4 landscape; margin: 8mm; }
.sheet { page-break-after: always; }
.sheet:last-child { page-break-after: auto; }
.hdr { text-align: center; margin-bottom: 8px; }
.hdr .ttl { font-size: 15px; font-weight: 700; }
.hdr .school { font-size: 13px; font-weight: 700; margin-top: 2px; }
.hdr .meta { font-size: 11px; margin-top: 4px; }
table { width: 100%; border-collapse: collapse; table-layout: fixed; }
th, td { border: 1px solid #000; padding: 3px 4px; text-align: center; vertical-align: middle; word-wrap: break-word; }
th { font-weight: 700; font-size: 11px; }
td { height: 34px; }
td .subj { font-weight: 600; }
td .tchr { font-size: 10px; }
td .subj + .subj { margin-top: 2px; border-top: 1px dotted #999; padding-top: 2px; }
/* Two different subjects in the same period — printed, not hidden. */
td.clash { background: #ffe4e6; }
.lec { width: 34px; }
.duty { width: 90px; }
.spare { width: 110px; font-size: 10px; text-align: left; padding-left: 6px; }
@media print { .no-print { display: none !important; } body { padding: 0; } }
</style></head>
<body>
<div class="no-print" style="text-align:right;padding:6px 0 12px;">
    <button onclick="window.print()" style="background:#1a7a3e;color:#fff;border:none;padding:8px 22px;border-radius:6px;font-size:13px;font-weight:600;cursor:pointer;">Print</button>
    <button onclick="window.close()" style="background:#555;color:#fff;border:none;padding:8px 16px;border-radius:6px;font-size:13px;cursor:pointer;margin-left:8px;">Close</button>
</div>
${body}
</body></html>`;
}


