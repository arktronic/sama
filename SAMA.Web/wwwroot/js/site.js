// Time zone utilities using Luxon
(function() {
    'use strict';

    // Check if Luxon is available
    if (typeof luxon === 'undefined' || !luxon.DateTime) {
        console.warn('Luxon library not loaded. Timestamps will use server-side formatting.');
        return;
    }

    const DateTime = luxon.DateTime;

    /**
     * Format a timestamp to local time with a common pattern
     * @param {string} isoTimestamp - ISO 8601 timestamp string
     * @param {string} format - Format pattern (short, long, or custom Luxon format)
     * @returns {string} Formatted timestamp
     */
    function formatTimestamp(isoTimestamp, format) {
        if (!isoTimestamp) {
            return '';
        }

        try {
            const dt = DateTime.fromISO(isoTimestamp);
            
            if (!dt.isValid) {
                console.warn('Invalid timestamp:', isoTimestamp);
                return isoTimestamp;
            }

            // Common format patterns
            switch (format) {
                case 'short':
                    // "MMM dd, h:mm a" → "Jan 12, 3:45 PM"
                    return dt.toFormat('MMM dd, h:mm a');
                
                case 'long':
                    // "MMMM dd, yyyy 'at' h:mm a" → "January 12, 2025 at 3:45 PM"
                    return dt.toFormat('MMMM dd, yyyy \'at\' h:mm a');
                
                default:
                    // Custom Luxon format string
                    return dt.toFormat(format);
            }
        } catch (error) {
            console.error('Error formatting timestamp:', error);
            return isoTimestamp;
        }
    }

    /**
     * Initialize all timestamps on the page
     * Looks for elements with data-timestamp attribute
     */
    function initializeTimestamps() {
        const timestampElements = document.querySelectorAll('[data-timestamp]');
        
        timestampElements.forEach(function(element) {
            const isoTimestamp = element.getAttribute('data-timestamp');
            const format = element.getAttribute('data-format') || 'long';
            
            if (isoTimestamp) {
                const formatted = formatTimestamp(isoTimestamp, format);
                if (formatted) {
                    element.textContent = formatted;
                    
                    // Add hover tooltip with full ISO timestamp
                    try {
                        const dt = DateTime.fromISO(isoTimestamp);
                        if (dt.isValid) {
                            element.title = dt.toLocaleString(DateTime.DATETIME_FULL);
                        }
                    } catch (error) {
                        console.error('Error creating tooltip:', error);
                    }
                }
            }
        });
    }

    // Initialize on DOM ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initializeTimestamps);
    } else {
        initializeTimestamps();
    }

    // Re-initialize after HTMX swaps (for dynamic content)
    if (typeof htmx !== 'undefined') {
        document.body.addEventListener('htmx:afterSwap', initializeTimestamps);
    }

    // Expose for manual use if needed
    window.SAMA = window.SAMA || {};
    window.SAMA.formatTimestamp = formatTimestamp;
})();
