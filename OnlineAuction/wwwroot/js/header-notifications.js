(function () {
    'use strict';

    var POLL_INTERVAL_MS = 15000;

    var root = document.getElementById('headerNotifications');
    if (!root) return;

    var button = document.getElementById('notificationButton');
    var dropdown = document.getElementById('notificationDropdown');
    var badge = document.getElementById('notificationBadge');
    var readAllBtn = document.getElementById('readAllNotifications');
    var listEl = document.getElementById('notificationList');
    var emptyEl = document.getElementById('notificationEmpty');

    function getCsrfToken() {
        var meta = document.querySelector('meta[name="request-verification-token"]');
        return meta ? meta.getAttribute('content') : '';
    }

    function getItems() {
        return Array.from(document.querySelectorAll('.notification-item'));
    }

    function markItemReadUi(el) {
        el.classList.remove('is-unread', 'bg-blue-50/40');
        el.classList.add('is-read');
        var dot = el.querySelector('.notification-dot');
        if (dot) dot.classList.replace('bg-blue-600', 'bg-transparent');
    }

    function updateBadge(unreadCount) {
        if (!badge) return;

        if (unreadCount > 0) {
            badge.textContent = unreadCount > 9 ? '9+' : String(unreadCount);
            badge.classList.remove('hidden');
        } else {
            badge.classList.add('hidden');
        }
    }

    function countUnreadFromDom() {
        return getItems().filter(function (el) {
            return el.classList.contains('is-unread');
        }).length;
    }

    function postJson(url, body) {
        return fetch(url, {
            method: 'POST',
            credentials: 'same-origin',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': getCsrfToken()
            },
            body: JSON.stringify(body || {})
        });
    }

    function markReadOnServer(notificationId) {
        return postJson('/Notification/MarkRead/' + notificationId, {});
    }

    function markAllReadOnServer() {
        return postJson('/Notification/MarkAllRead', {});
    }

    function escapeHtml(text) {
        var div = document.createElement('div');
        div.textContent = text || '';
        return div.innerHTML;
    }

    function buildNotificationButton(item) {
        var isRead = !!item.isRead;
        var li = document.createElement('li');
        li.innerHTML =
            '<button type="button" class="notification-item flex w-full gap-3 border-b border-slate-50 px-4 py-3 text-left transition hover:bg-slate-50 ' +
            (isRead ? 'is-read' : 'is-unread bg-blue-50/40') + '"' +
            ' data-notification-id="' + item.id + '"' +
            ' data-notification-url="' + escapeHtml(item.relatedUrl || '') + '">' +
            '<span class="notification-dot mt-1.5 h-2 w-2 shrink-0 rounded-full ' +
            (isRead ? 'bg-transparent' : 'bg-blue-600') + '"></span>' +
            '<span class="min-w-0 flex-1">' +
            '<span class="block text-sm font-medium text-slate-900">' + escapeHtml(item.title) + '</span>' +
            '<span class="mt-0.5 block text-xs leading-relaxed text-slate-500">' + escapeHtml(item.message) + '</span>' +
            '<span class="mt-1 block text-[11px] text-slate-400">' + escapeHtml(item.timeAgo) + '</span>' +
            '</span></button>';

        var btn = li.querySelector('.notification-item');
        bindItemClick(btn);
        return li;
    }

    function renderNotificationList(notifications) {
        if (!listEl) return;

        listEl.innerHTML = '';
        (notifications || []).forEach(function (item) {
            listEl.appendChild(buildNotificationButton(item));
        });

        if (emptyEl) {
            emptyEl.classList.toggle('hidden', notifications && notifications.length > 0);
        }
    }

    function refreshFromServer() {
        return fetch('/Notification/List', { credentials: 'same-origin' })
            .then(function (res) {
                if (!res.ok) return null;
                return res.json();
            })
            .then(function (data) {
                if (!data) return;
                if (typeof data.unreadCount === 'number') {
                    updateBadge(data.unreadCount);
                }
                if (Array.isArray(data.notifications)) {
                    renderNotificationList(data.notifications);
                }
            })
            .catch(function () { /* ignore */ });
    }

    function prependNotificationItem(item) {
        if (!listEl || !item) return;

        var existing = document.querySelector('.notification-item[data-notification-id="' + item.id + '"]');
        if (existing) {
            existing.closest('li').remove();
        }

        listEl.insertBefore(buildNotificationButton(item), listEl.firstChild);
        if (emptyEl) emptyEl.classList.add('hidden');
    }

    function bindItemClick(el) {
        el.addEventListener('click', function () {
            var id = Number(el.getAttribute('data-notification-id'));
            var url = el.getAttribute('data-notification-url');

            markItemReadUi(el);
            markReadOnServer(id)
                .then(function (res) { return res.ok ? res.json() : null; })
                .then(function (data) {
                    if (data && typeof data.unreadCount === 'number') {
                        updateBadge(data.unreadCount);
                    } else {
                        updateBadge(countUnreadFromDom());
                    }
                })
                .catch(function () {
                    updateBadge(countUnreadFromDom());
                });

            if (url) window.location.href = url;
        });
    }

    button.addEventListener('click', function (e) {
        e.stopPropagation();
        closeLanguageDropdown();
        var isHidden = dropdown.classList.toggle('hidden');
        button.setAttribute('aria-expanded', String(!isHidden));
        if (!isHidden) {
            refreshFromServer();
        }
    });

    readAllBtn.addEventListener('click', function (e) {
        e.stopPropagation();
        getItems().forEach(markItemReadUi);
        markAllReadOnServer()
            .then(function (res) { return res.ok ? res.json() : null; })
            .then(function (data) {
                updateBadge(data && typeof data.unreadCount === 'number' ? data.unreadCount : 0);
            })
            .catch(function () {
                updateBadge(0);
            });
    });

    getItems().forEach(bindItemClick);

    document.addEventListener('click', function (event) {
        if (!root.contains(event.target)) {
            dropdown.classList.add('hidden');
            button.setAttribute('aria-expanded', 'false');
        }
    });

    document.addEventListener('visibilitychange', function () {
        if (document.visibilityState === 'visible') {
            refreshFromServer();
        }
    });

    setInterval(refreshFromServer, POLL_INTERVAL_MS);

    function closeLanguageDropdown() {
        var langDropdown = document.getElementById('languageDropdown');
        var langButton = document.getElementById('languageButton');
        if (langDropdown) langDropdown.classList.add('hidden');
        if (langButton) langButton.setAttribute('aria-expanded', 'false');
    }

    window.headerNotifications = {
        refresh: refreshFromServer,
        refreshBadge: function (unreadCount) {
            if (typeof unreadCount === 'number') {
                updateBadge(unreadCount);
            } else {
                refreshFromServer();
            }
        },
        prependItem: prependNotificationItem,
        markReadById: function (notificationId) {
            var el = document.querySelector('.notification-item[data-notification-id="' + notificationId + '"]');
            if (el) markItemReadUi(el);
            markReadOnServer(notificationId)
                .then(function (res) { return res.ok ? res.json() : null; })
                .then(function (data) {
                    if (data && typeof data.unreadCount === 'number') {
                        updateBadge(data.unreadCount);
                    }
                })
                .catch(function () { /* ignore */ });
        }
    };
})();
