/**
 * Administration Module - State Management
 * 
 * Captures and restores Administration module state including:
 * - Active view (Users, Audit, AuditDetail)
 * - Selected user or audit entry
 * - Filter parameters
 * - Pagination state
 * - Sort order
 */

(function() {
  'use strict';

  function init() {
    if (!window.StateManager) {
      setTimeout(init, 100);
      return;
    }

    // ────────────────────────────────────────────────────────────
    // CAPTURE Administration State
    // ────────────────────────────────────────────────────────────
    window.ModuleStateCapture.Administration = function() {
      const urlParams = new URLSearchParams(window.location.search);
      const state = {
        // URL parameters
        userId: urlParams.get('userId'),
        auditId: urlParams.get('id'),
        page: urlParams.get('page'),
        pageSize: urlParams.get('pageSize'),
        sortBy: urlParams.get('sortBy'),
        sortOrder: urlParams.get('sortOrder'),
        from: urlParams.get('from'),
        to: urlParams.get('to'),
        action: urlParams.get('action'),
        entity: urlParams.get('entity'),
        userName: urlParams.get('userName'),
        
        // Current action/view
        view: getActionFromPath(),
        
        // UI state
        selectedRow: getSelectedRow(),
        expandedDetails: captureExpandedDetails(),
        
        // Filter values
        activeFilters: captureActiveFilters()
      };

      return state;
    };

    // ────────────────────────────────────────────────────────────
    // RESTORE Administration State
    // ────────────────────────────────────────────────────────────
    window.ModuleStateRestore.Administration = function(state) {
      if (!state) return;

      // If the view changed and we need to navigate
      const currentView = getActionFromPath();
      if (state.view && state.view !== currentView) {
        navigateToView(state);
        return;
      }

      // Restore selected row
      if (state.selectedRow) {
        setTimeout(() => {
          highlightRow(state.selectedRow);
        }, 200);
      }

      // Restore expanded details
      if (state.expandedDetails && state.expandedDetails.length > 0) {
        setTimeout(() => {
          restoreExpandedDetails(state.expandedDetails);
        }, 250);
      }

      // Restore active filters
      if (state.activeFilters && state.activeFilters.length > 0) {
        setTimeout(() => {
          restoreActiveFilters(state.activeFilters);
        }, 300);
      }
    };

    // ────────────────────────────────────────────────────────────
    // Helper Functions
    // ────────────────────────────────────────────────────────────

    function getActionFromPath() {
      const path = window.location.pathname.toLowerCase();
      if (path.includes('/users')) return 'Users';
      if (path.includes('/auditdetail')) return 'AuditDetail';
      if (path.includes('/audit')) return 'Audit';
      return 'Index';
    }

    function navigateToView(state) {
      const urlParams = new URLSearchParams();
      
      // Add all relevant params
      if (state.userId) urlParams.set('userId', state.userId);
      if (state.auditId) urlParams.set('id', state.auditId);
      if (state.page) urlParams.set('page', state.page);
      if (state.pageSize) urlParams.set('pageSize', state.pageSize);
      if (state.sortBy) urlParams.set('sortBy', state.sortBy);
      if (state.sortOrder) urlParams.set('sortOrder', state.sortOrder);
      if (state.from) urlParams.set('from', state.from);
      if (state.to) urlParams.set('to', state.to);
      if (state.action) urlParams.set('action', state.action);
      if (state.entity) urlParams.set('entity', state.entity);
      if (state.userName) urlParams.set('userName', state.userName);

      const baseUrl = window.location.origin + '/Administration';
      const queryString = urlParams.toString();
      const targetUrl = `${baseUrl}/${state.view}${queryString ? '?' + queryString : ''}`;
      
      window.location.href = targetUrl;
    }

    function getSelectedRow() {
      const selected = document.querySelector('.user-row.selected, .audit-row.selected, tr.active');
      if (selected) {
        return selected.getAttribute('data-user-id') || 
               selected.getAttribute('data-audit-id') ||
               selected.getAttribute('data-id');
      }
      return null;
    }

    function highlightRow(rowId) {
      const row = document.querySelector(`[data-user-id="${rowId}"]`) ||
                 document.querySelector(`[data-audit-id="${rowId}"]`) ||
                 document.querySelector(`[data-id="${rowId}"]`);
      if (row) {
        row.classList.add('selected', 'active');
        row.scrollIntoView({ behavior: 'smooth', block: 'center' });
      }
    }

    function captureExpandedDetails() {
      const expanded = [];
      document.querySelectorAll('.detail-row.show, .expanded-row.show').forEach(detail => {
        const detailId = detail.getAttribute('data-detail-id') || 
                        detail.getAttribute('id');
        if (detailId) {
          expanded.push(detailId);
        }
      });
      return expanded;
    }

    function restoreExpandedDetails(detailIds) {
      detailIds.forEach(detailId => {
        const detail = document.querySelector(`[data-detail-id="${detailId}"]`) ||
                      document.getElementById(detailId);
        if (detail) {
          detail.classList.add('show');
          const toggle = detail.previousElementSibling?.querySelector('[data-bs-toggle]');
          if (toggle && toggle.classList.contains('collapsed')) {
            toggle.click?.();
          }
        }
      });
    }

    function captureActiveFilters() {
      const filters = [];
      document.querySelectorAll('.filter-chip.active, .admin-filter.active').forEach(chip => {
        filters.push({
          key: chip.getAttribute('data-filter-key') || chip.getAttribute('data-filter'),
          value: chip.getAttribute('data-filter-value') || chip.textContent.trim()
        });
      });
      return filters;
    }

    function restoreActiveFilters(filters) {
      filters.forEach(filter => {
        const chip = document.querySelector(`[data-filter-key="${filter.key}"]`) ||
                    document.querySelector(`[data-filter="${filter.key}"]`);
        if (chip) {
          chip.classList.add('active');
        }
      });
    }

    // ────────────────────────────────────────────────────────────
    // Mark Containers for Auto-Preservation
    // ────────────────────────────────────────────────────────────

    function markContainers() {
      // Mark main content for scroll preservation
      const containers = [
        '.admin-content',
        '.users-list',
        '.audit-list',
        '.table-container'
      ];
      
      containers.forEach(selector => {
        const container = document.querySelector(selector);
        if (container) {
          container.setAttribute('data-preserve-scroll', 'true');
        }
      });

      // Mark filter forms
      document.querySelectorAll('[data-admin-filter-form], .filter-form').forEach(form => {
        form.setAttribute('data-preserve-form', 'true');
      });

      // Mark search inputs
      document.querySelectorAll('.admin-search, input[name="search"]').forEach(input => {
        input.setAttribute('data-preserve-search', 'true');
      });

      // Mark tabs
      const tabs = document.querySelector('.admin-tabs, .nav-tabs');
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
        if (settings.url && settings.url.toLowerCase().indexOf('/administration') !== -1) {
          setTimeout(markContainers, 100);
        }
      });
    }
  }

  init();
})();
