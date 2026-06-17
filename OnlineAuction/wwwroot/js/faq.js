(function () {
    'use strict';

    var navButtons = document.querySelectorAll('[data-faq-nav]');
    var sections = Array.from(document.querySelectorAll('[data-faq-section]'));
    var searchInput = document.getElementById('faqSearch');
    var noResults = document.getElementById('faqNoResults');
    var isProgrammaticScroll = false;
    var scrollTimer = null;

    function getScrollOffset() {
        var header = document.getElementById('siteHeader');
        return (header ? header.offsetHeight : 72) + 24;
    }

    function setActiveNav(id) {
        navButtons.forEach(function (btn) {
            var isActive = btn.getAttribute('data-faq-nav') === id;
            btn.classList.toggle('bg-blue-700', isActive);
            btn.classList.toggle('text-white', isActive);
            btn.classList.toggle('text-slate-700', !isActive);
            btn.classList.toggle('hover:bg-slate-50', !isActive);

            var icon = btn.querySelector('svg');
            if (icon) {
                icon.classList.toggle('text-white', isActive);
                icon.classList.toggle('text-slate-400', !isActive);
            }
        });
    }

    function scrollToSection(id) {
        var target = document.getElementById('faq-' + id);
        if (!target) return;

        isProgrammaticScroll = true;
        clearTimeout(scrollTimer);
        setActiveNav(id);

        var top = target.getBoundingClientRect().top + window.pageYOffset - getScrollOffset();
        window.scrollTo({ top: Math.max(0, top), behavior: 'smooth' });

        scrollTimer = setTimeout(function () {
            isProgrammaticScroll = false;
        }, 900);
    }

    function updateActiveFromScroll() {
        if (isProgrammaticScroll || sections.length === 0) return;

        var offset = getScrollOffset() + 16;
        var activeId = sections[0].getAttribute('data-faq-section');

        sections.forEach(function (section) {
            if (section.offsetTop <= window.scrollY + offset) {
                activeId = section.getAttribute('data-faq-section');
            }
        });

        setActiveNav(activeId);
    }

    navButtons.forEach(function (btn) {
        btn.addEventListener('click', function () {
            scrollToSection(btn.getAttribute('data-faq-nav'));
        });
    });

    document.querySelectorAll('.faq-question').forEach(function (question) {
        question.addEventListener('click', function () {
            var item = question.closest('.faq-item');
            var answer = item.querySelector('.faq-answer');
            var chevron = item.querySelector('.faq-chevron');
            var isOpen = !answer.classList.contains('hidden');

            answer.classList.toggle('hidden', isOpen);
            chevron.classList.toggle('rotate-180', !isOpen);
        });
    });

    if (searchInput) {
        searchInput.addEventListener('input', function () {
            var query = searchInput.value.trim().toLowerCase();
            var visibleCount = 0;

            sections.forEach(function (section) {
                var sectionVisible = false;

                section.querySelectorAll('.faq-item').forEach(function (item) {
                    var text = item.getAttribute('data-faq-question') || '';
                    var answerText = item.querySelector('.faq-answer p')?.textContent.toLowerCase() || '';
                    var match = !query || text.includes(query) || answerText.includes(query);
                    item.classList.toggle('hidden', !match);
                    if (match) sectionVisible = true;
                });

                section.classList.toggle('hidden', !sectionVisible);
                if (sectionVisible) visibleCount++;
            });

            if (noResults) {
                noResults.classList.toggle('hidden', visibleCount > 0 || !query);
            }
        });
    }

    window.addEventListener('scroll', updateActiveFromScroll, { passive: true });
    updateActiveFromScroll();
})();
