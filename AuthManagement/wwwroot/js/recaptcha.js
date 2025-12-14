// Google reCAPTCHA v2 Integration for Blazor
// Replace the siteKey with your actual reCAPTCHA site key from https://www.google.com/recaptcha/admin

window.recaptchaInterop = {
    dotNetRef: null,
    widgetId: null,

    // Initialize reCAPTCHA
    init: function (dotNetReference, elementId, siteKey) {
        this.dotNetRef = dotNetReference;
        
        // Wait for grecaptcha to be ready
        if (typeof grecaptcha === 'undefined' || !grecaptcha.render) {
            // If grecaptcha is not ready, wait and retry
            setTimeout(() => this.init(dotNetReference, elementId, siteKey), 100);
            return;
        }

        try {
            const element = document.getElementById(elementId);
            if (!element) {
                console.error('reCAPTCHA element not found:', elementId);
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
        } catch (error) {
            console.error('reCAPTCHA init error:', error);
        }
    },

    // Called when user successfully completes the reCAPTCHA
    onSuccess: function (response) {
        if (this.dotNetRef) {
            this.dotNetRef.invokeMethodAsync('OnRecaptchaSuccess', response);
        }
    },

    // Called when the reCAPTCHA expires
    onExpired: function () {
        if (this.dotNetRef) {
            this.dotNetRef.invokeMethodAsync('OnRecaptchaExpired');
        }
    },

    // Called when there's an error
    onError: function () {
        if (this.dotNetRef) {
            this.dotNetRef.invokeMethodAsync('OnRecaptchaError');
        }
    },

    // Reset the reCAPTCHA
    reset: function () {
        if (this.widgetId !== null && typeof grecaptcha !== 'undefined') {
            try {
                grecaptcha.reset(this.widgetId);
            } catch (error) {
                console.error('reCAPTCHA reset error:', error);
            }
        }
    },

    // Get the response token
    getResponse: function () {
        if (this.widgetId !== null && typeof grecaptcha !== 'undefined') {
            try {
                return grecaptcha.getResponse(this.widgetId);
            } catch (error) {
                console.error('reCAPTCHA getResponse error:', error);
                return '';
            }
        }
        return '';
    },

    // Dispose
    dispose: function () {
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

window.disposeRecaptcha = function () {
    window.recaptchaInterop.dispose();
};
