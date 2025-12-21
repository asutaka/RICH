// ===================================================================
// FILE NÀY CHỨA TOÀN BỘ CÁC CẢI TIẾN CHO CHART.JS
// Hãy COPY các đoạn code bên dưới vào đúng vị trí trong chart.js  
// ===================================================================

// 1. THÊM VÀO PHẦN STATE (sau dòng 28, sau let currentCandleIndex = -1;):
let cachedSymbols = null;  // Cache symbols to avoid reloading

// 2. THAY THẾ DOMContentLoaded (dòng 31-34):
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

// 3. THÊM VÀO CUỐI setupEventListeners() (trước dấu đóng ngoặc }):
// Keyboard shortcut: Ctrl/Cmd + K to focus symbol search
document.addEventListener('keydown', (e) => {
    if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
        e.preventDefault();
        const searchInput = document.getElementById('symbolSearch');
        if (searchInput) {
            searchInput.focus();
            searchInput.select();
        }
    }
});

// Setup symbol search functionality
setupSymbolSearch();

// 4. CẬP NHẬT loadChartData (thêm localStorage.setItem sau dòng currentSymbol = symbol;):
async function loadChartData(symbol) {
    currentSymbol = symbol;
    localStorage.setItem('lastSelectedSymbol', symbol);  // <-- THÊM DÒNG NÀY
    showLoading(true);
    // ... rest of function stays the same

    // 5. THÊM CÁC HÀM MỚI VÀO CUỐI FILE (trước "// Setup tooltip close button"):

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
                    // Always show the placeholder option
                    option.style.display = '';
                    return;
                }

                const text = option.textContent.toUpperCase();
                option.style.display = text.includes(filter) ? '' : 'none';
            });

            // Auto-select if only one match (excluding placeholder)
            const visibleOptions = Array.from(options).filter(opt =>
                opt.value !== '' && opt.style.display !== 'none'
            );

            if (visibleOptions.length === 1) {
                selectElement.value = visibleOptions[0].value;
            }
        });

        // Clear search on select change
        selectElement.addEventListener('change', () => {
            if (searchInput) {
                searchInput.value = '';
                // Reset all options to visible
                const options = selectElement.querySelectorAll('option');
                options.forEach(option => {
                    option.style.display = '';
                });
            }
        });

        // Allow Enter key in search to load first match
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

// ===================================================================
// TÓM TẮT CÁC TÍNH NĂNG ĐÃ THÊM:
// ===================================================================
// ✅ Symbols được cache - không cần reload mỗi lần
// ✅ Loading indicator khi tải symbols
// ✅ Search/filter symbols trong dropdown
// ✅ Auto-select nếu chỉ có 1 kết quả tìm kiếm
// ✅ Enter key để chọn kết quả đầu tiên
// ✅ Ctrl/Cmd + K để focus vào search box
// ✅ localStorage lưu mã cuối cùng đã chọn
// ✅ Tự động load lại mã cuối cùng khi refresh trang
// ===================================================================
