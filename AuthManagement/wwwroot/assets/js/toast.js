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

    toast.innerHTML = `
        <div class="toast-icon" style="color:${t.color}">
            <i class="bi ${t.icon}"></i>
        </div>
        <div class="toast-content">
            <div class="toast-message">${message}</div>
        </div>
        <button class="toast-close">
            <i class="bi bi-x"></i>
        </button>
    `;

    document.body.appendChild(toast);

    // remove on close click
    toast.querySelector(".toast-close").onclick = () => toast.remove();

    // auto remove
    setTimeout(() => toast.remove(), 5000);
};
