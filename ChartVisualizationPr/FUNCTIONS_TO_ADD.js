// =================================================================
// COPY 3 HÀM NÀY VÀO CUỐI FILE chart.js
// (Trước dòng "// Setup tooltip close button")
// =================================================================

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
