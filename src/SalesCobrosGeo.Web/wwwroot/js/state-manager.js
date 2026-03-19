/**
 * State Manager - Application State Persistence Architecture
 * 
 * Captures and restores complete application state including:
 * - Scroll position
 * - Active filters
 * - Selected sections/items
 * - Form state
 * - UI component state (tabs, panels, etc.)
 * 
 * Architecture: Modular state providers with automatic capture/restore
 */

(function (window) {
  'use strict';

  /**
   * State Manager Core
   */
  const StateManager = {
    providers: {},
    
    /**
     * Register a state provider
     * @param {string} key - Unique identifier for this state provider
     * @param {object} provider - Object with capture() and restore() methods
     */
    register(key, provider) {
      if (!provider.capture || !provider.restore) {
        console.error(`StateManager: Provider "${key}" must have capture() and restore() methods`);
        return;
      }
      this.providers[key] = provider;
    },

    /**
     * Capture current application state
     * @returns {object} Complete state snapshot
     */
    captureState() {
      const state = {
        timestamp: Date.now(),
        url: window.location.href,
        title: document.title,
        providers: {}
      };

      // Capture state from all registered providers
      for (const [key, provider] of Object.entries(this.providers)) {
        try {
          state.providers[key] = provider.capture();
        } catch (error) {
          console.error(`StateManager: Error capturing state from provider "${key}"`, error);
        }
      }

      return state;
    },

    /**
     * Restore application state
     * @param {object} state - State snapshot to restore
     */
    restoreState(state) {
      if (!state || !state.providers) {
        console.warn('StateManager: Invalid state to restore');
        return;
      }

      // Restore state to all registered providers
      for (const [key, provider] of Object.entries(this.providers)) {
        if (state.providers[key]) {
          try {
            provider.restore(state.providers[key]);
          } catch (error) {
            console.error(`StateManager: Error restoring state to provider "${key}"`, error);
          }
        }
      }
    },

    /**
     * Save current state to history
     */
    saveToHistory() {
      const state = this.captureState();
      const currentState = history.state || {};
      history.replaceState({ ...currentState, appState: state }, '');
    },

    /**
     * Load state from history
     */
    loadFromHistory() {
      const historyState = history.state;
      if (historyState && historyState.appState) {
        // Delay restoration to allow DOM to stabilize
        setTimeout(() => {
          this.restoreState(historyState.appState);
        }, 100);
      }
    }
  };

  // ────────────────────────────────────────────────────────────
  // Built-in State Providers
  // ────────────────────────────────────────────────────────────

  /**
   * Scroll Position Provider
   */
  StateManager.register('scroll', {
    capture() {
      return {
        x: window.scrollX || window.pageXOffset,
        y: window.scrollY || window.pageYOffset,
        container: this.captureContainerScrolls()
      };
    },

    restore(state) {
      if (state.x !== undefined && state.y !== undefined) {
        window.scrollTo(state.x, state.y);
      }
      if (state.container) {
        this.restoreContainerScrolls(state.container);
      }
    },

    captureContainerScrolls() {
      const containers = document.querySelectorAll('[data-preserve-scroll]');
      const scrolls = {};
      containers.forEach((container, index) => {
        const id = container.id || `container-${index}`;
        scrolls[id] = {
          scrollTop: container.scrollTop,
          scrollLeft: container.scrollLeft
        };
      });
      return scrolls;
    },

    restoreContainerScrolls(scrolls) {
      const containers = document.querySelectorAll('[data-preserve-scroll]');
      containers.forEach((container, index) => {
        const id = container.id || `container-${index}`;
        if (scrolls[id]) {
          container.scrollTop = scrolls[id].scrollTop;
          container.scrollLeft = scrolls[id].scrollLeft;
        }
      });
    }
  });

  /**
   * Form Input Provider
   */
  StateManager.register('forms', {
    capture() {
      const forms = {};
      document.querySelectorAll('[data-preserve-form]').forEach((form, index) => {
        const formId = form.id || form.name || `form-${index}`;
        const formData = {};
        
        form.querySelectorAll('input, select, textarea').forEach(field => {
          const name = field.name || field.id;
          if (!name || field.type === 'password') return;

          if (field.type === 'checkbox' || field.type === 'radio') {
            formData[name] = field.checked;
          } else {
            formData[name] = field.value;
          }
        });
        
        forms[formId] = formData;
      });
      return forms;
    },

    restore(state) {
      document.querySelectorAll('[data-preserve-form]').forEach((form, index) => {
        const formId = form.id || form.name || `form-${index}`;
        const formData = state[formId];
        if (!formData) return;

        form.querySelectorAll('input, select, textarea').forEach(field => {
          const name = field.name || field.id;
          if (!name || !(name in formData)) return;

          if (field.type === 'checkbox' || field.type === 'radio') {
            field.checked = formData[name];
          } else {
            field.value = formData[name];
          }

          // Trigger change event for any listeners
          field.dispatchEvent(new Event('change', { bubbles: true }));
        });
      });
    }
  });

  /**
   * Active Tab Provider
   */
  StateManager.register('tabs', {
    capture() {
      const tabs = {};
      document.querySelectorAll('[data-preserve-tabs]').forEach((container, index) => {
        const containerId = container.id || `tabs-${index}`;
        const activeTab = container.querySelector('.nav-link.active, .tab.active, [aria-selected="true"]');
        if (activeTab) {
          tabs[containerId] = {
            selector: this.getSelector(activeTab),
            id: activeTab.id,
            href: activeTab.getAttribute('href')
          };
        }
      });
      return tabs;
    },

    restore(state) {
      document.querySelectorAll('[data-preserve-tabs]').forEach((container, index) => {
        const containerId = container.id || `tabs-${index}`;
        const tabInfo = state[containerId];
        if (!tabInfo) return;

        // Try to find and activate the tab
        let tab = null;
        if (tabInfo.id) tab = container.querySelector(`#${tabInfo.id}`);
        if (!tab && tabInfo.href) tab = container.querySelector(`[href="${tabInfo.href}"]`);
        if (!tab && tabInfo.selector) tab = container.querySelector(tabInfo.selector);

        if (tab && tab.click) {
          tab.click();
        }
      });
    },

    getSelector(element) {
      if (element.id) return `#${element.id}`;
      const classes = Array.from(element.classList).filter(c => c !== 'active' && !c.startsWith('is-'));
      if (classes.length > 0) return `.${classes.join('.')}`;
      return element.tagName.toLowerCase();
    }
  });

  /**
   * Search/Filter Provider
   */
  StateManager.register('filters', {
    capture() {
      const filters = {};
      
      // Capture search inputs
      document.querySelectorAll('[data-preserve-search], input[type="search"]').forEach((input, index) => {
        const id = input.id || input.name || `search-${index}`;
        filters[id] = {
          value: input.value,
          type: 'search'
        };
      });

      // Capture filter dropdowns/selects
      document.querySelectorAll('[data-preserve-filter]').forEach((filter, index) => {
        const id = filter.id || filter.name || `filter-${index}`;
        filters[id] = {
          value: filter.value || filter.getAttribute('data-filter-value'),
          type: 'filter',
          tag: filter.tagName.toLowerCase()
        };
      });

      // Capture active filter chips/badges
      const activeFilters = [];
      document.querySelectorAll('[data-active-filter]').forEach(chip => {
        activeFilters.push({
          key: chip.getAttribute('data-filter-key'),
          value: chip.getAttribute('data-filter-value'),
          text: chip.textContent.trim()
        });
      });
      if (activeFilters.length > 0) {
        filters._activeChips = activeFilters;
      }

      return filters;
    },

    restore(state) {
      // Restore search inputs
      for (const [id, data] of Object.entries(state)) {
        if (id === '_activeChips') continue;

        const element = document.getElementById(id) || 
                       document.querySelector(`[name="${id}"]`) ||
                       document.querySelector(`[data-preserve-search][data-id="${id}"]`);
        
        if (element && data.value) {
          element.value = data.value;
          element.dispatchEvent(new Event('input', { bubbles: true }));
          element.dispatchEvent(new Event('change', { bubbles: true }));
        }
      }

      // Restore active filter chips (if any custom handler exists)
      if (state._activeChips && window.restoreActiveFilters) {
        window.restoreActiveFilters(state._activeChips);
      }
    }
  });

  /**
   * Section/Module Specific Provider (for Sales, Cobros, Maintenance, etc.)
   */
  StateManager.register('module', {
    capture() {
      const moduleState = {
        controller: document.body.getAttribute('data-controller'),
        action: document.body.getAttribute('data-action')
      };

      // Capture module-specific state (if registered)
      const controller = moduleState.controller;
      if (controller && window.ModuleStateCapture && window.ModuleStateCapture[controller]) {
        try {
          moduleState.custom = window.ModuleStateCapture[controller]();
        } catch (error) {
          console.error(`StateManager: Error capturing custom state for ${controller}`, error);
        }
      }

      return moduleState;
    },

    restore(state) {
      if (!state) return;

      // Restore module-specific state (if registered)
      const controller = state.controller;
      if (controller && state.custom && window.ModuleStateRestore && window.ModuleStateRestore[controller]) {
        try {
          window.ModuleStateRestore[controller](state.custom);
        } catch (error) {
          console.error(`StateManager: Error restoring custom state for ${controller}`, error);
        }
      }
    }
  });

  // ────────────────────────────────────────────────────────────
  // Integration with Navigation System
  // ────────────────────────────────────────────────────────────

  /**
   * Auto-capture state before navigation
   */
  function captureOnNavigation() {
    StateManager.saveToHistory();
  }

  /**
   * Auto-restore state after navigation
   */
  function restoreOnNavigation() {
    StateManager.loadFromHistory();
  }

  // Capture state before unload
  window.addEventListener('beforeunload', captureOnNavigation);

  // Capture state periodically for AJAX navigation
  setInterval(() => {
    if (document.hasFocus()) {
      captureOnNavigation();
    }
  }, 2000);

  // Restore state on popstate (back/forward button)
  window.addEventListener('popstate', function() {
    setTimeout(restoreOnNavigation, 150);
  });

  // Capture state after AJAX navigation
  if (window.jQuery) {
    $(document).on('ajaxSuccess', function(event, xhr, settings) {
      if (settings.type === 'GET') {
        setTimeout(captureOnNavigation, 300);
      }
    });
  }

  // Capture state when clicking ajax links
  document.addEventListener('click', function(e) {
    const ajaxLink = e.target.closest('[data-ajax-link="true"]');
    if (ajaxLink) {
      captureOnNavigation();
    }
  });

  // Initial load - restore if available
  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', restoreOnNavigation);
  } else {
    restoreOnNavigation();
  }

  // ────────────────────────────────────────────────────────────
  // Public API
  // ────────────────────────────────────────────────────────────

  window.StateManager = StateManager;

  // Helper for modules to register their custom state capture/restore
  window.ModuleStateCapture = {};
  window.ModuleStateRestore = {};

})(window);
