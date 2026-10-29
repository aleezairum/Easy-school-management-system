/* ============================================================================
   mobile.js - the portal on a phone.

   A table with many columns squeezed into a phone's width breaks every word
   over several lines. On a phone each table instead gets:
     - a minimum width worked out from its column headings, so the columns keep
       readable widths, and
     - a box that scrolls sideways (unless it already sits in one, e.g.
       .table-responsive), so nothing is cut off at the screen's edge.
   Tables the pages draw later (after Load, Search ...) get the same.
   Desktop is not touched. A table can opt out with class "no-mobile-scroll".
   ============================================================================ */
(function () {
    var phone = window.matchMedia('(max-width: 767px)');

    // About how wide a column must be for its heading to sit on one line.
    function columnWidth(cell) {
        var text = (cell.innerText || '').trim();
        var longest = text.split(/\s+/).reduce(function (m, w) { return Math.max(m, w.length); }, 0);
        return Math.max(44, Math.min(160, Math.max(text.length * 6.5, longest * 8) + 22)) * (cell.colSpan || 1);
    }

    function fit(table) {
        if (table.classList.contains('no-mobile-scroll') || table.closest('.no-mobile-scroll')) return;
        if (table.parentElement && table.parentElement.closest('table')) return;   // a table inside a table
        var row = (table.tHead && table.tHead.rows[0]) || table.rows[0];
        if (!row || !row.cells.length) return;
        var min = 0;
        for (var i = 0; i < row.cells.length; i++) min += columnWidth(row.cells[i]);
        table.style.setProperty('--m-minw', Math.round(min) + 'px');

        // Already in something that scrolls sideways?
        for (var e = table.parentElement; e && !e.matches('.content-area, .modal-body, body'); e = e.parentElement) {
            var ox = getComputedStyle(e).overflowX;
            if (ox === 'auto' || ox === 'scroll') return;
        }
        var box = document.createElement('div');
        box.className = 'm-scroll';
        table.parentNode.insertBefore(box, table);
        box.appendChild(table);
    }

    var pending = false;
    function fitAll() {
        pending = false;
        if (!phone.matches) return;
        document.querySelectorAll('.content-area table, .modal-body table').forEach(fit);
    }
    function later() {
        if (!pending) { pending = true; requestAnimationFrame(fitAll); }
    }

    document.addEventListener('DOMContentLoaded', function () {
        fitAll();
        var area = document.querySelector('.main-content') || document.body;
        new MutationObserver(function (list) {
            for (var i = 0; i < list.length; i++) {
                if (list[i].addedNodes.length) { later(); return; }
            }
        }).observe(area, { childList: true, subtree: true });
        document.addEventListener('shown.bs.modal', later);
    });
    if (phone.addEventListener) phone.addEventListener('change', later);
})();
