(function () {
    'use strict';

    var STORAGE_KEY = 'rarecard_read_notifications';
    var root = document.getElementById('headerNotifications');
    if (!root) return;

    var button = document.getElementById('notificationButton');
    var dropdown = document.getElementById('notificationDropdown');
    var badge = document.getElementById('notificationBadge');
    var readAllBtn = document.getElementById('readAllNotifications');
    var items = Array.from(document.querySelectorAll('.notification-item'));

    function getReadIds() {
        try {
            var raw = localStorage.getItem(STORAGE_KEY);
            return raw ? JSON.parse(raw) : [];
        } catch (e) {
            return [];
        }
    }

    function saveReadIds(ids) {
        try {
            localStorage.setItem(STORAGE_KEY, JSON.stringify(ids));
        } catch (e) { /* ignore */ }
    }

    function markItemRead(el, readIds) {
        var id = Number(el.getAttribute('data-notification-id'));
        if (!readIds.includes(id)) readIds.push(id);
        el.classList.remove('is-unread', 'bg-blue-50/40');
        el.classList.add('is-read');
        var dot = el.querySelector('.notification-dot');
        if (dot) dot.classList.replace('bg-blue-600', 'bg-transparent');
        return readIds;
    }

    function updateBadge(readIds) {
        var unread = items.filter(function (el) {
            var id = Number(el.getAttribute('data-notification-id'));
            return !readIds.includes(id);
        }).length;

        if (!badge) return;

        if (unread > 0) {
            badge.textContent = unread > 9 ? '9+' : String(unread);
            badge.classList.remove('hidden');
        } else {
            badge.classList.add('hidden');
        }
    }

    function applyStoredState() {
        var readIds = getReadIds();
        items.forEach(function (el) {
            var id = Number(el.getAttribute('data-notification-id'));
            if (readIds.includes(id)) markItemRead(el, readIds);
        });
        updateBadge(readIds);
    }

    button.addEventListener('click', function (e) {
        e.stopPropagation();
        closeLanguageDropdown();
        var isHidden = dropdown.classList.toggle('hidden');
        button.setAttribute('aria-expanded', String(!isHidden));
    });

    readAllBtn.addEventListener('click', function (e) {
        e.stopPropagation();
        var readIds = getReadIds();
        items.forEach(function (el) {
            readIds = markItemRead(el, readIds);
        });
        saveReadIds(readIds);
        updateBadge(readIds);
    });

    items.forEach(function (el) {
        el.addEventListener('click', function () {
            var readIds = getReadIds();
            readIds = markItemRead(el, readIds);
            saveReadIds(readIds);
            updateBadge(readIds);

            var url = el.getAttribute('data-notification-url');
            if (url) window.location.href = url;
        });
    });

    document.addEventListener('click', function (event) {
        if (!root.contains(event.target)) {
            dropdown.classList.add('hidden');
            button.setAttribute('aria-expanded', 'false');
        }
    });

    function closeLanguageDropdown() {
        var langDropdown = document.getElementById('languageDropdown');
        var langButton = document.getElementById('languageButton');
        if (langDropdown) langDropdown.classList.add('hidden');
        if (langButton) langButton.setAttribute('aria-expanded', 'false');
    }

    applyStoredState();
})();
