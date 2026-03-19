/**
 * Sales Module - State Management
 * 
 * Captures and restores Sales module state including:
 * - Filter parameters (from, to, day, zone, seller)
 * - Selected sale ID (in Details view)
 * - Form state (edit mode)
 * - Scroll position in lists
 * - Active week/day group
 */

(function() {
  'use strict';

  function init() {
    if (!window.StateManager) {
      setTimeout(init, 100);
      return;
    }

    // ────────────────────────────────────────────────────────────
    // CAPTURE Sales State
    // ────────────────────────────────────────────────────────────
    window.ModuleStateCapture.Sales = function() {
      const urlParams = new URLSearchParams(window.location.search);
      const state = {
        // URL parameters
        from: urlParams.get('from'),
        to: urlParams.get('to'),
        day: urlParams.get('day'),
        zone: urlParams.get('zone'),
        seller: urlParams.get('seller'),
        
        // Selected sale (if in Details view)
        saleId: urlParams.get('id'),
        
        // Current action/view
        action: getActionFromPath(),
        
        // UI state
        expandedWeeks: captureExpandedWeeks(),
        selectedRow: getSelectedSaleRow(),
        
        // Filter values (in case they're in forms)
        filterForm: captureFilterForm()
      };

      return state;
    };

    // ────────────────────────────────────────────────────────────
    // RESTORE Sales State
    // ────────────────────────────────────────────────────────────
    window.ModuleStateRestore.Sales = function(state) {
      if (!state) return;

      // If we have a specific action context, handle accordingly
      if (state.action === 'Details' && state.saleId) {
        // If we're not already on the Details page, navigate there
        const currentPath = window.location.pathname.toLowerCase();
        if (!currentPath.includes('/details')) {
          navigateToSaleDetails(state.saleId);
          return;
        }
      }

      // Restore filter form values
      if (state.filterForm) {
        restoreFilterForm(state.filterForm);
      }

      // Restore expanded weeks
      if (state.expandedWeeks && state.expandedWeeks.length > 0) {
        setTimeout(() => {
          restoreExpandedWeeks(state.expandedWeeks);
        }, 200);
      }

      // Restore selected row
      if (state.selectedRow) {
        setTimeout(() => {
          highlightSaleRow(state.selectedRow);
        }, 300);
      }
    };

    // ────────────────────────────────────────────────────────────
    // Helper Functions
    // ────────────────────────────────────────────────────────────

    function getActionFromPath() {
      const path = window.location.pathname.toLowerCase();
      if (path.includes('/details')) return 'Details';
      if (path.includes('/create')) return 'Create';
      if (path.includes('/edit')) return 'Edit';
      if (path.includes('/form')) return 'Form';
      return 'Index';
    }

    function captureExpandedWeeks() {
      const expanded = [];
      document.querySelectorAll('.week-group.expanded, .week-card.show').forEach(week => {
        const weekId = week.getAttribute('data-week-id') || 
                      week.getAttribute('id') ||
                      week.querySelector('[data-week-start]')?.getAttribute('data-week-start');
        if (weekId) {
          expanded.push(weekId);
        }
      });
      return expanded;
    }

    function restoreExpandedWeeks(weekIds) {
      weekIds.forEach(weekId => {
        const week = document.querySelector(`[data-week-id="${weekId}"]`) ||
                    document.getElementById(weekId) ||
                    document.querySelector(`[data-week-start="${weekId}"]`);
        if (week) {
          week.classList.add('expanded', 'show');
          const collapseBtn = week.querySelector('[data-bs-toggle="collapse"]');
          if (collapseBtn && !collapseBtn.classList.contains('collapsed')) {
            collapseBtn.click?.();
          }
        }
      });
    }

    function getSelectedSaleRow() {
      const selected = document.querySelector('.sale-row.selected, .sale-card.active');
      if (selected) {
        return selected.getAttribute('data-sale-id') || 
               selected.getAttribute('data-id');
      }
      return null;
    }

    function highlightSaleRow(saleId) {
      const row = document.querySelector(`[data-sale-id="${saleId}"]`) ||
                 document.querySelector(`[data-id="${saleId}"]`);
      if (row) {
        row.classList.add('selected', 'active');
        row.scrollIntoView({ behavior: 'smooth', block: 'center' });
      }
    }

    function navigateToSaleDetails(saleId) {
      const baseUrl = window.location.origin + window.location.pathname.replace(/\/[^\/]*$/, '');
      window.location.href = `${baseUrl}/Details?id=${saleId}`;
    }

    function captureFilterForm() {
      const form = document.getElementById('salesFilterForm') || 
                  document.querySelector('[data-sales-filter-form]');
      if (!form) return null;

      const formData = {};
      form.querySelectorAll('input, select').forEach(field => {
        const name = field.name || field.id;
        if (name && field.value) {
          formData[name] = field.value;
        }
      });
      return formData;
    }

    function restoreFilterForm(formData) {
      const form = document.getElementById('salesFilterForm') || 
                  document.querySelector('[data-sales-filter-form]');
      if (!form) return;

      for (const [name, value] of Object.entries(formData)) {
        const field = form.querySelector(`[name="${name}"], #${name}`);
        if (field) {
          field.value = value;
          field.dispatchEvent(new Event('change', { bubbles: true }));
        }
      }
    }

    // ────────────────────────────────────────────────────────────
    // Mark Containers for Auto-Preservation
    // ────────────────────────────────────────────────────────────

    function markContainers() {
      // Mark sales list for scroll preservation
      const salesList = document.querySelector('.sales-list, .sales-content, .sales-weeks');
      if (salesList) {
        salesList.setAttribute('data-preserve-scroll', 'true');
      }

      // Mark filter form
      const filterForm = document.getElementById('salesFilterForm') || 
                        document.querySelector('[data-sales-filter-form]');
      if (filterForm) {
        filterForm.setAttribute('data-preserve-form', 'true');
      }

      // Mark search inputs
      document.querySelectorAll('.sales-search, input[name="search"]').forEach(input => {
        input.setAttribute('data-preserve-search', 'true');
      });

      // Mark tabs if any
      const tabs = document.querySelector('.sales-tabs, .nav-tabs');
      if (tabs) {
        tabs.setAttribute('data-preserve-tabs', 'true');
      }
    }

    // Initialize on DOM ready
    if (document.readyState === 'loading') {
      document.addEventListener('DOMContentLoaded', markContainers);
    } else {
      markContainers();
    }

    // Re-mark after AJAX updates
    if (window.jQuery) {
      $(document).on('ajaxSuccess', function(event, xhr, settings) {
        if (settings.url && settings.url.toLowerCase().indexOf('/sales') !== -1) {
          setTimeout(markContainers, 100);
        }
      });
    }
  }

  init();
})();
