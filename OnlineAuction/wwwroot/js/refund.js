(function () {
    const form = document.getElementById('refundForm');
    if (!form) return;

    const i18n = (window.refundConfig && window.refundConfig.i18n) || {};

    const orderSelect = document.getElementById('orderReference');
    const manualOrderField = document.getElementById('manualOrderField');
    const refundAmount = document.getElementById('refundAmount');

    function setError(id, message) {
        const el = document.getElementById(id);
        if (el) el.textContent = message;
    }

    function clearErrors() {
        ['orderReferenceError', 'fullNameError', 'emailError', 'refundReasonError', 'descriptionError', 'agreePolicyError']
            .forEach(function (id) { setError(id, ''); });
    }

    function isValidEmail(email) {
        return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
    }

    if (orderSelect) {
        orderSelect.addEventListener('change', function () {
            const isOther = orderSelect.value === 'other';
            manualOrderField?.classList.toggle('hidden', !isOther);

            if (!isOther && orderSelect.selectedOptions[0]) {
                const amount = orderSelect.selectedOptions[0].getAttribute('data-amount');
                if (amount && refundAmount && !refundAmount.value) {
                    refundAmount.placeholder = amount;
                }
            }
        });
    }

    form.addEventListener('submit', function (event) {
        event.preventDefault();
        clearErrors();

        let valid = true;
        const orderValue = orderSelect?.value || '';
        const manualRef = document.getElementById('manualOrderRef')?.value.trim() || '';
        const fullName = document.getElementById('fullName')?.value.trim() || '';
        const email = document.getElementById('email')?.value.trim() || '';
        const reason = document.getElementById('refundReason')?.value || '';
        const description = document.getElementById('description')?.value.trim() || '';
        const agreePolicy = document.getElementById('agreePolicy')?.checked;

        let orderRef = orderValue;
        if (!orderValue) {
            setError('orderReferenceError', i18n.orderRequired || 'Please select an order.');
            valid = false;
        } else if (orderValue === 'other' && !manualRef) {
            setError('orderReferenceError', i18n.manualRequired || 'Please enter your order reference.');
            valid = false;
        } else if (orderValue === 'other') {
            orderRef = manualRef;
        }

        if (!fullName) {
            setError('fullNameError', i18n.fullNameRequired || 'Full name is required.');
            valid = false;
        }
        if (!email || !isValidEmail(email)) {
            setError('emailError', i18n.emailInvalid || 'Please enter a valid email address.');
            valid = false;
        }
        if (!reason) {
            setError('refundReasonError', i18n.reasonRequired || 'Please select a refund reason.');
            valid = false;
        }
        if (!description || description.length < 20) {
            setError('descriptionError', i18n.descriptionRequired || 'Please provide a detailed description (at least 20 characters).');
            valid = false;
        }
        if (!agreePolicy) {
            setError('agreePolicyError', i18n.policyRequired || 'You must agree to the refund policy.');
            valid = false;
        }

        if (!valid) return;

        const requestId = 'RF-' + new Date().toISOString().slice(0, 10).replace(/-/g, '') + '-' + String(Math.floor(Math.random() * 9000) + 1000);
        const params = new URLSearchParams({
            requestId: requestId,
            orderRef: orderRef,
            reason: reason
        });

        window.location.href = '/Refund/Confirmation?' + params.toString();
    });
})();
