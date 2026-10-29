/* ============================================================================
   fitPage.js - fits a printed exam report to the A4 page.

   The report's wrapper says which way the paper lies:
       <div class="page-wrapper" data-fit="landscape">   (or "portrait")
   and the page loads this file at the end of <body>.

   The WHOLE report is scaled as one piece - header, title bars and tables
   together, like the print dialog's Scale - so its proportions never change:

   1. Wider than the paper: long names may wrap first; if it is still too
      wide, the report is made smaller until it fits (down to 70%).
   2. One page with room to spare: the report is made larger (up to 125%),
      but never so much that a name or heading wraps onto another line; any
      height still left is shared out among the table rows.
   3. Longer than one page: only fitted to the width, never enlarged.

   Print with: Paper A4, Scale Default, Margins Default.
   ============================================================================ */
(function () {
    const MM = 96 / 25.4;
    // The same paper margins on every report, and room kept free at the bottom
    // so a printer's rounding never pushes the last row onto a second page.
    const TOP = 5, BOTTOM = 6, SIDE = 8, SPARE = 10;
    const MIN_ZOOM = 0.7, MAX_ZOOM = 1.25, ROW_GROW = 1.5;

    const style = document.createElement('style');
    style.textContent = `
        @media print { body { margin: 0 !important; padding: ${TOP}mm ${SIDE}mm ${BOTTOM}mm !important; } }
        [data-fit] { box-sizing: border-box; max-width: none !important; margin-left: auto !important; margin-right: auto !important; }
        [data-fit].fit-wrap td, [data-fit].fit-wrap th { white-space: normal !important; }`;
    document.head.appendChild(style);

    function fitReport() {
        const wrap = document.querySelector('[data-fit]');
        if (!wrap) return;
        const landscape = wrap.dataset.fit === 'landscape';
        const pageW = (landscape ? 297 : 210) - 2 * SIDE;                    // mm
        const pageH = ((landscape ? 210 : 297) - TOP - BOTTOM - SPARE) * MM; // px on paper

        const rows = [...wrap.querySelectorAll('table')]
            .filter(t => !t.parentElement.closest('table'))
            .flatMap(t => [...t.tBodies].flatMap(b => [...b.rows]));
        rows.forEach(r => { r.style.height = ''; });
        wrap.classList.remove('fit-wrap');

        // Scaled by z, the wrapper is laid out (pageW / z) wide so that on paper it is pageW.
        let zoom = 1;
        const setZoom = z => { zoom = z; wrap.style.zoom = z; wrap.style.width = (pageW / z) + 'mm'; };
        const tooWide = () => wrap.scrollWidth > wrap.clientWidth + 1;
        const paperHeight = () => wrap.getBoundingClientRect().height;
        const ownHeight = () => wrap.offsetHeight;   // in the report's own (unscaled) pixels

        // 1. Fit the width: wrap long names, then make it smaller.
        setZoom(1);
        if (tooWide()) wrap.classList.add('fit-wrap');
        while (tooWide() && zoom > MIN_ZOOM + 0.001) setZoom(Math.round((zoom - 0.05) * 100) / 100);
        if (paperHeight() > pageH) return;           // more than one page: leave it there

        // 2. One page: as large as fits, as long as nothing wraps more than at 100%.
        const base = zoom, baseOwn = ownHeight();
        for (let z = MAX_ZOOM; z > base + 0.001; z = Math.round((z - 0.05) * 100) / 100) {
            setZoom(z);
            if (!tooWide() && paperHeight() <= pageH && ownHeight() <= baseOwn * 1.01 + 2) break;
            setZoom(base);
        }

        // 3. Share the height still free among the rows (each grows to at most ROW_GROW times).
        if (!rows.length) return;
        let add = (pageH - paperHeight()) / zoom / rows.length;   // in the report's own pixels
        while (add >= 1) {
            rows.forEach(r => { r.style.height = ''; });
            const h = rows.map(r => r.offsetHeight);
            rows.forEach((r, i) => { r.style.height = (h[i] + Math.min(add, h[i] * (ROW_GROW - 1))) + 'px'; });
            if (paperHeight() <= pageH) return;
            add -= 1;
        }
        rows.forEach(r => { r.style.height = ''; });
    }

    window.fitReport = fitReport;
    if (document.readyState === 'complete') fitReport();
    else window.addEventListener('load', fitReport);
    window.addEventListener('beforeprint', fitReport);
})();
