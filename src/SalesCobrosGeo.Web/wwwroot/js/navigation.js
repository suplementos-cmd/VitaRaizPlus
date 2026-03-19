/**
 * Navigation Controls Enhancement
 * Manages browser history navigation buttons state (← →)
 * Architecture: Global navigation system for entire application
 * Tracks AJAX partial view navigation and section changes
 */

(function () {
  'use strict';

  // Navigation state tracking
  let navigationStack = [];
  let currentIndex = -1;

  /**
   * Update button states based on navigation stack
   */
  function updateNavigationButtons() {
    const backButtons = document.querySelectorAll('.app-nav-back');
    const forwardButtons = document.querySelectorAll('.app-nav-forward');

    // Can go back if we have previous entries
    const canGoBack = currentIndex > 0;
    backButtons.forEach(btn => {
      btn.disabled = !canGoBack;
      btn.setAttribute('aria-disabled', !canGoBack);
      btn.style.display = canGoBack ? '' : 'none';
    });

    // Can go forward if we're not at the end
    const canGoForward = currentIndex < navigationStack.length - 1;
    forwardButtons.forEach(btn => {
      btn.disabled = !canGoForward;
      btn.setAttribute('aria-disabled', !canGoForward);
      btn.style.display = canGoForward ? '' : 'none';
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
   * Enhanced back button with stack navigation
   */
  window.addEventListener('click', function (e) {
    const backBtn = e.target.closest('.app-nav-back');
    const forwardBtn = e.target.closest('.app-nav-forward');

    if (backBtn && !backBtn.disabled) {
      e.preventDefault();
      if (currentIndex > 0) {
        currentIndex--;
        const entry = navigationStack[currentIndex];
        history.back();
      }
      updateNavigationButtons();
    } else if (forwardBtn && !forwardBtn.disabled) {
      e.preventDefault();
      if (currentIndex < navigationStack.length - 1) {
        currentIndex++;
        const entry = navigationStack[currentIndex];
        history.forward();
      }
      updateNavigationButtons();
    }
  });

  /**
   * Initialize navigation system
   */
  function init() {
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
  }

  // Initialize when DOM is ready
  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', init);
  } else {
    init();
  }

  // Periodic update as fallback (reduced frequency)
  setInterval(updateNavigationButtons, 2000);
})();
