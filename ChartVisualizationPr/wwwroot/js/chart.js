// API Configuration
const API_BASE_URL = window.location.origin;

// Chart instances
let chartMain = null;
let chartRSI = null;
let chartVolume = null;
let candlestickSeries = null;
let ma20Series = null;
let upperBandSeries = null;
let lowerBandSeries = null;
let rsiSeries = null;
let rsiMa9Series = null;
let rsiWma45Series = null;
let volumeSeries = null;
let chartGroup = null;
let groupSeries = null;
let chartForeign = null;
let foreignSeries = null;
let cachedSymbols = null;
let chartNetTrade = null;
let netTradeSeries = null;

// State
let currentSymbol = '';
let currentMarkerMode = null;
let markers = [];
let candlesData = [];  // Store candle data for tooltip
let indicatorsData = [];  // Store indicator data for tooltip
let isFirstLoad = true;  // Track first data load
let currentCandleIndex = -1;  // Track current focused candle index

// Initialize
document.addEventListener('DOMContentLoaded', () => {
    initializeCharts();
    setupEventListeners();
    loadSymbols().then(() => {
        // Restore last selected symbol from localStorage
        const lastSymbol = localStorage.getItem('lastSelectedSymbol');
        if (lastSymbol && cachedSymbols && cachedSymbols.includes(lastSymbol)) {
            document.getElementById('symbolSelect').value = lastSymbol;
            loadChartData(lastSymbol);
        }
    });
});

