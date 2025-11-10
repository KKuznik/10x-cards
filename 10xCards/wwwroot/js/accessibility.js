// Accessibility utilities for improved WCAG 2.0 compliance

/**
 * Focus on the first element with validation errors
 * @param {string} formSelector - CSS selector for the form container
 */
window.focusFirstError = function (formSelector) {
    try {
        const form = document.querySelector(formSelector);
        if (!form) {
            console.warn('Form not found:', formSelector);
            return;
        }

        // Find first element with aria-invalid="true"
        const firstInvalidElement = form.querySelector('[aria-invalid="true"]');
        if (firstInvalidElement) {
            firstInvalidElement.focus();
            // Scroll into view if needed
            firstInvalidElement.scrollIntoView({ behavior: 'smooth', block: 'center' });
        }
    } catch (error) {
        console.error('Error focusing first error:', error);
    }
};

/**
 * Set focus on a specific element by ID
 * @param {string} elementId - ID of the element to focus
 */
window.setFocusById = function (elementId) {
    try {
        const element = document.getElementById(elementId);
        if (element) {
            element.focus();
            element.scrollIntoView({ behavior: 'smooth', block: 'center' });
        } else {
            console.warn('Element not found:', elementId);
        }
    } catch (error) {
        console.error('Error setting focus:', error);
    }
};

/**
 * Announce a message to screen readers using aria-live
 * @param {string} message - Message to announce
 * @param {string} priority - 'polite' or 'assertive'
 */
window.announceToScreenReader = function (message, priority = 'polite') {
    try {
        // Create or get announcement container
        let announcer = document.getElementById('aria-announcer');
        if (!announcer) {
            announcer = document.createElement('div');
            announcer.id = 'aria-announcer';
            announcer.className = 'visually-hidden';
            announcer.setAttribute('role', 'status');
            announcer.setAttribute('aria-live', priority);
            announcer.setAttribute('aria-atomic', 'true');
            document.body.appendChild(announcer);
        }

        // Update priority if different
        announcer.setAttribute('aria-live', priority);

        // Clear and set new message
        announcer.textContent = '';
        setTimeout(() => {
            announcer.textContent = message;
        }, 100);
    } catch (error) {
        console.error('Error announcing to screen reader:', error);
    }
};

