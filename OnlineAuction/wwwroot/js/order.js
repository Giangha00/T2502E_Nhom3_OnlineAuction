(function () {
    'use strict';

    var i18n = (window.orderConfig && window.orderConfig.i18n) || {};

    function formatMoney(value) {
        return '$' + value.toLocaleString('en-US', {
            minimumFractionDigits: 2,
            maximumFractionDigits: 2
        });
    }

    function formatCountdown(deadlineMs) {
        var diff = Math.max(0, deadlineMs - Date.now());
        var totalMinutes = Math.floor(diff / 60000);
        var hours = Math.floor(totalMinutes / 60);
        var minutes = totalMinutes % 60;

        if (hours > 0) {
            var hoursTemplate = i18n.remainingHours || '{0}H {1}M REMAINING';
            return hoursTemplate.replace('{0}', hours).replace('{1}', String(minutes).padStart(2, '0'));
        }

        var minutesTemplate = i18n.remainingMinutes || '{0}M REMAINING';
        return minutesTemplate.replace('{0}', String(minutes).padStart(2, '0'));
    }

    function updateDeadlines() {
        document.querySelectorAll('.order-won-card').forEach(function (card) {
            var deadline = card.getAttribute('data-deadline');
            var target = card.querySelector('.order-deadline-countdown');
            if (!deadline || !target) return;

            var deadlineMs = Date.parse(deadline);
            if (Number.isNaN(deadlineMs)) return;

            target.textContent = formatCountdown(deadlineMs);
        });
    }

    function initPaymentOptions() {
        var options = document.querySelectorAll('.order-payment-option');
        if (!options.length) return;

        options.forEach(function (option) {
            option.addEventListener('click', function () {
                options.forEach(function (item) {
                    item.classList.remove('border-blue-600', 'bg-blue-50/30');
                    item.classList.add('border-slate-200');
                });
                option.classList.add('border-blue-600', 'bg-blue-50/30');
                option.classList.remove('border-slate-200');

                var radio = option.querySelector('input[type="radio"]');
                if (radio) radio.checked = true;
            });
        });
    }

    function parseAmount(card, attribute) {
        var value = parseFloat(card.getAttribute(attribute) || '0');
        return Number.isFinite(value) ? value : 0;
    }

    function getSelectedCards() {
        return Array.from(document.querySelectorAll('.order-invoice-card')).filter(function (card) {
            var checkbox = card.querySelector('.order-invoice-select');
            if (!checkbox) return false;
            return checkbox.checked || card.getAttribute('data-mandatory') === 'true';
        });
    }

    function syncHiddenOrderIds(selectedCards) {
        var container = document.getElementById('selectedOrderIdsContainer');
        if (!container) return;

        container.innerHTML = '';
        selectedCards.forEach(function (card) {
            var orderId = card.getAttribute('data-order-id');
            if (!orderId) return;

            var input = document.createElement('input');
            input.type = 'hidden';
            input.name = 'selectedOrderIds';
            input.value = orderId;
            container.appendChild(input);
        });
    }

    function updateSummary() {
        var selectedCards = getSelectedCards();
        var subtotal = 0;
        var shipping = 0;
        var insurance = 0;
        var platformFee = 0;
        var deposit = 0;
        var total = 0;

        selectedCards.forEach(function (card) {
            subtotal += parseAmount(card, 'data-subtotal');
            shipping += parseAmount(card, 'data-shipping');
            insurance += parseAmount(card, 'data-insurance');
            platformFee += parseAmount(card, 'data-platform-fee');
            deposit += parseAmount(card, 'data-deposit');
            total += parseAmount(card, 'data-total');
        });

        var subtotalLabel = document.getElementById('orderSummarySubtotalLabel');
        var subtotalEl = document.getElementById('orderSummarySubtotal');
        var shippingEl = document.getElementById('orderSummaryShipping');
        var insuranceEl = document.getElementById('orderSummaryInsurance');
        var platformFeeEl = document.getElementById('orderSummaryPlatformFee');
        var depositEl = document.getElementById('orderSummaryDeposit');
        var totalEl = document.getElementById('orderSummaryTotal');
        var completeButton = document.getElementById('orderCompleteButton');
        var selectionError = document.getElementById('orderSelectionError');

        if (subtotalLabel) {
            var template = i18n.subtotalTemplate || 'Subtotal ({0} item(s))';
            subtotalLabel.textContent = template.replace('{0}', String(selectedCards.length));
        }

        if (subtotalEl) subtotalEl.textContent = formatMoney(subtotal);
        if (shippingEl) shippingEl.textContent = formatMoney(shipping);
        if (insuranceEl) insuranceEl.textContent = formatMoney(insurance);
        if (platformFeeEl) platformFeeEl.textContent = formatMoney(platformFee);
        if (depositEl) {
            depositEl.textContent = deposit > 0 ? '-' + formatMoney(deposit) : '—';
        }
        if (totalEl) totalEl.textContent = formatMoney(total);

        document.querySelectorAll('.order-invoice-card').forEach(function (card) {
            var checkbox = card.querySelector('.order-invoice-select');
            var isSelected = checkbox && (checkbox.checked || card.getAttribute('data-mandatory') === 'true');
            card.classList.toggle('order-invoice-card--selected', isSelected);
        });

        syncHiddenOrderIds(selectedCards);

        var hasSelection = selectedCards.length > 0;
        if (completeButton) {
            completeButton.disabled = !hasSelection;
        }

        if (selectionError) {
            selectionError.classList.toggle('is-visible', !hasSelection);
            selectionError.textContent = hasSelection
                ? ''
                : (i18n.noSelection || 'Please select at least one product to pay.');
        }
    }

    function initInvoiceSelection() {
        var cards = document.querySelectorAll('.order-invoice-card');
        if (!cards.length) return;

        cards.forEach(function (card) {
            var checkbox = card.querySelector('.order-invoice-select');
            if (!checkbox) return;

            checkbox.addEventListener('change', updateSummary);
        });

        var form = document.getElementById('orderCheckoutForm');
        if (form) {
            form.addEventListener('submit', function (event) {
                updateSummary();
                if (getSelectedCards().length === 0) {
                    event.preventDefault();
                    var selectionError = document.getElementById('orderSelectionError');
                    var message = i18n.noSelection || 'Please select at least one product to pay.';
                    if (selectionError) {
                        selectionError.classList.add('is-visible');
                        selectionError.textContent = message;
                    }
                    if (typeof window.showAlertModal === 'function') {
                        window.showAlertModal({
                            title: (window.confirmModalConfig && window.confirmModalConfig.i18n && window.confirmModalConfig.i18n.errorTitle) || 'Error',
                            message: message,
                            variant: 'danger'
                        });
                    }
                }
            });
        }

        updateSummary();
    }

    function cleanOrderQueryParam(name) {
        if (!window.history || !window.history.replaceState) {
            return;
        }

        var pattern = new RegExp('([?&])' + name + '=1(&|$)');
        var cleanUrl = window.location.pathname + window.location.search
            .replace(pattern, function (_, prefix, suffix) {
                return suffix === '&' ? prefix : '';
            })
            .replace(/\?$/, '')
            .replace(/[?&]$/, '');
        window.history.replaceState({}, document.title, cleanUrl || window.location.pathname);
    }

    function showOrderAlert(title, message) {
        if (!message || typeof window.showAlertModal !== 'function') {
            return;
        }

        window.showAlertModal({
            title: title || 'Success',
            message: message,
            variant: 'success'
        });
    }

    function getCsrfToken() {
        var meta = document.querySelector('meta[name="request-verification-token"]');
        if (meta && meta.getAttribute('content')) {
            return meta.getAttribute('content');
        }

        var input = document.querySelector('#orderCheckoutForm input[name="__RequestVerificationToken"]');
        return input ? input.value : '';
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

    function initClearBuyNow() {
        var button = document.getElementById('clearBuyNowBtn');
        var configEl = document.getElementById('clearBuyNowConfig');
        if (!button || !configEl) {
            return;
        }

        button.addEventListener('click', function () {
            var url = configEl.getAttribute('data-url') || '/Order/ClearBuyNow';
            var confirmOptions = {
                title: configEl.getAttribute('data-title') || 'Clear all Buy Now items?',
                message: configEl.getAttribute('data-message') || '',
                note: configEl.getAttribute('data-note') || '',
                confirmText: configEl.getAttribute('data-confirm') || 'Clear all',
                variant: 'danger'
            };

            var confirmPromise = typeof window.showConfirmModal === 'function'
                ? window.showConfirmModal(confirmOptions)
                : Promise.resolve(window.confirm(confirmOptions.message || confirmOptions.title));

            confirmPromise.then(function (confirmed) {
                if (!confirmed) {
                    return;
                }

                button.disabled = true;

                var token = getCsrfToken();
                var body = new URLSearchParams();
                if (token) {
                    body.append('__RequestVerificationToken', token);
                }

                var headers = {
                    'Content-Type': 'application/x-www-form-urlencoded',
                    'X-Requested-With': 'XMLHttpRequest'
                };
                if (token) {
                    headers.RequestVerificationToken = token;
                }

                fetch(url, {
                    method: 'POST',
                    credentials: 'same-origin',
                    headers: headers,
                    body: body.toString()
                })
                    .then(function (response) {
                        return response.text().then(function (text) {
                            var data = null;
                            if (text) {
                                try {
                                    data = JSON.parse(text);
                                } catch (e) {
                                    data = null;
                                }
                            }

                            return {
                                ok: response.ok,
                                status: response.status,
                                data: data,
                                raw: text
                            };
                        });
                    })
                    .then(function (result) {
                        if (!result.ok || !result.data || result.data.success === false) {
                            button.disabled = false;
                            var errorMessage = (result.data && result.data.message)
                                || (result.status === 400
                                    ? 'Security token expired. Please refresh the page and try again.'
                                    : result.status === 404
                                        ? 'Clear action is unavailable. Please restart the app and try again.'
                                        : 'Unable to clear Buy Now items.');
                            if (typeof window.showAlertModal === 'function') {
                                window.showAlertModal({
                                    title: (window.confirmModalConfig && window.confirmModalConfig.i18n && window.confirmModalConfig.i18n.errorTitle) || 'Error',
                                    message: errorMessage,
                                    variant: 'danger'
                                });
                            }
                            return;
                        }

                        if (typeof result.data.orderCount === 'number') {
                            updateOrderBadge(result.data.orderCount);
                        }

                        window.location.assign(
                            result.data.clearedCount > 0 ? '/Order?cleared=1' : '/Order');
                    })
                    .catch(function () {
                        button.disabled = false;
                        if (typeof window.showAlertModal === 'function') {
                            window.showAlertModal({
                                title: (window.confirmModalConfig && window.confirmModalConfig.i18n && window.confirmModalConfig.i18n.errorTitle) || 'Error',
                                message: 'Unable to clear Buy Now items.',
                                variant: 'danger'
                            });
                        }
                    });
            });
        });
    }

    updateDeadlines();
    window.setInterval(updateDeadlines, 60000);
    initPaymentOptions();
    initInvoiceSelection();
    initClearBuyNow();

    if (window.orderConfig && window.orderConfig.showAddedModal) {
        showOrderAlert(
            i18n.itemAddedTitle || 'Success',
            i18n.itemAddedMessage || 'Item added to Payment Center.');
        cleanOrderQueryParam('added');
    }

    if (window.orderConfig && window.orderConfig.showClearedModal) {
        showOrderAlert(
            i18n.clearedTitle || 'Success',
            i18n.clearedMessage || 'Buy Now items cleared from your orders.');
        cleanOrderQueryParam('cleared');
    }

    if (window.orderConfig && window.orderConfig.paymentError) {
        if (typeof window.showAlertModal === 'function') {
            window.showAlertModal({
                title: (window.confirmModalConfig && window.confirmModalConfig.i18n && window.confirmModalConfig.i18n.errorTitle) || 'Error',
                message: window.orderConfig.paymentError,
                variant: 'danger'
            });
        }
    }
})();
