importScripts('/notification/firebase-config.js');
importScripts('https://www.gstatic.com/firebasejs/10.14.1/firebase-app-compat.js');
importScripts('https://www.gstatic.com/firebasejs/10.14.1/firebase-messaging-compat.js');

if (self.FIREBASE_CONFIG) {
    firebase.initializeApp(self.FIREBASE_CONFIG);
    var messaging = firebase.messaging();

    messaging.onBackgroundMessage(function (payload) {
        var title = (payload.notification && payload.notification.title) || 'RareCard';
        var body = (payload.notification && payload.notification.body) || '';
        var data = payload.data || {};
        var relatedUrl = data.relatedUrl || '/';

        return self.registration.showNotification(title, {
            body: body,
            icon: '/favicon.ico',
            data: {
                relatedUrl: relatedUrl,
                notificationId: data.notificationId || ''
            }
        });
    });

    self.addEventListener('notificationclick', function (event) {
        event.notification.close();
        var data = event.notification.data || {};
        var relatedUrl = data.relatedUrl;
        if (!relatedUrl) return;

        event.waitUntil(
            clients.matchAll({ type: 'window', includeUncontrolled: true }).then(function (clientList) {
                var targetUrl = relatedUrl;
                if (data.notificationId) {
                    var sep = targetUrl.indexOf('?') >= 0 ? '&' : '?';
                    targetUrl = targetUrl + sep + 'notificationId=' + encodeURIComponent(data.notificationId);
                }

                for (var i = 0; i < clientList.length; i++) {
                    var client = clientList[i];
                    if ('focus' in client) {
                        client.postMessage({ type: 'fcm-notification-click', notificationId: data.notificationId, relatedUrl: targetUrl });
                        return client.focus();
                    }
                }
                if (clients.openWindow) {
                    return clients.openWindow(targetUrl);
                }
            })
        );
    });
}
