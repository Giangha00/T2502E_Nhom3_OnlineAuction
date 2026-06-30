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
            foreColor: labelColor
        },
        dataLabels: { enabled: false },
        stroke: { curve: "smooth", width: 3 },
        grid: {
            borderColor: gridColor,
            strokeDashArray: 4
        },
        xaxis: {
            labels: { style: { colors: labelColor } }
        },
        yaxis: {
            labels: { style: { colors: labelColor } }
        },
        tooltip: { theme: isDarkMode ? "dark" : "light" }
    };

    function buildDashboardUrl(params) {
        const url = new URL(window.location.href);
        const merged = {
            dateFrom: filter.dateFrom,
            dateTo: filter.dateTo,
            status: filter.status || "",
            categoryId: filter.categoryId || "",
            registrationDate: "",
            registrationGranularity: filter.registrationGranularity || "day",
            ...params
        };

        Object.entries(merged).forEach(([key, value]) => {
            if (value === null || value === undefined || value === "") {
                url.searchParams.delete(key);
            } else {
                url.searchParams.set(key, value);
            }
        });

        return url.toString();
    }

    function renderRevenueChart() {
        const revenueSeries = chartData.revenue || [];
        const revenueTotal = revenueSeries.reduce((sum, point) => sum + Number(point.value || 0), 0);

        if (revenueTotal <= 0) {
            document.querySelector("#dashboard-revenue-empty")?.classList.remove("hidden");
            return;
        }

        const revenueChart = new ApexCharts(document.querySelector("#dashboard-revenue-chart"), {
            ...baseChartOptions,
            series: [{
                name: "Revenue",
                data: revenueSeries.map((point) => Number(point.value || 0))
            }],
            chart: {
                ...baseChartOptions.chart,
                type: "area",
                height: 280
            },
            colors: ["#465fff"],
            fill: {
                type: "gradient",
                gradient: {
                    shadeIntensity: 1,
                    opacityFrom: 0.35,
                    opacityTo: 0.05,
                    stops: [0, 100]
                }
            },
            xaxis: {
                ...baseChartOptions.xaxis,
                categories: revenueSeries.map((point) => point.label)
            },
            yaxis: {
                ...baseChartOptions.yaxis,
                labels: {
                    ...baseChartOptions.yaxis.labels,
                    formatter: (value) => `$${Math.round(value).toLocaleString()}`
                }
            }
        });

        revenueChart.render();
    }

    function renderBidsChart() {
        const bidsSeries = chartData.bids || [];
        const bidsTotal = bidsSeries.reduce((sum, point) => sum + Number(point.value || 0), 0);

        if (bidsTotal <= 0) {
            document.querySelector("#dashboard-bids-empty")?.classList.remove("hidden");
            return;
        }

        const bidsChart = new ApexCharts(document.querySelector("#dashboard-bids-chart"), {
            ...baseChartOptions,
            series: [{
                name: "Bids",
                data: bidsSeries.map((point) => Number(point.value || 0))
            }],
            chart: {
                ...baseChartOptions.chart,
                type: "bar",
                height: 280
            },
            colors: ["#12B76A"],
            plotOptions: {
                bar: {
                    borderRadius: 6,
                    columnWidth: "45%"
                }
            },
            xaxis: {
                ...baseChartOptions.xaxis,
                categories: bidsSeries.map((point) => point.label)
            },
            yaxis: {
                ...baseChartOptions.yaxis,
                labels: {
                    ...baseChartOptions.yaxis.labels,
                    formatter: (value) => Math.round(value).toLocaleString()
                }
            }
        });

        bidsChart.render();
    }

    function renderStatusChart() {
        const statusBreakdown = chartData.statusBreakdown || [];

        if (statusBreakdown.length === 0) {
            document.querySelector("#dashboard-status-empty")?.classList.remove("hidden");
            return;
        }

        const statusChart = new ApexCharts(document.querySelector("#dashboard-status-chart"), {
            chart: {
                ...baseChartOptions.chart,
                type: "donut",
                height: 320,
                events: {
                    dataPointSelection: function (_event, _chartContext, config) {
                        const item = statusBreakdown[config.dataPointIndex];
                        if (!item || !item.status) {
                            return;
                        }

                        window.location.href = buildDashboardUrl({
                            status: item.status,
                            categoryId: ""
                        });
                    }
                }
            },
            series: statusBreakdown.map((item) => Number(item.count || 0)),
            labels: statusBreakdown.map((item) => item.label),
            colors: ["#465fff", "#12B76A", "#F79009", "#F04438", "#7A5AF8", "#667085"],
            legend: {
                position: "bottom",
                labels: { colors: labelColor }
            },
            dataLabels: { enabled: true }
        });

        statusChart.render();
    }

    function renderCategoryChart() {
        const categoryBreakdown = chartData.categoryBreakdown || [];

        if (categoryBreakdown.length === 0) {
            document.querySelector("#dashboard-category-empty")?.classList.remove("hidden");
            return;
        }

        const categoryChart = new ApexCharts(document.querySelector("#dashboard-category-chart"), {
            chart: {
                ...baseChartOptions.chart,
                type: "pie",
                height: 320,
                events: {
                    dataPointSelection: function (_event, _chartContext, config) {
                        const item = categoryBreakdown[config.dataPointIndex];
                        if (!item) {
                            return;
                        }

                        if (item.categoryId) {
                            window.location.href = `/Admin/Auction?categoryId=${item.categoryId}`;
                            return;
                        }

                        window.location.href = buildDashboardUrl({
                            categoryId: "",
                            status: filter.status || ""
                        });
                    }
                }
            },
            series: categoryBreakdown.map((item) => Number(item.bidVolume || 0)),
            labels: categoryBreakdown.map((item) => item.categoryName),
            colors: ["#465fff", "#12B76A", "#F79009", "#F04438", "#7A5AF8", "#667085"],
            legend: {
                position: "bottom",
                labels: { colors: labelColor }
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
                    }
                }
            },
            dataLabels: { enabled: true }
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
        const total = series.reduce((sum, point) => sum + Number(point.value || 0), 0);
        const chartElement = document.querySelector("#dashboard-registration-chart");
        const emptyElement = document.querySelector("#dashboard-registration-empty");

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
            series: [{
                name: "Registrations",
                data: series.map((point) => Number(point.value || 0))
            }],
            chart: {
                ...baseChartOptions.chart,
                type: "bar",
                height: 280,
                events: {
                    dataPointSelection: function (_event, _chartContext, config) {
                        const point = series[config.dataPointIndex];
                        if (!point || !point.filterKey) {
                            return;
                        }

                        const targetUrl = buildDashboardUrl({
                            registrationDate: point.filterKey,
                            registrationGranularity: granularity
                        });

                        window.location.href = targetUrl;
                        setTimeout(function () {
                            document.getElementById("dashboard-new-users")?.scrollIntoView({ behavior: "smooth" });
                        }, 100);
                    }
                }
            },
            colors: ["#465fff"],
            plotOptions: {
                bar: {
                    borderRadius: 6,
                    columnWidth: "45%"
                }
            },
            xaxis: {
                ...baseChartOptions.xaxis,
                categories: series.map((point) => point.label)
            },
            yaxis: {
                ...baseChartOptions.yaxis,
                labels: {
                    ...baseChartOptions.yaxis.labels,
                    formatter: (value) => Math.round(value).toLocaleString()
                }
            }
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
                const granularity = tab.getAttribute("data-registration-granularity") || "day";

                tabs.forEach(function (item) {
                    item.classList.remove("border-brand-500", "bg-brand-500", "text-white");
                    item.classList.add("border-gray-300", "text-gray-700", "dark:border-gray-700", "dark:text-gray-300");
                });

                tab.classList.add("border-brand-500", "bg-brand-500", "text-white");
                tab.classList.remove("border-gray-300", "text-gray-700", "dark:border-gray-700", "dark:text-gray-300");

                renderRegistrationChart(granularity);
            });
        });
    }

    renderRevenueChart();
    renderBidsChart();
    renderStatusChart();
    renderCategoryChart();
    renderRegistrationChart(activeRegistrationGranularity);
    bindRegistrationTabs();

    if (window.location.hash === "#dashboard-new-users" || filter.registrationDate) {
        document.getElementById("dashboard-new-users")?.scrollIntoView({ behavior: "smooth" });
    }
})();