function initializeCharts() {
    const chartOptions = {
        layout: {
            background: { color: 'transparent' },
            textColor: '#e5e7eb',
        },
        grid: {
            vertLines: { color: 'rgba(255, 255, 255, 0.05)' },
            horzLines: { color: 'rgba(255, 255, 255, 0.05)' },
        },
        crosshair: {
            mode: LightweightCharts.CrosshairMode.Normal,
        },
        timeScale: {
            borderColor: 'rgba(255, 255, 255, 0.1)',
            timeVisible: true,
            rightOffset: 10,
            barSpacing: 6,
            minBarSpacing: 0.5,
            fixLeftEdge: false,
            fixRightEdge: false,
            lockVisibleTimeRangeOnResize: false,
            rightBarStaysOnScroll: false,
            borderVisible: true,
            visible: true,
            shiftVisibleRangeOnNewBar: false,
        },
        rightPriceScale: {
            borderColor: 'rgba(255, 255, 255, 0.1)',
        },
    };

    // Main chart - use auto width
    const mainContainer = document.getElementById('chartMain');
    chartMain = LightweightCharts.createChart(mainContainer, {
        ...chartOptions,
        autoSize: true,
        height: 500,
        timeScale: {
            ...chartOptions.timeScale,
            handleScroll: false,
            handleScale: false,
        },
    });

    candlestickSeries = chartMain.addCandlestickSeries({
        upColor: '#10b981',
        downColor: '#ef4444',
        borderUpColor: '#10b981',
        borderDownColor: '#ef4444',
        wickUpColor: '#10b981',
        wickDownColor: '#ef4444',
    });

    ma20Series = chartMain.addLineSeries({
        color: '#FFB800',
        lineWidth: 2,
        priceLineVisible: false,
        lastValueVisible: false,
    });

    upperBandSeries = chartMain.addLineSeries({
        color: '#275BE8',
        lineWidth: 1,
        priceLineVisible: false,
        lastValueVisible: false,
    });

    lowerBandSeries = chartMain.addLineSeries({
        color: '#275BE8',
        lineWidth: 1,
        priceLineVisible: false,
        lastValueVisible: false,
    });

    // RSI chart - use auto width
    const rsiContainer = document.getElementById('chartRSI');
    chartRSI = LightweightCharts.createChart(rsiContainer, {
        ...chartOptions,
        autoSize: true,
        height: 200,
    });

    rsiSeries = chartRSI.addLineSeries({
        color: '#7c3aed',
        lineWidth: 2,
        priceLineVisible: false,
        lastValueVisible: false,
    });

    rsiMa9Series = chartRSI.addLineSeries({
        color: '#10b981',
        lineWidth: 1.5,
        priceLineVisible: false,
        lastValueVisible: false,
    });

    rsiWma45Series = chartRSI.addLineSeries({
        color: '#ef4444',
        lineWidth: 1.5,
        priceLineVisible: false,
        lastValueVisible: false,
    });

    // Volume chart - use auto width
    const volumeContainer = document.getElementById('chartVolume');
    chartVolume = LightweightCharts.createChart(volumeContainer, {
        ...chartOptions,
        autoSize: true,
        height: 200,
    });

    volumeSeries = chartVolume.addHistogramSeries({
        color: '#00d4ff',
        priceFormat: {
            type: 'volume',
        },
    });

    // Group (Institutional Investors) chart - use auto width
    const groupContainer = document.getElementById('chartGroup');
    chartGroup = LightweightCharts.createChart(groupContainer, {
        ...chartOptions,
        autoSize: true,
        height: 200,
    });

    groupSeries = chartGroup.addHistogramSeries({
        color: '#7c3aed',  // Purple color for institutional investors
        priceFormat: {
            type: 'volume',
        },
    });

    // Foreign (International Investors) chart
    const foreignContainer = document.getElementById('chartForeign');
    chartForeign = LightweightCharts.createChart(foreignContainer, {
        ...chartOptions,
        autoSize: true,
        height: 200,
    });
    foreignSeries = chartForeign.addHistogramSeries({
        color: '#00d4ff',  // Cyan (giống Volume color)
        priceFormat: {
            type: 'volume',
        },
    });

    // Net Trade chart
    const netTradeContainer = document.getElementById('chartNetTrade');
    chartNetTrade = LightweightCharts.createChart(netTradeContainer, {
        ...chartOptions,
        autoSize: true,
        height: 200,
    });
    netTradeSeries = chartNetTrade.addHistogramSeries({
        color: '#00d4ff',  // Cyan (giống Volume color)
        priceFormat: {
            type: 'volume',
        },
    });
    // Sync time scales - bidirectional sync between charts
    let isSyncing = false; // Prevent circular updates

    // Function to sync time range from source to targets
    const syncTimeRange = (sourceChart, targetCharts) => {
        if (isSyncing) return;

        try {
            isSyncing = true;
            const timeRange = sourceChart.timeScale().getVisibleRange();
            if (!timeRange) return;

            // Apply range to all target charts
            targetCharts.forEach(targetChart => {
                try {
                    targetChart.timeScale().setVisibleRange(timeRange);
                } catch (err) {
                    // Silently ignore if chart doesn't have data yet
                    if (err.message !== 'Value is null') {
                        console.warn('Sync error:', err.message);
                    }
                }
            });
        } finally {
            isSyncing = false;
        }
    };

    // Subscribe main chart changes to sync with RSI, Volume, and Group
    chartMain.timeScale().subscribeVisibleTimeRangeChange(() => {
        syncTimeRange(chartMain, [chartRSI, chartVolume, chartGroup, chartForeign]);
    });

    // // Subscribe RSI chart changes to sync with Main, Volume, and Group
    // chartRSI.timeScale().subscribeVisibleTimeRangeChange(() => {
    //     syncTimeRange(chartRSI, [chartMain, chartVolume, chartGroup, chartForeign]);
    // });

    // // Subscribe Volume chart changes to sync with Main, RSI, and Group
    // chartVolume.timeScale().subscribeVisibleTimeRangeChange(() => {
    //     syncTimeRange(chartVolume, [chartMain, chartRSI, chartGroup, chartForeign]);
    // });

    // // Subscribe Group chart changes to sync with Main, RSI, and Volume
    // chartGroup.timeScale().subscribeVisibleTimeRangeChange(() => {
    //     syncTimeRange(chartGroup, [chartMain, chartRSI, chartVolume, chartForeign]);
    // });

    // // Subscribe Foreign chart changes to sync with Main, RSI, Volume, and Group
    // chartForeign.timeScale().subscribeVisibleTimeRangeChange(() => {
    //     syncTimeRange(chartForeign, [chartMain, chartRSI, chartVolume, chartGroup]);
    // });

    // Sync crosshair position across all charts and update info panel
    chartMain.subscribeCrosshairMove((param) => {
        if (!param || !param.time) {
            // Clear crosshair on other charts when mouse leaves
            try {
                chartRSI.clearCrosshairPosition();
                chartVolume.clearCrosshairPosition();
                chartGroup.clearCrosshairPosition();
                chartForeign.clearCrosshairPosition();
                chartNetTrade.clearCrosshairPosition();
            } catch (e) {
                // Ignore errors when charts don't have data yet
            }
            // Hide info panel
            document.getElementById('candleInfo').classList.add('hidden');
            return;
        }

        // Sync crosshair to RSI, Volume, and Group charts
        try {
            chartRSI.setCrosshairPosition(0, param.time, rsiSeries);
            chartVolume.setCrosshairPosition(0, param.time, volumeSeries);
            chartGroup.setCrosshairPosition(0, param.time, groupSeries);
            chartForeign.setCrosshairPosition(0, param.time, foreignSeries);
            chartNetTrade.setCrosshairPosition(0, param.time, netTradeSeries);
        } catch (e) {
            // Ignore errors when charts don't have data yet
        }

        // Update info panel with candle and indicator values
        const candle = candlesData.find(c => c.time === param.time);
        const indicator = indicatorsData.find(i => i.time === param.time);

        if (candle) {
            const infoPanel = document.getElementById('candleInfo');
            infoPanel.classList.remove('hidden');

            // Format date
            const date = new Date(param.time * 1000);
            document.getElementById('infoDate').textContent = date.toLocaleDateString('vi-VN');

            // OHLC values
            document.getElementById('infoOpen').textContent = candle.open.toFixed(2);
            document.getElementById('infoHigh').textContent = candle.high.toFixed(2);
            document.getElementById('infoLow').textContent = candle.low.toFixed(2);
            document.getElementById('infoClose').textContent = candle.close.toFixed(2);

            // Indicator values
            if (indicator) {
                document.getElementById('infoMA20').textContent = indicator.ma20 ? indicator.ma20.toFixed(2) : '-';
                document.getElementById('infoUpper').textContent = indicator.upperBand ? indicator.upperBand.toFixed(2) : '-';
                document.getElementById('infoLower').textContent = indicator.lowerBand ? indicator.lowerBand.toFixed(2) : '-';
                document.getElementById('infoRSI').textContent = indicator.rsi ? indicator.rsi.toFixed(2) : '-';
                document.getElementById('infoVolume').textContent = indicator.volume ? indicator.volume.toLocaleString() : '-';
                document.getElementById('infoNetTrade').textContent = indicator.netTrade ? indicator.netTrade.toLocaleString() : '-';
            }
        }
    });

    // Also sync from RSI chart to others
    chartRSI.subscribeCrosshairMove((param) => {
        if (!param || !param.time) {
            try {
                chartMain.clearCrosshairPosition();
                chartVolume.clearCrosshairPosition();
                chartGroup.clearCrosshairPosition();
                chartForeign.clearCrosshairPosition();
                chartNetTrade.clearCrosshairPosition();
            } catch (e) {
                // Ignore errors when charts don't have data yet
            }
            return;
        }

        try {
            chartMain.setCrosshairPosition(0, param.time, candlestickSeries);
            chartVolume.setCrosshairPosition(0, param.time, volumeSeries);
            chartGroup.setCrosshairPosition(0, param.time, groupSeries);
            chartForeign.setCrosshairPosition(0, param.time, foreignSeries);
            chartNetTrade.setCrosshairPosition(0, param.time, netTradeSeries);
        } catch (e) {
            // Ignore errors when charts don't have data yet
        }
    });

    // Also sync from Volume chart to others
    chartVolume.subscribeCrosshairMove((param) => {
        if (!param || !param.time) {
            try {
                chartMain.clearCrosshairPosition();
                chartRSI.clearCrosshairPosition();
                chartGroup.clearCrosshairPosition();
                chartForeign.clearCrosshairPosition();
                chartNetTrade.clearCrosshairPosition();
            } catch (e) {
                // Ignore errors when charts don't have data yet
            }
            return;
        }

        try {
            chartMain.setCrosshairPosition(0, param.time, candlestickSeries);
            chartRSI.setCrosshairPosition(0, param.time, rsiSeries);
            chartGroup.setCrosshairPosition(0, param.time, groupSeries);
            chartForeign.setCrosshairPosition(0, param.time, foreignSeries);
            chartNetTrade.setCrosshairPosition(0, param.time, netTradeSeries);
        } catch (e) {
            // Ignore errors when charts don't have data yet
        }
    });

    chartGroup.subscribeCrosshairMove((param) => {
        if (!param || !param.time) {
            try {
                chartMain.clearCrosshairPosition();
                chartRSI.clearCrosshairPosition();
                chartVolume.clearCrosshairPosition();
                chartForeign.clearCrosshairPosition();
                chartNetTrade.clearCrosshairPosition();
            } catch (e) {
                // Ignore errors when charts don't have data yet
            }
            return;
        }

        try {
            chartMain.setCrosshairPosition(0, param.time, candlestickSeries);
            chartRSI.setCrosshairPosition(0, param.time, rsiSeries);
            chartVolume.setCrosshairPosition(0, param.time, volumeSeries);
            chartForeign.setCrosshairPosition(0, param.time, foreignSeries);
            chartNetTrade.setCrosshairPosition(0, param.time, netTradeSeries);
        } catch (e) {
            // Ignore errors when charts don't have data yet
        }
    });

    chartForeign.subscribeCrosshairMove((param) => {
        if (!param || !param.time) {
            try {
                chartMain.clearCrosshairPosition();
                chartRSI.clearCrosshairPosition();
                chartVolume.clearCrosshairPosition();
                chartGroup.clearCrosshairPosition();
                chartNetTrade.clearCrosshairPosition();
            } catch (e) {
                // Ignore errors when charts don't have data yet
            }
            return;
        }

        try {
            chartMain.setCrosshairPosition(0, param.time, candlestickSeries);
            chartRSI.setCrosshairPosition(0, param.time, rsiSeries);
            chartVolume.setCrosshairPosition(0, param.time, volumeSeries);
            chartGroup.setCrosshairPosition(0, param.time, groupSeries);
            chartNetTrade.setCrosshairPosition(0, param.time, netTradeSeries);
        } catch (e) {
            // Ignore errors when charts don't have data yet
        }
    });

    chartNetTrade.subscribeCrosshairMove((param) => {
        if (!param || !param.time) {
            try {
                chartMain.clearCrosshairPosition();
                chartRSI.clearCrosshairPosition();
                chartVolume.clearCrosshairPosition();
                chartGroup.clearCrosshairPosition();
                chartForeign.clearCrosshairPosition();
            } catch (e) {
                // Ignore errors when charts don't have data yet
            }
            return;
        }

        try {
            chartMain.setCrosshairPosition(0, param.time, candlestickSeries);
            chartRSI.setCrosshairPosition(0, param.time, rsiSeries);
            chartVolume.setCrosshairPosition(0, param.time, volumeSeries);
            chartGroup.setCrosshairPosition(0, param.time, groupSeries);
            chartForeign.setCrosshairPosition(0, param.time, foreignSeries);
        } catch (e) {
            // Ignore errors when charts don't have data yet
        }
    });

    // Handle chart clicks
    chartMain.subscribeClick(handleChartClick);
}

