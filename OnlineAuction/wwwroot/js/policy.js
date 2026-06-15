(function () {
    'use strict';

    var navLinks = document.querySelectorAll('[data-policy-nav]');
    var contentSections = document.querySelectorAll('[data-policy-section]');

    var legacyMap = {
        'terms-of-use': 'terms-of-service',
        'payment-policy': 'bidding-rules',
        'auction-rules': 'bidding-rules',
        'user-responsibilities': 'terms-of-service'
    };

    function setActiveNav(id) {
        navLinks.forEach(function (link) {
            var isActive = link.getAttribute('data-policy-nav') === id;
            link.classList.toggle('bg-blue-700', isActive);
            link.classList.toggle('text-white', isActive);
            link.classList.toggle('text-slate-700', !isActive);
            link.classList.toggle('hover:bg-white', !isActive);
        });
    }

    navLinks.forEach(function (link) {
        link.addEventListener('click', function (e) {
            e.preventDefault();
            var id = link.getAttribute('data-policy-nav');
            var target = document.getElementById(id);
            setActiveNav(id);
            if (target) {
                target.scrollIntoView({ behavior: 'smooth', block: 'start' });
                history.replaceState(null, '', '#' + id);
            }
        });
    });

    if (contentSections.length && navLinks.length) {
        var observer = new IntersectionObserver(function (entries) {
            entries.forEach(function (entry) {
                if (entry.isIntersecting) {
                    setActiveNav(entry.target.getAttribute('data-policy-section'));
                }
            });
        }, { rootMargin: '-20% 0px -55% 0px', threshold: 0 });

        contentSections.forEach(function (el) { observer.observe(el); });
    }

    function scrollToHash() {
        var hash = window.location.hash.replace('#', '');
        if (!hash) return;

        var targetId = legacyMap[hash] || hash;
        var target = document.getElementById(targetId);
        if (target) {
            setActiveNav(targetId);
            setTimeout(function () {
                target.scrollIntoView({ behavior: 'smooth', block: 'start' });
            }, 100);
        }
    }

    scrollToHash();
    window.addEventListener('hashchange', scrollToHash);
})();
