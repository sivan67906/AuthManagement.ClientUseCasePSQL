// Input Validation and Sanitization Helper
window.InputValidation = {
    // Issue #2 FIX: Corrected emoji pattern - only match actual emojis, not regular characters
    emojiPattern: /[\uD83C-\uDBFF][\uDC00-\uDFFF]|[\u2600-\u26FF]|[\u2700-\u27BF]/g,
    
    // Filter input based on type
    filterInput: function (element, inputType) {
        if (!element) return;

        const value = element.value;
        let filtered = value;

        switch (inputType) {
            case 'email':
                // Allow only valid email characters
                filtered = value.replace(/[^a-zA-Z0-9._%+-@]/g, '');
                break;

            case 'name':
                // Allow letters, numbers, spaces, hyphens, underscores, dots
                filtered = value.replace(/[^a-zA-Z0-9\s\-_.]/g, '');
                // Remove emojis
                filtered = filtered.replace(this.emojiPattern, '');
                break;

            case 'alphanumeric':
                // Allow only letters, numbers, and spaces
                filtered = value.replace(/[^a-zA-Z0-9\s]/g, '');
                break;
            
            case 'url':
                // Allow URL characters including /
                filtered = value.replace(/[^a-zA-Z0-9\-_./:\?=&]/g, '');
                break;

            case 'description':
                // Remove HTML, scripts, and emojis but allow basic punctuation
                filtered = value.replace(/<[^>]+>/g, '')
                    .replace(/<script[^>]*>.*?<\/script>/gi, '')
                    .replace(this.emojiPattern, '')
                    .replace(/[<>]/g, '');
                break;

            case 'general':
            default:
                // Remove HTML, scripts, and emojis
                filtered = value.replace(/<[^>]+>/g, '')
                    .replace(/<script[^>]*>.*?<\/script>/gi, '')
                    .replace(this.emojiPattern, '')
                    .replace(/[<>"'&;\\`|${}[\]^~]/g, '');
                break;
        }

        if (filtered !== value) {
            element.value = filtered;
            // Trigger change event
            element.dispatchEvent(new Event('change', { bubbles: true }));
        }
    },

    // Attach input filter to element
    attachInputFilter: function (elementId, inputType) {
        const element = document.getElementById(elementId);
        if (element) {
            element.addEventListener('input', function () {
                window.InputValidation.filterInput(this, inputType);
            });
        }
    },

    // Issue #3 FIX: Removed - Search icon handling now done entirely in Blazor
    // The addSearchClearButton function is no longer used

    // Truncate text with ellipsis
    truncateText: function (text, maxLength) {
        if (!text || text.length <= maxLength) return text;
        return text.substring(0, maxLength) + '...';
    }
};

// Disable button to prevent double submission
window.disableButtonOnSubmit = function (buttonElement) {
    if (buttonElement) {
        buttonElement.disabled = true;
    }
};

// Re-enable button
window.enableButton = function (buttonElement) {
    if (buttonElement) {
        buttonElement.disabled = false;
    }
};