function setupEventListeners() {
    document.getElementById('symbolSelect').addEventListener('change', (e) => {
        const symbol = e.target.value;
        if (symbol) {
            loadChartData(symbol);
        }
    });

    document.getElementById('markerBuy').addEventListener('click', () => {
        toggleMarkerMode('buy');
    });

    document.getElementById('markerSell').addEventListener('click', () => {
        toggleMarkerMode('sell');
    });

    document.getElementById('markerNote').addEventListener('click', () => {
        toggleMarkerMode('note');
    });

    // Go to latest candle button
    document.getElementById('btnGoToLatest').addEventListener('click', () => {
        if (!candlesData || candlesData.length === 0) return;

        // Reset to latest candle
        currentCandleIndex = candlesData.length - 1;

        // Fit content to show all data with latest candle visible
        chartMain.timeScale().fitContent();

        // Wait a bit for main chart to finish, then sync other charts
        setTimeout(() => {
            const mainRange = chartMain.timeScale().getVisibleRange();
            if (mainRange) {
                chartRSI.timeScale().setVisibleRange(mainRange);
                chartVolume.timeScale().setVisibleRange(mainRange);
            }
        }, 50);
    });

    // Keyboard navigation - Arrow keys to move by one candle using index-based approach
    document.addEventListener('keydown', (e) => {
        if (!candlesData || candlesData.length < 2) return;

        // Only handle arrow keys
        if (e.key !== 'ArrowLeft' && e.key !== 'ArrowRight') return;

        // Don't interfere if user is typing in an input
        if (e.target.tagName === 'INPUT' || e.target.tagName === 'SELECT' || e.target.tagName === 'TEXTAREA') {
            return;
        }

        e.preventDefault();

        try {
            const timeScale = chartMain.timeScale();
            const visibleRange = timeScale.getVisibleRange();
            if (!visibleRange) return;

            // Initialize current index if not set (find the last visible candle)
            if (currentCandleIndex === -1) {
                // Find the rightmost visible candle
                for (let i = candlesData.length - 1; i >= 0; i--) {
                    if (candlesData[i].time <= visibleRange.to) {
                        currentCandleIndex = i;
                        break;
                    }
                }
                if (currentCandleIndex === -1) currentCandleIndex = candlesData.length - 1;
            }

            // Move to next/previous candle
            let newIndex = currentCandleIndex;
            if (e.key === 'ArrowLeft') {
                newIndex = Math.max(0, currentCandleIndex - 1);
            } else {
                newIndex = Math.min(candlesData.length - 1, currentCandleIndex + 1);
            }

            // If we can't move further, do nothing
            if (newIndex === currentCandleIndex) return;

            // Calculate the time difference we need to shift
            const oldCandle = candlesData[currentCandleIndex];
            currentCandleIndex = newIndex;
            const newCandle = candlesData[currentCandleIndex];

            // Calculate the shift amount (difference between old and new candle time)
            const shiftAmount = newCandle.time - oldCandle.time;

            // Keep the same visible range width (no zoom change)
            const rangeWidth = visibleRange.to - visibleRange.from;

            // Shift the range by exactly the time difference between candles
            const newFrom = visibleRange.from + shiftAmount;
            const newTo = visibleRange.to + shiftAmount;

            // Apply the new range
            timeScale.setVisibleRange({
                from: newFrom,
                to: newTo
            });
        } catch (error) {
            console.warn('Keyboard navigation error:', error);
        }
    });

    // Ctrl+K shortcut
    document.addEventListener('keydown', (e) => {
        if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
            e.preventDefault();
            document.getElementById('symbolSearch')?.focus();
        }
    });

    // Chart visibility toggles
    document.getElementById('toggleRSI')?.addEventListener('change', (e) => {
        const container = document.getElementById('chartRSI').parentElement;
        if (e.target.checked) {
            container.classList.remove('hidden');
            chartRSI.resize();
        } else {
            container.classList.add('hidden');
        }
    });

    document.getElementById('toggleVolume')?.addEventListener('change', (e) => {
        const container = document.getElementById('chartVolume').parentElement;
        if (e.target.checked) {
            container.classList.remove('hidden');
            chartVolume.resize();
        } else {
            container.classList.add('hidden');
        }
    });

    document.getElementById('toggleGroup')?.addEventListener('change', (e) => {
        const container = document.getElementById('chartGroup').parentElement;
        if (e.target.checked) {
            container.classList.remove('hidden');
            chartGroup.resize();
        } else {
            container.classList.add('hidden');
        }
    });

    document.getElementById('toggleForeign')?.addEventListener('change', (e) => {
        const container = document.getElementById('chartForeign').parentElement;
        if (e.target.checked) {
            container.classList.remove('hidden');
            chartForeign.resize();
        } else {
            container.classList.add('hidden');
        }
    });

    document.getElementById('toggleNetTrade')?.addEventListener('change', (e) => {
        const container = document.getElementById('chartNetTrade').parentElement;
        if (e.target.checked) {
            container.classList.remove('hidden');
            chartNetTrade.resize();
        } else {
            container.classList.add('hidden');
        }
    });

    setupSymbolSearch(); // Dòng cuối hàm
}

