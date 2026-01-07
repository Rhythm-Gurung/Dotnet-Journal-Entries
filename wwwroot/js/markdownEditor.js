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
