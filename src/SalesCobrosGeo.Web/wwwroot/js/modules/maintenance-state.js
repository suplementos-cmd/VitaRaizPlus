/**
 * Maintenance Module - State Capture & Restore
 * Preserves section, viewId, editId, and create state
 */

(function() {
  'use strict';

  // Wait for StateManager to be available
  function init() {
    if (!window.StateManager) {
      setTimeout(init, 100);
      return;
    }

    /**
     * Capture Maintenance-specific state
     */
    window.ModuleStateCapture.Maintenance = function() {
      const state = {
        section: null,
        viewId: null,
        editId: null,
        create: false,
        selectedCard: null,
        activeTab: null
      };

      // Capture from URL params
      const urlParams = new URLSearchParams(window.location.search);
      state.section = urlParams.get('section') || 'catalogos';
      state.viewId = urlParams.get('viewId');
      state.editId = urlParams.get('editId');
      state.create = urlParams.get('create') === 'true';

      // Capture selected card (desktop)
      const selectedCard = document.querySelector('.maint-d-card.selected');
      if (selectedCard) {
        state.selectedCard = {
          id: selectedCard.getAttribute('data-item-id'),
          index: Array.from(selectedCard.parentElement.children).indexOf(selectedCard)
        };
      }

      // Capture active desktop nav tab (if present in old versions)
      const activeTab = document.querySelector('.maint-d-tab.active');
      if (activeTab) {
        state.activeTab = activeTab.getAttribute('href') || 
                         activeTab.getAttribute('data-section');
      }

      // Capture quick section navigator in editor (if visible)
      const activeQuickSec = document.querySelector('.maint-d-quick-sec.active');
      if (activeQuickSec) {
        state.quickSection = activeQuickSec.getAttribute('data-section') ||
                            activeQuickSec.getAttribute('href');
      }

      return state;
    };

    /**
     * Restore Maintenance-specific state
     */
    window.ModuleStateRestore.Maintenance = function(state) {
      if (!state) return;

      // Note: URL params are already restored by browser navigation
      // We just need to restore UI state

      // Restore selected card highlight (desktop)
      if (state.selectedCard) {
        setTimeout(() => {
          const cards = document.querySelectorAll('.maint-d-card');
          const card = Array.from(cards).find(c => 
            c.getAttribute('data-item-id') === state.selectedCard.id
          );
          if (card) {
            card.classList.add('selected');
          } else if (cards[state.selectedCard.index]) {
            cards[state.selectedCard.index].classList.add('selected');
          }
        }, 200);
      }

      // Restore active section in quick navigator
      if (state.quickSection) {
        setTimeout(() => {
          const quickSec = document.querySelector(`.maint-d-quick-sec[data-section="${state.quickSection}"], .maint-d-quick-sec[href*="${state.quickSection}"]`);
          if (quickSec) {
            document.querySelectorAll('.maint-d-quick-sec').forEach(s => s.classList.remove('active'));
            quickSec.classList.add('active');
          }
        }, 200);
      }
    };

    // Mark containers for state preservation
    function markContainers() {
      // Mark main content area for scroll preservation
      const mainContent = document.querySelector('.maint-d-panel, .maint-m');
      if (mainContent) {
        mainContent.setAttribute('data-preserve-scroll', 'true');
      }

      // Mark cards container
      const cardsContainer = document.querySelector('.maint-d-cards, .maint-m-list');
      if (cardsContainer) {
        cardsContainer.setAttribute('data-preserve-scroll', 'true');
      }

      // Mark aside for scroll
      const aside = document.querySelector('.maint-d-detail');
      if (aside) {
        aside.setAttribute('data-preserve-scroll', 'true');
      }
    }

    // Initialize on page load
    if (document.readyState === 'loading') {
      document.addEventListener('DOMContentLoaded', markContainers);
    } else {
      markContainers();
    }

    // Re-mark after AJAX navigation
    if (window.jQuery) {
      $(document).on('ajaxSuccess', function(event, xhr, settings) {
        if (settings.url && settings.url.indexOf('/Maintenance') !== -1) {
          setTimeout(markContainers, 100);
        }
      });
    }
  }

  init();
})();
