/**
 * SAMA Theme Switcher
 * Handles light/dark theme toggling with localStorage persistence
 * and system preference detection
 */

(function() {
  'use strict';

  const THEME_STORAGE_KEY = 'sama-theme';
  const THEME_ATTRIBUTE = 'data-theme';
  const LIGHT = 'light';
  const DARK = 'dark';

  /**
   * Get the current theme from localStorage or system preference
   */
  function getInitialTheme() {
    // Check localStorage first
    const savedTheme = localStorage.getItem(THEME_STORAGE_KEY);
    if (savedTheme === LIGHT || savedTheme === DARK) {
      return savedTheme;
    }

    // Fall back to system preference
    if (window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches) {
      return DARK;
    }

    // Default to light
    return LIGHT;
  }

  /**
   * Apply the theme to the document
   */
  function applyTheme(theme) {
    document.documentElement.setAttribute(THEME_ATTRIBUTE, theme);
    localStorage.setItem(THEME_STORAGE_KEY, theme);

    // Update theme toggle button icon if it exists
    updateThemeToggleIcon(theme);

    // Dispatch custom event for other components
    window.dispatchEvent(new CustomEvent('themechanged', { detail: { theme } }));
  }

  /**
   * Toggle between light and dark themes
   */
  function toggleTheme() {
    const currentTheme = document.documentElement.getAttribute(THEME_ATTRIBUTE);
    const newTheme = currentTheme === DARK ? LIGHT : DARK;
    applyTheme(newTheme);
  }

  /**
   * Update theme toggle button icon
   */
  function updateThemeToggleIcon(theme) {
    const toggleButtons = document.querySelectorAll('[data-theme-toggle]');
    toggleButtons.forEach(button => {
      const lightIcon = button.querySelector('[data-theme-icon="light"]');
      const darkIcon = button.querySelector('[data-theme-icon="dark"]');

      if (lightIcon && darkIcon) {
        if (theme === DARK) {
          lightIcon.style.display = 'inline-block';
          darkIcon.style.display = 'none';
          button.setAttribute('aria-label', 'Switch to light mode');
        } else {
          lightIcon.style.display = 'none';
          darkIcon.style.display = 'inline-block';
          button.setAttribute('aria-label', 'Switch to dark mode');
        }
      }
    });
  }

  /**
   * Listen for system theme changes
   */
  function listenForSystemThemeChanges() {
    if (window.matchMedia) {
      const darkModeQuery = window.matchMedia('(prefers-color-scheme: dark)');
      
      // Modern browsers
      if (darkModeQuery.addEventListener) {
        darkModeQuery.addEventListener('change', (e) => {
          // Only auto-switch if user hasn't manually set a preference
          if (!localStorage.getItem(THEME_STORAGE_KEY)) {
            applyTheme(e.matches ? DARK : LIGHT);
          }
        });
      } 
      // Legacy browsers
      else if (darkModeQuery.addListener) {
        darkModeQuery.addListener((e) => {
          if (!localStorage.getItem(THEME_STORAGE_KEY)) {
            applyTheme(e.matches ? DARK : LIGHT);
          }
        });
      }
    }
  }

  /**
   * Initialize theme on page load
   */
  function initTheme() {
    const initialTheme = getInitialTheme();
    document.documentElement.setAttribute(THEME_ATTRIBUTE, initialTheme);
    localStorage.setItem(THEME_STORAGE_KEY, initialTheme);
    
    // Dispatch custom event for other components
    window.dispatchEvent(new CustomEvent('themechanged', { detail: { theme: initialTheme } }));
    
    listenForSystemThemeChanges();
  }

  /**
   * Set up event listeners for theme toggle buttons
   */
  function setupThemeToggleButtons() {
    const toggleButtons = document.querySelectorAll('[data-theme-toggle]');
    toggleButtons.forEach(button => {
      button.addEventListener('click', toggleTheme);
    });
    
    // Update icons after DOM is ready
    const currentTheme = document.documentElement.getAttribute(THEME_ATTRIBUTE);
    updateThemeToggleIcon(currentTheme);
  }

  // Initialize theme immediately to prevent flash
  initTheme();

  // Set up toggle buttons after DOM is ready
  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', setupThemeToggleButtons);
  } else {
    setupThemeToggleButtons();
  }

  // Expose API for programmatic theme changes
  window.SAMA = window.SAMA || {};
  window.SAMA.theme = {
    get: () => document.documentElement.getAttribute(THEME_ATTRIBUTE),
    set: applyTheme,
    toggle: toggleTheme
  };

})();
