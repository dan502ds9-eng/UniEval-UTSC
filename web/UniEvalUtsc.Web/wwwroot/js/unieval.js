(() => {
    const focusableSelector = [
        "a[href]",
        "button:not([disabled])",
        "input:not([disabled])",
        "select:not([disabled])",
        "textarea:not([disabled])",
        "summary",
        "[tabindex]:not([tabindex='-1'])"
    ].join(",");
    const focusTraps = new WeakMap();
    let dialogOpener = null;

    function focusables(container) {
        return Array.from(container.querySelectorAll(focusableSelector))
            .filter(element => element.getClientRects().length > 0);
    }

    window.unieval = {
        trapFocus(container, dotNetReference) {
            dialogOpener = document.activeElement instanceof HTMLElement
                ? document.activeElement
                : null;
            const state = {
                dotNetReference,
                onKeyDown(event) {
                    if (event.key === "Escape") {
                        event.preventDefault();
                        event.stopPropagation();
                        void dotNetReference.invokeMethodAsync("CerrarPorEscape");
                        return;
                    }

                    if (event.key !== "Tab") {
                        return;
                    }

                    const items = focusables(container);
                    if (items.length === 0) {
                        event.preventDefault();
                        container.focus();
                        return;
                    }

                    const first = items[0];
                    const last = items[items.length - 1];
                    if (event.shiftKey && (document.activeElement === first || document.activeElement === container)) {
                        event.preventDefault();
                        last.focus();
                    } else if (!event.shiftKey && document.activeElement === last) {
                        event.preventDefault();
                        first.focus();
                    }
                }
            };

            focusTraps.set(container, state);
            document.addEventListener("keydown", state.onKeyDown, true);
            const firstFocusable = focusables(container)[0];
            (firstFocusable ?? container).focus();
        },

        releaseFocus(container) {
            const state = focusTraps.get(container);
            if (state) {
                document.removeEventListener("keydown", state.onKeyDown, true);
                focusTraps.delete(container);
            }

            if (dialogOpener?.isConnected) {
                dialogOpener.focus();
            }

            dialogOpener = null;
        },

        focusElement(element) {
            element?.focus();
        },

        speak(text) {
            if (!("speechSynthesis" in window)) {
                return false;
            }

            window.speechSynthesis.cancel();
            const utterance = new SpeechSynthesisUtterance(text);
            utterance.lang = "es-MX";
            utterance.rate = 0.95;
            const voces = window.speechSynthesis.getVoices();
            utterance.voice = voces.find(voz => voz.lang.toLowerCase() === "es-mx")
                ?? voces.find(voz => voz.lang.toLowerCase().startsWith("es-"))
                ?? null;
            window.speechSynthesis.speak(utterance);
            return true;
        },

        cancelSpeech() {
            if ("speechSynthesis" in window) {
                window.speechSynthesis.cancel();
            }
        },

        getLocalStorage(key) {
            try {
                return window.localStorage.getItem(key);
            } catch {
                return null;
            }
        },

        setLocalStorage(key, value) {
            try {
                window.localStorage.setItem(key, value);
                return true;
            } catch {
                return false;
            }
        }
    };
})();
