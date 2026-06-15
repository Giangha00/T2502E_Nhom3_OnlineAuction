(function () {
    'use strict';

    function formatCountdown(deadlineMs) {
        var diff = Math.max(0, deadlineMs - Date.now());
        var totalMinutes = Math.floor(diff / 60000);
        var hours = Math.floor(totalMinutes / 60);
        var minutes = totalMinutes % 60;

        if (hours > 0) {
            return hours + 'H ' + String(minutes).padStart(2, '0') + 'M REMAINING';
        }

        return String(minutes).padStart(2, '0') + 'M REMAINING';
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

    updateDeadlines();
    window.setInterval(updateDeadlines, 60000);
    initPaymentOptions();
})();
