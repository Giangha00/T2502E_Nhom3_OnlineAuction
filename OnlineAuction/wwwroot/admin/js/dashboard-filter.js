(function () {
  const form = document.getElementById("dashboardFilterForm");
  const dateFromInput = document.getElementById("dashboardDateFrom");
  const dateToInput = document.getElementById("dashboardDateTo");
  const applyButton = document.getElementById("dashboardFilterApply");
  const applyLabel = document.getElementById("dashboardFilterApplyLabel");
  const applySpinner = document.getElementById("dashboardFilterApplySpinner");
  const exportLink = document.getElementById("dashboardExportLink");
  const rangeTrigger = document.getElementById("dashboardDateRangePicker");

  if (!form || !dateFromInput || !dateToInput || !rangeTrigger) {
    return;
  }

  function updateExportLink(startMoment, endMoment) {
    if (!exportLink) {
      return;
    }

    const url = new URL(exportLink.href, window.location.origin);
    const from = startMoment
      ? startMoment.format("YYYY-MM-DD")
      : dateFromInput.value;
    const to = endMoment ? endMoment.format("YYYY-MM-DD") : dateToInput.value;

    url.searchParams.set("dateFrom", from);
    url.searchParams.set("dateTo", to);
    url.searchParams.delete("dateRange");
    exportLink.href = url.toString();
  }

  function syncHiddenDates(picker) {
    if (!picker) {
      return;
    }

    dateFromInput.value = picker.startDate.format("YYYY-MM-DD");
    dateToInput.value = picker.endDate.format("YYYY-MM-DD");
    updateExportLink(picker.startDate, picker.endDate);
  }

  function showLoading() {
    if (!applyButton || !applyLabel || !applySpinner) {
      return;
    }

    applyButton.disabled = true;
    applyLabel.classList.add("hidden");
    applySpinner.classList.remove("hidden");
  }

  form.addEventListener("submit", showLoading);

  if (
    window.AdminListFilter &&
    typeof window.AdminListFilter.setupDateRangePicker === "function"
  ) {
    window.AdminListFilter.setupDateRangePicker(
      "#dashboardDateRangePicker",
      function (picker) {
        if (!picker) {
          return;
        }

        syncHiddenDates(picker);
        showLoading();
        form.submit();
      },
      {
        allowClear: false,
        maxSpan: { days: 365 },
        locale: {
          cancelLabel: "Cancel",
        },
      },
    );
  }

  updateExportLink();
})();
