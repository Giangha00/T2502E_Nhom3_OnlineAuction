(function () {
    const form = document.getElementById("contactForm");
    if (!form) return;

    const fullNameInput = document.getElementById("fullName");
    const emailInput = document.getElementById("email");
    const messageInput = document.getElementById("message");

    const nameErrorSpan = document.getElementById("nameError");
    const emailErrorSpan = document.getElementById("emailError");
    const messageErrorSpan = document.getElementById("messageError");

    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

    // Real-time validation
    fullNameInput?.addEventListener("blur", validateName);
    emailInput?.addEventListener("blur", validateEmail);
    messageInput?.addEventListener("blur", validateMessage);

    function validateName() {
        clearError(nameErrorSpan, fullNameInput);
        if (fullNameInput.value.trim() === "") {
            showError(nameErrorSpan, fullNameInput, "This field is required");
            return false;
        }
        return true;
    }

    function validateEmail() {
        clearError(emailErrorSpan, emailInput);
        if (emailInput.value.trim() === "") {
            showError(emailErrorSpan, emailInput, "This field is required");
            return false;
        } else if (!emailRegex.test(emailInput.value)) {
            showError(emailErrorSpan, emailInput, "Invalid email format");
            return false;
        }
        return true;
    }

    function validateMessage() {
        clearError(messageErrorSpan, messageInput);
        if (messageInput.value.trim() === "") {
            showError(messageErrorSpan, messageInput, "This field is required");
            return false;
        }
        return true;
    }

    function showError(errorSpan, input, message) {
        errorSpan.innerText = message;
        errorSpan.style.display = "block";
        input.classList.add("border-red-500", "focus:border-red-500", "focus:ring-red-500");
        input.classList.remove("border-stone-300", "focus:border-amber-500", "focus:ring-amber-500");
    }

    function clearError(errorSpan, input) {
        errorSpan.innerText = "";
        errorSpan.style.display = "none";
        input.classList.remove("border-red-500", "focus:border-red-500", "focus:ring-red-500");
        input.classList.add("border-stone-300", "focus:border-amber-500", "focus:ring-amber-500");
    }

    form.addEventListener("submit", function (e) {
        e.preventDefault();

        // Clear all errors first
        nameErrorSpan.innerText = "";
        emailErrorSpan.innerText = "";
        messageErrorSpan.innerText = "";

        let isValid = true;

        // Validate all fields
        if (!validateName()) isValid = false;
        if (!validateEmail()) isValid = false;
        if (!validateMessage()) isValid = false;

        if (isValid) {
            // Show success feedback
            const submitBtn = form.querySelector("button[type='submit']");
            const originalText = submitBtn.innerText;

            submitBtn.innerText = "✓ Message Sent Successfully!";
            submitBtn.disabled = true;
            submitBtn.classList.add("bg-green-700", "hover:bg-green-700");
            submitBtn.classList.remove("bg-amber-700", "hover:bg-amber-800");

            // Reset form
            form.reset();

            // Reset button after 3 seconds
            setTimeout(() => {
                submitBtn.innerText = originalText;
                submitBtn.disabled = false;
                submitBtn.classList.remove("bg-green-700", "hover:bg-green-700");
                submitBtn.classList.add("bg-amber-700", "hover:bg-amber-800");
            }, 3000);
        }
    });
})();
