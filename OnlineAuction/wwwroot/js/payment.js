(function () {
    const form = document.getElementById('paymentForm');
    if (!form) return;

    const paymentMethods = document.querySelectorAll('input[name="paymentMethod"]');
    const cardDetails = document.getElementById('cardDetails');
    const bankDetails = document.getElementById('bankDetails');

    function updatePaymentDetails() {
        const selected = document.querySelector('input[name="paymentMethod"]:checked');
        if (!selected) return;

        const method = selected.value;
        cardDetails?.classList.toggle('hidden', method !== 'card');
        bankDetails?.classList.toggle('hidden', method !== 'bank-transfer');
    }

    paymentMethods.forEach(function (radio) {
        radio.addEventListener('change', updatePaymentDetails);
    });
    updatePaymentDetails();

    function setError(id, message) {
        const el = document.getElementById(id);
        if (el) el.textContent = message;
    }

    function clearErrors() {
        ['fullNameError', 'phoneError', 'emailError', 'addressError', 'cityError', 'paymentMethodError', 'termsError']
            .forEach(function (id) { setError(id, ''); });
    }

    function isValidEmail(email) {
        return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
    }

    form.addEventListener('submit', function (event) {
        event.preventDefault();
        clearErrors();

        let valid = true;
        const fullName = document.getElementById('fullName')?.value.trim() || '';
        const phone = document.getElementById('phone')?.value.trim() || '';
        const email = document.getElementById('email')?.value.trim() || '';
        const address = document.getElementById('address')?.value.trim() || '';
        const city = document.getElementById('city')?.value.trim() || '';
        const agreeTerms = document.getElementById('agreeTerms')?.checked;
        const selectedMethod = document.querySelector('input[name="paymentMethod"]:checked');

        if (!fullName) {
            setError('fullNameError', 'Full name is required.');
            valid = false;
        }
        if (!phone) {
            setError('phoneError', 'Phone number is required.');
            valid = false;
        }
        if (!email || !isValidEmail(email)) {
            setError('emailError', 'Please enter a valid email address.');
            valid = false;
        }
        if (!address) {
            setError('addressError', 'Street address is required.');
            valid = false;
        }
        if (!city) {
            setError('cityError', 'City is required.');
            valid = false;
        }
        if (!selectedMethod) {
            setError('paymentMethodError', 'Please select a payment method.');
            valid = false;
        }
        if (!agreeTerms) {
            setError('termsError', 'You must agree to the payment policy.');
            valid = false;
        }

        if (!valid) return;

        const orderRef = document.getElementById('orderRef')?.value || '';
        const auctionName = document.getElementById('auctionName')?.value || '';
        const totalAmount = document.getElementById('totalAmount')?.value || '0';
        const methodLabel = selectedMethod.getAttribute('data-label') || selectedMethod.value;

        const params = new URLSearchParams({
            orderRef: orderRef,
            auctionName: auctionName,
            total: totalAmount,
            method: methodLabel
        });

        window.location.href = '/Payment/Confirmation?' + params.toString();
    });
})();
