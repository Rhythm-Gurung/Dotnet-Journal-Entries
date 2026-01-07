// Lightweight contenteditable helpers for rich-text toggles

export function setHtml(el, html) {
    if (!el) return;
    el.innerHTML = html ?? '';
}

export function getHtml(el) {
    if (!el) return '';
    return el.innerHTML ?? '';
}

export function exec(el, command, value) {
    if (!el) return;
    el.focus();
    document.execCommand(command, false, value ?? null);
}

// Check if a specific format is currently active at the cursor position
export function queryCommandState(command) {
    return document.queryCommandState(command);
}

// Check which block format (h1, h2, p, etc.) is active
export function queryBlockFormat() {
    const selection = window.getSelection();
    if (!selection || selection.rangeCount === 0) return 'p';
    
    let node = selection.anchorNode;
    while (node && node.nodeType !== 1) {
        node = node.parentNode;
    }
    
    while (node) {
        const tagName = node.tagName?.toLowerCase();
        if (['h1', 'h2', 'h3', 'p', 'ul', 'ol'].includes(tagName)) {
            return tagName;
        }
        node = node.parentNode;
    }
    return 'p';
}