function toggleMarkerMode(mode) {
    const buttons = document.querySelectorAll('.marker-btn');
    buttons.forEach(btn => btn.classList.remove('active'));

    if (currentMarkerMode === mode) {
        currentMarkerMode = null;
    } else {
        currentMarkerMode = mode;
        document.getElementById(`marker${mode.charAt(0).toUpperCase() + mode.slice(1)}`).classList.add('active');
    }
}

async function loadChartData(symbol) {
    localStorage.setItem('lastSelectedSymbol', symbol);
    currentSymbol = symbol;
    showLoading(true);

    try {
        const candleResponse = await fetch(`${API_BASE_URL}/api/chartdata/candles/${symbol}`);
        const candles = await candleResponse.json();

        const indicatorResponse = await fetch(`${API_BASE_URL}/api/chartdata/indicators/${symbol}`);
        const indicators = await indicatorResponse.json();

        const markerResponse = await fetch(`${API_BASE_URL}/api/chartdata/markers/${symbol}`);
        markers = await markerResponse.json();

        const groupResponse = await fetch(`${API_BASE_URL}/api/chartdata/group/${symbol}`);
        const groupData = await groupResponse.json();

        const foreignResponse = await fetch(`${API_BASE_URL}/api/chartdata/foreign/${symbol}`);
        const foreignData = await foreignResponse.json();

        const netTradeResponse = await fetch(`${API_BASE_URL}/api/chartdata/nettrade/${symbol}`);
        const netTradeData = await netTradeResponse.json();
        console.log('✅ netTradeData fetched:', netTradeData);

        console.log('📊 Sample timestamps:');
        console.log('  Candles:', candles.slice(0, 3).map(c => ({ time: c.time, date: new Date(c.time * 1000) })));
        console.log('  Group:', groupData.slice(0, 3).map(g => ({ time: g.time, date: new Date(g.time * 1000) })));
        console.log('  Foreign:', foreignData.slice(0, 3).map(f => ({ time: f.time, date: new Date(f.time * 1000) })));
        console.log('  NetTrade:', netTradeData.slice(0, 3).map(n => ({ time: n.time, date: new Date(n.time * 1000) })));
        // Store data for tooltip
        candlesData = candles;
        indicatorsData = indicators;

        // Reset candle index for keyboard navigation
        currentCandleIndex = -1;

        updateCharts(candles, indicators, groupData, foreignData, netTradeData);
        updateMarkers();

        showLoading(false);
    } catch (error) {
        console.error('Error loading chart data:', error);
        showLoading(false);
    }
}

