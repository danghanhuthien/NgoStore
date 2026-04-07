/* ══════════════════════════════════════════════════════════════════
   maison.js  –  Shared JS for MAISON Fashion
   Runs on every page via _Layout.cshtml
══════════════════════════════════════════════════════════════════ */

document.addEventListener('DOMContentLoaded', function () {

    /* ── 1. HEADER: scroll shadow + back-to-top ───────────────────── */
    const header = document.getElementById('main-header');
    const backTop = document.getElementById('backTop');

    window.addEventListener('scroll', () => {
        const y = window.scrollY;
        if (header) header.classList.toggle('scrolled', y > 40);
        if (backTop) backTop.classList.toggle('visible', y > 400);
    }, { passive: true });

    /* ── 2. REVEAL ON SCROLL (IntersectionObserver) ───────────────── */
    const reveals = document.querySelectorAll('.reveal');
    if (reveals.length) {
        const io = new IntersectionObserver((entries) => {
            entries.forEach(e => {
                if (e.isIntersecting) {
                    e.target.classList.add('in-view');
                    io.unobserve(e.target);
                }
            });
        }, { threshold: 0.1 });
        reveals.forEach(r => io.observe(r));
    }

    /* ── 3. MARQUEE: duplicate track for seamless loop ────────────── */
    const track = document.getElementById('marquee-track');
    if (track) {
        track.innerHTML += track.innerHTML;
    }

    /* ── 4. FLASH MESSAGES: auto-dismiss after 4 s ────────────────── */
    document.querySelectorAll('.flash-msg').forEach(msg => {
        setTimeout(() => {
            msg.style.transition = 'opacity 0.5s';
            msg.style.opacity = '0';
            setTimeout(() => msg.remove(), 500);
        }, 4000);
    });

    /* ── 5. SEARCH OVERLAY: close on ESC ──────────────────────────── */
    const searchOverlay = document.getElementById('search-overlay');
    if (searchOverlay) {
        document.addEventListener('keydown', e => {
            if (e.key === 'Escape') searchOverlay.classList.remove('open');
        });
        // Auto-focus input when opened
        const searchInput = searchOverlay.querySelector('.search-input');
        searchOverlay.addEventListener('transitionend', () => {
            if (searchOverlay.classList.contains('open') && searchInput) {
                searchInput.focus();
            }
        });
    }

    /* ── 6. MOBILE NAV: close on ESC ──────────────────────────────── */
    const mobileNav = document.getElementById('mobile-nav');
    if (mobileNav) {
        document.addEventListener('keydown', e => {
            if (e.key === 'Escape') mobileNav.classList.remove('open');
        });
    }

});
