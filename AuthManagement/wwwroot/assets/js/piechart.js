(() => {
    const config = {
        fontFamily: 'Public Sans, sans-serif',
        colors: {
            primary: '#7367F0',
            secondary: '#82868b',
            success: '#28C76F',
            white: '#fff',
            cardColor: '#fff',
            textMuted: '#6e6b7b',
            headingColor: '#5e5873',
            borderColor: '#ebe9f1'
        },
        chartColors: {
            line: {
                series1: '#7367F0',
                series2: '#28C76F',
                series3: '#FF9F43'
            },
            donut: {
                series1: '#7367F0',
                series2: '#28C76F',
                series3: '#FF9F43',
                series4: '#EA5455'
            }
        },
        legendColor: '#5e5873'
    };

    // Use chartColors AFTER config is created
    const colors = ['#FF9F43', config.chartColors.line.series2];

    // Extract values
    const fontFamily = config.fontFamily;
    const labelColor = config.colors.textMuted;
    const headingColor = config.colors.headingColor;
    const borderColor = config.colors.borderColor;
    const chartColors = config.chartColors;
    const legendColor = config.legendColor;




    // ---------------- Delivery Exceptions ----------------
    const deliveryExceptionsEl = document.querySelector('#deliveryExceptionsChart');
    if (deliveryExceptionsEl) {
        const deliveryExceptionsChart = {
            chart: { height: 365, type: 'donut' },
            labels: ['Incorrect address', 'Weather conditions', 'Federal Holidays', 'Damage during transit'],
            series: [13, 25, 22, 40],
            colors: [
                chartColors.donut.series1,
                chartColors.donut.series2,
                chartColors.donut.series3,
                chartColors.donut.series4
            ],
            stroke: { width: 0 },
            dataLabels: { enabled: false },
            legend: { show: true, position: 'bottom', offsetY: 10, markers: { size: 4, strokeWidth: 0 }, itemMargin: { horizontal: 15, vertical: 5 }, fontSize: '13px', fontFamily, fontWeight: 400, labels: { colors: legendColor, useSeriesColors: false } },
            tooltip: { theme: false },
            plotOptions: {
                pie: { donut: { size: '75%', labels: { show: true, value: { fontSize: '38px', fontFamily, color: headingColor, fontWeight: 500, offsetY: -20, formatter: val => val + '%' }, name: { offsetY: 30, fontFamily }, total: { show: true, fontSize: '15px', fontFamily, color: legendColor, label: 'AVG. Exceptions', formatter: () => '30%' } } } }
            },
            responsive: [{ breakpoint: 1025, options: { chart: { height: 380 } } }]
        };
        new ApexCharts(deliveryExceptionsEl, deliveryExceptionsChart).render();
    }

    // ---------------- Shipment Statistics ----------------
    // ---------------- Shipment Statistics ----------------
    const shipmentEl = document.querySelector('#shipmentStatisticsChart');
    if (shipmentEl) {
        const shipmentChart = {
            series: [
                { name: 'Shipment', type: 'column', data: [38, 45, 33, 38, 32, 50, 48, 40, 42, 37] },
                { name: 'Delivery', type: 'line', data: [23, 28, 23, 32, 28, 44, 32, 38, 26, 34] }
            ],

            chart: {
                height: 320,
                type: 'line',
                stacked: false,
                parentHeightOffset: 0,
                toolbar: { show: false },
                zoom: { enabled: false }
            },

            markers: {
                size: 5,
                colors: ['#ffffff'],
                strokeColors: '#7367F0',   // Line marker border = Purple (matching image)
                hover: { size: 6 },
                borderRadius: 4
            },

            stroke: { curve: 'smooth', width: [0, 3], lineCap: 'round' },

            legend: {
                show: true,
                position: 'bottom',
                markers: { size: 6, offsetX: -3, strokeWidth: 0 },
                height: 40,
                itemMargin: { horizontal: 10, vertical: 0 },
                fontSize: '15px',
                fontFamily,
                fontWeight: 400,
                labels: { colors: headingColor, useSeriesColors: false },
                offsetY: 5
            },

            grid: { strokeDashArray: 8, borderColor },

            // --------------- EXACT IMAGE COLORS HERE ----------------
            colors: ['#FF9F43', '#7367F0'],
            // Bars = Orange, Line = Purple

            fill: { opacity: [1, 1] },

            plotOptions: {
                bar: {
                    columnWidth: '30%',
                    startingShape: 'rounded',
                    endingShape: 'rounded',
                    borderRadius: 3     // Image-ல round அதிகம்
                }
            },

            dataLabels: { enabled: false },

            xaxis: {
                tickAmount: 10,
                categories: ['1 Jan', '2 Jan', '3 Jan', '4 Jan', '5 Jan', '6 Jan', '7 Jan', '8 Jan', '9 Jan', '10 Jan'],
                labels: {
                    style: {
                        colors: labelColor,
                        fontSize: '13px',
                        fontFamily,
                        fontWeight: 400
                    }
                },
                axisBorder: { show: false },
                axisTicks: { show: false }
            },

            yaxis: {
                tickAmount: 4,
                min: 0,
                max: 50,
                labels: {
                    style: { colors: labelColor, fontSize: '13px', fontFamily, fontWeight: 400 },
                    formatter: val => val + '%'
                }
            },

            responsive: [
                { breakpoint: 1400, options: { chart: { height: 320 }, xaxis: { labels: { style: { fontSize: '10px' } } }, legend: { fontSize: '13px' } } },
                { breakpoint: 1025, options: { chart: { height: 415 }, plotOptions: { bar: { columnWidth: '50%' } } } },
                { breakpoint: 982, options: { plotOptions: { bar: { columnWidth: '30%' } } } },
                { breakpoint: 480, options: { chart: { height: 250 }, legend: { offsetY: 7 } } }
            ]
        };

        new ApexCharts(shipmentEl, shipmentChart).render();
    }

})();
