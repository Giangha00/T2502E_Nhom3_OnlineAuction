(function () {
    const STORAGE_KEY = 'auctionPaymentMethods';
    const form = document.getElementById('paymentInfoForm');
    if (!form) return;

    const listEl = document.getElementById('savedPaymentList');
    const emptyState = document.getElementById('paymentEmptyState');
    const countBadge = document.getElementById('savedCountBadge');
    const notification = document.getElementById('paymentNotification');
    const editingIdInput = document.getElementById('editingCardId');
    const saveBtn = document.getElementById('savePaymentBtn');

    const fields = {
        cardType: () => document.querySelector('input[name="cardType"]:checked'),
        cardNumber: () => document.getElementById('cardNumber'),
        cardHolder: () => document.getElementById('cardHolder'),
        expiryMonth: () => document.getElementById('expiryMonth'),
        expiryYear: () => document.getElementById('expiryYear'),
        cardCvv: () => document.getElementById('cardCvv'),
        billingAddress: () => document.getElementById('billingAddress'),
        setAsDefault: () => document.getElementById('setAsDefault')
    };

    const preview = {
        card: document.getElementById('cardPreview'),
        type: document.getElementById('previewType'),
        number: document.getElementById('previewNumber'),
        holder: document.getElementById('previewHolder'),
        expiry: document.getElementById('previewExpiry')
    };

    let methods = loadMethods();

    function loadMethods() {
        try {
            const stored = localStorage.getItem(STORAGE_KEY);
            if (stored) return JSON.parse(stored);
        } catch (e) { /* ignore */ }
        return window.paymentInfoConfig?.initialMethods || [];
    }

    function persistMethods() {
        localStorage.setItem(STORAGE_KEY, JSON.stringify(methods));
    }

    function typeLabel(type) {
        if (type === 'mastercard') return 'Mastercard';
        if (type === 'other') return 'Other';
        return 'Visa';
    }

    function typeIcon(type) {
        if (type === 'mastercard') return 'MC';
        if (type === 'other') return '••';
        return 'VISA';
    }

    function maskNumber(lastFour) {
        return '**** **** **** ' + (lastFour || '0000');
    }

    function onlyDigits(value) {
        return (value || '').replace(/\D/g, '');
    }

    function formatCardNumber(value) {
        const digits = onlyDigits(value).slice(0, 16);
        return digits.replace(/(\d{4})(?=\d)/g, '$1 ').trim();
    }

    function luhnCheck(num) {
        let sum = 0;
        let shouldDouble = false;
        for (let i = num.length - 1; i >= 0; i--) {
            let digit = parseInt(num.charAt(i), 10);
            if (shouldDouble) {
                digit *= 2;
                if (digit > 9) digit -= 9;
            }
            sum += digit;
            shouldDouble = !shouldDouble;
        }
        return sum % 10 === 0;
    }

    function isExpired(month, year) {
        if (!month || !year) return false;
        const now = new Date();
        const expYear = 2000 + parseInt(year, 10);
        const expMonth = parseInt(month, 10) - 1;
        const expDate = new Date(expYear, expMonth + 1, 0, 23, 59, 59);
        return expDate < now;
    }

    function setError(id, message) {
        const el = document.getElementById(id);
        if (el) el.textContent = message;
    }

    function clearErrors() {
        ['cardNumberError', 'cardHolderError', 'expiryError', 'cvvError', 'billingError']
            .forEach(function (id) { setError(id, ''); });
    }

    function showNotification(message, type) {
        if (!notification) return;
        notification.textContent = message;
        notification.classList.remove('hidden', 'is-success', 'is-error');
        notification.classList.add(type === 'success' ? 'is-success' : 'is-error');
        window.clearTimeout(showNotification._timer);
        showNotification._timer = window.setTimeout(function () {
            notification.classList.add('hidden');
        }, 4000);
    }

    function updatePreview() {
        const type = fields.cardType()?.value || 'visa';
        const number = onlyDigits(fields.cardNumber()?.value || '');
        const holder = (fields.cardHolder()?.value || 'YOUR NAME').toUpperCase();
        const month = fields.expiryMonth()?.value || 'MM';
        const year = fields.expiryYear()?.value || 'YY';

        if (preview.card) {
            preview.card.classList.remove('card-preview--visa', 'card-preview--mastercard', 'card-preview--other');
            preview.card.classList.add('card-preview--' + type);
            preview.card.classList.add('is-flipping');
            window.setTimeout(function () {
                preview.card?.classList.remove('is-flipping');
            }, 300);
        }

        if (preview.type) preview.type.textContent = type === 'mastercard' ? 'MASTERCARD' : type === 'other' ? 'CARD' : 'VISA';

        let displayNumber = '**** **** **** ****';
        if (number.length > 0) {
            const padded = (number + '0000000000000000').slice(0, 16);
            displayNumber = padded.replace(/(\d{4})(?=\d)/g, '$1 ').trim();
        }
        if (preview.number) preview.number.textContent = displayNumber;
        if (preview.holder) preview.holder.textContent = holder || 'YOUR NAME';
        if (preview.expiry) preview.expiry.textContent = month + '/' + year;
    }

    function getCardNumberForSave() {
        const raw = fields.cardNumber()?.value || '';
        const digits = onlyDigits(raw);
        const editingId = editingIdInput.value;

        if (editingId && raw.includes('*')) {
            const existing = methods.find(function (m) { return m.id === editingId; });
            if (existing) return { digits: null, lastFour: existing.lastFour, unchanged: true };
        }

        return { digits: digits, lastFour: digits.slice(-4), unchanged: false };
    }

    function validateForm() {
        clearErrors();
        let valid = true;

        const cardInfo = getCardNumberForSave();
        const cardNumber = cardInfo.digits || '';
        const holder = (fields.cardHolder()?.value || '').trim();
        const month = fields.expiryMonth()?.value || '';
        const year = fields.expiryYear()?.value || '';
        const cvv = onlyDigits(fields.cardCvv()?.value || '');
        const billing = (fields.billingAddress()?.value || '').trim();

        if (!cardInfo.unchanged) {
            if (!cardNumber) {
                setError('cardNumberError', 'Invalid card number');
                valid = false;
            } else if (cardNumber.length < 13 || cardNumber.length > 16 || !luhnCheck(cardNumber)) {
                setError('cardNumberError', 'Invalid card number');
                valid = false;
            }
        }

        if (!holder) {
            setError('cardHolderError', 'Card holder name is required');
            valid = false;
        } else if (!/^[a-zA-Z\s]+$/.test(holder)) {
            setError('cardHolderError', 'No special characters allowed');
            valid = false;
        }

        if (!month || !year) {
            setError('expiryError', 'Expiry date is required');
            valid = false;
        } else if (isExpired(month, year)) {
            setError('expiryError', 'Card has expired');
            valid = false;
        }

        if (!cvv) {
            setError('cvvError', 'Invalid CVV');
            valid = false;
        } else if (cvv.length < 3 || cvv.length > 4) {
            setError('cvvError', 'Invalid CVV');
            valid = false;
        }

        if (!billing) {
            setError('billingError', 'Billing address is required');
            valid = false;
        }

        return valid;
    }

    function buildCardHtml(method) {
        const defaultBadge = method.isDefault
            ? '<span class="rounded-full bg-amber-100 px-3 py-1 text-xs font-semibold uppercase tracking-wide text-amber-800">Default</span>'
            : '';
        const setDefaultBtn = method.isDefault
            ? ''
            : '<button type="button" data-action="set-default" class="rounded-lg border border-stone-200 px-3 py-1.5 text-xs font-medium text-amber-700 transition hover:border-amber-500 hover:bg-amber-50">Set Default</button>';

        return (
            '<article class="payment-card-item rounded-2xl border border-stone-200 bg-white p-5 shadow-sm transition hover:border-amber-300"' +
            ' data-card-id="' + method.id + '"' +
            ' data-card-type="' + method.cardType + '"' +
            ' data-holder="' + escapeAttr(method.holderName) + '"' +
            ' data-expiry-month="' + method.expiryMonth + '"' +
            ' data-expiry-year="' + method.expiryYear + '"' +
            ' data-billing="' + escapeAttr(method.billingAddress) + '"' +
            ' data-last-four="' + method.lastFour + '">' +
            '<div class="flex flex-wrap items-start justify-between gap-3">' +
            '<div class="flex items-center gap-3">' +
            '<span class="flex h-11 w-11 items-center justify-center rounded-xl bg-gradient-to-br from-amber-600 to-stone-800 text-xs font-bold text-white">' + typeIcon(method.cardType) + '</span>' +
            '<div>' +
            '<p class="text-sm font-semibold text-stone-900">' + typeLabel(method.cardType) + '</p>' +
            '<p class="mt-1 font-mono text-sm tracking-wider text-stone-600">' + method.maskedNumber + '</p>' +
            '<p class="mt-1 text-xs text-stone-500">Expires: ' + method.expiryMonth + '/' + method.expiryYear + '</p>' +
            '</div></div>' + defaultBadge + '</div>' +
            '<div class="mt-4 flex flex-wrap gap-2 border-t border-stone-100 pt-4">' +
            '<button type="button" data-action="edit" class="rounded-lg border border-stone-200 px-3 py-1.5 text-xs font-medium text-stone-700 transition hover:border-amber-500 hover:text-amber-700">Edit</button>' +
            '<button type="button" data-action="remove" class="rounded-lg border border-stone-200 px-3 py-1.5 text-xs font-medium text-red-600 transition hover:border-red-300 hover:bg-red-50">Remove</button>' +
            setDefaultBtn +
            '</div></article>'
        );
    }

    function escapeAttr(value) {
        return String(value || '').replace(/"/g, '&quot;').replace(/\n/g, '&#10;');
    }

    function renderList() {
        if (!listEl) return;

        if (methods.length === 0) {
            listEl.innerHTML = '';
            listEl.classList.add('hidden');
            emptyState?.classList.remove('hidden');
        } else {
            listEl.classList.remove('hidden');
            emptyState?.classList.add('hidden');
            listEl.innerHTML = methods.map(buildCardHtml).join('');
        }

        if (countBadge) {
            countBadge.textContent = methods.length + ' method' + (methods.length === 1 ? '' : 's');
        }
    }

    function resetForm() {
        form.reset();
        editingIdInput.value = '';
        document.querySelector('input[name="cardType"][value="visa"]').checked = true;
        saveBtn.textContent = 'Save Payment Information';
        document.querySelectorAll('.payment-card-item.is-editing').forEach(function (el) {
            el.classList.remove('is-editing');
        });
        clearErrors();
        updatePreview();
    }

    function populateFormFromCard(cardEl) {
        const type = cardEl.dataset.cardType || 'visa';
        const radio = document.querySelector('input[name="cardType"][value="' + type + '"]');
        if (radio) radio.checked = true;

        const lastFour = cardEl.dataset.lastFour || '0000';
        fields.cardNumber().value = '**** **** **** ' + lastFour;
        fields.cardHolder().value = cardEl.dataset.holder || '';
        fields.expiryMonth().value = cardEl.dataset.expiryMonth || '';
        fields.expiryYear().value = cardEl.dataset.expiryYear || '';
        fields.billingAddress().value = (cardEl.dataset.billing || '').replace(/&#10;/g, '\n');
        fields.cardCvv().value = '';
        fields.setAsDefault().checked = !!cardEl.querySelector('.bg-amber-100');

        editingIdInput.value = cardEl.dataset.cardId || '';
        saveBtn.textContent = 'Save Changes';

        document.querySelectorAll('.payment-card-item').forEach(function (el) {
            el.classList.toggle('is-editing', el === cardEl);
        });

        updatePreview();
        form.scrollIntoView({ behavior: 'smooth', block: 'start' });
    }

    form.addEventListener('submit', function (event) {
        event.preventDefault();

        if (!validateForm()) {
            showNotification('Unable to save payment information', 'error');
            return;
        }

        const cardInfo = getCardNumberForSave();
        const lastFour = cardInfo.unchanged ? cardInfo.lastFour : cardInfo.lastFour;
        const editingId = editingIdInput.value;
        const isDefault = fields.setAsDefault()?.checked || false;

        const newMethod = {
            id: editingId || 'card-' + Date.now(),
            cardType: fields.cardType()?.value || 'visa',
            maskedNumber: maskNumber(lastFour),
            lastFour: lastFour,
            holderName: fields.cardHolder()?.value.trim().toUpperCase(),
            expiryMonth: fields.expiryMonth()?.value || '',
            expiryYear: fields.expiryYear()?.value || '',
            billingAddress: fields.billingAddress()?.value.trim(),
            isDefault: isDefault
        };

        if (isDefault) {
            methods.forEach(function (m) { m.isDefault = false; });
        }

        if (editingId) {
            const index = methods.findIndex(function (m) { return m.id === editingId; });
            if (index >= 0) methods[index] = newMethod;
        } else {
            if (methods.length === 0) newMethod.isDefault = true;
            methods.push(newMethod);
        }

        if (!methods.some(function (m) { return m.isDefault; }) && methods.length > 0) {
            methods[0].isDefault = true;
        }

        persistMethods();
        renderList();
        resetForm();
        showNotification('Payment information saved successfully!', 'success');
    });

    document.getElementById('cancelPaymentBtn')?.addEventListener('click', resetForm);

    document.getElementById('scrollToFormBtn')?.addEventListener('click', function () {
        form.scrollIntoView({ behavior: 'smooth', block: 'start' });
    });

    listEl?.addEventListener('click', function (event) {
        const btn = event.target.closest('[data-action]');
        if (!btn) return;

        const cardEl = btn.closest('.payment-card-item');
        if (!cardEl) return;

        const cardId = cardEl.dataset.cardId;
        const action = btn.dataset.action;

        if (action === 'edit') {
            populateFormFromCard(cardEl);
            return;
        }

        if (action === 'remove') {
            methods = methods.filter(function (m) { return m.id !== cardId; });
            if (methods.length > 0 && !methods.some(function (m) { return m.isDefault; })) {
                methods[0].isDefault = true;
            }
            persistMethods();
            renderList();
            if (editingIdInput.value === cardId) resetForm();
            showNotification('Payment method removed', 'success');
            return;
        }

        if (action === 'set-default') {
            methods.forEach(function (m) { m.isDefault = m.id === cardId; });
            persistMethods();
            renderList();
            showNotification('Default payment method updated', 'success');
        }
    });

    fields.cardNumber()?.addEventListener('input', function (event) {
        const input = event.target;
        if (!input.value.includes('*')) {
            input.value = formatCardNumber(input.value);
        }
        updatePreview();
    });

    ['cardHolder', 'expiryMonth', 'expiryYear'].forEach(function (id) {
        document.getElementById(id)?.addEventListener('input', updatePreview);
        document.getElementById(id)?.addEventListener('change', updatePreview);
    });

    document.querySelectorAll('input[name="cardType"]').forEach(function (radio) {
        radio.addEventListener('change', updatePreview);
    });

    renderList();
    updatePreview();
})();
