(function () {
    'use strict';

    var switcher = document.getElementById('languageSwitcher');
    var button = document.getElementById('languageButton');
    var dropdown = document.getElementById('languageDropdown');

    if (!switcher || !button || !dropdown) return;

    button.addEventListener('click', function (e) {
        e.stopPropagation();
        closeNotifications();
        var isHidden = dropdown.classList.toggle('hidden');
        button.setAttribute('aria-expanded', String(!isHidden));
    });

    document.addEventListener('click', function (event) {
        if (!switcher.contains(event.target)) {
            dropdown.classList.add('hidden');
            button.setAttribute('aria-expanded', 'false');
        }
    });

    function closeNotifications() {
        var notifDropdown = document.getElementById('notificationDropdown');
        var notifButton = document.getElementById('notificationButton');
        if (notifDropdown) notifDropdown.classList.add('hidden');
        if (notifButton) notifButton.setAttribute('aria-expanded', 'false');
    }
})();
