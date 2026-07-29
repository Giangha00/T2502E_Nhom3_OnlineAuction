(function () {
    'use strict';

    function getCsrfToken() {
        var meta = document.querySelector('meta[name="request-verification-token"]');
        return meta ? meta.getAttribute('content') : '';
    }

    function openAuthModal() {
        if (typeof window.openAuthModal === 'function') {
            window.openAuthModal('login');
        }
    }

    function updateOrderBadge(count) {
        if (window.realtimeHub && typeof window.realtimeHub.updateOrderBadge === 'function') {
            window.realtimeHub.updateOrderBadge(count);
            return;
        }

        var badge = document.getElementById('orderCountBadge');
        var link = document.getElementById('orderNavLink');
        if (!badge || typeof count !== 'number') {
            return;
        }

        if (count > 0) {
            badge.textContent = count > 9 ? '9+' : String(count);
            badge.classList.remove('hidden');
            if (link) link.setAttribute('data-order-count', String(count));
        } else {
            badge.classList.add('hidden');
            if (link) link.setAttribute('data-order-count', '0');
        }
    }

    function notify(message, isSuccess) {
        var text = message || '';
        if (!text) {
            return;
        }

        if (typeof window.showAlertModal === 'function') {
            window.showAlertModal({
                title: isSuccess === false
                    ? ((window.confirmModalConfig && window.confirmModalConfig.i18n && window.confirmModalConfig.i18n.errorTitle) || 'Error')
                    : ((window.confirmModalConfig && window.confirmModalConfig.i18n && window.confirmModalConfig.i18n.successTitle) || 'Success'),
                message: text,
                variant: isSuccess === false ? 'danger' : 'success'
            });
            return;
        }

        window.alert(text);
    }

    function addToCart(auctionId, options) {
        options = options || {};
        var isLoggedIn = options.isLoggedIn !== false;

        if (!auctionId) {
            return Promise.reject(new Error('Missing listing id.'));
        }

        if (!isLoggedIn) {
            openAuthModal();
            return Promise.reject(new Error('Please sign in to continue.'));
        }

        var body = new URLSearchParams();
        body.append('auctionId', String(auctionId));

        var csrfToken = getCsrfToken();
        if (csrfToken) {
            body.append('__RequestVerificationToken', csrfToken);
        }

        return fetch('/BuyNow/AddToCart', {
            method: 'POST',
            credentials: 'same-origin',
            headers: {
                'Content-Type': 'application/x-www-form-urlencoded',
                'X-Requested-With': 'XMLHttpRequest'
            },
            body: body.toString()
        }).then(function (response) {
            if (response.status === 401) {
                openAuthModal();
                throw new Error(options.signInMessage || 'Please sign in to continue.');
            }

            return response.json().then(function (data) {
                if (!response.ok || !data.success) {
                    var err = new Error((data && data.message) || options.failedMessage || 'Unable to add to cart.');
                    err.serverMessage = (data && data.message) || '';
                    throw err;
                }

                return data;
            });
        });
    }

    window.buyNowCart = {
        add: addToCart
    };

    document.addEventListener('click', function (event) {
        var button = event.target.closest('.buy-now-add-to-cart');
        if (!button) {
            return;
        }

        event.preventDefault();
        event.stopPropagation();

        var auctionId = button.getAttribute('data-auction-id');
        var isLoggedIn = button.getAttribute('data-is-logged-in') === 'true';
        var originalText = button.getAttribute('data-label') || button.textContent;
        var addingText = button.getAttribute('data-adding-label') || 'Adding…';
        var failedText = button.getAttribute('data-failed-message') || 'Unable to add to cart. Please try again.';
        var successText = button.getAttribute('data-success-message') || '';
        var redirectOnSuccess = button.getAttribute('data-redirect-on-success') === 'true';

        button.disabled = true;
        button.textContent = addingText;

        addToCart(auctionId, { isLoggedIn: isLoggedIn })
            .then(function (data) {
                if (typeof data.orderCount === 'number') {
                    updateOrderBadge(data.orderCount);
                }

                if (redirectOnSuccess && data.redirectUrl) {
                    window.location.href = data.redirectUrl;
                    return;
                }

                button.disabled = false;
                button.textContent = originalText;

                var messageTargetId = button.getAttribute('data-message-target');
                var message = successText || data.message || 'Added to your orders.';
                if (messageTargetId) {
                    var messageEl = document.getElementById(messageTargetId);
                    if (messageEl) {
                        messageEl.textContent = message;
                        messageEl.classList.remove('hidden');
                    }
                }

                notify(message, true);
            })
            .catch(function (error) {
                button.disabled = false;
                button.textContent = originalText;
                if (error && error.message === 'Please sign in to continue.') {
                    return;
                }
                var msg = (error && error.serverMessage) || failedText;
                notify(msg, false);
            });
    });
})();
