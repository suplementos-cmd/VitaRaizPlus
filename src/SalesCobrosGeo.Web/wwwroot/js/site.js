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

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initImagePreview);
    } else {
        initImagePreview();
    }
})();
