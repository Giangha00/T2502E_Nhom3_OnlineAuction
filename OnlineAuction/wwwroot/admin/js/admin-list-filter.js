(function (window) {
    'use strict';

    function AdminListFilter(options) {
        this.options = Object.assign({
            searchDebounceMs: 500,
            filterControlSelector: '.admin-filter-control',
            pageLinkSelector: '.js-admin-page',
            loadingClass: ['opacity-60', 'pointer-events-none']
        }, options);

        this.activeRequest = null;
        this.searchDebounceTimer = null;
        this.lastSubmittedSearch = '';
        this.init();
    }

    AdminListFilter.prototype.init = function () {
        const opts = this.options;
        const filterForm = document.getElementById(opts.formId);
        const resultsContainer = document.getElementById(opts.resultsId);
        const searchInput = opts.searchInputId
            ? document.getElementById(opts.searchInputId)
            : filterForm?.querySelector('input[name="Search"]');

        if (!filterForm || !resultsContainer) {
            return;
        }

        this.filterForm = filterForm;
        this.resultsContainer = resultsContainer;
        this.searchInput = searchInput;
        this.lastSubmittedSearch = searchInput ? searchInput.value.trim() : '';

        const self = this;

        filterForm.addEventListener('submit', function (event) {
            event.preventDefault();
            self.load(1);
        });

        filterForm.querySelectorAll(opts.filterControlSelector).forEach(function (control) {
            control.addEventListener('change', function () {
                self.load(1);
            });
        });

        resultsContainer.addEventListener('click', function (event) {
            const pageLink = event.target.closest(opts.pageLinkSelector);
            if (!pageLink) {
                return;
            }

            event.preventDefault();

            const linkUrl = new URL(pageLink.href, window.location.origin);
            const page = Number.parseInt(linkUrl.searchParams.get('Page') || '1', 10) || 1;
            self.load(page);
        });

        if (searchInput) {
            searchInput.addEventListener('keydown', function (event) {
                if (event.key === 'Enter') {
                    event.preventDefault();
                    clearTimeout(self.searchDebounceTimer);
                    self.lastSubmittedSearch = searchInput.value.trim();
                    self.load(1);
                }
            });

            searchInput.addEventListener('input', function () {
                clearTimeout(self.searchDebounceTimer);

                const currentValue = searchInput.value.trim();

                if (currentValue === '') {
                    if (self.lastSubmittedSearch !== '') {
                        self.lastSubmittedSearch = '';
                        self.load(1);
                    }

                    return;
                }

                self.searchDebounceTimer = setTimeout(function () {
                    if (currentValue === self.lastSubmittedSearch) {
                        return;
                    }

                    self.lastSubmittedSearch = currentValue;
                    self.load(1);
                }, opts.searchDebounceMs);
            });
        }

        if (typeof opts.setupDateRange === 'function') {
            opts.setupDateRange(function () {
                self.load(1);
            });
        }

        if (typeof opts.onUpdated === 'function') {
            opts.onUpdated(resultsContainer);
        }
    };

    AdminListFilter.prototype.buildQueryString = function (page) {
        const formData = new FormData(this.filterForm);
        formData.set('Page', String(page || 1));

        const params = new URLSearchParams();
        formData.forEach(function (value, key) {
            if (value !== null && value !== undefined && String(value).length > 0) {
                params.set(key, value);
            }
        });

        const query = params.toString();
        return query ? '?' + query : '';
    };

    AdminListFilter.prototype.load = async function (page) {
        const opts = this.options;

        if (!this.filterForm || !this.resultsContainer) {
            return;
        }

        if (this.activeRequest) {
            this.activeRequest.abort();
        }

        this.activeRequest = new AbortController();
        const queryString = this.buildQueryString(page);
        const requestUrl = opts.listUrl + queryString;

        this.resultsContainer.classList.add(...opts.loadingClass);

        try {
            const response = await fetch(requestUrl, {
                method: 'GET',
                headers: {
                    'X-Requested-With': 'XMLHttpRequest'
                },
                signal: this.activeRequest.signal
            });

            if (!response.ok) {
                throw new Error('Failed to load list.');
            }

            const html = await response.text();
            this.resultsContainer.innerHTML = html;

            const pageInput = this.filterForm.querySelector('input[name="Page"]');
            if (pageInput) {
                pageInput.value = String(page || 1);
            }

            if (window.history && window.history.replaceState) {
                window.history.replaceState(null, '', requestUrl);
            }

            if (typeof opts.onUpdated === 'function') {
                opts.onUpdated(this.resultsContainer);
            }
        } catch (error) {
            if (error.name !== 'AbortError') {
                console.error(error);
            }
        } finally {
            this.resultsContainer.classList.remove(...opts.loadingClass);
        }
    };

    window.AdminListFilter = {
        init: function (options) {
            return new AdminListFilter(options);
        },

        setupDateRangePicker: function (inputSelector, submitCallback) {
            if (!window.jQuery || !window.jQuery.fn.daterangepicker) {
                return;
            }

            const $picker = window.jQuery(inputSelector);
            if (!$picker.length) {
                return;
            }

            $picker.daterangepicker({
                autoUpdateInput: false,
                locale: {
                    cancelLabel: 'Clear',
                    format: 'MM/DD/YYYY'
                }
            });

            $picker.off('apply.daterangepicker.adminFilter cancel.daterangepicker.adminFilter');
            $picker.on('apply.daterangepicker.adminFilter', function (event, picker) {
                window.jQuery(this).val(
                    picker.startDate.format('MM/DD/YYYY') + ' - ' + picker.endDate.format('MM/DD/YYYY')
                );
                submitCallback();
            });
            $picker.on('cancel.daterangepicker.adminFilter', function () {
                window.jQuery(this).val('');
                submitCallback();
            });
        },

        bindBulkDelete: function (container, options) {
            const root = container || document;
            const settings = Object.assign({
                formId: null,
                selectAllId: null,
                checkboxClass: null,
                submitDataAttr: 'bulkDelete',
                emptyMessage: 'Please select at least one item.',
                confirmMessage: 'Are you sure you want to delete selected items?'
            }, options);

            const bulkForm = settings.formId ? root.querySelector('#' + settings.formId) : null;
            const selectAll = settings.selectAllId ? root.querySelector('#' + settings.selectAllId) : null;
            const checkboxes = settings.checkboxClass
                ? root.querySelectorAll('.' + settings.checkboxClass)
                : [];

            if (selectAll) {
                selectAll.onchange = function () {
                    checkboxes.forEach(function (checkbox) {
                        checkbox.checked = selectAll.checked;
                    });
                };
            }

            if (!bulkForm) {
                return;
            }

            bulkForm.onsubmit = function (event) {
                if (event.submitter?.dataset[settings.submitDataAttr] !== 'true') {
                    return;
                }

                const selectedCount = bulkForm.querySelectorAll(
                    settings.checkboxClass ? '.' + settings.checkboxClass + ':checked' : 'input[type="checkbox"]:checked'
                ).length;

                if (selectedCount === 0) {
                    event.preventDefault();
                    alert(settings.emptyMessage);
                    return;
                }

                if (!confirm(settings.confirmMessage)) {
                    event.preventDefault();
                }
            };
        }
    };
})(window);
