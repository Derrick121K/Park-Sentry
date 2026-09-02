/**
 * ParkSentry browser camera module.
 * Manages getUserMedia lifecycle — no image persistence.
 */
window.ParkSentryCamera = (function () {
    let stream = null;
    let videoElement = null;
    let currentFacingMode = 'environment';

    function isSecureContext() {
        return window.isSecureContext === true;
    }

    function hasMediaDevices() {
        return !!(navigator.mediaDevices && navigator.mediaDevices.getUserMedia);
    }

    function getUserFriendlyError(err) {
        const name = err?.name || '';
        if (name === 'NotAllowedError' || name === 'PermissionDeniedError') {
            return 'Camera permission was denied. Please allow camera access in your browser settings or enter the registration manually.';
        }
        if (name === 'NotFoundError' || name === 'DevicesNotFoundError') {
            return 'No camera was found on this device. Please enter the registration manually.';
        }
        if (name === 'NotReadableError' || name === 'TrackStartError') {
            return 'The camera is unavailable or in use by another application. Please try again or enter the registration manually.';
        }
        if (name === 'OverconstrainedError') {
            return 'The requested camera is not available. Try switching cameras or enter the registration manually.';
        }
        if (!isSecureContext()) {
            return 'Camera access requires a secure connection (HTTPS). Please enter the registration manually.';
        }
        if (!hasMediaDevices()) {
            return 'Camera access is not supported in this browser. Please enter the registration manually.';
        }
        return 'Unable to access the camera. Please try again or enter the registration manually.';
    }

    async function enumerateCameras() {
        if (!hasMediaDevices()) return [];
        try {
            const devices = await navigator.mediaDevices.enumerateDevices();
            return devices.filter(d => d.kind === 'videoinput').map(d => ({
                deviceId: d.deviceId,
                label: d.label || 'Camera'
            }));
        } catch {
            return [];
        }
    }

    async function start(videoEl, facingMode) {
        await stop();
        if (!isSecureContext()) {
            throw new Error(getUserFriendlyError({ name: 'SecurityError' }));
        }
        if (!hasMediaDevices()) {
            throw new Error(getUserFriendlyError({ name: 'NotSupportedError' }));
        }

        currentFacingMode = facingMode || currentFacingMode;
        videoElement = videoEl;

        const constraints = {
            video: {
                facingMode: currentFacingMode,
                width: { ideal: 1280 },
                height: { ideal: 720 }
            },
            audio: false
        };

        try {
            stream = await navigator.mediaDevices.getUserMedia(constraints);
            videoElement.srcObject = stream;
            await videoElement.play();
            return { success: true, facingMode: currentFacingMode };
        } catch (err) {
            await stop();
            throw new Error(getUserFriendlyError(err));
        }
    }

    async function switchCamera() {
        currentFacingMode = currentFacingMode === 'environment' ? 'user' : 'environment';
        if (videoElement) {
            await start(videoElement, currentFacingMode);
        }
        return { facingMode: currentFacingMode };
    }

    async function stop() {
        if (stream) {
            stream.getTracks().forEach(track => track.stop());
            stream = null;
        }
        if (videoElement) {
            videoElement.srcObject = null;
        }
    }

    /**
     * Demo capture — does not perform OCR.
     * Returns failure so the app can prompt manual entry.
     */
    function captureDemo() {
        return {
            success: false,
            registrationNumber: null,
            rawDetectedText: null,
            confidence: null,
            errorMessage: 'DEMO SCANNER: Licence disc OCR is not available yet. Please enter the registration manually.',
            provider: 'DEMO / BROWSER CAMERA'
        };
    }

    function cleanup() {
        return stop();
    }

    // Stop camera when navigating away
    window.addEventListener('pagehide', () => { stop(); });
    document.addEventListener('visibilitychange', () => {
        if (document.visibilityState === 'hidden') {
            stop();
        }
    });

    return {
        isSecureContext,
        hasMediaDevices,
        enumerateCameras,
        start,
        switchCamera,
        stop,
        captureDemo,
        cleanup
    };
})();
