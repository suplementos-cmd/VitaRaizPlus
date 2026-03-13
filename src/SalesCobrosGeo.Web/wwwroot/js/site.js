// Mobile image preview overlay for sales and forms.
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
            full.addEventListener('click', (e) => {
                e.stopPropagation();
            });
        }

        overlay.addEventListener('click', () => {
            overlay.classList.remove('open');
        });

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
                if (!src) {
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

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', () => {
            initImagePreview();
            initHeartbeat();
        });
    } else {
        initImagePreview();
        initHeartbeat();
    }
})();
