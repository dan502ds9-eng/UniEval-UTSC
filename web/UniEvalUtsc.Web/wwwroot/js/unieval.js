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
    const enterHandlers = new WeakMap();
    let dialogOpener = null;

    function focusables(container) {
        return Array.from(container.querySelectorAll(focusableSelector))
            .filter(element => element.getClientRects().length > 0);
    }

    function setTextScale(scale) {
        document.documentElement.dataset.ts = scale;
        document.documentElement.style.fontSize = `${scale}%`;
        window.unieval.setLocalStorage("unieval-text-scale", scale);
    }

    window.unieval = {
        initializePreferences() {
            const storedTheme = window.unieval.getLocalStorage("unieval-theme");
            const theme = storedTheme ?? (window.matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light");
            const storedScale = window.unieval.getLocalStorage("unieval-text-scale");
            const scale = ["100", "125", "150", "200"].includes(storedScale) ? storedScale : "100";
            document.documentElement.dataset.theme = theme;
            setTextScale(scale);
            return { theme, scale };
        },

        setTheme(theme) {
            document.documentElement.dataset.theme = theme;
            window.unieval.setLocalStorage("unieval-theme", theme);
        },

        setTextScale,

        bindEnterAdvance(element, nextElement) {
            const previousHandler = enterHandlers.get(element);
            if (previousHandler) {
                element.removeEventListener("keydown", previousHandler);
            }

            const handler = event => {
                if (event.key !== "Enter") {
                    return;
                }

                event.preventDefault();
                if (nextElement) {
                    nextElement.focus();
                } else {
                    element.form?.requestSubmit();
                }
            };
            element.addEventListener("keydown", handler);
            enterHandlers.set(element, handler);
        },

        bindMenu(trigger, menu, dotNetReference, openMethod, closeMethod) {
            const moveFocus = (index) => {
                const items = Array.from(menu.querySelectorAll("[role='menuitem']:not([disabled]), [role='menuitemradio']:not([disabled])"));
                if (items.length > 0) {
                    items[(index + items.length) % items.length].focus();
                }
            };
            const openAndFocus = (index) => {
                void dotNetReference.invokeMethodAsync(openMethod);
                window.setTimeout(() => moveFocus(index), 50);
            };
            trigger.addEventListener("keydown", event => {
                if (event.key === "ArrowDown" || event.key === "ArrowUp") {
                    event.preventDefault();
                    openAndFocus(event.key === "ArrowUp" ? -1 : 0);
                } else if (event.key === "Enter" || event.key === " ") {
                    event.preventDefault();
                    openAndFocus(0);
                } else if (event.key === "Escape") {
                    event.preventDefault();
                    void dotNetReference.invokeMethodAsync(closeMethod);
                }
            });
            menu.addEventListener("keydown", event => {
                const items = Array.from(menu.querySelectorAll("[role='menuitem']:not([disabled]), [role='menuitemradio']:not([disabled])"));
                const current = items.indexOf(document.activeElement);
                if (event.key === "ArrowDown" || event.key === "ArrowUp") {
                    event.preventDefault();
                    moveFocus(current + (event.key === "ArrowDown" ? 1 : -1));
                } else if (event.key === "Home" || event.key === "End") {
                    event.preventDefault();
                    moveFocus(event.key === "Home" ? 0 : -1);
                } else if (event.key === "Escape") {
                    event.preventDefault();
                    void dotNetReference.invokeMethodAsync(closeMethod);
                    trigger.focus();
                }
            });
        },

        updatePageMetadata() {
            const heading = document.querySelector("main h1, h1");
            if (!heading) {
                return null;
            }

            const title = heading.textContent.trim();
            heading.setAttribute("tabindex", "-1");
            heading.focus();
            document.title = `${title} · UniEval UTSC`;
            return title;
        },

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

        focusById(id) {
            document.getElementById(id)?.focus();
        },

        print() {
            return new Promise(resolve => {
                window.requestAnimationFrame(() => {
                    window.print();
                    resolve();
                });
            });
        },

        downloadFile(fileName, contentBase64, contentType) {
            const binary = atob(contentBase64);
            const bytes = new Uint8Array(binary.length);
            for (let index = 0; index < binary.length; index += 1) {
                bytes[index] = binary.charCodeAt(index);
            }

            const url = URL.createObjectURL(new Blob([bytes], { type: contentType }));
            const link = document.createElement("a");
            link.href = url;
            link.download = fileName;
            document.body.append(link);
            link.click();
            link.remove();
            window.setTimeout(() => URL.revokeObjectURL(url), 1000);
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
        },

        getSessionStorage(key) {
            try {
                return window.sessionStorage.getItem(key);
            } catch (error) {
                console.error("No fue posible leer sessionStorage.", error);
                return null;
            }
        },

        setSessionStorage(key, value) {
            try {
                window.sessionStorage.setItem(key, value);
                return true;
            } catch (error) {
                console.error("No fue posible escribir en sessionStorage.", error);
                return false;
            }
        },

        removeSessionStorage(key) {
            try {
                window.sessionStorage.removeItem(key);
                return true;
            } catch (error) {
                console.error("No fue posible eliminar la sesión de sessionStorage.", error);
                return false;
            }
        }
    };
})();
