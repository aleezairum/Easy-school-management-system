/* App Logins (Views/MobileApp/Logins.cshtml): students' and staff logins for
 * the EasySchool mobile app, PIN resets, switching a login off, login slips.
 * A separate file: Razor eats a bare @ inside a .cshtml script block.
 */
let _stu = [], _staff = [], _roles = [], _school = null;

function alEsc(v) { return (v ?? '').toString().replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;'); }
function alWhen(v) { if (!v) return '<span class="text-muted">never</span>'; const d = new Date(v); return d.toLocaleDateString('en-GB', { day: '2-digit', month: 'short' }) + ' ' + d.toLocaleTimeString('en-GB', { hour: '2-digit', minute: '2-digit' }); }
function alStatus(r) {
    if (!r.accountId) return '<span class="badge badge-none">No login</span>';
    if (!r.isActive) return '<span class="badge badge-off">Off</span>';
    return r.mustChangePassword ? '<span class="badge bg-warning text-dark">Not signed in yet</span>' : '<span class="badge bg-success">Active</span>';
}
function alPin(r) { return r.initialPassword ? `<span class="pin">${alEsc(r.initialPassword)}</span>` : (r.accountId ? '<span class="hint">changed by user</span>' : ''); }
async function alJson(res) { const b = await res.json().catch(() => ({})); if (!res.ok) throw new Error(b.message || `The server answered ${res.status}`); return b; }

$(async function () {
    const [classes, campuses, roles, school] = await Promise.all([
        fetch(`${API_BASE}/class`).then(r => r.ok ? r.json() : []).catch(() => []),
        fetch(`${API_BASE}/campus`).then(r => r.ok ? r.json() : []).catch(() => []),
        fetch(`${API_BASE}/app-admin/roles`).then(r => r.ok ? r.json() : []).catch(() => []),
        fetch(`${API_BASE}/app/school-info`).then(r => r.ok ? r.json() : null).catch(() => null)
    ]);
    _roles = roles; _school = school;
    classes.forEach(c => $('#sClass').append(`<option value="${c.vid}">${alEsc(c.vName)}</option>`));
    campuses.forEach(c => $('#fCampus').append(`<option value="${c.vid}">${alEsc(c.vName)}</option>`));
    roles.forEach(r => $('#fRole').append(`<option value="${r.roleId}">${alEsc(r.roleName)}</option>`));
    const teacher = roles.find(r => /teacher/i.test(r.roleName)); if (teacher) $('#fRole').val(String(teacher.roleId));
    $('#stuAll').on('change', function () { $('.stu-chk').prop('checked', this.checked); });
    $('#staffAll').on('change', function () { $('.staff-chk').prop('checked', this.checked); });
});

async function alLoadSections() {
    $('#sSection').find('option:not(:first)').remove();
    if (!$('#sClass').val()) return;
    const list = await fetch(`${API_BASE}/section/byClass/${$('#sClass').val()}`).then(r => r.ok ? r.json() : []).catch(() => []);
    list.forEach(s => $('#sSection').append(`<option value="${s.vid}">${alEsc(s.vName)}</option>`));
    if (list.length === 1) $('#sSection').val(String(list[0].vid));
}

// ── students ──────────────────────────────────────────────────────────
async function alLoadStudents() {
    const sec = $('#sSection').val();
    if (!sec) { toastWarning('Choose a class and section'); return; }
    try { _stu = await alJson(await fetch(`${API_BASE}/app-admin/students?sectionId=${sec}`)); }
    catch (e) { toastError(e.message); return; }
    $('#stuAll').prop('checked', false);
    if (!_stu.length) { $('#stuBody').html('<tr><td colspan="9" class="text-center text-muted py-4">No students in this section.</td></tr>'); return; }
    $('#stuBody').html(_stu.map((r, i) => `<tr>
        <td><input type="checkbox" class="form-check-input stu-chk" data-i="${i}" /></td>
        <td>${alEsc(r.code)}</td><td class="fw-semibold">${alEsc(r.name)}</td><td>${alEsc(r.detail)}</td>
        <td class="pin">${alEsc(r.loginId || '')}</td><td>${alPin(r)}</td><td>${alStatus(r)}</td><td class="small">${r.accountId ? alWhen(r.lastLoginAt) : ''}</td>
        <td class="text-nowrap">${r.accountId ? `
            <button class="btn btn-sm btn-outline-secondary" title="New PIN" onclick="alReset(${r.accountId}, 'stu')"><i class="bi bi-arrow-repeat"></i></button>
            <button class="btn btn-sm ${r.isActive ? 'btn-outline-danger' : 'btn-outline-success'}" title="${r.isActive ? 'Switch off' : 'Switch on'}" onclick="alActive(${r.accountId}, ${!r.isActive}, 'stu')"><i class="bi ${r.isActive ? 'bi-slash-circle' : 'bi-check-circle'}"></i></button>` : ''}</td>
    </tr>`).join(''));
}

function alPicked(kind) {
    const list = kind === 'stu' ? _stu : _staff;
    const ticked = $(kind === 'stu' ? '.stu-chk:checked' : '.staff-chk:checked').map((_, el) => list[+el.dataset.i]).get();
    return ticked.length ? ticked : list;
}

async function alGenerate() {
    if (!_stu.length) { toastWarning('Load a section first'); return; }
    const ids = alPicked('stu').filter(r => !r.accountId).map(r => r.personId);
    if (!ids.length) { toastSuccess('Everyone chosen already has a login'); return; }
    try {
        const r = await alJson(await fetch(`${API_BASE}/app-admin/students/generate`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ StudentIds: ids }) }));
        toastSuccess(`${r.made} login(s) created. Print the slips for the parents.`);
        alLoadStudents();
    } catch (e) { toastError(e.message); }
}

