// Google reCAPTCHA v2 Integration for Blazor
// Replace the siteKey with your actual reCAPTCHA site key from https://www.google.com/recaptcha/admin

window.recaptchaInterop = {
    dotNetRef: null,
    widgetId: null,
    expirationTimer: null,
    errorOverlayWatcher: null,
    mutationObserver: null,

    // Initialize reCAPTCHA
    init: function (dotNetReference, elementId, siteKey) {
        console.log('[reCAPTCHA] Initializing...', { elementId, siteKey });
        this.dotNetRef = dotNetReference;
        
        // Wait for grecaptcha to be ready
        if (typeof grecaptcha === 'undefined' || !grecaptcha.render) {
            console.log('[reCAPTCHA] Waiting for grecaptcha to load...');
            setTimeout(() => this.init(dotNetReference, elementId, siteKey), 100);
            return;
        }

        try {
            const element = document.getElementById(elementId);
            if (!element) {
                console.error('[reCAPTCHA] Element not found:', elementId);
                return;
            }

            // Clear any existing widget
            element.innerHTML = '';

            this.widgetId = grecaptcha.render(elementId, {
                'sitekey': siteKey,
                'callback': this.onSuccess.bind(this),
                'expired-callback': this.onExpired.bind(this),
                'error-callback': this.onError.bind(this),
                'theme': 'light',
                'size': 'normal'
            });
            
            console.log('[reCAPTCHA] Widget rendered successfully. Widget ID:', this.widgetId);
            console.log('[reCAPTCHA] Callbacks registered: success, expired, error');
            
            // Start watching for Google error overlays with both methods
            this.startErrorOverlayWatcher();
            this.startMutationObserver(elementId);
        } catch (error) {
            console.error('[reCAPTCHA] Init error:', error);
        }
    },

    // Start MutationObserver to watch for DOM changes and hide errors immediately
    startMutationObserver: function (elementId) {
        const targetNode = document.getElementById(elementId);
        if (!targetNode) return;

        // Create observer to watch for any DOM changes
        this.mutationObserver = new MutationObserver((mutations) => {
            mutations.forEach((mutation) => {
                mutation.addedNodes.forEach((node) => {
                    if (node.nodeType === Node.ELEMENT_NODE) {
                        // Check if this element or its children contain error messages
                        this.hideErrorElements(node);
                    }
                });
            });
        });

        // Start observing
        this.mutationObserver.observe(targetNode, {
            childList: true,
            subtree: true,
            attributes: false
        });

        console.log('[reCAPTCHA] MutationObserver started');
    },

    // Hide any element that contains Google error messages
    hideErrorElements: function (element) {
        const errorTexts = ['expired', 'Expired', 'Check the checkbox', 'check the box'];
        
        // Check if this element contains error text
        if (element.textContent) {
            const hasError = errorTexts.some(text => element.textContent.includes(text));
            if (hasError) {
                // Hide it aggressively
                element.style.display = 'none';
                element.style.visibility = 'hidden';
                element.style.opacity = '0';
                element.style.position = 'absolute';
                element.style.left = '-9999px';
                element.style.zIndex = '-9999';
                console.log('[reCAPTCHA] Hid error element via MutationObserver');
            }
        }

        // Also check children
        if (element.children) {
            Array.from(element.children).forEach(child => this.hideErrorElements(child));
        }
    },

    // Start watching for and removing Google error overlays
    startErrorOverlayWatcher: function () {
        // Clear any existing watcher
        if (this.errorOverlayWatcher) {
            clearInterval(this.errorOverlayWatcher);
        }
        
        // Check every 100ms for error overlays and remove them
        this.errorOverlayWatcher = setInterval(() => {
            this.removeGoogleErrorOverlays();
        }, 100);
        
        console.log('[reCAPTCHA] Started error overlay watcher');
    },

    // Stop watching for error overlays
    stopErrorOverlayWatcher: function () {
        if (this.errorOverlayWatcher) {
            clearInterval(this.errorOverlayWatcher);
            this.errorOverlayWatcher = null;
            console.log('[reCAPTCHA] Stopped error overlay watcher');
        }
    },

    // Called when user successfully completes the reCAPTCHA
    onSuccess: function (response) {
        console.log('[reCAPTCHA] Success callback triggered');
        if (this.dotNetRef) {
            try {
                // Call synchronous C# method
                this.dotNetRef.invokeMethod('OnRecaptchaSuccess', response);
                
                // Start a manual expiration timer (2 minutes) for test environments
                // Google's test key doesn't always trigger expired-callback reliably
                this.clearExpirationTimer();
                this.expirationTimer = setTimeout(() => {
                    console.log('[reCAPTCHA] Manual expiration timer triggered (2 min)');
                    this.onExpired();
                }, 120000); // 2 minutes
            } catch (error) {
                console.error('[reCAPTCHA] Error calling OnRecaptchaSuccess:', error);
            }
        }
    },

    // Called when the reCAPTCHA expires
    onExpired: function () {
        console.log('[reCAPTCHA] Expired callback triggered');
        this.clearExpirationTimer();
        
        // Forcefully remove any Google error overlays
        this.removeGoogleErrorOverlays();
        
        if (this.dotNetRef) {
            try {
                // Call synchronous C# method
                this.dotNetRef.invokeMethod('OnRecaptchaExpired');
            } catch (error) {
                console.error('[reCAPTCHA] Error calling OnRecaptchaExpired:', error);
            }
        }
    },

    // Called when there's an error
    onError: function () {
        console.log('[reCAPTCHA] Error callback triggered');
        this.clearExpirationTimer();
        
        // Forcefully remove any Google error overlays
        this.removeGoogleErrorOverlays();
        
        if (this.dotNetRef) {
            try {
                // Call synchronous C# method
                this.dotNetRef.invokeMethod('OnRecaptchaError');
            } catch (error) {
                console.error('[reCAPTCHA] Error calling OnRecaptchaError:', error);
            }
        }
    },

    // Forcefully remove Google's error overlays from DOM
    removeGoogleErrorOverlays: function () {
        try {
            // Target all possible Google error message selectors
            const errorSelectors = [
                '.rc-anchor-error-msg-container',
                '.rc-anchor-error-message',
                '.rc-anchor-error-msg',
                '.rc-anchor-alert',
                '[class*="rc-anchor-error"]'
            ];
            
            errorSelectors.forEach(selector => {
                document.querySelectorAll(selector).forEach(el => {
                    // Hide the element completely
                    el.style.display = 'none';
                    el.style.visibility = 'hidden';
                    el.style.opacity = '0';
                    el.style.height = '0';
                    el.style.maxHeight = '0';
                    el.style.overflow = 'hidden';
                    el.style.padding = '0';
                    el.style.margin = '0';
                    el.style.position = 'absolute';
                    el.style.left = '-9999px';
                });
            });
            
            // Also hide any absolutely positioned divs within the reCAPTCHA that might be error messages
            const recaptchaContainers = document.querySelectorAll('#recaptcha-register, #recaptcha-forgot-password');
            recaptchaContainers.forEach(container => {
                const absPositionedDivs = container.querySelectorAll('div[style*="position: absolute"]');
                absPositionedDivs.forEach(el => {
                    if (el.textContent && (
                        el.textContent.includes('expired') || 
                        el.textContent.includes('Expired') ||
                        el.textContent.includes('Check the checkbox') ||
                        el.textContent.includes('check the box')
                    )) {
                        el.style.display = 'none';
                        el.style.visibility = 'hidden';
                        el.style.opacity = '0';
                        el.style.height = '0';
                        el.style.overflow = 'hidden';
                    }
                });
            });
        } catch (error) {
            // Silently fail - don't log too much noise
        }
    },

    // Clear expiration timer
    clearExpirationTimer: function () {
        if (this.expirationTimer) {
            clearTimeout(this.expirationTimer);
            this.expirationTimer = null;
        }
    },

    // Reset the reCAPTCHA
    reset: function () {
        console.log('[reCAPTCHA] Reset called');
        this.clearExpirationTimer();
        if (this.widgetId !== null && typeof grecaptcha !== 'undefined') {
            try {
                grecaptcha.reset(this.widgetId);
                console.log('[reCAPTCHA] Widget reset successfully');
            } catch (error) {
                console.error('[reCAPTCHA] Reset error:', error);
            }
        }
    },

    // Get the response token
    getResponse: function () {
        if (this.widgetId !== null && typeof grecaptcha !== 'undefined') {
            try {
                const response = grecaptcha.getResponse(this.widgetId);
                console.log('[reCAPTCHA] Response retrieved:', response ? 'Valid token' : 'Empty');
                return response;
            } catch (error) {
                console.error('[reCAPTCHA] GetResponse error:', error);
                return '';
            }
        }
        return '';
    },

    // Manual trigger for testing expiration
    triggerExpiration: function () {
        console.log('[reCAPTCHA] Manual expiration trigger');
        this.onExpired();
    },

    // Dispose
    dispose: function () {
        console.log('[reCAPTCHA] Disposing');
        this.clearExpirationTimer();
        this.stopErrorOverlayWatcher();
        
        // Disconnect MutationObserver
        if (this.mutationObserver) {
            this.mutationObserver.disconnect();
            this.mutationObserver = null;
            console.log('[reCAPTCHA] MutationObserver disconnected');
        }
        
        this.dotNetRef = null;
        this.widgetId = null;
    }
};

// Expose functions globally for Blazor interop
window.initRecaptcha = function (dotNetRef, elementId, siteKey) {
    window.recaptchaInterop.init(dotNetRef, elementId, siteKey);
};

window.resetRecaptcha = function () {
    window.recaptchaInterop.reset();
};

window.getRecaptchaResponse = function () {
    return window.recaptchaInterop.getResponse();
};

window.triggerRecaptchaExpiration = function () {
    window.recaptchaInterop.triggerExpiration();
};

window.disposeRecaptcha = function () {
    window.recaptchaInterop.dispose();
};

// Add helper for testing in console
console.log('[reCAPTCHA] Interop loaded. Test expiration with: window.triggerRecaptchaExpiration()');
