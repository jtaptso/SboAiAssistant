// Scroll an element to its bottom — called after each new message renders
window.scrollToBottom = (elementId) => {
    const el = document.getElementById(elementId);
    if (el) el.scrollTop = el.scrollHeight;
};
