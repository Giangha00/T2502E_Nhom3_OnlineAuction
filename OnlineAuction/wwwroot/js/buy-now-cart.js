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
                    throw new Error((data && data.message) || options.failedMessage || 'Unable to add to cart.');
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
        var redirectOnSuccess = button.getAttribute('data-redirect-on-success') !== 'false';

        button.disabled = true;
        button.textContent = addingText;

        addToCart(auctionId, { isLoggedIn: isLoggedIn })
            .then(function (data) {
                if (redirectOnSuccess && data.redirectUrl) {
                    window.location.href = data.redirectUrl;
                    return;
                }

                button.disabled = false;
                button.textContent = originalText;

                var messageTargetId = button.getAttribute('data-message-target');
                if (messageTargetId) {
                    var messageEl = document.getElementById(messageTargetId);
                    if (messageEl) {
                        messageEl.textContent = data.message || 'Added to your orders.';
                        messageEl.classList.remove('hidden');
                    }
                }
            })
            .catch(function (error) {
                button.disabled = false;
                button.textContent = originalText;
                window.alert(error.message || 'Unable to add to cart. Please try again.');
            });
    });
})();
