window.showToast = function (type, message) {

    const types = {
        success: { color: "#00b894", icon: "bi-check-circle" },
        error: { color: "#e74c3c", icon: "bi-exclamation-octagon" },
        warning: { color: "#f1c40f", icon: "bi-exclamation-triangle" },
        info: { color: "#0984e3", icon: "bi-info-circle" }
    };

    let t = types[type] || types.info;

    let toast = document.createElement("div");
    toast.className = "custom-toast shadow-sm";
    toast.style.borderLeft = `6px solid ${t.color}`;

    // Check if message contains HTML (bullet list)
    const isHtmlContent = message.includes('<ul') || message.includes('<li');

    toast.innerHTML = `
        <div class="toast-icon" style="color:${t.color}">
            <i class="bi ${t.icon}"></i>
        </div>
        <div class="toast-content">
            <div class="toast-message${isHtmlContent ? ' toast-message-list' : ''}">${message}</div>
        </div>
        <button class="toast-close">
            <i class="bi bi-x"></i>
        </button>
    `;

    document.body.appendChild(toast);

    // remove on close click
    toast.querySelector(".toast-close").onclick = () => toast.remove();

    // auto remove - give more time for multiple errors
    const timeout = isHtmlContent ? 8000 : 5000;
    setTimeout(() => toast.remove(), timeout);
};

// Helper function to format multiple errors as bullet points
window.formatErrorsAsBullets = function (errors) {
    if (!errors || errors.length === 0) return '';
    if (errors.length === 1) return errors[0];
    
    let html = '<ul class="toast-error-list">';
    errors.forEach(error => {
        html += `<li>${error}</li>`;
    });
    html += '</ul>';
    return html;
};
