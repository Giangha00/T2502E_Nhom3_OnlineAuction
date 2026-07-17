(function () {
  const form = document.getElementById("dashboardFilterForm");
  const dateFromInput = document.getElementById("dashboardDateFrom");
  const dateToInput = document.getElementById("dashboardDateTo");
  const applyButton = document.getElementById("dashboardFilterApply");
  const applyLabel = document.getElementById("dashboardFilterApplyLabel");
  const applySpinner = document.getElementById("dashboardFilterApplySpinner");
  const exportLink = document.getElementById("dashboardExportLink");
  const rangeTrigger = document.getElementById("dashboardDateRangePicker");
  const presetButtons = document.querySelectorAll("[data-dashboard-preset]");

  if (!form || !dateFromInput || !dateToInput || !rangeTrigger) {
    return;
  }

  function formatDateInput(date) {
    return moment(date).format("YYYY-MM-DD");
  }

  function formatDisplayRange(start, end) {
    return `${moment(start).format("MM/DD/YYYY")} - ${moment(end).format("MM/DD/YYYY")}`;
  }

  function setDateRange(start, end) {
    dateFromInput.value = formatDateInput(start);
    dateToInput.value = formatDateInput(end);

    const displayInput = document.getElementById("dashboardDateRangeValue");
    const label = rangeTrigger.querySelector(".admin-daterange-label");
    const display = formatDisplayRange(start, end);

    if (displayInput) {
      displayInput.value = display;
    }

    if (label) {
      label.textContent = display;
    }

    updateExportLink(moment(start), moment(end));
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

    setDateRange(picker.startDate, picker.endDate);
  }

  function showLoading() {
    if (!applyButton || !applyLabel || !applySpinner) {
      return;
    }

    applyButton.disabled = true;
    applyLabel.classList.add("hidden");
    applySpinner.classList.remove("hidden");
  }

  function applyPreset(preset) {
    const today = moment().startOf("day");
    let start;
    let end = today.clone();

    switch (preset) {
      case "7":
        start = today.clone().subtract(6, "days");
        break;
      case "30":
        start = today.clone().subtract(29, "days");
        break;
      case "this-month":
        start = today.clone().startOf("month");
        break;
      case "last-month":
        start = today.clone().subtract(1, "month").startOf("month");
        end = today.clone().subtract(1, "month").endOf("month");
        break;
      default:
        return;
    }

    setDateRange(start, end);
    showLoading();
    form.submit();
  }

  presetButtons.forEach(function (button) {
    button.addEventListener("click", function () {
      presetButtons.forEach(function (item) {
        item.classList.remove("is-active");
      });
      button.classList.add("is-active");
      applyPreset(button.getAttribute("data-dashboard-preset"));
    });
  });

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