function updateCharts(candles, indicators, groupData = [], foreignData = [], netTradeData = []) {
    const candleData = candles.map(c => ({
        time: c.time,
        open: c.open,
        high: c.high,
        low: c.low,
        close: c.close,
    }));
    candlestickSeries.setData(candleData);

    const ma20Data = indicators
        .filter(i => i.ma20 !== null && i.ma20 !== undefined && typeof i.ma20 === 'number')
        .map(i => ({ time: i.time, value: i.ma20 }));
    ma20Series.setData(ma20Data);

    const upperBandData = indicators
        .filter(i => i.upperBand !== null && i.upperBand !== undefined && typeof i.upperBand === 'number' && !isNaN(i.upperBand))
        .map(i => ({ time: i.time, value: i.upperBand }));
    if (upperBandData.length > 0) upperBandSeries.setData(upperBandData);

    const lowerBandData = indicators
        .filter(i => i.lowerBand !== null && i.lowerBand !== undefined && typeof i.lowerBand === 'number' && !isNaN(i.lowerBand))
        .map(i => ({ time: i.time, value: i.lowerBand }));
    if (lowerBandData.length > 0) lowerBandSeries.setData(lowerBandData);

    const rsiData = indicators
        .filter(i => i.rsi !== null && i.rsi !== undefined && typeof i.rsi === 'number')
        .map(i => ({ time: i.time, value: i.rsi }));
    rsiSeries.setData(rsiData);

    const rsiMa9Data = indicators
        .filter(i => i.rsiMa9 !== null && i.rsiMa9 !== undefined && typeof i.rsiMa9 === 'number' && !isNaN(i.rsiMa9))
        .map(i => ({ time: i.time, value: i.rsiMa9 }));
    if (rsiMa9Data.length > 0) rsiMa9Series.setData(rsiMa9Data);

    const rsiWma45Data = indicators
        .filter(i => i.rsiWma45 !== null && i.rsiWma45 !== undefined && typeof i.rsiWma45 === 'number' && !isNaN(i.rsiWma45))
        .map(i => ({ time: i.time, value: i.rsiWma45 }));
    if (rsiWma45Data.length > 0) rsiWma45Series.setData(rsiWma45Data);

    const volumeData = indicators.map(i => ({
        time: i.time,
        value: i.volume,
        color: i.volume > 0 ? '#00d4ff' : '#ef4444',
    }));
    volumeSeries.setData(volumeData);

    // Set Group (Institutional Investors) data
    if (groupData && groupData.length > 0) {
        const groupChartData = groupData.map(g => ({
            time: g.time,
            value: g.value,
            color: g.value >= 0 ? '#7c3aed' : '#ef4444', // Purple for positive, Red for negative
        }));
        groupSeries.setData(groupChartData);
    }

    // Foreign data display
    if (foreignData && foreignData.length > 0) {
        const foreignChartData = foreignData.map(f => ({
            time: f.time,
            value: f.value,
            color: f.value >= 0 ? '#00d4ff' : '#ef4444', // Cyan for positive, Red for negative
        }));
        foreignSeries.setData(foreignChartData);
    }

    // Net Trade data display
    if (netTradeData && netTradeData.length > 0) {
        const netTradeChartData = netTradeData.map(nt => ({
            time: nt.time,
            value: nt.value,
            color: nt.value >= 0 ? '#00d4ff' : '#ef4444', // Cyan for positive, Red for negative
        }));
        console.log('✅ netTradeChartData:', netTradeChartData); // <-- THÊM DÒNG NÀY
        netTradeSeries.setData(netTradeChartData);
    }
    else {
        console.log('❌ netTradeData is empty or null:', netTradeData); // <-- THÊM DÒNG NÀY
    }
    // Only fit content on first load, then let sync handle it
    if (isFirstLoad) {
        setTimeout(() => {
            // Instead of fitContent (which zooms to show ALL data),
            // scroll to show recent data with comfortable zoom level
            const dataLength = candleData.length;
            if (dataLength > 0) {
                // Show last 100 candles or all if less than 100
                const barsToShow = Math.min(100, dataLength);
                const fromIndex = Math.max(0, dataLength - barsToShow);

                chartMain.timeScale().setVisibleLogicalRange({
                    from: fromIndex,
                    to: dataLength - 1
                });
            }

            // Sync other charts
            setTimeout(() => {
                const mainRange = chartMain.timeScale().getVisibleRange();
                if (mainRange) {
                    chartRSI.timeScale().setVisibleRange(mainRange);
                    chartVolume.timeScale().setVisibleRange(mainRange);
                    chartGroup.timeScale().setVisibleRange(mainRange);
                    chartForeign.timeScale().setVisibleRange(mainRange);
                    chartNetTrade.timeScale().setVisibleRange(mainRange);
                }
                isFirstLoad = false;
            }, 50);
        }, 200);
    }
    else {
        // On subsequent loads, keep the current time range
        const currentRange = chartMain.timeScale().getVisibleRange();
        if (currentRange) {
            chartRSI.timeScale().setVisibleRange(currentRange);
            chartVolume.timeScale().setVisibleRange(currentRange);
            chartGroup.timeScale().setVisibleRange(currentRange);
            chartForeign.timeScale().setVisibleRange(currentRange);
            chartNetTrade.timeScale().setVisibleRange(currentRange);
        }
    }
}

