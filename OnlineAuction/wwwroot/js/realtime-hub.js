(function () {
    'use strict';

    var connection = null;
    var joinedAuctionId = null;
    var isConnected = false;

    function loadSignalR() {
        return new Promise(function (resolve, reject) {
            if (window.signalR && window.signalR.HubConnectionBuilder) {
                resolve();
                return;
            }

            var script = document.createElement('script');
            script.src = 'https://cdnjs.cloudflare.com/ajax/libs/microsoft-signalr/8.0.7/signalr.min.js';
            script.onload = resolve;
            script.onerror = reject;
            document.head.appendChild(script);
        });
    }

    function updateOrderBadge(count) {
        var badge = document.getElementById('orderCountBadge');
        var link = document.getElementById('orderNavLink');
        if (!badge) return;

        if (count > 0) {
            badge.textContent = count > 9 ? '9+' : String(count);
            badge.classList.remove('hidden');
            if (link) link.setAttribute('data-order-count', String(count));
        } else {
            badge.classList.add('hidden');
            if (link) link.setAttribute('data-order-count', '0');
        }
    }

    function handleNotificationReceived(payload) {
        if (!payload) return;

        if (window.headerNotifications) {
            if (payload.notification) {
                window.headerNotifications.prependItem(payload.notification);
            }
            if (typeof payload.unreadCount === 'number') {
                window.headerNotifications.refreshBadge(payload.unreadCount);
            } else {
                window.headerNotifications.refreshBadge();
            }
        }

        if (payload.notification) {
            showNotificationToast(payload.notification.title, payload.notification.message);
        }
    }

    function showNotificationToast(title, message) {
        var text = [title, message].filter(Boolean).join(' — ');
        if (!text) {
            return;
        }

        var toast = document.createElement('div');
        toast.className = 'fixed bottom-4 right-4 z-[100] max-w-sm rounded-lg border border-slate-200 bg-white px-4 py-3 shadow-lg';
        toast.innerHTML = '<p class="text-sm font-semibold text-slate-900"></p><p class="mt-1 text-xs text-slate-600"></p>';
        toast.querySelector('p').textContent = title || 'Notification';
        toast.querySelectorAll('p')[1].textContent = message || '';
        document.body.appendChild(toast);
        window.setTimeout(function () { toast.remove(); }, 5000);
    }

    function handleBidUpdated(payload) {
        if (!payload) return;
        window.dispatchEvent(new CustomEvent('auction:bid-updated', { detail: payload }));
    }

    function connectHub() {
        return loadSignalR().then(function () {
            connection = new signalR.HubConnectionBuilder()
                .withUrl('/hubs/app', { withCredentials: true })
                .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
                .configureLogging(signalR.LogLevel.Warning)
                .build();

            connection.on('NotificationReceived', handleNotificationReceived);
            connection.on('OrderCountUpdated', function (payload) {
                if (payload && typeof payload.orderCount === 'number') {
                    updateOrderBadge(payload.orderCount);
                }
            });
            connection.on('BidUpdated', handleBidUpdated);

            connection.onreconnected(function () {
                isConnected = true;
                if (joinedAuctionId) {
                    connection.invoke('JoinAuction', joinedAuctionId).catch(function () { /* ignore */ });
                }
            });

            connection.onclose(function () {
                isConnected = false;
            });

            return connection.start().then(function () {
                isConnected = true;
            });
        });
    }

    function joinAuction(auctionId) {
        if (!auctionId) return Promise.resolve();
        joinedAuctionId = auctionId;
        if (!connection || connection.state !== signalR.HubConnectionState.Connected) {
            return Promise.resolve();
        }
        return connection.invoke('JoinAuction', auctionId).catch(function () { /* ignore */ });
    }

    function leaveAuction(auctionId) {
        if (!connection || connection.state !== signalR.HubConnectionState.Connected) {
            return Promise.resolve();
        }
        return connection.invoke('LeaveAuction', auctionId).catch(function () { /* ignore */ });
    }

    window.realtimeHub = {
        joinAuction: joinAuction,
        leaveAuction: leaveAuction,
        isConnected: function () { return isConnected; }
    };

    document.addEventListener('DOMContentLoaded', function () {
        connectHub().then(function () {
            var config = window.productDetailConfig;
            if (config && config.auctionId) {
                joinAuction(config.auctionId);
            }
        }).catch(function () {
            isConnected = false;
        });

        window.addEventListener('beforeunload', function () {
            if (joinedAuctionId) {
                leaveAuction(joinedAuctionId);
            }
        });
    });
})();
