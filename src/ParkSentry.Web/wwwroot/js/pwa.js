/**
 * ParkSentry PWA registration and offline detection.
 */
window.ParkSentryPwa = (function () {
    let onlineHandler = null;
    let offlineHandler = null;

    function registerServiceWorker() {
        if ('serviceWorker' in navigator) {
            return navigator.serviceWorker.register('/service-worker.js', { scope: '/' })
                .catch(() => { /* registration optional in dev */ });
        }
        return Promise.resolve(null);
    }

    function isOnline() {
        return navigator.onLine;
    }

    function subscribeOnlineStatus(dotNetRef) {
        onlineHandler = () => { dotNetRef.invokeMethodAsync('OnOnline'); };
        offlineHandler = () => { dotNetRef.invokeMethodAsync('OnOffline'); };
        window.addEventListener('online', onlineHandler);
        window.addEventListener('offline', offlineHandler);
    }

    function unsubscribeOnlineStatus() {
        if (onlineHandler) window.removeEventListener('online', onlineHandler);
        if (offlineHandler) window.removeEventListener('offline', offlineHandler);
        onlineHandler = null;
        offlineHandler = null;
    }

    return {
        registerServiceWorker,
        isOnline,
        subscribeOnlineStatus,
        unsubscribeOnlineStatus
    };
})();