function updateMarkers() {
    const markerData = markers.map(m => ({
        time: m.time,
        position: m.position,
        color: m.color,
        shape: getMarkerShape(m.shape),
        text: m.text,
        id: m.id,
    }));
    candlestickSeries.setMarkers(markerData);
}

function getMarkerShape(shape) {
    const shapeMap = {
        'circle': 'circle',
        'square': 'square',
        'arrowUp': 'arrowUp',
        'arrowDown': 'arrowDown',
    };
    return shapeMap[shape] || 'circle';
}

async function handleChartClick(param) {
    if (!param.time) return;

    // Update current candle index based on clicked time
    // This allows keyboard navigation to start from clicked position
    const clickedIndex = candlesData.findIndex(c => c.time === param.time);
    if (clickedIndex !== -1) {
        currentCandleIndex = clickedIndex;
    }

    const clickedMarker = markers.find(m => m.time === param.time);

    // If clicked on existing marker, show tooltip
    if (clickedMarker) {
        showMarkerTooltip(clickedMarker);
        return;
    }

    // If no marker mode selected, do nothing
    if (!currentMarkerMode) return;

    // Create new marker
    const marker = {
        symbol: currentSymbol,
        time: param.time,
        position: currentMarkerMode === 'sell' ? 'aboveBar' : 'belowBar',
        color: getMarkerColor(currentMarkerMode),
        shape: getMarkerShapeType(currentMarkerMode),
        text: getMarkerText(currentMarkerMode),
    };

    await saveMarker(marker);
}

