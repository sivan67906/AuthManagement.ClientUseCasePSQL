window.getClipboardText = async function () {
    return await navigator.clipboard.readText();
};
