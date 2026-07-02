(function () {
    const form = document.getElementById("dashboardFilterForm");
    const dateFromInput = document.getElementById("dashboardDateFrom");
    const dateToInput = document.getElementById("dashboardDateTo");
    const applyButton = document.getElementById("dashboardFilterApply");
    const applyLabel = document.getElementById("dashboardFilterApplyLabel");
    const applySpinner = document.getElementById("dashboardFilterApplySpinner");
    const exportLink = document.getElementById("dashboardExportLink");

    if (!form || !dateFromInput || !dateToInput) {
        return;
    }

    function formatDate(date) {
        const year = date.getFullYear();
        const month = String(date.getMonth() + 1).padStart(2, "0");
        const day = String(date.getDate()).padStart(2, "0");
        return `${year}-${month}-${day}`;
    }

    function utcToday() {
        const now = new Date();
        return new Date(Date.UTC(now.getFullYear(), now.getMonth(), now.getDate()));
    }

    function applyPreset(preset) {
        const today = utcToday();
        let from = today;
        let to = today;

        switch (preset) {
            case "7d":
                from = new Date(today);
                from.setUTCDate(from.getUTCDate() - 6);
                break;
            case "30d":
                from = new Date(today);
                from.setUTCDate(from.getUTCDate() - 29);
                break;
            case "thisMonth":
                from = new Date(Date.UTC(today.getUTCFullYear(), today.getUTCMonth(), 1));
                break;
            case "lastMonth":
                from = new Date(Date.UTC(today.getUTCFullYear(), today.getUTCMonth() - 1, 1));
                to = new Date(Date.UTC(today.getUTCFullYear(), today.getUTCMonth(), 0));
                break;
            default:
                return;
        }

        dateFromInput.value = formatDate(from);
        dateToInput.value = formatDate(to);
        updateExportLink();
    }

    function updateExportLink() {
        if (!exportLink) {
            return;
        }

        const url = new URL(exportLink.href, window.location.origin);
        url.searchParams.set("dateFrom", dateFromInput.value);
        url.searchParams.set("dateTo", dateToInput.value);
        url.searchParams.delete("dateRange");
        exportLink.href = url.toString();
    }

    document.querySelectorAll("[data-dashboard-preset]").forEach(function (button) {
        button.addEventListener("click", function () {
            applyPreset(button.getAttribute("data-dashboard-preset"));
        });
    });

    dateFromInput.addEventListener("change", updateExportLink);
    dateToInput.addEventListener("change", updateExportLink);

    form.addEventListener("submit", function () {
        if (!applyButton || !applyLabel || !applySpinner) {
            return;
        }

        applyButton.disabled = true;
        applyLabel.classList.add("hidden");
        applySpinner.classList.remove("hidden");
    });

    updateExportLink();
})();
