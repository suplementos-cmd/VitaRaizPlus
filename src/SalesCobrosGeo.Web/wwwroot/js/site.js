(function () {
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

    function initImagePreview() {
        const overlay = ensureViewer();
        const full = overlay.querySelector('.image-preview-full');

        document.querySelectorAll('[data-previewable="true"]').forEach((el) => {
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
        if (!body || body.dataset.authenticated !== 'true') {
            return;
        }

        postHeartbeat();
        window.setInterval(postHeartbeat, 120000);
        document.addEventListener('visibilitychange', () => {
            if (!document.hidden) {
                postHeartbeat();
            }
        });
    }

    function initToasts() {
        document.querySelectorAll('[data-auto-toast="true"]').forEach((toast) => {
            window.setTimeout(() => {
                toast.setAttribute('data-hiding', 'true');
                window.setTimeout(() => toast.remove(), 260);
            }, 3200);
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

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', () => {
            document.body.classList.add('app-ready');
            initImagePreview();
            initHeartbeat();
            initToasts();
            setupServiceWorker();
        });
    } else {
        document.body.classList.add('app-ready');
        initImagePreview();
        initHeartbeat();
        initToasts();
        setupServiceWorker();
    }
})();
