(function () {
    'use strict';

    var sections = Array.from(document.querySelectorAll('[data-faq-section]'));

    function getScrollOffset() {
        var header = document.getElementById('siteHeader');
        return (header ? header.offsetHeight : 72) + 16;
    }

    function scrollToSection(id) {
        var target = document.getElementById('faq-' + id);
        if (!target) return;

        var top = target.getBoundingClientRect().top + window.pageYOffset - getScrollOffset();
        window.scrollTo({ top: Math.max(0, top), behavior: 'smooth' });
    }

    document.querySelectorAll('a[href^="#faq-"]').forEach(function (link) {
        link.addEventListener('click', function (e) {
            var id = link.getAttribute('href').replace('#faq-', '');
            if (document.getElementById('faq-' + id)) {
                e.preventDefault();
                scrollToSection(id);
            }
        });
    });

    document.querySelectorAll('.faq-question').forEach(function (question) {
        question.addEventListener('click', function () {
            var item = question.closest('.faq-item');
            var answer = item.querySelector('.faq-answer');
            var chevron = item.querySelector('.faq-chevron');
            var isOpen = !answer.classList.contains('hidden');

            answer.classList.toggle('hidden', isOpen);
            if (chevron) {
                chevron.classList.toggle('rotate-180', !isOpen);
            }
        });
    });

    if (window.location.hash) {
        var hashId = window.location.hash.replace('#faq-', '');
        if (hashId && document.getElementById('faq-' + hashId)) {
            setTimeout(function () {
                scrollToSection(hashId);
            }, 150);
        }
    }
})();
