// ── Fee Report Print Helpers ──────────────────────────────────────

function fmtMonthLabel(monthKey) {
    // monthKey = "2026-04"
    const [y, m] = monthKey.split('-');
    const monthNames = ['Jan','Feb','Mar','Apr','May','Jun','Jul','Aug','Sep','Oct','Nov','Dec'];
    return `${monthNames[parseInt(m) - 1]}-${String(y).slice(-2)}`;
}

function getDateRange() {
    const selectedDates = monthPicker ? monthPicker.selectedDates : [];
    let dateFrom, dateTo;
    if (!selectedDates || selectedDates.length === 0) {
        const now = new Date();
        dateFrom = formatDate(new Date(now.getFullYear(), now.getMonth(), 1));
        dateTo   = formatDate(new Date(now.getFullYear(), now.getMonth() + 1, 0));
    } else {
        const first = selectedDates[0];
        const last  = selectedDates.length > 1 ? selectedDates[selectedDates.length - 1] : selectedDates[0];
        dateFrom = formatDate(new Date(first.getFullYear(), first.getMonth(), 1));
        dateTo   = formatDate(new Date(last.getFullYear(),  last.getMonth() + 1, 0));
    }
    return { dateFrom, dateTo };
}

// ── PRINT REPORT ──────────────────────────────────────────────
function printReport() {
    if (!allData || allData.length === 0) {
        toastError('No data to print. Please load records first.');
        return;
    }

    const status = $('#clientStatus').val();
    const { dateFrom, dateTo } = getDateRange();

    // Determine which sections (Paid / Unpaid) to render
    let sections = [];
    if (status === 'true') {
        const paid = allData.filter(d => d.isClear === true);
        if (paid.length) sections.push({ type: 'paid',   label: 'Paid Fee Report',   data: paid });
    } else if (status === 'false') {
        const unpaid = allData.filter(d => d.isClear === false);
        if (unpaid.length) sections.push({ type: 'unpaid', label: 'Unpaid Fee Report', data: unpaid });
    } else {
        // All: paid first, then unpaid
        const paid   = allData.filter(d => d.isClear === true);
        const unpaid = allData.filter(d => d.isClear === false);
        if (paid.length)   sections.push({ type: 'paid',   label: 'Paid Fee Report',   data: paid });
        if (unpaid.length) sections.push({ type: 'unpaid', label: 'Unpaid Fee Report', data: unpaid });
    }

    if (sections.length === 0) {
        toastError('No records found for the selected status.');
        return;
    }

    const html = buildReportHTML(sections, dateFrom, dateTo);
    const win  = window.open('', '_blank', 'width=1200,height=800');
    win.document.write(html);
    win.document.close();
    win.focus();
    setTimeout(() => { win.print(); }, 600);
}

