(function () {
    'use strict';

    var form = document.getElementById('contactForm');
    if (!form) return;

    var i18n = (window.contactConfig && window.contactConfig.i18n) || {};

    var fullNameInput = document.getElementById('fullName');
    var emailInput = document.getElementById('email');
    var messageInput = document.getElementById('message');

    var nameErrorSpan = document.getElementById('nameError');
    var emailErrorSpan = document.getElementById('emailError');
    var messageErrorSpan = document.getElementById('messageError');

    var emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

    fullNameInput?.addEventListener('blur', validateName);
    emailInput?.addEventListener('blur', validateEmail);
    messageInput?.addEventListener('blur', validateMessage);

    function validateName() {
        clearError(nameErrorSpan, fullNameInput);
        if (fullNameInput.value.trim() === '') {
            showError(nameErrorSpan, fullNameInput, i18n.required || 'This field is required');
            return false;
        }
        return true;
    }

    function validateEmail() {
        clearError(emailErrorSpan, emailInput);
        if (emailInput.value.trim() === '') {
            showError(emailErrorSpan, emailInput, 'This field is required');
            return false;
        }
        if (!emailRegex.test(emailInput.value)) {
            showError(emailErrorSpan, emailInput, i18n.emailInvalid || 'Invalid email format');
            return false;
        }
        return true;
    }

    function validateMessage() {
        clearError(messageErrorSpan, messageInput);
        if (messageInput.value.trim() === '') {
            showError(messageErrorSpan, messageInput, i18n.required || 'This field is required');
            return false;
        }
        return true;
    }

    function showError(errorSpan, input, message) {
        errorSpan.textContent = message;
        errorSpan.classList.remove('hidden');
        input.classList.add('border-red-400', 'ring-2', 'ring-red-100');
    }

    function clearError(errorSpan, input) {
        errorSpan.textContent = '';
        errorSpan.classList.add('hidden');
        input.classList.remove('border-red-400', 'ring-2', 'ring-red-100');
    }

    form.addEventListener('submit', function (e) {
        e.preventDefault();

        var isValid = validateName() & validateEmail() & validateMessage();

        if (!isValid) return;

        var submitBtn = form.querySelector('button[type="submit"]');
        var originalHtml = submitBtn.innerHTML;

        submitBtn.textContent = i18n.submitted || '✓ Request Submitted';
        submitBtn.disabled = true;
        submitBtn.classList.add('bg-emerald-700', 'hover:bg-emerald-700');
        submitBtn.classList.remove('bg-blue-700', 'hover:bg-blue-800');

        form.reset();

        setTimeout(function () {
            submitBtn.innerHTML = originalHtml;
            submitBtn.disabled = false;
            submitBtn.classList.remove('bg-emerald-700', 'hover:bg-emerald-700');
            submitBtn.classList.add('bg-blue-700', 'hover:bg-blue-800');
        }, 3000);
    });
})();
