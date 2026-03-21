/**
 * Navigation Controls Enhancement v2.0
 * Manages browser history navigation buttons state (← →)
 * Architecture: Discrete, state-aware navigation system
 * Features: Memory of navigation stack, state restoration, AJAX support
 */

(function () {
  'use strict';

  // Navigation state tracking
  let navigationStack = [];
  let currentIndex = -1;
  let isInitialized = false;

  /**
   * Update button states based on navigation stack
   * Buttons are shown/hidden via CSS based on :disabled state
   */
  function updateNavigationButtons() {
    const backButtons = document.querySelectorAll('.app-nav-back');
    const forwardButtons = document.querySelectorAll('.app-nav-forward');

    // Can go back if we have previous entries in our stack OR browser history
    const canGoBack = currentIndex > 0 || window.history.length > 1;
    backButtons.forEach(btn => {
      btn.disabled = !canGoBack;
      btn.setAttribute('aria-disabled', String(!canGoBack));
    });

    // Can go forward if we're not at the end of our stack
    const canGoForward = currentIndex < navigationStack.length - 1;
    forwardButtons.forEach(btn => {
      btn.disabled = !canGoForward;
      btn.setAttribute('aria-disabled', String(!canGoForward));
    });
  }

  /**
   * Push new navigation entry with complete state
   */
  function pushNavigation(url, title) {
    // Capture current state before pushing
    const appState = window.StateManager ? window.StateManager.captureState() : null;

    // Remove any forward history when pushing new navigation
    if (currentIndex < navigationStack.length - 1) {
      navigationStack = navigationStack.slice(0, currentIndex + 1);
    }

    // Avoid duplicate entries for the same URL
    const lastEntry = navigationStack[navigationStack.length - 1];
    if (lastEntry && lastEntry.url === (url || window.location.href)) {
      updateNavigationButtons();
      return;
    }

    navigationStack.push({
      url: url || window.location.href,
      title: title || document.title,
      timestamp: Date.now(),
      state: appState
    });

    currentIndex = navigationStack.length - 1;

    // Update history state with navigation index AND app state
    const historyState = {
      navIndex: currentIndex,
      appState: appState
    };
    
    if (history.state?.navIndex !== currentIndex) {
      history.replaceState(historyState, '');
    }

    updateNavigationButtons();
  }

  /**
   * Handle popstate (back/forward browser buttons or programmatic navigation)
   */
  window.addEventListener('popstate', function (event) {
    if (event.state && typeof event.state.navIndex === 'number') {
      currentIndex = event.state.navIndex;
    } else {
      // Fallback: try to determine direction
      const currentUrl = window.location.href;
      const stackIndex = navigationStack.findIndex(entry => entry.url === currentUrl);
      if (stackIndex !== -1) {
        currentIndex = stackIndex;
      }
    }
    
    updateNavigationButtons();

    // Restore application state if available
    if (event.state && event.state.appState && window.StateManager) {
      setTimeout(() => {
        window.StateManager.restoreState(event.state.appState);
      }, 150);
    } else if (navigationStack[currentIndex] && navigationStack[currentIndex].state && window.StateManager) {
      setTimeout(() => {
        window.StateManager.restoreState(navigationStack[currentIndex].state);
      }, 150);
    }
  });

  /**
   * Intercept pushState to track navigation
   */
  const originalPushState = history.pushState;
  history.pushState = function (state, title, url) {
    const result = originalPushState.call(this, state, title, url);
    pushNavigation(url?.toString(), title);
    return result;
  };

  /**
   * Intercept replaceState for better tracking
   */
  const originalReplaceState = history.replaceState;
  history.replaceState = function (state, title, url) {
    const result = originalReplaceState.call(this, state, title, url);
    // Update current entry only
    if (navigationStack[currentIndex]) {
      navigationStack[currentIndex].url = url?.toString() || window.location.href;
      navigationStack[currentIndex].title = title || document.title;
    }
    updateNavigationButtons();
    return result;
  };

  /**
   * Track AJAX navigation (data-ajax-link)
   */
  document.addEventListener('click', function (e) {
    const ajaxLink = e.target.closest('[data-ajax-link="true"]');
    if (ajaxLink && ajaxLink.href) {
      // AJAX link clicked - will trigger partial navigation
      setTimeout(() => {
        pushNavigation(ajaxLink.href, ajaxLink.textContent || ajaxLink.getAttribute('aria-label'));
      }, 100);
    }
  });

  /**
   * Track successful AJAX requests (if using jQuery AJAX)
   */
  if (window.jQuery) {
    $(document).on('ajaxSuccess', function(event, xhr, settings) {
      if (settings.url && settings.type === 'GET') {
        setTimeout(updateNavigationButtons, 50);
      }
    });
  }

  /**
   * Enhanced navigation button click handlers
   */
  window.addEventListener('click', function (e) {
    const backBtn = e.target.closest('.app-nav-back');
    const forwardBtn = e.target.closest('.app-nav-forward');

    if (backBtn && !backBtn.disabled) {
      e.preventDefault();
      e.stopPropagation();
      history.back();
    } else if (forwardBtn && !forwardBtn.disabled) {
      e.preventDefault();
      e.stopPropagation();
      history.forward();
    }
  });

  /**
   * Initialize navigation system
   */
  function init() {
    if (isInitialized) return;
    isInitialized = true;

    // Initialize stack with current page
    navigationStack = [{
      url: window.location.href,
      title: document.title,
      timestamp: Date.now()
    }];
    currentIndex = 0;

    // Set history state
    if (!history.state || history.state.navIndex === undefined) {
      history.replaceState({ navIndex: 0 }, '');
    } else {
      currentIndex = history.state.navIndex;
    }

    updateNavigationButtons();

    // Update on page visibility change (handles tab switches)
    document.addEventListener('visibilitychange', function() {
      if (!document.hidden) {
        updateNavigationButtons();
      }
    });

    // Update on focus (handles returning from external navigation)
    window.addEventListener('focus', function() {
      setTimeout(updateNavigationButtons, 100);
    });
  }

  // Initialize when DOM is ready
  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', init);
  } else {
    init();
  }

  // Expose for debugging (optional)
  window.NavigationDebug = {
    getStack: () => navigationStack,
    getCurrentIndex: () => currentIndex,
    refresh: updateNavigationButtons
  };
})();