function getMarkerColor(mode) {
    const colorMap = {
        'buy': '#10b981',
        'sell': '#ef4444',
        'note': '#7c3aed',
    };
    return colorMap[mode] || '#00d4ff';
}

function getMarkerShapeType(mode) {
    const shapeMap = {
        'buy': 'arrowUp',
        'sell': 'arrowDown',
        'note': 'circle',
    };
    return shapeMap[mode] || 'circle';
}

function getMarkerText(mode) {
    const textMap = {
        'buy': 'B',
        'sell': 'S',
        'note': 'N',
    };
    return textMap[mode] || '';
}

async function saveMarker(marker) {
    try {
        const response = await fetch(`${API_BASE_URL}/api/chartdata/markers`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(marker),
        });

        if (response.ok) {
            const saved = await response.json();
            markers.push(saved);
            updateMarkers();
        }
    } catch (error) {
        console.error('Error saving marker:', error);
    }
}

async function deleteMarker(id) {
    try {
        const response = await fetch(`${API_BASE_URL}/api/chartdata/markers/${id}`, {
            method: 'DELETE',
        });

        if (response.ok) {
            markers = markers.filter(m => m.id !== id);
            updateMarkers();
        }
    } catch (error) {
        console.error('Error deleting marker:', error);
    }
}

function showLoading(show) {
    const loading = document.getElementById('loading');
    if (show) {
        loading.classList.remove('hidden');
    } else {
        loading.classList.add('hidden');
    }
}

// Marker Tooltip Functions
function showMarkerTooltip(marker) {
    const candle = candlesData.find(c => c.time === marker.time);
    const indicator = indicatorsData.find(i => i.time === marker.time);

    if (!candle || !indicator) return;

    const tooltip = document.getElementById('markerTooltip');
    const tooltipTitle = document.getElementById('tooltipTitle');
    const tooltipContent = document.getElementById('tooltipContent');

    // Format date
    const date = new Date(marker.time * 1000);
    const dateStr = date.toLocaleDateString('vi-VN');

    // Set title
    const markerTypeMap = {
        'buy': '📍 Entry MUA',
        'sell': '📍 Entry BÁN',
        'note': '📍 Ghi chú'
    };
    const markerType = marker.shape === 'arrowUp' ? 'buy' : marker.shape === 'arrowDown' ? 'sell' : 'note';
    tooltipTitle.textContent = `${markerTypeMap[markerType]} - ${dateStr}`;

    // Build content
    let content = '';

    // Price data
    content += `<div class="tooltip-row">
        <span class="tooltip-label">💰 Close</span>
        <span class="tooltip-value">${candle.close.toLocaleString('vi-VN')}</span>
    </div>`;

    content += `<div class="tooltip-row">
        <span class="tooltip-label">📊 High / Low</span>
        <span class="tooltip-value">${candle.high.toLocaleString('vi-VN')} / ${candle.low.toLocaleString('vi-VN')}</span>
    </div>`;

    // MA20
    if (indicator.ma20) {
        content += `<div class="tooltip-row">
            <span class="tooltip-label">🟡 MA20</span>
            <span class="tooltip-value">${indicator.ma20.toLocaleString('vi-VN')}</span>
        </div>`;
    }

    // Bollinger Bands
    if (indicator.upperBand && indicator.lowerBand) {
        content += `<div class="tooltip-row">
            <span class="tooltip-label">🔵 BB Upper</span>
            <span class="tooltip-value">${indicator.upperBand.toLocaleString('vi-VN')}</span>
        </div>`;
        content += `<div class="tooltip-row">
            <span class="tooltip-label">🔵 BB Lower</span>
            <span class="tooltip-value">${indicator.lowerBand.toLocaleString('vi-VN')}</span>
        </div>`;
    }

    // RSI
    if (indicator.rsi) {
        const rsiClass = indicator.rsi > 70 ? 'negative' : indicator.rsi < 30 ? 'positive' : '';
        content += `<div class="tooltip-row">
            <span class="tooltip-label">🟣 RSI</span>
            <span class="tooltip-value ${rsiClass}">${indicator.rsi.toFixed(2)}</span>
        </div>`;
    }

    // RSI MA9
    if (indicator.rsiMa9) {
        content += `<div class="tooltip-row">
            <span class="tooltip-label">🟢 RSI MA9</span>
            <span class="tooltip-value">${indicator.rsiMa9.toFixed(2)}</span>
        </div>`;
    }

    // RSI WMA45
    if (indicator.rsiWma45) {
        content += `<div class="tooltip-row">
            <span class="tooltip-label">🔴 RSI WMA45</span>
            <span class="tooltip-value">${indicator.rsiWma45.toFixed(2)}</span>
        </div>`;
    }

    // Volume
    content += `<div class="tooltip-row">
        <span class="tooltip-label">📊 Volume</span>
        <span class="tooltip-value">${indicator.volume.toLocaleString('vi-VN')}</span>
    </div>`;

    tooltipContent.innerHTML = content;

    // Position tooltip near mouse
    tooltip.style.left = '50%';
    tooltip.style.top = '50%';
    tooltip.style.transform = 'translate(-50%, -50%)';
    tooltip.classList.remove('hidden');
}

