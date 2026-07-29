(function () {
    'use strict';

    var config = window.fcmConfig;
    if (!config || !config.enabled || !config.vapidKey) {
        return;
    }

    var currentToken = null;

    function getCsrfToken() {
        var meta = document.querySelector('meta[name="request-verification-token"]');
        return meta ? meta.getAttribute('content') : '';
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

    function registerToken(token) {
        currentToken = token;
        return postJson('/Notification/RegisterDevice', {
            fcmToken: token,
            deviceInfo: navigator.userAgent
        }).then(function (res) {
            if (!res.ok) {
                console.warn('[FCM] RegisterDevice failed:', res.status);
            }
            return res;
        });
    }

    function unregisterToken() {
        if (!currentToken) {
            return Promise.resolve();
        }

        var token = currentToken;
        currentToken = null;
        return postJson('/Notification/UnregisterDevice', { fcmToken: token });
    }

    function showToast(title, body, isSuccess) {
        var toast = document.createElement('div');
        var toneClass = isSuccess === false
            ? 'border-red-200 bg-red-50'
            : 'border-slate-200 bg-white';
        toast.className = 'fixed bottom-4 right-4 z-[100] max-w-sm rounded-lg border px-4 py-3 shadow-lg ' + toneClass;
        toast.innerHTML = '<p class="text-sm font-semibold text-slate-900"></p><p class="mt-1 text-xs text-slate-600"></p>';
        toast.querySelector('p').textContent = title || '';
        toast.querySelectorAll('p')[1].textContent = body || '';
        document.body.appendChild(toast);
        setTimeout(function () { toast.remove(); }, 5000);
    }

    function handleForegroundMessage(payload) {
        var data = payload.data || {};
        var title = (payload.notification && payload.notification.title) || 'Notification';
        var body = (payload.notification && payload.notification.body) || '';

        if (window.headerNotifications) {
            if (data.notificationId) {
                window.headerNotifications.prependItem({
                    id: data.notificationId,
                    title: title,
                    message: body,
                    timeAgo: 'Just now',
                    relatedUrl: data.relatedUrl || '/',
                    isRead: false
                });
                window.headerNotifications.refreshBadge();
            } else if (window.realtimeHub && !window.realtimeHub.isConnected()) {
                window.headerNotifications.refresh();
            }
        }

        showToast(title, body, true);
    }

    function loadFirebaseScripts() {
        return new Promise(function (resolve, reject) {
            if (window.firebase && window.firebase.messaging) {
                resolve();
                return;
            }

            var appScript = document.createElement('script');
            appScript.src = 'https://www.gstatic.com/firebasejs/10.14.1/firebase-app-compat.js';
            appScript.onload = function () {
                var msgScript = document.createElement('script');
                msgScript.src = 'https://www.gstatic.com/firebasejs/10.14.1/firebase-messaging-compat.js';
                msgScript.onload = resolve;
                msgScript.onerror = reject;
                document.head.appendChild(msgScript);
            };
            appScript.onerror = reject;
            document.head.appendChild(appScript);
        });
    }

    function initFcm() {
        loadFirebaseScripts()
            .then(function () {
                if (!window.firebase || !config.firebase) {
                    console.warn('[FCM] Firebase scripts or config missing.');
                    return;
                }

                if (!firebase.apps.length) {
                    firebase.initializeApp(config.firebase);
                }

                if (!('Notification' in window) || !('serviceWorker' in navigator)) {
                    console.warn('[FCM] Browser does not support notifications or service workers.');
                    return;
                }

                return navigator.serviceWorker.register('/firebase-messaging-sw.js')
                    .then(function (registration) {
                        var messaging = firebase.messaging();

                        return Notification.requestPermission().then(function (permission) {
                            if (permission !== 'granted') {
                                console.info('[FCM] Notification permission:', permission);
                                return null;
                            }

                            return messaging.getToken({
                                vapidKey: config.vapidKey,
                                serviceWorkerRegistration: registration
                            });
                        });
                    })
                    .then(function (token) {
                        if (token) {
                            console.info('[FCM] Token registered with server.');
                            return registerToken(token);
                        }
                        console.warn('[FCM] No FCM token — push notifications disabled for this browser.');
                    })
                    .then(function () {
                        var messaging = firebase.messaging();
                        messaging.onMessage(handleForegroundMessage);
                    });
            })
            .catch(function (err) {
                console.warn('[FCM] Initialization failed:', err);
            });
    }

    document.addEventListener('DOMContentLoaded', function () {
        initFcm();

        var params = new URLSearchParams(window.location.search);
        var notificationId = params.get('notificationId');
        if (notificationId && window.headerNotifications) {
            window.headerNotifications.markReadById(notificationId);
        }
    });

    if ('serviceWorker' in navigator) {
        navigator.serviceWorker.addEventListener('message', function (event) {
            if (!event.data || event.data.type !== 'fcm-notification-click') return;
            if (event.data.notificationId && window.headerNotifications) {
                window.headerNotifications.markReadById(event.data.notificationId);
            }
            if (event.data.relatedUrl) {
                window.location.href = event.data.relatedUrl;
            }
        });
    }

    document.querySelectorAll('form[action*="Logout"]').forEach(function (form) {
        form.addEventListener('submit', function () {
            unregisterToken();
        });
    });

    window.fcmNotifications = {
        unregister: unregisterToken,
        showToast: showToast
    };
})();