// ── staff ─────────────────────────────────────────────────────────────
async function alLoadStaff() {
    let url = `${API_BASE}/app-admin/staff`; if ($('#fCampus').val()) url += `?campusId=${$('#fCampus').val()}`;
    try { _staff = await alJson(await fetch(url)); } catch (e) { toastError(e.message); return; }
    $('#staffAll').prop('checked', false);
    const roleOptions = sel => _roles.map(r => `<option value="${r.roleId}" ${r.roleId === sel ? 'selected' : ''}>${alEsc(r.roleName)}</option>`).join('');
    $('#staffBody').html(_staff.map((r, i) => `<tr>
        <td><input type="checkbox" class="form-check-input staff-chk" data-i="${i}" /></td>
        <td>${alEsc(r.code)}</td><td class="fw-semibold">${alEsc(r.name)}</td><td>${alEsc(r.detail)}</td>
        <td><select class="form-select form-select-sm" id="role${i}" style="min-width:120px">${r.accountId ? '' : '<option value="">-</option>'}${roleOptions(r.roleId)}</select></td>
        <td class="pin">${alEsc(r.loginId || '')}</td><td>${alPin(r)}</td><td>${alStatus(r)}</td><td class="small">${r.accountId ? alWhen(r.lastLoginAt) : ''}</td>
        <td class="text-nowrap">
            <button class="btn btn-sm ${r.accountId ? 'btn-outline-primary' : 'btn-success'}" title="${r.accountId ? 'Save role' : 'Create login'}" onclick="alSaveStaff(${i})"><i class="bi ${r.accountId ? 'bi-check-lg' : 'bi-key'}"></i></button>
            ${r.accountId ? `<button class="btn btn-sm btn-outline-secondary" title="New PIN" onclick="alReset(${r.accountId}, 'staff')"><i class="bi bi-arrow-repeat"></i></button>
            <button class="btn btn-sm ${r.isActive ? 'btn-outline-danger' : 'btn-outline-success'}" title="${r.isActive ? 'Switch off' : 'Switch on'}" onclick="alActive(${r.accountId}, ${!r.isActive}, 'staff')"><i class="bi ${r.isActive ? 'bi-slash-circle' : 'bi-check-circle'}"></i></button>` : ''}
        </td>
    </tr>`).join(''));
}

async function alSaveStaff(i) {
    const r = _staff[i], roleId = +($(`#role${i}`).val() || $('#fRole').val());
    if (!roleId) { toastWarning('Choose a role'); return; }
    try {
        await alJson(await fetch(`${API_BASE}/app-admin/staff`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ EmployeeId: r.personId, RoleId: roleId }) }));
        toastSuccess(r.accountId ? 'Role saved' : 'Login created');
        alLoadStaff();
    } catch (e) { toastError(e.message); }
}

// ── any login ─────────────────────────────────────────────────────────
async function alReset(accountId, kind) {
    const go = typeof confirmDelete === 'function'
        ? await confirmDelete('Give this login a new PIN? The old password stops working; the new PIN is shown here for a new slip.')
        : confirm('Give this login a new PIN?');
    if (!go) return;
    try {
        const r = await alJson(await fetch(`${API_BASE}/app-admin/accounts/${accountId}/reset`, { method: 'POST' }));
        toastSuccess(`New PIN: ${r.password}`);
        kind === 'stu' ? alLoadStudents() : alLoadStaff();
    } catch (e) { toastError(e.message); }
}

