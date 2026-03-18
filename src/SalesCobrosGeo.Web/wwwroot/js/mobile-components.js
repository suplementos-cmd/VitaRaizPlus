/**
 * JAVASCRIPT - Mobile Components & UI/UX
 * Bottom Sheet, Toast Notifications, Swipe Actions, etc.
 * VitaRaizPlus - Marzo 2026
 */

/* ===========================================================================
   BOTTOM SHEET
   =========================================================================== */

class BottomSheet {
  constructor(elementId) {
    this.sheet = document.getElementById(elementId);
    this.overlay = this.sheet?.previousElementSibling;
    this.handle = this.sheet?.querySelector('.mobile-bottom-sheet-handle');
    this.startY = 0;
    this.currentY = 0;
    this.isDragging = false;
    
    if (this.sheet) {
      this.init();
    }
  }

  init() {
    // Touch events para drag
    this.handle?.addEventListener('touchstart', (e) => this.handleTouchStart(e));
    this.handle?.addEventListener('touchmove', (e) => this.handleTouchMove(e));
    this.handle?.addEventListener('touchend', () => this.handleTouchEnd());
    
    // Click en overlay para cerrar
    this.overlay?.addEventListener('click', () => this.close());
  }

  open() {
    this.overlay?.classList.add('active');
    this.sheet?.classList.add('active');
    document.body.style.overflow = 'hidden';
  }

  close() {
    this.overlay?.classList.remove('active');
    this.sheet?.classList.remove('active');
    document.body.style.overflow = '';
  }

  handleTouchStart(e) {
    this.isDragging = true;
    this.startY = e.touches[0].clientY;
  }

  handleTouchMove(e) {
    if (!this.isDragging) return;
    
    this.currentY = e.touches[0].clientY;
    const diff = this.currentY - this.startY;
    
    if (diff > 0) {
      this.sheet.style.transform = `translateY(${diff}px)`;
    }
  }

  handleTouchEnd() {
    if (!this.isDragging) return;
    
    this.isDragging = false;
    const diff = this.currentY - this.startY;
    
    if (diff > 100) {
      this.close();
    }
    
    this.sheet.style.transform = '';
  }
}

/* ===========================================================================
   TOAST NOTIFICATIONS
   =========================================================================== */

class ToastManager {
  constructor() {
    this.container = this.createContainer();
  }

  createContainer() {
    let container = document.querySelector('.toast-container');
    if (!container) {
      container = document.createElement('div');
      container.className = 'toast-container';
      document.body.appendChild(container);
    }
    return container;
  }

  show(message, type = 'info', duration = 5000) {
    const toast = document.createElement('div');
    toast.className = `toast ${type}`;
    
    const icons = {
      success: '✓',
      error: '✕',
      warning: '!',
      info: 'i'
    };
    
    toast.innerHTML = `
      <div class="toast-icon">${icons[type]}</div>
      <div class="toast-content">
        <div class="toast-message">${message}</div>
      </div>
      <button class="toast-close" aria-label="Cerrar">×</button>
      <div class="toast-progress"></div>
    `;
    
    const closeBtn = toast.querySelector('.toast-close');
    closeBtn.addEventListener('click', () => this.hide(toast));
    
    this.container.appendChild(toast);
    
    // Auto-dismiss
    if (duration > 0) {
      setTimeout(() => this.hide(toast), duration);
    }
    
    return toast;
  }

  hide(toast) {
    toast.style.opacity = '0';
    toast.style.transform = 'translateX(100%)';
    setTimeout(() => toast.remove(), 300);
  }

  success(message, duration) {
    return this.show(message, 'success', duration);
  }

  error(message, duration) {
    return this.show(message, 'error', duration);
  }

  warning(message, duration) {
    return this.show(message, 'warning', duration);
  }

  info(message, duration) {
    return this.show(message, 'info', duration);
  }
}

// Instancia global
window.toast = new ToastManager();

/* ===========================================================================
   SWIPE ACTIONS
   =========================================================================== */

class SwipeActions {
  constructor(element) {
    this.element = element;
    this.content = element.querySelector('.swipeable-content');
    this.actions = element.querySelector('.swipeable-actions');
    this.startX = 0;
    this.currentX = 0;
    this.isDragging = false;
    this.maxSwipe = this.actions?.offsetWidth || 160;
    
    this.init();
  }

  init() {
    this.content.addEventListener('touchstart', (e) => this.handleTouchStart(e));
    this.content.addEventListener('touchmove', (e) => this.handleTouchMove(e));
    this.content.addEventListener('touchend', () => this.handleTouchEnd());
  }

  handleTouchStart(e) {
    this.isDragging = true;
    this.startX = e.touches[0].clientX;
  }

