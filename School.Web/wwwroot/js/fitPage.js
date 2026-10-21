/* ============================================================================
   fitPage.js - makes a printed exam report use the whole page.

   The report's wrapper says which way the paper lies:
       <div class="page-wrapper" data-fit="landscape">   (or "portrait")
   and the page loads this file at the end of <body>.

   1. The wrapper is made exactly as wide as the printable page, so the screen
      shows what the paper will get.
   2. The report's tables are zoomed to the largest size that still fits that
      width - a wide sheet (many subjects, long names) is made smaller instead
      of running off the right edge, a narrow one is made bigger.
   3. If the whole report fits on one page, it is made as big as the page's
      height allows too, and the height still left over is shared out among the
      table rows, so the page is filled to the bottom.
      A report longer than one page is only fitted to the width, never enlarged.

   Print settings to use: Paper A4, Scale Default, Margins Default.
   ============================================================================ */
(function () {
    const MM = 96 / 25.4;
    // The same paper margins on every report (each page used to set its own).
    const TOP = 4, BOTTOM = 6, SIDE = 8, SPARE = 3;
    // A short report is not blown up past this, and a row grows to at most
    // ROW_GROW times its own height: five rows stretched over a page look broken.
    const MAX_ZOOM = 1.4, ROW_GROW = 1.8;
    const style = document.createElement('style');
    style.textContent = `@media print { body { margin: 0 !important; padding: ${TOP}mm ${SIDE}mm ${BOTTOM}mm !important; } }
                         [data-fit] { box-sizing: border-box; max-width: none !important; margin-left: auto !important; margin-right: auto !important; }`;
    document.head.appendChild(style);

    function fitReport() {
        const wrap = document.querySelector('[data-fit]');
        if (!wrap) return;
        const landscape = wrap.dataset.fit === 'landscape';
        const pageW = (landscape ? 297 : 210) - 2 * SIDE;
        const pageH = ((landscape ? 210 : 297) - TOP - BOTTOM - SPARE) * MM;
        wrap.style.width = pageW + 'mm';

        // Top-level tables only: a table inside another one zooms with its parent.
        const tables = [...wrap.querySelectorAll('table')].filter(t => !t.parentElement.closest('table'));
        if (!tables.length) return;
        const rows = tables.flatMap(t => [...t.tBodies].flatMap(b => [...b.rows]));
        rows.forEach(r => { r.style.height = ''; });

        const setZoom = z => tables.forEach(t => { t.style.zoom = z; });
        // Each table's height at its designed size. Zoomed in, a table whose width is
        // fixed by the page (two side by side, say) would not get wider, only wrap
        // more - names broken over three lines. Such a zoom is refused like a too
        // wide one: zoomed by z, a table must be no more than z times as tall.
        setZoom(1);
        const designed = tables.map(t => t.getBoundingClientRect().height);
        let zoom = 1;
        const wrapsMore = () => zoom > 1 && tables.some((t, i) => t.getBoundingClientRect().height > designed[i] * zoom * 1.02 + 2);
        const tooWide = () => wrap.scrollWidth > wrap.clientWidth + 1
            || tables.some(t => t.getBoundingClientRect().width > wrap.clientWidth + 1)
            || wrapsMore();
        const tooTall = () => wrap.scrollHeight > pageH;

        let fitsWidth = null, fitsPage = null;
        for (let z = MAX_ZOOM; z >= 0.5; z = Math.round((z - 0.05) * 100) / 100) {
            setZoom(zoom = z);
            if (tooWide()) continue;
            if (fitsWidth === null) fitsWidth = z;
            if (!tooTall()) { fitsPage = z; break; }
        }
        if (fitsPage === null) {               // longer than one page: fit the width only
            setZoom(Math.min(fitsWidth ?? 0.5, 1));
            return;
        }
        setZoom(fitsPage);

        // Share the height still free among the rows. A row's height is set in the
        // table's own (zoomed) units, so the screen pixels are divided by the zoom.
        if (!rows.length) return;
        let add = (pageH - wrap.scrollHeight) / rows.length;
        while (add >= 1) {
            rows.forEach(r => { r.style.height = ''; });
            const base = rows.map(r => r.getBoundingClientRect().height);
            rows.forEach((r, i) => { r.style.height = ((base[i] + Math.min(add, base[i] * (ROW_GROW - 1))) / fitsPage) + 'px'; });
            if (!tooTall()) break;
            add -= 1;
        }
        if (add < 1) rows.forEach(r => { r.style.height = ''; });
    }

    window.fitReport = fitReport;
    if (document.readyState === 'complete') fitReport();
    else window.addEventListener('load', fitReport);
    window.addEventListener('beforeprint', fitReport);
})();
