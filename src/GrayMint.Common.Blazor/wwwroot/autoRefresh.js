// AutoRefresh asks it before each refresh
export function isPageHidden() {
    return document.hidden;
}

// tells AutoRefresh each time the page is shown again, and for how long it was hidden; dispose() stops it
export function watchPageShown(component) {
    let hiddenTime = document.hidden ? Date.now() : null;
    const listener = () => {
        if (document.hidden) {
            hiddenTime = Date.now();
        } else if (hiddenTime !== null) {
            component.invokeMethodAsync("OnPageShown", Date.now() - hiddenTime);
            hiddenTime = null;
        }
    };

    document.addEventListener("visibilitychange", listener);
    return { dispose: () => document.removeEventListener("visibilitychange", listener) };
}