function hideMarkerTooltip() {
    const tooltip = document.getElementById('markerTooltip');
    tooltip.classList.add('hidden');
}

// Load symbols from API with caching and enhanced UX
async function loadSymbols(forceRefresh = false) {
    const selectElement = document.getElementById('symbolSelect');

    // Use cache if available and not forcing refresh
    if (cachedSymbols && !forceRefresh) {
        populateDropdown(cachedSymbols);
        return;
    }

    // Show loading state
    selectElement.innerHTML = '<option value="">Đang tải...</option>';
    selectElement.disabled = true;

    try {
        const response = await fetch(`${API_BASE_URL}/api/chartdata/symbols`);
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        cachedSymbols = await response.json();
        populateDropdown(cachedSymbols);
    } catch (error) {
        console.error('Error loading symbols:', error);
        selectElement.innerHTML = '<option value="">Lỗi tải dữ liệu</option>';
    } finally {
        selectElement.disabled = false;
    }
}

// Populate dropdown with symbols
function populateDropdown(symbols) {
    const selectElement = document.getElementById('symbolSelect');
    selectElement.innerHTML = '<option value="">Chọn mã</option>';

    symbols.forEach(symbol => {
        const option = document.createElement('option');
        option.value = symbol;
        option.textContent = symbol;
        selectElement.appendChild(option);
    });
}

// Setup symbol search functionality
function setupSymbolSearch() {
    const searchInput = document.getElementById('symbolSearch');
    const selectElement = document.getElementById('symbolSelect');

    if (!searchInput || !selectElement) return;

    searchInput.addEventListener('input', (e) => {
        const filter = e.target.value.toUpperCase();
        const options = selectElement.querySelectorAll('option');

        options.forEach(option => {
            if (option.value === '') {
                option.style.display = '';
                return;
            }

            const text = option.textContent.toUpperCase();
            option.style.display = text.includes(filter) ? '' : 'none';
        });

        const visibleOptions = Array.from(options).filter(opt =>
            opt.value !== '' && opt.style.display !== 'none'
        );

        if (visibleOptions.length === 1) {
            selectElement.value = visibleOptions[0].value;
        }
    });

    selectElement.addEventListener('change', () => {
        if (searchInput) {
            searchInput.value = '';
            const options = selectElement.querySelectorAll('option');
            options.forEach(option => {
                option.style.display = '';
            });
        }
    });

    searchInput.addEventListener('keydown', (e) => {
        if (e.key === 'Enter') {
            const visibleOptions = Array.from(selectElement.querySelectorAll('option')).filter(opt =>
                opt.value !== '' && opt.style.display !== 'none'
            );

            if (visibleOptions.length > 0) {
                selectElement.value = visibleOptions[0].value;
                selectElement.dispatchEvent(new Event('change'));
                searchInput.blur();
            }
        }
    });
}

// Setup tooltip close button
document.addEventListener('DOMContentLoaded', () => {
    const tooltipClose = document.getElementById('tooltipClose');
    if (tooltipClose) {
        tooltipClose.addEventListener('click', hideMarkerTooltip);
    }
});