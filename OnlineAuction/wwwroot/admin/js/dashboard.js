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

    const revenueSeries = chartData.revenue || [];
    const bidsSeries = chartData.bids || [];
    const statusBreakdown = chartData.statusBreakdown || [];

    const revenueTotal = revenueSeries.reduce((sum, point) => sum + Number(point.value || 0), 0);
    const bidsTotal = bidsSeries.reduce((sum, point) => sum + Number(point.value || 0), 0);

    if (revenueTotal > 0) {
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
    } else {
        document.querySelector("#dashboard-revenue-empty")?.classList.remove("hidden");
    }

    if (bidsTotal > 0) {
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
    } else {
        document.querySelector("#dashboard-bids-empty")?.classList.remove("hidden");
    }

    if (statusBreakdown.length > 0) {
        const statusChart = new ApexCharts(document.querySelector("#dashboard-status-chart"), {
            chart: {
                ...baseChartOptions.chart,
                type: "donut",
                height: 320
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
})();
