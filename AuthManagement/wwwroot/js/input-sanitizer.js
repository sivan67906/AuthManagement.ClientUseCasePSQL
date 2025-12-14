// COMPREHENSIVE INPUT SANITIZATION - PREVENTS TYPING INVALID CHARACTERS
// Blocks emojis, special characters, HTML/script injection BEFORE they appear
// NOTE: Password fields are EXCLUDED - they must allow ALL characters

(function () {
    'use strict';

    // Validation patterns for different input types
    const PATTERNS = {
        email: /^[a-zA-Z0-9@._-]*$/,
        name: /^[a-zA-Z0-9\s'._-]*$/,
        alphanumeric: /^[a-zA-Z0-9]*$/,
        description: /^[a-zA-Z0-9\s.,!?;:()'"\-\n\r]*$/,
        general: /^[a-zA-Z0-9\s.,!?;:()'"\-]*$/,
        url: /^[a-zA-Z0-9_./:?=&#%-]*$/
    };

    // Check if element is a password field
    function isPasswordField(element) {
        if (!element) return false;
        return element.type === 'password' ||
            element.dataset.inputType === 'password' ||
            element.classList.contains('password-input') ||
            element.id?.toLowerCase().includes('password') ||
            element.name?.toLowerCase().includes('password');
    }

    // Sanitize a single input element
    function sanitizeInput(element) {
        if (!element || element.dataset.sanitized === 'true') {
            return;
        }

        // NEVER sanitize password fields - they must allow ALL characters
        if (isPasswordField(element)) {
            element.dataset.sanitized = 'skipped-password';
            return;
        }

        // Determine pattern
        let patternType = 'general';
        if (element.type === 'email' || element.dataset.inputType === 'email') {
            patternType = 'email';
        } else if (element.dataset.inputType === 'name') {
            patternType = 'name';
        } else if (element.dataset.inputType === 'alphanumeric') {
            patternType = 'alphanumeric';
        } else if (element.dataset.inputType === 'url') {
            patternType = 'url';
        } else if (element.dataset.inputType === 'description' || element.tagName.toLowerCase() === 'textarea') {
            patternType = 'description';
        }

        const pattern = PATTERNS[patternType];

        // PREVENT invalid keystrokes
        element.addEventListener('keypress', function (e) {
            // Allow control keys
            if (e.ctrlKey || e.metaKey || e.altKey) return;

            // Allow special keys (Enter, Backspace, Tab, Arrow keys, etc.)
            if (e.key.length > 1) return;

            // Get the character that would be entered
            const char = e.key || String.fromCharCode(e.which || e.keyCode);

            // Test against pattern
            if (!pattern.test(char)) {
                e.preventDefault();
                e.stopPropagation();
                return false;
            }
        }, true);

        // CLEAN pasted content
        element.addEventListener('paste', function (e) {
            e.preventDefault();
            const pastedText = (e.clipboardData || window.clipboardData).getData('text');

            // Clean the pasted text
            let cleanedText = pastedText
                // Remove emojis
                .replace(/[\u{1F600}-\u{1F64F}]/gu, '')
                .replace(/[\u{1F300}-\u{1F5FF}]/gu, '')
                .replace(/[\u{1F680}-\u{1F6FF}]/gu, '')
                .replace(/[\u{2600}-\u{26FF}]/gu, '')
                .replace(/[\u{2700}-\u{27BF}]/gu, '')
                // Remove HTML
                .replace(/<[^>]*>/g, '')
                .replace(/&lt;/g, '')
                .replace(/&gt;/g, '');

            // Filter characters
            cleanedText = cleanedText.split('').filter(char => pattern.test(char)).join('');

            // Insert at cursor
            const start = this.selectionStart;
            const end = this.selectionEnd;
            this.value = this.value.substring(0, start) + cleanedText + this.value.substring(end);
            this.setSelectionRange(start + cleanedText.length, start + cleanedText.length);

            // Trigger events
            this.dispatchEvent(new Event('input', { bubbles: true }));
            this.dispatchEvent(new Event('change', { bubbles: true }));
        }, true);

        element.dataset.sanitized = 'true';
    }

    // Add clear button to search inputs
    function addClearButton(searchInput) {
        if (!searchInput || searchInput.dataset.clearButton === 'true') {
            return;
        }

        const wrapper = document.createElement('div');
        wrapper.style.cssText = 'position: relative; display: inline-block; width: 100%;';
        searchInput.parentNode.insertBefore(wrapper, searchInput);
        wrapper.appendChild(searchInput);

        const clearBtn = document.createElement('button');
        clearBtn.type = 'button';
        clearBtn.innerHTML = '✖';
        clearBtn.style.cssText = `
            position: absolute;
            right: 10px;
            top: 50%;
            transform: translateY(-50%);
            background: transparent;
            border: none;
            color: #999;
            font-size: 16px;
            cursor: pointer;
            padding: 5px 8px;
            display: ${searchInput.value ? 'block' : 'none'};
            z-index: 10;
        `;

        clearBtn.onclick = () => {
            searchInput.value = '';
            searchInput.dispatchEvent(new Event('input', { bubbles: true }));
            searchInput.dispatchEvent(new Event('change', { bubbles: true }));
            clearBtn.style.display = 'none';
        };

        searchInput.addEventListener('input', () => {
            clearBtn.style.display = searchInput.value ? 'block' : 'none';
        });

        searchInput.style.paddingRight = '35px';
        wrapper.appendChild(clearBtn);
        searchInput.dataset.clearButton = 'true';
    }

    // Initialize
    function initializeAllInputs() {
        // Get all text inputs and textareas, but EXCLUDE password fields
        document.querySelectorAll('input[type="text"]:not([data-sanitized])').forEach(input => {
            if (!isPasswordField(input)) {
                sanitizeInput(input);
            }
        });
        document.querySelectorAll('input[type="email"]:not([data-sanitized])').forEach(input => {
            if (!isPasswordField(input)) {
                sanitizeInput(input);
            }
        });
        document.querySelectorAll('textarea:not([data-sanitized])').forEach(input => {
            sanitizeInput(input);
        });

        // Explicitly skip password fields
        document.querySelectorAll('input[type="password"]').forEach(input => {
            input.dataset.sanitized = 'skipped-password';
        });
    }

    function initializeAllSearchBoxes() {
        document.querySelectorAll('input[type="text"][placeholder*="earch"]:not([data-clear-button="true"])').forEach(addClearButton);
    }

    // Export
    window.InputSanitizer = {
        initializeAllInputs,
        initializeAllSearchBoxes,
        addClearButton,
        sanitizeInput
    };

    // Auto-initialize
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', () => {
            initializeAllInputs();
            initializeAllSearchBoxes();
        });
    } else {
        initializeAllInputs();
        initializeAllSearchBoxes();
    }

    // Periodic re-initialization for Blazor
    setInterval(() => {
        initializeAllInputs();
        initializeAllSearchBoxes();
    }, 500);

})();