// ── Build Report HTML (Full Page) ─────────────────────────────
function buildReportHTML(sections, dateFrom, dateTo) {
    const fmtDisplay = d => new Date(d).toLocaleDateString('en-GB', { day:'2-digit', month:'2-digit', year:'numeric' });

    let fullBody = '';  // accumulates all section blocks

    sections.forEach((sec, secIdx) => {
        const isPaid = sec.type === 'paid';

        // ── Derive unique months & fee-types for this section ──
        const monthSet   = new Set();
        const feeTypeSet = new Set();

        sec.data.forEach(r => {
            const d = new Date(r.vDate);
            monthSet.add(`${d.getFullYear()}-${String(d.getMonth()+1).padStart(2,'0')}`);
            if (r.feeTypeName) feeTypeSet.add(r.feeTypeName);
        });

        const months   = [...monthSet].sort();
        const feeTypes = [...feeTypeSet];

        // columns: feeType × month
        const cols = [];
        feeTypes.forEach(ft => months.forEach(m => cols.push({ feeType: ft, month: m, key: `${ft}||${m}` })));

        // ── Group: class+section → student → col key → amount ──
        const groupMap = new Map();

        sec.data.forEach(r => {
            const gk   = `${r.classID ?? r.classId ?? r.className ?? ''}||${r.sectionID ?? r.sectionId ?? r.sectionName ?? ''}`;
            const sk   = String(r.studentId || r.studentName || 'unknown');
            const d    = new Date(r.vDate);
            const mKey = `${d.getFullYear()}-${String(d.getMonth()+1).padStart(2,'0')}`;
            const ck   = `${r.feeTypeName}||${mKey}`;
            const amt  = isPaid ? (r.clearedAmount || 0) : (r.pendingAmount || 0);

            if (!groupMap.has(gk)) groupMap.set(gk, {
                className:   r.className   || '-',
                sectionName: r.sectionName || '',
                classId:     r.classID   ?? r.classId   ?? null,
                sectionId:   r.sectionID ?? r.sectionId ?? null,
                students: new Map()
            });

            const grp = groupMap.get(gk);
            if (!grp.students.has(sk)) grp.students.set(sk, {
                rollNo:      r.classRollNo || r.rollNo || '-',
                studentName: r.studentName || '-',
                contact:     r.contactNo   || r.contact || r.fatherContact || '-',
                cells: {}
            });

            const stu = grp.students.get(sk);
            stu.cells[ck] = (stu.cells[ck] || 0) + amt;
        });

        // ── Build shared table header (feeType row + month row) ──
        const thStyle   = 'border:1px solid #999;padding:4px 6px;text-align:center;background:#e8ecf0;vertical-align:middle;';
        const thStyle2  = 'border:1px solid #999;padding:4px 6px;text-align:center;background:#eef1f5;white-space:nowrap;';

        let hRow1 = `<th rowspan="2" style="${thStyle}">Sr#</th>
                     <th rowspan="2" style="${thStyle}">Roll No.</th>
                     <th rowspan="2" style="${thStyle}">Student Name</th>
                     <th rowspan="2" style="${thStyle}">Contact</th>`;
        feeTypes.forEach(ft => {
            hRow1 += `<th colspan="${months.length}" style="${thStyle}">${ft}</th>`;
        });
        hRow1 += `<th rowspan="2" style="${thStyle}">Total</th>`;

        let hRow2 = '';
        feeTypes.forEach(() => {
            months.forEach(m => {
                hRow2 += `<th style="${thStyle2}">${fmtMonthLabel(m)}</th>`;
            });
        });

        // ── Build per-group tables ──
        let groupsHtml    = '';
        let secGrandTotal = 0;

        // Order by the school's own class order (SMSClass.VID), then section.
        //
        // This used to scrape digits out of the class NAME, which cannot work on
        // a real class list: "Montessori", "Play Group", "Nursery", "Prep" and
        // "New Admission" all scraped to 0 and landed in arbitrary order, "9" and
        // "9 Junior" tied, and "Private Students 2026-27" scraped to 202627 and
        // sorted after Class 10. The name is a label; the id is the order.
        const sortedGroups = [...groupMap.entries()].sort((a, b) => {
            const ca = a[1].classId, cb = b[1].classId;
            if (ca != null && cb != null && ca !== cb) return ca - cb;
            // Only rows with no id fall back to the name, so a report from an
            // older API build still comes out in a sensible order.
            if (ca == null || cb == null) {
                const byName = (a[1].className || '').localeCompare(b[1].className || '', undefined, { numeric: true });
                if (byName !== 0) return byName;
            }

            const sa = a[1].sectionId, sb = b[1].sectionId;
            if (sa != null && sb != null && sa !== sb) return sa - sb;
            return (a[1].sectionName || '').localeCompare(b[1].sectionName || '', undefined, { numeric: true });
        });

        sortedGroups.forEach(([, grp]) => {
            const label = grp.sectionName
                ? `${grp.className} (${grp.sectionName})`
                : grp.className;

            const colTotals = {};
            cols.forEach(c => colTotals[c.key] = 0);
            let groupTotal = 0;

            // Sort students by rollNo numerically
            const sortedStudents = [...grp.students.entries()].sort((a, b) => {
                const ra = parseInt(a[1].rollNo) || 0;
                const rb = parseInt(b[1].rollNo) || 0;
                return ra - rb;
            });

            let studentRows = '';
            let srNo = 0;
            sortedStudents.forEach(([, stu]) => {
                srNo++;
                let rowTotal = 0;
                let cells    = '';
                cols.forEach(c => {
                    const amt = stu.cells[c.key] || 0;
                    rowTotal       += amt;
                    colTotals[c.key] += amt;
                    cells += `<td style="border:1px solid #ccc;padding:3px 6px;text-align:right;">${amt === 0 ? '0' : formatAmount(amt)}</td>`;
                });
                groupTotal += rowTotal;
                studentRows += `<tr>
                    <td style="border:1px solid #ccc;padding:3px 6px;text-align:center;">${srNo}</td>
                    <td style="border:1px solid #ccc;padding:3px 6px;text-align:center;">${stu.rollNo}</td>
                    <td style="border:1px solid #ccc;padding:3px 6px;font-weight:600;">${stu.studentName}</td>
                    <td style="border:1px solid #ccc;padding:3px 6px;font-size:10px;white-space:nowrap;">${stu.contact}</td>
                    ${cells}
                    <td style="border:1px solid #ccc;padding:3px 6px;text-align:right;font-weight:700;">${formatAmount(rowTotal)}</td>
                </tr>`;
            });

            let totalCells = '';
            cols.forEach(c => {
                totalCells += `<td style="border:1px solid #ccc;padding:3px 6px;text-align:right;font-weight:700;background:#f0f4f8;">${formatAmount(colTotals[c.key])}</td>`;
            });

            secGrandTotal += groupTotal;

            groupsHtml += `
            <div style="margin-bottom:20px;">
                <div style="background:#000;color:#fff;padding:5px 10px;font-size:13px;font-weight:700;text-align:center;">
                    Class : ${label}
                </div>
                <table style="width:100%;border-collapse:collapse;font-size:11px;">
                    <thead>
                        <tr>${hRow1}</tr>
                        <tr>${hRow2}</tr>
                    </thead>
                    <tbody>
                        ${studentRows}
                        <tr>
                            <td colspan="4" style="border:1px solid #ccc;padding:3px 6px;text-align:right;font-weight:700;background:#f0f4f8;">Total</td>
                            ${totalCells}
                            <td style="border:1px solid #ccc;padding:3px 6px;text-align:right;font-weight:700;background:#f0f4f8;">${formatAmount(groupTotal)}</td>
                        </tr>
                    </tbody>
                </table>
            </div>`;
        });

        // ── Wrap this section ──
        const pageBreak  = secIdx > 0 ? 'page-break-before:always;' : '';
        const titleLabel = isPaid ? 'Student Paid Fee Report' : 'Student Unpaid Fee Report';
        const rangeLabel = isPaid ? 'Paid' : 'Unpaid';

        fullBody += `
        <div style="${pageBreak}padding:0 0 16px 0;">
            <div style="text-align:center;margin-bottom:10px;">
                <div style="background:#d8d8d8;border:1px solid #aaa;padding:6px;font-size:14px;font-weight:700;letter-spacing:.5px;">
                    ${titleLabel}
                </div>
                <div style="background:#e4e4e4;border:1px solid #aaa;border-top:none;padding:5px;font-size:12px;">
                    ${rangeLabel} Fee Report From ( ${fmtDisplay(dateFrom)} ) To ( ${fmtDisplay(dateTo)} )
                </div>
            </div>

            ${groupsHtml}

            <div style="text-align:right;margin-top:6px;">
                <span style="font-size:12px;font-weight:600;margin-right:8px;">Grand Total</span>
                <span style="border:2px solid #000;padding:4px 18px;font-size:13px;font-weight:700;">Rs.${formatAmount(secGrandTotal)}</span>
            </div>
        </div>`;
    });

    return `<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <title>Fee Report</title>
    <style>
        * { box-sizing: border-box; margin: 0; padding: 0; }
        body {
            font-family: Arial, sans-serif;
            font-size: 11px;
            color: #000;
            background: #fff;
            padding: 10px 14px;
        }
        @page {
            size: A4 landscape;
            margin: 8mm 10mm;
        }
        @media print {
            body { padding: 0; }
            .no-print { display: none !important; }
        }
        table { border-collapse: collapse; width: 100%; }
        th, td { font-size: 11px; }
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

    ${fullBody}
</body>
</html>`;
}