async function alActive(accountId, value, kind) {
    try {
        await alJson(await fetch(`${API_BASE}/app-admin/accounts/${accountId}/active?value=${value}`, { method: 'PUT' }));
        toastSuccess(value ? 'Login switched on' : 'Login switched off - the app signs out at once');
        kind === 'stu' ? alLoadStudents() : alLoadStaff();
    } catch (e) { toastError(e.message); }
}

// ── login slips: 8 to an A4 page, cut along the dotted lines ──────────
function alPrintSlips(kind) {
    const rows = alPicked(kind).filter(r => r.accountId && r.initialPassword && r.isActive);
    if (!rows.length) { toastWarning('Nobody chosen has a new login with a PIN to print. Create logins (or give a new PIN) first.'); return; }
    const school = alEsc(_school?.name || 'School'), code = alEsc(_school?.schoolCode || '');
    // The Android app is downloaded from the school's own site (no Play Store); the QR opens that page.
    const dl = _school?.downloadUrl || (location.origin + '/EasySchool/app');
    const qr = location.origin + '/EasySchool/app/qr.png';
    const get = `<li>Install the <b>EasySchool</b> app: scan the code or open <b>${alEsc(dl.replace(/^https?:\/\//, ''))}</b> on the phone.</li>`;
    const slip = r => `<div class="slip">
        <div class="head"><b>${school}</b><span>EasySchool app login</span></div>
        <div class="who">${alEsc(r.name)}${kind === 'stu' ? ` <small>${alEsc([r.className, r.sectionName].filter(Boolean).join(' - '))}</small>` : ` <small>${alEsc(r.detail || '')}</small>`}</div>
        <div class="body"><div class="left">
        <table><tr><td>School Code</td><td class="v">${code}</td></tr><tr><td>Login ID</td><td class="v">${alEsc(r.loginId)}</td></tr><tr><td>PIN</td><td class="v">${alEsc(r.initialPassword)}</td></tr></table>
        <ol>${kind === 'stu'
            ? get + '<li>Enter the School Code, then choose <b>Parent</b> (all your children) or <b>Student</b>.</li><li>Sign in with this Login ID and PIN, then set your own password.</li>'
            : get + '<li>Enter the School Code and choose <b>Staff</b>.</li><li>Sign in with this Login ID and PIN, then set your own password.</li>'}</ol>
        </div><div class="qr"><img src="${qr}" alt="" /><span>Scan to download</span></div></div>
    </div>`;
    const w = window.open('', '_blank', 'width=900,height=900');
    if (!w) { toastError('Allow pop-ups to print'); return; }
    w.document.write(`<!doctype html><html><head><meta charset="utf-8"><title>App login slips</title><style>
        @page { size: A4; margin: 8mm; }
        body { font-family: Arial, sans-serif; margin: 0; color: #111; }
        .grid { display: grid; grid-template-columns: 1fr 1fr; }
        .slip { border: 1px dashed #888; padding: 9px 11px; height: 66mm; box-sizing: border-box; page-break-inside: avoid; }
        .head { display: flex; justify-content: space-between; align-items: baseline; border-bottom: 2px solid #0B6E3A; padding-bottom: 3px; }
        .head b { font-size: 11.5px; } .head span { font-size: 10px; color: #0B6E3A; font-weight: 700; }
        .who { font-size: 13px; font-weight: 700; margin: 6px 0 4px; } .who small { font-weight: 400; color: #555; }
        table { border-collapse: collapse; width: 100%; font-size: 12px; } td { padding: 2px 4px; border: 1px solid #ccc; }
        td.v { font-family: Consolas, monospace; font-size: 15px; font-weight: 700; letter-spacing: .08em; }
        ol { margin: 6px 0 0; padding-left: 16px; font-size: 10px; line-height: 1.35; }
        .body { display: flex; gap: 8px; } .left { flex: 1; min-width: 0; }
        .qr { width: 27mm; text-align: center; padding-top: 2px; } .qr img { width: 27mm; height: 27mm; display: block; } .qr span { font-size: 8.5px; color: #0B6E3A; font-weight: 700; }
        .no-print { text-align: right; padding: 6px; } @media print { .no-print { display: none; } }
    </style></head><body><div class="no-print"><button onclick="window.print()">Print</button></div><div class="grid">${rows.map(slip).join('')}</div></body></html>`);
    w.document.close();
    // Print once the QR codes have loaded, or they come out blank.
    Promise.all([...w.document.images].map(i => i.complete ? 0 : new Promise(r => { i.onload = i.onerror = r; })))
        .then(() => setTimeout(() => w.print(), 200));
}