  handleTouchMove(e) {
    if (!this.isDragging) return;
    
    this.currentX = e.touches[0].clientX;
    const diff = this.startX - this.currentX;
    
    if (diff > 0 && diff <= this.maxSwipe) {
      this.content.style.transform = `translateX(-${diff}px)`;
    }
  }

  handleTouchEnd() {
    if (!this.isDragging) return;
    
    this.isDragging = false;
    const diff = this.startX - this.currentX;
    
    if (diff > this.maxSwipe / 2) {
      this.content.style.transform = `translateX(-${this.maxSwipe}px)`;
    } else {
      this.content.style.transform = '';
    }
  }

  reset() {
    this.content.style.transform = '';
  }
}

// Inicializar swipe actions
document.addEventListener('DOMContentLoaded', () => {
  document.querySelectorAll('.swipeable-item').forEach(item => {
    new SwipeActions(item);
  });
});

/* ===========================================================================
   PULL TO REFRESH
   =========================================================================== */

class PullToRefresh {
  constructor(element, onRefresh) {
    this.element = element;
    this.onRefresh = onRefresh;
    this.indicator = element.querySelector('.pull-to-refresh-indicator');
    this.startY = 0;
    this.currentY = 0;
    this.isDragging = false;
    this.threshold = 60;
    
    this.init();
  }

  init() {
    this.element.addEventListener('touchstart', (e) => this.handleTouchStart(e));
    this.element.addEventListener('touchmove', (e) => this.handleTouchMove(e));
    this.element.addEventListener('touchend', () => this.handleTouchEnd());
  }

  handleTouchStart(e) {
    if (this.element.scrollTop === 0) {
      this.isDragging = true;
      this.startY = e.touches[0].clientY;
    }
  }

  handleTouchMove(e) {
    if (!this.isDragging) return;
    
    this.currentY = e.touches[0].clientY;
    const diff = this.currentY - this.startY;
    
    if (diff > 0 && diff < 100) {
      e.preventDefault();
      this.element.classList.add('pulling');
    }
  }

  handleTouchEnd() {
    if (!this.isDragging) return;
    
    this.isDragging = false;
    const diff = this.currentY - this.startY;
    
    if (diff > this.threshold) {
      this.refresh();
    } else {
      this.element.classList.remove('pulling');
    }
  }

  async refresh() {
    if (this.onRefresh) {
      await this.onRefresh();
    }
    setTimeout(() => {
      this.element.classList.remove('pulling');
    }, 500);
  }
}

/* ===========================================================================
   MOBILE TABS
   =========================================================================== */

function initMobileTabs() {
  document.querySelectorAll('.mobile-tabs').forEach(tabGroup => {
    const tabs = tabGroup.querySelectorAll('.mobile-tab');
    
    tabs.forEach(tab => {
      tab.addEventListener('click', () => {
        // Remove active de todos
        tabs.forEach(t => t.classList.remove('active'));
        
        // Agregar active al clickeado
        tab.classList.add('active');
        
        // Scroll al tab
        tab.scrollIntoView({ behavior: 'smooth', block: 'nearest', inline: 'center' });
        
        // Disparar evento custom para manejar el contenido
        const event = new CustomEvent('tabChange', { detail: { tabId: tab.dataset.tabId } });
        tabGroup.dispatchEvent(event);
      });
    });
  });
}

/* ===========================================================================
   BUTTON LOADING STATE
   =========================================================================== */

function setButtonLoading(button, isLoading) {
  if (isLoading) {
    button.classList.add('btn-loading');
    button.disabled = true;
  } else {
    button.classList.remove('btn-loading');
    button.disabled = false;
  }
}

/* ===========================================================================
   RIPPLE EFFECT
   =========================================================================== */

function addRippleEffect(element) {
  element.classList.add('ripple');
  
  element.addEventListener('click', function(e) {
    const rect = this.getBoundingClientRect();
    const x = e.clientX - rect.left;
    const y = e.clientY - rect.top;
    
    const ripple = document.createElement('span');
    ripple.style.left = x + 'px';
    ripple.style.top = y + 'px';
    ripple.classList.add('ripple-effect');
    
    this.appendChild(ripple);
    
    setTimeout(() => ripple.remove(), 600);
  });
}

/* ===========================================================================
   INITIALIZATION
   =========================================================================== */

document.addEventListener('DOMContentLoaded', () => {
  // Inicializar tabs
  initMobileTabs();
  
  // Agregar ripple a botones
  document.querySelectorAll('.btn-mobile, .btn-primary, .icon-button-mobile').forEach(btn => {
    addRippleEffect(btn);
  });
  
  console.log('✓ Mobile components initialized');
});

/* ===========================================================================
   EXPORTS
   =========================================================================== */

window.VitaRaiz = {
  BottomSheet,
  ToastManager,
  SwipeActions,
  PullToRefresh,
  toast: window.toast,
  setButtonLoading,
  addRippleEffect
};
