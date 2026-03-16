(function () {
    let heartbeatInitialized = false;
    let ajaxUiInitialized = false;

    function ensureViewer() {
        let overlay = document.getElementById('imagePreviewOverlay');
        if (overlay) {
            return overlay;
        }

        overlay = document.createElement('div');
        overlay.id = 'imagePreviewOverlay';
        overlay.className = 'image-preview-overlay';
        overlay.innerHTML = '<img alt="Vista previa" class="image-preview-full" />';
        document.body.appendChild(overlay);

        const full = overlay.querySelector('.image-preview-full');
        if (full) {
            full.addEventListener('click', (e) => e.stopPropagation());
        }

        overlay.addEventListener('click', () => overlay.classList.remove('open'));
        document.addEventListener('keydown', (e) => {
            if (e.key === 'Escape') {
                overlay.classList.remove('open');
            }
        });

        return overlay;
    }

    function initImagePreview(root) {
        const overlay = ensureViewer();
        const full = overlay.querySelector('.image-preview-full');

        root.querySelectorAll('[data-previewable="true"]').forEach((el) => {
            if (el.dataset.previewBound === 'true') {
                return;
            }

            el.dataset.previewBound = 'true';
            el.addEventListener('click', (e) => {
                e.preventDefault();
                e.stopPropagation();
                const src = el.getAttribute('src');
                if (!src || !full) {
                    return;
                }

                full.setAttribute('src', src);
                overlay.classList.add('open');
            });
        });
    }

    async function postHeartbeat() {
        const body = document.body;
        if (!body || body.dataset.authenticated !== 'true' || !navigator.geolocation) {
            return;
        }

        const token = document.querySelector('meta[name="request-verification-token"]')?.getAttribute('content');
        if (!token) {
            return;
        }

        navigator.geolocation.getCurrentPosition(async (position) => {
            const coordinates = `${position.coords.latitude.toFixed(6)}, ${position.coords.longitude.toFixed(6)}`;
            try {
                await fetch('/Account/Heartbeat', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'RequestVerificationToken': token
                    },
                    body: JSON.stringify({ coordinates })
                });
            } catch {
                // Ignore heartbeat transport errors to avoid interrupting UI usage.
            }
        }, () => {
            // Ignore user denial or GPS issues.
        }, {
            enableHighAccuracy: false,
            maximumAge: 60000,
            timeout: 10000
        });
    }

    function initHeartbeat() {
        const body = document.body;
        if (!body || body.dataset.authenticated !== 'true' || heartbeatInitialized) {
            return;
        }

        heartbeatInitialized = true;
        postHeartbeat();
        window.setInterval(postHeartbeat, 120000);
        document.addEventListener('visibilitychange', () => {
            if (!document.hidden) {
                postHeartbeat();
            }
        });
    }

    function initToasts(root) {
        root.querySelectorAll('[data-auto-toast="true"]').forEach((toast) => {
            if (toast.dataset.toastBound === 'true') {
                return;
            }

            toast.dataset.toastBound = 'true';
            const delay = Number.parseInt(toast.dataset.toastDelay || '3200', 10);
            window.setTimeout(() => {
                toast.setAttribute('data-hiding', 'true');
                window.setTimeout(() => toast.remove(), 260);
            }, Number.isFinite(delay) ? delay : 3200);
        });
    }

    function bindCollectorFeatures(root) {
        const mobileSearchShell = root.querySelector('[data-cobros-search-shell="true"]');
        const mobileSearchInput = root.querySelector('#collectorMobileSearch');

        const bindSearch = (inputId, listId, rowSelector, countId, emptyId) => {
            const input = $(root).find(`#${inputId}`);
            const list = $(root).find(`#${listId}`);
            const count = $(root).find(`#${countId}`);
            const empty = $(root).find(`#${emptyId}`);
            if (input.length === 0 || list.length === 0 || count.length === 0 || input.data('bound') === true) {
                return;
            }

            input.data('bound', true);
            const rows = list.find(rowSelector);
            count.text(String(rows.length));

            const applyFilter = () => {
                const term = String(input.val() || '').trim().toLowerCase();
                let visible = 0;
                rows.each(function () {
                    const row = $(this);
                    const haystack = String(row.attr('data-search') || '');
                    const match = term.length === 0 || haystack.includes(term);
                    row.prop('hidden', !match);
                    if (match) {
                        visible += 1;
                    }
                });
                count.text(String(visible));
                if (empty.length > 0) {
                    empty.prop('hidden', visible !== 0);
                }
            };

            input.on('input', applyFilter);
        };

        bindSearch('collectorMobileSearch', 'collectorMobileSalesList', '.collector-mobile-sale-row', 'collectorMobileCount', 'collectorMobileEmptySearch');
        bindSearch('collectorDesktopSearch', 'collectorDesktopSalesList', '.collector-desktop-record', 'collectorDesktopCount', 'collectorDesktopEmptySearch');

        if (mobileSearchShell && mobileSearchInput && mobileSearchShell.dataset.bound !== 'true') {
            mobileSearchShell.dataset.bound = 'true';
            if (mobileSearchInput.value) {
                mobileSearchShell.hidden = false;
            }
        }

        root.querySelectorAll('[data-tree-toggle]').forEach((button) => {
            if (button.dataset.bound === 'true') {
                return;
            }

            button.dataset.bound = 'true';
            button.addEventListener('click', () => {
                const group = button.closest('.collector-desktop-treegroup');
                const list = group?.querySelector('.collector-desktop-statuslist');
                if (!group || !list) {
                    return;
                }

                const willOpen = list.hasAttribute('hidden');
                list.toggleAttribute('hidden', !willOpen);
                button.setAttribute('aria-expanded', willOpen ? 'true' : 'false');
                button.classList.toggle('open', willOpen);
                group.classList.toggle('is-open', willOpen);
            });
        });
    }

    function bindSalesFeatures(root) {
        const searchShell = root.querySelector('[data-sales-search-shell="true"]');
        const searchToggle = root.querySelector('[data-sales-search-toggle="true"]');
        const searchInput = root.querySelector('#salesAppSearch');
        const records = Array.from(root.querySelectorAll('[data-sales-search]'));

        if (searchToggle && searchShell && searchToggle.dataset.bound !== 'true') {
            searchToggle.dataset.bound = 'true';
            searchToggle.addEventListener('click', () => {
                const isOpen = searchShell.dataset.open === 'true';
                searchShell.dataset.open = isOpen ? 'false' : 'true';
                searchShell.hidden = isOpen;
                if (!isOpen) {
                    window.setTimeout(() => searchInput?.focus(), 60);
                }
            });
        }

        if (searchInput && searchInput.dataset.bound !== 'true') {
            searchInput.dataset.bound = 'true';
            const applySalesFilter = () => {
                const term = String(searchInput.value || '').trim().toLowerCase();
                records.forEach((record) => {
                    const haystack = String(record.getAttribute('data-sales-search') || '');
                    record.hidden = term.length > 0 && !haystack.includes(term);
                });
            };

            searchInput.addEventListener('input', applySalesFilter);
        }
    }

    function bindContextActions(root) {
        root.querySelectorAll('[data-context-action]').forEach((button) => {
            if (button.dataset.bound === 'true') {
                return;
            }

            button.dataset.bound = 'true';
            button.addEventListener('click', () => {
                const action = button.dataset.contextAction;
                if (action === 'toggle-sales-search') {
                    const toggle = root.querySelector('[data-sales-search-toggle="true"]');
                    toggle?.click();
                }

                if (action === 'toggle-cobros-search') {
                    const shell = root.querySelector('[data-cobros-search-shell="true"]');
                    const input = root.querySelector('#collectorMobileSearch');
                    if (!shell) {
                        return;
                    }

                    const isHidden = shell.hidden;
                    shell.hidden = !isHidden;
                    if (isHidden) {
                        window.setTimeout(() => input?.focus(), 60);
                    } else if (input) {
                        input.value = '';
                        input.dispatchEvent(new Event('input', { bubbles: true }));
                    }
                }

                if (action === 'focus-dashboard-filter') {
                    const field = root.querySelector('#from');
                    field?.focus();
                    field?.scrollIntoView({ behavior: 'smooth', block: 'center' });
                }
            });
        });
    }

    function bindAdminUsers(root) {
        const page = root.querySelector('.admin-console-page');
        if (!page) {
            document.body.classList.remove('admin-modal-open');
            const persistedTheme = document.body.dataset.persistedTheme;
            if (persistedTheme) {
                document.body.dataset.roleTheme = persistedTheme;
            }
            return;
        }

        const searchInput = root.querySelector('#adminUserSearch');
        const roleFilter = root.querySelector('#adminRoleFilter');
        const statusFilter = root.querySelector('#adminStatusFilter');
        const rows = Array.from(root.querySelectorAll('.admin-user-rowcard'));

        if (searchInput && roleFilter && statusFilter && searchInput.dataset.bound !== 'true') {
            searchInput.dataset.bound = 'true';
            const applyUserFilter = () => {
                const term = String(searchInput.value || '').trim().toUpperCase();
                const role = String(roleFilter.value || '').trim().toUpperCase();
                const status = String(statusFilter.value || '').trim().toUpperCase();

                rows.forEach((row) => {
                    const matchText = !term || String(row.getAttribute('data-user-search') || '').includes(term);
                    const matchRole = !role || String(row.getAttribute('data-user-role') || '') === role;
                    const matchStatus = !status || String(row.getAttribute('data-user-status') || '') === status;
                    row.hidden = !(matchText && matchRole && matchStatus);
                });
            };

            searchInput.addEventListener('input', applyUserFilter);
            roleFilter.addEventListener('change', applyUserFilter);
            statusFilter.addEventListener('change', applyUserFilter);
        }

        const select = root.querySelector('.admin-theme-select');
        const roleSelect = root.querySelector('.admin-role-select');
        const themeSuggest = root.querySelector('[data-role-theme-suggest="true"]');
        if (select && select.dataset.bound !== 'true') {
            select.dataset.bound = 'true';
            let themeCustomized = false;

            const applyThemeValue = (value, markCustomized) => {
                if (!value) {
                    return;
                }

                if (markCustomized) {
                    themeCustomized = true;
                }

                select.value = value;
                syncThemeSelect();
            };

            const syncThemeSelect = () => {
                const selected = select.options[select.selectedIndex];
                const tone = selected?.dataset.themeTone || '#203a72';
                const accent = selected?.dataset.themeAccent || '#5f8cff';
                const surface = selected?.dataset.themeSurface || '#eef3ff';
                select.style.color = tone;
                select.style.borderColor = tone;
                select.style.boxShadow = `0 0 0 4px color-mix(in srgb, ${accent} 16%, transparent)`;

                const dialog = select.closest('.admin-editor-dialog');
                if (dialog) {
                    dialog.style.setProperty('--admin-theme-tone', tone);
                    dialog.style.setProperty('--admin-theme-accent', accent);
                    dialog.style.setProperty('--admin-theme-surface', surface);
                }

                document.body.dataset.roleTheme = select.value || document.body.dataset.persistedTheme || 'root';
            };

            select.addEventListener('change', () => applyThemeValue(select.value, true));

            if (roleSelect && roleSelect.dataset.bound !== 'true') {
                roleSelect.dataset.bound = 'true';
                roleSelect.addEventListener('change', () => {
                    const selectedRole = roleSelect.options[roleSelect.selectedIndex];
                    const suggestedTheme = selectedRole?.dataset.defaultTheme;
                    if (!themeCustomized && suggestedTheme) {
                        applyThemeValue(suggestedTheme, false);
                    }
                });
            }

            if (themeSuggest && themeSuggest.dataset.bound !== 'true') {
                themeSuggest.dataset.bound = 'true';
                themeSuggest.addEventListener('click', () => {
                    const selectedRole = roleSelect?.options[roleSelect.selectedIndex];
                    const suggestedTheme = selectedRole?.dataset.defaultTheme;
                    if (suggestedTheme) {
                        themeCustomized = false;
                        applyThemeValue(suggestedTheme, false);
                    }
                });
            }

            syncThemeSelect();
        }

        const modal = root.querySelector('.admin-editor-modal');
        if (modal) {
            document.body.classList.add('admin-modal-open');
            const focusTarget = modal.querySelector('#displayName') || modal.querySelector('#username') || modal.querySelector('input, select, textarea');
            window.setTimeout(() => {
                focusTarget?.focus();
                if (window.innerWidth <= 767) {
                    modal.querySelector('.admin-editor-dialog')?.scrollTo({ top: 0, behavior: 'instant' });
                }
            }, 80);
        } else {
            document.body.classList.remove('admin-modal-open');
            const persistedTheme = document.body.dataset.persistedTheme;
            if (persistedTheme) {
                document.body.dataset.roleTheme = persistedTheme;
            }
        }
    }

    function bindInlineToggles(root) {
        root.querySelectorAll('[data-toggle-submit="true"]').forEach((toggle) => {
            if (toggle.dataset.bound === 'true') {
                return;
            }

            toggle.dataset.bound = 'true';
            toggle.addEventListener('change', () => {
                const form = toggle.closest('form');
                const hidden = form?.querySelector('input[type="hidden"][name="isActive"]');
                const label = form?.querySelector('.app-switch-label');
                if (!form || !hidden) {
                    return;
                }

                hidden.value = toggle.checked ? 'true' : 'false';
                if (label) {
                    label.textContent = toggle.checked ? 'Activo' : 'Inactivo';
                }

                if (typeof window.jQuery !== 'undefined') {
                    $(form).trigger('submit');
                } else {
                    form.submit();
                }
            });
        });
    }

    function replaceAjaxTarget(targetSelector, html) {
        const target = document.querySelector(targetSelector);
        if (!target) {
            return false;
        }

        const parsed = new DOMParser().parseFromString(html, 'text/html');
        const replacement = parsed.querySelector(targetSelector);
        if (!replacement) {
            return false;
        }

        target.replaceWith(replacement);
        document.title = parsed.title || document.title;
        initializePage(document);
        return true;
    }

    function requestAjax(url, options) {
        const token = document.querySelector('meta[name="request-verification-token"]')?.getAttribute('content') || '';
        return $.ajax({
            url,
            method: options.method || 'GET',
            data: options.data,
            headers: {
                'X-Requested-With': 'XMLHttpRequest',
                'RequestVerificationToken': token
            }
        });
    }

    function initAjaxUi() {
        if (ajaxUiInitialized || typeof window.jQuery === 'undefined') {
            return;
        }

        ajaxUiInitialized = true;

        $(document).on('submit', 'form[data-ajaxify="true"]', function (event) {
            event.preventDefault();
            const form = $(this);
            const target = form.data('ajaxTarget');
            if (!target) {
                return;
            }

            const submitButton = form.find('button[type="submit"]').first();
            const originalLabel = submitButton.length > 0 ? submitButton.html() : '';
            const workingLabel = submitButton.data('workingLabel');
            if (submitButton.length > 0) {
                submitButton.prop('disabled', true);
                if (workingLabel) {
                    submitButton.text(workingLabel);
                }
            }

            requestAjax(form.attr('action') || window.location.href, {
                method: (form.attr('method') || 'GET').toUpperCase(),
                data: form.serialize()
            }).done((html) => {
                if (!replaceAjaxTarget(target, html)) {
                    window.location.href = form.attr('action') || window.location.href;
                }
            }).fail(() => {
                if (submitButton.length > 0) {
                    submitButton.prop('disabled', false);
                    submitButton.html(originalLabel);
                }
                window.location.href = form.attr('action') || window.location.href;
            });
        });

        $(document).on('click', 'a[data-ajax-link="true"]', function (event) {
            const link = $(this);
            const target = link.data('ajaxTarget');
            const href = link.attr('href');
            if (!target || !href) {
                return;
            }

            event.preventDefault();
            requestAjax(href, { method: 'GET' }).done((html) => {
                if (!replaceAjaxTarget(target, html)) {
                    window.location.href = href;
                }
            }).fail(() => {
                window.location.href = href;
            });
        });
    }

    function isLocalhost() {
        const host = window.location.hostname;
        return host === 'localhost' || host === '127.0.0.1' || host === '::1';
    }

    async function unregisterServiceWorkers() {
        if (!('serviceWorker' in navigator)) {
            return;
        }

        try {
            const registrations = await navigator.serviceWorker.getRegistrations();
            await Promise.all(registrations.map((registration) => registration.unregister()));
        } catch {
            // Ignore unregister issues in browsers that restrict it.
        }

        if ('caches' in window) {
            try {
                const keys = await caches.keys();
                await Promise.all(keys.map((key) => caches.delete(key)));
            } catch {
                // Ignore cache cleanup issues.
            }
        }
    }

    async function setupServiceWorker() {
        if (!('serviceWorker' in navigator)) {
            return;
        }

        if (isLocalhost()) {
            await unregisterServiceWorkers();
            return;
        }

        navigator.serviceWorker.register('/service-worker.js').catch(() => {
            // Ignore registration issues.
        });
    }

    function initializePage(root) {
        initImagePreview(root);
        initToasts(root);
        bindCollectorFeatures(root);
        bindSalesFeatures(root);
        bindContextActions(root);
        bindAdminUsers(root);
        bindInlineToggles(root);
    }

    function boot() {
        document.body.classList.add('app-ready');
        if (!document.body.dataset.persistedTheme && document.body.dataset.roleTheme) {
            document.body.dataset.persistedTheme = document.body.dataset.roleTheme;
        }
        initializePage(document);
        initHeartbeat();
        initAjaxUi();
        setupServiceWorker();
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', boot);
    } else {
        boot();
    }
})();
