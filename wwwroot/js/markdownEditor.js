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

export function queryCommandState(command) {
    try {
        return document.queryCommandState(command);
    } catch (e) {
        return false;
    }
}

export function isHeading(el, level) {
    if (!el) return false;
    const selection = window.getSelection();
    if (!selection || selection.rangeCount === 0) return false;
    
    const range = selection.getRangeAt(0);
    let node = range.commonAncestorContainer;
    
    // Walk up the DOM tree to find a block element
    while (node && node !== el) {
        if (node.nodeType === Node.ELEMENT_NODE) {
            const tag = node.tagName.toLowerCase();
            // Check if this is a heading tag matching the level (h2, h3, etc.)
            if (tag === level) return true;
            // Stop if we hit another block-level element
            if (['h1', 'h2', 'h3', 'h4', 'h5', 'h6', 'p', 'div', 'li'].includes(tag)) {
                return false;
            }
        }
        node = node.parentNode;
    }
    return false;
}

export function isInList(el, listType) {
    if (!el) return false;
    const selection = window.getSelection();
    if (!selection || selection.rangeCount === 0) return false;
    
    const range = selection.getRangeAt(0);
    let node = range.commonAncestorContainer;
    const tagName = listType === 'ul' ? 'UL' : 'OL';
    
    // Walk up the DOM tree to find the list element
    while (node && node !== el) {
        if (node.nodeType === Node.ELEMENT_NODE && node.tagName === tagName) {
            return true;
        }
        node = node.parentNode;
    }
    return false;
}

