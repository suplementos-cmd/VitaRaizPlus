/**
 * Cobros Module - State Management
 * 
 * Captures and restores Cobros module state including:
 * - Active profile (cobrador/ruta)
 * - View type (CollectorHome, CollectorQueue, CollectorRoute, SupervisorDashboard)
 * - Filter parameters (day, zone, status, groupBy, filter)
 * - Date range filters (from, to)
 * - Selected client/payment
 * - Scroll position and expanded groups
 */

(function() {
  'use strict';

  function init() {
    if (!window.StateManager) {
      setTimeout(init, 100);
      return;
    }

    // ────────────────────────────────────────────────────────────
    // CAPTURE Cobros State
    // ────────────────────────────────────────────────────────────
    window.ModuleStateCapture.Cobros = function() {
      const urlParams = new URLSearchParams(window.location.search);
      const state = {
        // URL parameters
        profile: urlParams.get('profile'),
        groupBy: urlParams.get('groupBy'),
        filter: urlParams.get('filter'),
        day: urlParams.get('day'),
        status: urlParams.get('status'),
        zone: urlParams.get('zone'),
        from: urlParams.get('from'),
        to: urlParams.get('to'),
        outcome: urlParams.get('outcome'),
        
        // Current action/view
        action: getActionFromPath(),
        
        // UI state
        selectedClient: getSelectedClient(),
        expandedGroups: captureExpandedGroups(),
        activeTab: getActiveTab(),
        
        // Filter chips/badges
        activeFilters: captureActiveFilters()
      };

      return state;
    };

    // ────────────────────────────────────────────────────────────
    // RESTORE Cobros State
    // ────────────────────────────────────────────────────────────
    window.ModuleStateRestore.Cobros = function(state) {
      if (!state) return;

      // For complex Cobros views, we might need to reconstruct the URL
      const currentAction = getActionFromPath();
      
      // If the action changed, we may need to navigate
      if (state.action && state.action !== 'Index' && state.action !== currentAction) {
        navigateToAction(state);
        return;
      }

      // Restore active tab
      if (state.activeTab) {
        setTimeout(() => {
          restoreActiveTab(state.activeTab);
        }, 150);
      }

      // Restore expanded groups
      if (state.expandedGroups && state.expandedGroups.length > 0) {
        setTimeout(() => {
          restoreExpandedGroups(state.expandedGroups);
        }, 200);
      }

      // Restore selected client
      if (state.selectedClient) {
        setTimeout(() => {
          highlightClient(state.selectedClient);
        }, 300);
      }

      // Restore active filters
      if (state.activeFilters && state.activeFilters.length > 0) {
        setTimeout(() => {
          restoreActiveFilters(state.activeFilters);
        }, 250);
      }
    };

    // ────────────────────────────────────────────────────────────
    // Helper Functions
    // ────────────────────────────────────────────────────────────

    function getActionFromPath() {
      const path = window.location.pathname.toLowerCase();
      if (path.includes('/collectorhome')) return 'CollectorHome';
      if (path.includes('/collectorqueue')) return 'CollectorQueue';
      if (path.includes('/collectorroute')) return 'CollectorRoute';
      if (path.includes('/supervisordashboard')) return 'SupervisorDashboard';
      if (path.includes('/supervisormonitor')) return 'SupervisorMonitor';
      if (path.includes('/collectionhistory')) return 'CollectionHistory';
      if (path.includes('/register')) return 'Register';
      return 'Index';
    }

    function navigateToAction(state) {
      const urlParams = new URLSearchParams();
      
      // Add all relevant params
      if (state.profile) urlParams.set('profile', state.profile);
      if (state.groupBy) urlParams.set('groupBy', state.groupBy);
      if (state.filter) urlParams.set('filter', state.filter);
      if (state.day) urlParams.set('day', state.day);
      if (state.status) urlParams.set('status', state.status);
      if (state.zone) urlParams.set('zone', state.zone);
      if (state.from) urlParams.set('from', state.from);
      if (state.to) urlParams.set('to', state.to);
      if (state.outcome) urlParams.set('outcome', state.outcome);

      const baseUrl = window.location.origin + '/Cobros';
      const queryString = urlParams.toString();
      const targetUrl = `${baseUrl}/${state.action}${queryString ? '?' + queryString : ''}`;
      
      window.location.href = targetUrl;
    }

    function getSelectedClient() {
      const selected = document.querySelector('.client-card.selected, .client-row.active, .cobro-card.selected');
      if (selected) {
        return selected.getAttribute('data-client-id') || 
               selected.getAttribute('data-cobro-id') ||
               selected.getAttribute('data-id');
      }
      return null;
    }

    function highlightClient(clientId) {
      const client = document.querySelector(`[data-client-id="${clientId}"]`) ||
                    document.querySelector(`[data-cobro-id="${clientId}"]`) ||
                    document.querySelector(`[data-id="${clientId}"]`);
      if (client) {
        client.classList.add('selected', 'active');
        client.scrollIntoView({ behavior: 'smooth', block: 'center' });
      }
    }

    function captureExpandedGroups() {
      const expanded = [];
      document.querySelectorAll('.group-card.expanded, .day-group.show, .status-group.show, .zone-group.show').forEach(group => {
        const groupId = group.getAttribute('data-group-id') || 
                       group.getAttribute('id') ||
                       group.getAttribute('data-day') ||
                       group.getAttribute('data-status') ||
                       group.getAttribute('data-zone');
        if (groupId) {
          expanded.push(groupId);
        }
      });
      return expanded;
    }

    function restoreExpandedGroups(groupIds) {
      groupIds.forEach(groupId => {
        const group = document.querySelector(`[data-group-id="${groupId}"]`) ||
                     document.getElementById(groupId) ||
                     document.querySelector(`[data-day="${groupId}"]`) ||
                     document.querySelector(`[data-status="${groupId}"]`) ||
                     document.querySelector(`[data-zone="${groupId}"]`);
        if (group) {
          group.classList.add('expanded', 'show');
          const toggle = group.querySelector('[data-bs-toggle="collapse"]');
          if (toggle && toggle.classList.contains('collapsed')) {
            toggle.click?.();
          }
        }
      });
    }

    function getActiveTab() {
      const activeTab = document.querySelector('.nav-tabs .nav-link.active, .day-tabs .tab.active');
      if (activeTab) {
        return {
          id: activeTab.id,
          href: activeTab.getAttribute('href'),
          dataDay: activeTab.getAttribute('data-day'),
          dataFilter: activeTab.getAttribute('data-filter')
        };
      }
      return null;
    }

    function restoreActiveTab(tabInfo) {
      let tab = null;
      if (tabInfo.id) tab = document.getElementById(tabInfo.id);
      if (!tab && tabInfo.dataDay) tab = document.querySelector(`[data-day="${tabInfo.dataDay}"]`);
      if (!tab && tabInfo.dataFilter) tab = document.querySelector(`[data-filter="${tabInfo.dataFilter}"]`);
      if (!tab && tabInfo.href) tab = document.querySelector(`[href="${tabInfo.href}"]`);
      
      if (tab && tab.click) {
        tab.click();
      }
    }

    function captureActiveFilters() {
      const filters = [];
      document.querySelectorAll('.filter-chip.active, .quick-filter.active').forEach(chip => {
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
        '.cobros-content',
        '.collector-queue',
        '.collector-route',
        '.client-list',
        '.cobros-list'
      ];
      
      containers.forEach(selector => {
        const container = document.querySelector(selector);
        if (container) {
          container.setAttribute('data-preserve-scroll', 'true');
        }
      });

      // Mark filter forms
      document.querySelectorAll('[data-cobros-filter-form], .filter-form').forEach(form => {
        form.setAttribute('data-preserve-form', 'true');
      });

      // Mark search inputs
      document.querySelectorAll('.cobros-search, input[name="search"]').forEach(input => {
        input.setAttribute('data-preserve-search', 'true');
      });

      // Mark day tabs
      const dayTabs = document.querySelector('.day-tabs, .nav-tabs');
      if (dayTabs) {
        dayTabs.setAttribute('data-preserve-tabs', 'true');
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
        if (settings.url && settings.url.toLowerCase().indexOf('/cobros') !== -1) {
          setTimeout(markContainers, 100);
        }
      });
    }
  }

  init();
})();
