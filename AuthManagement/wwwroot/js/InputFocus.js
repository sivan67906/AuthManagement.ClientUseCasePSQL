window.focusInputById = (id) => {
    setTimeout(() => {
        const el = document.getElementById(id);
        if (el && !el.disabled && el.offsetParent !== null) {
            el.focus();
        }
    }, 150);
};
