// Google reCAPTCHA v2 Integration for Blazor
// Uses dynamic container replacement to force fresh widget on expiration

window.recaptchaInterop = {
    dotNetRef: null,
    widgetId: null,
    expirationTimer: null,
    wrapperElementId: null,
    currentSiteKey: null,
    renderCount: 0,

    // Initialize reCAPTCHA
    init: function (dotNetReference, elementId, siteKey) {
        console.log('[reCAPTCHA] Initializing...', { elementId, siteKey });
        this.dotNetRef = dotNetReference;
        this.wrapperElementId = elementId;
        this.currentSiteKey = siteKey;
        
        // Wait for grecaptcha to be ready
        if (typeof grecaptcha === 'undefined' || !grecaptcha.render) {
            console.log('[reCAPTCHA] Waiting for grecaptcha to load...');
            setTimeout(() => this.init(dotNetReference, elementId, siteKey), 100);
            return;
        }

        this.createAndRenderWidget();
    },

    // Create a new inner container and render widget to it
    createAndRenderWidget: function() {
        try {
            const wrapper = document.getElementById(this.wrapperElementId);
            if (!wrapper) {
                console.error('[reCAPTCHA] Wrapper element not found:', this.wrapperElementId);
                return;
            }

            // Clear wrapper and create new inner container with unique ID
            wrapper.innerHTML = '';
            this.renderCount++;
            const innerId = this.wrapperElementId + '-inner-' + this.renderCount;
            
            const innerDiv = document.createElement('div');
            innerDiv.id = innerId;
            wrapper.appendChild(innerDiv);

            // Render to the new inner container
            this.widgetId = grecaptcha.render(innerId, {
                'sitekey': this.currentSiteKey,
                'callback': this.onSuccess.bind(this),
                'expired-callback': this.onExpired.bind(this),
                'error-callback': this.onError.bind(this),
                'theme': 'light',
                'size': 'normal'
            });
            
            console.log('[reCAPTCHA] Widget rendered. ID:', this.widgetId, 'Container:', innerId);
        } catch (error) {
            console.error('[reCAPTCHA] Render error:', error);
        }
    },

    // Called when user successfully completes the reCAPTCHA
    onSuccess: function (response) {
        console.log('[reCAPTCHA] Success callback triggered');
        if (this.dotNetRef) {
            try {
                this.dotNetRef.invokeMethod('OnRecaptchaSuccess', response);
                
                // Start a manual expiration timer (2 minutes) for test environments
                this.clearExpirationTimer();
                this.expirationTimer = setTimeout(() => {
                    console.log('[reCAPTCHA] Manual expiration timer triggered (2 min)');
                    this.onExpired();
                }, 120000);
            } catch (error) {
                console.error('[reCAPTCHA] Error calling OnRecaptchaSuccess:', error);
            }
        }
    },

    // Called when the reCAPTCHA expires
    onExpired: function () {
        console.log('[reCAPTCHA] Expired callback triggered');
        this.clearExpirationTimer();
        
        // Create a fresh widget (destroys old one including the error message)
        this.createAndRenderWidget();
        
        // Notify Blazor component
        if (this.dotNetRef) {
            try {
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
        
        // Create a fresh widget
        this.createAndRenderWidget();
        
        if (this.dotNetRef) {
            try {
                this.dotNetRef.invokeMethod('OnRecaptchaError');
            } catch (error) {
                console.error('[reCAPTCHA] Error calling OnRecaptchaError:', error);
            }
        }
    },

    // Clear expiration timer
    clearExpirationTimer: function () {
        if (this.expirationTimer) {
            clearTimeout(this.expirationTimer);
            this.expirationTimer = null;
        }
    },

    // Reset the reCAPTCHA (called from Blazor)
    reset: function () {
        console.log('[reCAPTCHA] Reset called from Blazor');
        this.clearExpirationTimer();
        this.createAndRenderWidget();
    },

    // Get the response token
    getResponse: function () {
        if (this.widgetId !== null && typeof grecaptcha !== 'undefined') {
            try {
                const response = grecaptcha.getResponse(this.widgetId);
                console.log('[reCAPTCHA] Response:', response ? 'Valid' : 'Empty');
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
        this.dotNetRef = null;
        this.widgetId = null;
        this.wrapperElementId = null;
        this.currentSiteKey = null;
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

console.log('[reCAPTCHA] Interop loaded. Test with: window.triggerRecaptchaExpiration()');
