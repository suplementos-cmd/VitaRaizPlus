/**
 * Dashboard Module - State Management
 * 
 * Captures and restores Dashboard module state including:
 * - Date range filters (from, to)
 * - Active view (Index, Sales, Collections)
 * - Grouping/display mode (collectionsBy, seller, zone)
 * - Chart interactions and filters
 * - Active KPI cards or sections
 */

(function() {
  'use strict';

  function init() {
    if (!window.StateManager) {
      setTimeout(init, 100);
      return;
    }

    // ────────────────────────────────────────────────────────────
    // CAPTURE Dashboard State
    // ────────────────────────────────────────────────────────────
    window.ModuleStateCapture.Dashboard = function() {
      const urlParams = new URLSearchParams(window.location.search);
      const state = {
        // URL parameters
        from: urlParams.get('from'),
        to: urlParams.get('to'),
        collectionsBy: urlParams.get('collectionsBy'),
        seller: urlParams.get('seller'),
        zone: urlParams.get('zone'),
        day: urlParams.get('day'),
        value: urlParams.get('value'),
        
        // Current action/view
        action: getActionFromPath(),
        
        // UI state
        expandedCharts: captureExpandedCharts(),
        activeKpiCard: getActiveKpiCard(),
        selectedTimeRange: getSelectedTimeRange(),
        
        // Filter chips/badges
        activeFilters: captureActiveFilters()
      };

      return state;
    };

    // ────────────────────────────────────────────────────────────
    // RESTORE Dashboard State
    // ────────────────────────────────────────────────────────────
    window.ModuleStateRestore.Dashboard = function(state) {
      if (!state) return;

      // If the action changed and we need to navigate
      const currentAction = getActionFromPath();
      if (state.action && state.action !== 'Index' && state.action !== currentAction) {
        navigateToAction(state);
        return;
      }

      // Restore time range selection
      if (state.selectedTimeRange) {
        setTimeout(() => {
          restoreTimeRange(state.selectedTimeRange);
        }, 150);
      }

      // Restore expanded charts
      if (state.expandedCharts && state.expandedCharts.length > 0) {
        setTimeout(() => {
          restoreExpandedCharts(state.expandedCharts);
        }, 200);
      }

      // Restore active KPI card
      if (state.activeKpiCard) {
        setTimeout(() => {
          highlightKpiCard(state.activeKpiCard);
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
      if (path.includes('/sales')) return 'Sales';
      if (path.includes('/collections')) return 'Collections';
      return 'Index';
    }

    function navigateToAction(state) {
      const urlParams = new URLSearchParams();
      
      // Add all relevant params
      if (state.from) urlParams.set('from', state.from);
      if (state.to) urlParams.set('to', state.to);
      if (state.collectionsBy) urlParams.set('collectionsBy', state.collectionsBy);
      if (state.seller) urlParams.set('seller', state.seller);
      if (state.zone) urlParams.set('zone', state.zone);
      if (state.day) urlParams.set('day', state.day);
      if (state.value) urlParams.set('value', state.value);

      const baseUrl = window.location.origin + '/Dashboard';
      const queryString = urlParams.toString();
      const targetUrl = `${baseUrl}/${state.action}${queryString ? '?' + queryString : ''}`;
      
      window.location.href = targetUrl;
    }

    function captureExpandedCharts() {
      const expanded = [];
      document.querySelectorAll('.chart-card.expanded, .chart-container.show').forEach(chart => {
        const chartId = chart.getAttribute('data-chart-id') || 
                       chart.getAttribute('id');
        if (chartId) {
          expanded.push(chartId);
        }
      });
      return expanded;
    }

    function restoreExpandedCharts(chartIds) {
      chartIds.forEach(chartId => {
        const chart = document.querySelector(`[data-chart-id="${chartId}"]`) ||
                     document.getElementById(chartId);
        if (chart) {
          chart.classList.add('expanded', 'show');
        }
      });
    }

    function getActiveKpiCard() {
      const active = document.querySelector('.kpi-card.active, .stat-card.selected');
      if (active) {
        return active.getAttribute('data-kpi-id') || 
               active.getAttribute('id') ||
               active.querySelector('.kpi-title, .stat-title')?.textContent.trim();
      }
      return null;
    }

    function highlightKpiCard(kpiId) {
      const card = document.querySelector(`[data-kpi-id="${kpiId}"]`) ||
                  document.getElementById(kpiId) ||
                  Array.from(document.querySelectorAll('.kpi-card, .stat-card'))
                    .find(c => c.querySelector('.kpi-title, .stat-title')?.textContent.trim() === kpiId);
      if (card) {
        card.classList.add('active', 'selected');
      }
    }

    function getSelectedTimeRange() {
      const rangeBtn = document.querySelector('.time-range-btn.active, .date-filter.active');
      if (rangeBtn) {
        return {
          id: rangeBtn.id,
          range: rangeBtn.getAttribute('data-range'),
          from: rangeBtn.getAttribute('data-from'),
          to: rangeBtn.getAttribute('data-to')
        };
      }
      return null;
    }

    function restoreTimeRange(rangeInfo) {
      let btn = null;
      if (rangeInfo.id) btn = document.getElementById(rangeInfo.id);
      if (!btn && rangeInfo.range) btn = document.querySelector(`[data-range="${rangeInfo.range}"]`);
      
      if (btn && btn.click) {
        btn.click();
      }
    }

    function captureActiveFilters() {
      const filters = [];
      document.querySelectorAll('.filter-chip.active, .dashboard-filter.active').forEach(chip => {
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
        '.dashboard-content',
        '.dashboard-charts',
        '.dashboard-main'
      ];
      
      containers.forEach(selector => {
        const container = document.querySelector(selector);
        if (container) {
          container.setAttribute('data-preserve-scroll', 'true');
        }
      });

      // Mark filter forms
      document.querySelectorAll('[data-dashboard-filter-form], .filter-form').forEach(form => {
        form.setAttribute('data-preserve-form', 'true');
      });

      // Mark date pickers
      document.querySelectorAll('.date-picker, input[type="date"]').forEach(input => {
        input.setAttribute('data-preserve-filter', 'true');
      });

      // Mark tabs
      const tabs = document.querySelector('.dashboard-tabs, .nav-tabs');
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
        if (settings.url && settings.url.toLowerCase().indexOf('/dashboard') !== -1) {
          setTimeout(markContainers, 100);
        }
      });
    }
  }

  init();
})();
