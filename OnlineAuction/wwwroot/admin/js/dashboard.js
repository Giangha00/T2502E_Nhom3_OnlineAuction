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
  const labels = chartData.labels || {};
  const isDarkMode = document.documentElement.classList.contains("dark");
  const labelColor = isDarkMode ? "#98A2B3" : "#667085";
  const gridColor = isDarkMode ? "#1D2939" : "#E4E7EC";
  const palette = ["#465fff", "#12B76A", "#F79009", "#F04438", "#7A5AF8", "#667085"];

  const baseChartOptions = {
    chart: {
      fontFamily: "Outfit, sans-serif",
      toolbar: { show: false },
      foreColor: labelColor,
    },
    dataLabels: { enabled: false },
    grid: {
      borderColor: gridColor,
      strokeDashArray: 4,
    },
    tooltip: { theme: isDarkMode ? "dark" : "light" },
  };

  function formatCurrency(value) {
    return `$${Number(value || 0).toLocaleString(undefined, {
      minimumFractionDigits: 0,
      maximumFractionDigits: 0,
    })}`;
  }

  function renderRevenueDonutChart() {
    const revenue = chartData.revenue || {};
    const buyerFee = Number(revenue.buyerFee || 0);
    const sellerFee = Number(revenue.sellerFee || 0);
    const total = buyerFee + sellerFee;
    const chartElement = document.querySelector("#dashboard-revenue-donut-chart");
    const emptyElement = document.querySelector("#dashboard-revenue-donut-empty");

    if (!chartElement) {
      return;
    }

    if (total <= 0) {
      chartElement.innerHTML = "";
      emptyElement?.classList.remove("hidden");
      return;
    }

    emptyElement?.classList.add("hidden");

    const donutChart = new ApexCharts(chartElement, {
      chart: {
        ...baseChartOptions.chart,
        type: "donut",
        height: 300,
      },
      series: [buyerFee, sellerFee],
      labels: [
        labels.buyerCheckoutFees || "Buyer checkout fees",
        labels.sellerSuccessFees || "Seller success fees",
      ],
      colors: ["#12B76A", "#F79009"],
      legend: {
        position: "bottom",
        labels: { colors: labelColor },
      },
      plotOptions: {
        pie: {
          donut: {
            size: "68%",
            labels: {
              show: true,
              name: { color: labelColor },
              value: {
                color: labelColor,
                formatter: (value) => formatCurrency(value),
              },
              total: {
                show: true,
                label: labels.commission || "Commission",
                color: labelColor,
                formatter: () => formatCurrency(total),
              },
            },
          },
        },
      },
      tooltip: {
        theme: isDarkMode ? "dark" : "light",
        y: {
          formatter: (value) => formatCurrency(value),
        },
      },
    });

    donutChart.render();
  }

  function renderAuctionStatusChart() {
    const status = chartData.auctionStatus || {};
    const ongoing = Number(status.ongoing || 0);
    const ended = Number(status.ended || 0);
    const cancelled = Number(status.cancelled || 0);
    const total = ongoing + ended + cancelled;
    const chartElement = document.querySelector("#dashboard-auction-status-chart");
    const emptyElement = document.querySelector("#dashboard-auction-status-empty");

    if (!chartElement) {
      return;
    }

    if (total <= 0) {
      chartElement.innerHTML = "";
      emptyElement?.classList.remove("hidden");
      return;
    }

    emptyElement?.classList.add("hidden");

    const statusChart = new ApexCharts(chartElement, {
      chart: {
        ...baseChartOptions.chart,
        type: "bar",
        height: 280,
      },
      series: [
        {
          name: "Auctions",
          data: [ongoing, ended, cancelled],
        },
      ],
      plotOptions: {
        bar: {
          borderRadius: 8,
          columnWidth: "48%",
          distributed: true,
        },
      },
      colors: ["#465fff", "#12B76A", "#F04438"],
      xaxis: {
        categories: [
          labels.ongoing || "Ongoing",
          labels.ended || "Ended",
          labels.cancelled || "Cancelled",
        ],
        labels: { style: { colors: labelColor } },
      },
      yaxis: {
        labels: {
          style: { colors: labelColor },
          formatter: (value) => Math.round(value).toLocaleString(),
        },
      },
      dataLabels: {
        enabled: true,
        formatter: (value) => Math.round(value).toLocaleString(),
        style: {
          fontSize: "12px",
          fontWeight: 600,
        },
      },
      legend: { show: false },
      grid: {
        borderColor: gridColor,
        strokeDashArray: 4,
      },
    });

    statusChart.render();
  }

  function renderCategoryChart() {
    const categoryBreakdown = chartData.categoryBreakdown || [];
    const chartElement = document.querySelector("#dashboard-category-chart");
    const emptyElement = document.querySelector("#dashboard-category-empty");

    if (!chartElement) {
      return;
    }

    if (categoryBreakdown.length === 0) {
      chartElement.innerHTML = "";
      emptyElement?.classList.remove("hidden");
      return;
    }

    emptyElement?.classList.add("hidden");

    const totalVolume = categoryBreakdown.reduce(
      (sum, item) => sum + Number(item.bidVolume || 0),
      0,
    );

    const categoryChart = new ApexCharts(chartElement, {
      chart: {
        ...baseChartOptions.chart,
        type: "donut",
        height: 300,
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
      colors: palette,
      legend: {
        position: "bottom",
        labels: { colors: labelColor },
      },
      plotOptions: {
        pie: {
          donut: {
            size: "62%",
            labels: {
              show: true,
              total: {
                show: true,
                label: "Total bids",
                color: labelColor,
                formatter: () => formatCurrency(totalVolume),
              },
            },
          },
        },
      },
      tooltip: {
        theme: isDarkMode ? "dark" : "light",
        y: {
          formatter: function (value, opts) {
            const item = categoryBreakdown[opts.seriesIndex];
            if (!item) {
              return formatCurrency(value);
            }

            return `${formatCurrency(value)} | ${item.bidCount} bids | ${item.percentage}%`;
          },
        },
      },
      dataLabels: { enabled: false },
    });

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
    const chartElement = document.querySelector("#dashboard-registration-chart");
    const emptyElement = document.querySelector("#dashboard-registration-empty");

    if (!chartElement) {
      return;
    }

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
        type: "area",
        height: 300,
      },
      colors: ["#7A5AF8"],
      stroke: {
        curve: "smooth",
        width: 3,
      },
      fill: {
        type: "gradient",
        gradient: {
          shadeIntensity: 0.35,
          opacityFrom: 0.45,
          opacityTo: 0.05,
        },
      },
      plotOptions: {
        bar: {
          borderRadius: 6,
          columnWidth: "45%",
        },
      },
      xaxis: {
        labels: { style: { colors: labelColor } },
        categories: series.map((point) => point.label),
      },
      yaxis: {
        labels: {
          style: { colors: labelColor },
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
          item.classList.remove("is-active");
        });

        tab.classList.add("is-active");

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

  renderRevenueDonutChart();
  renderAuctionStatusChart();
  renderCategoryChart();
  renderRegistrationChart(activeRegistrationGranularity);
  bindRegistrationTabs();
})();
