(function () {
  const dataElement = document.getElementById("dashboard-chart-data");
  if (!dataElement || typeof ApexCharts === "undefined") {
    return;
  }

  let chartData;

  try {
    chartData = JSON.parse(dataElement.textContent);
  } catch {
    return;
  }

  const filter = chartData.filter || {};
  const isDarkMode = document.documentElement.classList.contains("dark");
  const labelColor = isDarkMode ? "#98A2B3" : "#667085";
  const gridColor = isDarkMode ? "#1D2939" : "#E4E7EC";

  const baseChartOptions = {
    chart: {
      fontFamily: "Outfit, sans-serif",
      toolbar: { show: false },
      foreColor: labelColor,
    },
    dataLabels: { enabled: false },
    stroke: { curve: "smooth", width: 3 },
    grid: {
      borderColor: gridColor,
      strokeDashArray: 4,
    },
    xaxis: {
      labels: { style: { colors: labelColor } },
    },
    yaxis: {
      labels: { style: { colors: labelColor } },
    },
    tooltip: { theme: isDarkMode ? "dark" : "light" },
  };

  function renderCategoryChart() {
    const categoryBreakdown = chartData.categoryBreakdown || [];

    if (categoryBreakdown.length === 0) {
      document
        .querySelector("#dashboard-category-empty")
        ?.classList.remove("hidden");
      return;
    }

    const categoryChart = new ApexCharts(
      document.querySelector("#dashboard-category-chart"),
      {
        chart: {
          ...baseChartOptions.chart,
          type: "pie",
          height: 320,
          events: {
            dataPointSelection: function (_event, _chartContext, config) {
              const item = categoryBreakdown[config.dataPointIndex];
              if (!item || !item.categoryId) {
                return;
              }

              window.location.href = `/Admin/Auction?categoryId=${item.categoryId}`;
            },
          },
        },
        series: categoryBreakdown.map((item) => Number(item.bidVolume || 0)),
        labels: categoryBreakdown.map((item) => item.categoryName),
        colors: [
          "#465fff",
          "#12B76A",
          "#F79009",
          "#F04438",
          "#7A5AF8",
          "#667085",
        ],
        legend: {
          position: "bottom",
          labels: { colors: labelColor },
        },
        tooltip: {
          theme: isDarkMode ? "dark" : "light",
          y: {
            formatter: function (value, opts) {
              const item = categoryBreakdown[opts.seriesIndex];
              if (!item) {
                return value;
              }

              return `$${Number(value).toLocaleString()} | ${item.bidCount} bids | ${item.percentage}%`;
            },
          },
        },
        dataLabels: { enabled: true },
      },
    );

    categoryChart.render();
  }

  let registrationChart = null;
  let activeRegistrationGranularity = filter.registrationGranularity || "day";

  function getRegistrationSeries(granularity) {
    const registration = chartData.registration || {};
    return registration[granularity] || [];
  }

  function renderRegistrationChart(granularity) {
    activeRegistrationGranularity = granularity;
    const series = getRegistrationSeries(granularity);
    const total = series.reduce(
      (sum, point) => sum + Number(point.value || 0),
      0,
    );
    const chartElement = document.querySelector(
      "#dashboard-registration-chart",
    );
    const emptyElement = document.querySelector(
      "#dashboard-registration-empty",
    );

    if (total <= 0) {
      if (registrationChart) {
        registrationChart.destroy();
        registrationChart = null;
      }

      chartElement.innerHTML = "";
      emptyElement?.classList.remove("hidden");
      return;
    }

    emptyElement?.classList.add("hidden");

    const options = {
      ...baseChartOptions,
      series: [
        {
          name: "Registrations",
          data: series.map((point) => Number(point.value || 0)),
        },
      ],
      chart: {
        ...baseChartOptions.chart,
        type: "bar",
        height: 280,
      },
      colors: ["#465fff"],
      plotOptions: {
        bar: {
          borderRadius: 6,
          columnWidth: "45%",
        },
      },
      xaxis: {
        ...baseChartOptions.xaxis,
        categories: series.map((point) => point.label),
      },
      yaxis: {
        ...baseChartOptions.yaxis,
        labels: {
          ...baseChartOptions.yaxis.labels,
          formatter: (value) => Math.round(value).toLocaleString(),
        },
      },
    };

    if (registrationChart) {
      registrationChart.destroy();
    }

    registrationChart = new ApexCharts(chartElement, options);
    registrationChart.render();
  }

  function bindRegistrationTabs() {
    const tabs = document.querySelectorAll(".dashboard-registration-tab");

    tabs.forEach(function (tab) {
      tab.addEventListener("click", function () {
        const granularity =
          tab.getAttribute("data-registration-granularity") || "day";

        tabs.forEach(function (item) {
          item.classList.remove(
            "border-brand-500",
            "bg-brand-500",
            "text-white",
          );
          item.classList.add(
            "border-gray-300",
            "text-gray-700",
            "dark:border-gray-700",
            "dark:text-gray-300",
          );
        });

        tab.classList.add("border-brand-500", "bg-brand-500", "text-white");
        tab.classList.remove(
          "border-gray-300",
          "text-gray-700",
          "dark:border-gray-700",
          "dark:text-gray-300",
        );

        const granularityInput = document.querySelector(
          'input[name="registrationGranularity"]',
        );
        if (granularityInput) {
          granularityInput.value = granularity;
        }

        renderRegistrationChart(granularity);
      });
    });
  }

  renderCategoryChart();
  renderRegistrationChart(activeRegistrationGranularity);
  bindRegistrationTabs();
})();
