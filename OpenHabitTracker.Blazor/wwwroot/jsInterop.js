// This is a JavaScript module that is loaded on demand.
// It can export any number of functions, and may import other JavaScript modules if required.

export function showPrompt(message) {
  return prompt(message, 'Type anything here');
}

export function setMode(mode) {
    document.documentElement.setAttribute('data-bs-theme', mode);
};

export function setTheme(theme) {
    document.getElementById("theme-link").href = `_content/OpenHabitTracker.Blazor/bootstrap/${theme}/bootstrap.min.css`;
}

export function focusElement(element) {
    element.focus();
}

export function setElementProperty(element, property, value) {
    element[property] = value;
}

export function getWindowDimensions() {
    return {
        width: window.innerWidth,
        height: window.innerHeight
    };
}

export function getElementDimensions(element) {
    return {
        width: element.clientWidth,
        height: element.clientHeight
    };
}

export function saveAsFile(filename, bytesBase64) {
    var link = document.createElement('a');
    link.download = filename;
    link.href = "data:application/octet-stream;base64," + bytesBase64;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
}

function calculateAutoHeight(e) {
    if (e && e.target) {
        e.target.style.height = 'auto';
        e.target.style.height = this.scrollHeight + 'px';
        e.target.style.overflowY = 'hidden';
    }
}

export function recalculateAutoHeight(textarea) {
    if (textarea) {
        // fire input to trigger autosize in case the text is long
        const inputEvent = new Event('input', { bubbles: false, cancelable: true });
        textarea.dispatchEvent(inputEvent);
    }
}

export function setCalculateAutoHeight(textarea) {
    if (textarea) {
        textarea.oninput = calculateAutoHeight;

        // fire input immediatelly to trigger autosize in case the text is long
        const inputEvent = new Event('input', { bubbles: false, cancelable: true });
        textarea.dispatchEvent(inputEvent);

        textarea.rows = 2;
    }
}

export function handleTabKey(textarea) {
    if (textarea && !textarea.tabKeyHandlerAdded) {
        textarea.addEventListener('keydown', function (event) {
            if (event.key === 'Tab') {
                event.preventDefault();

                const start = textarea.selectionStart;
                const end = textarea.selectionEnd;

                const newValue = textarea.value.substring(0, start) + '\t' + textarea.value.substring(end);
                textarea.value = newValue;

                textarea.setSelectionRange(start + 1, start + 1);

                const inputEvent = new Event('input', { bubbles: true, cancelable: true });
                textarea.dispatchEvent(inputEvent);

                textarea.tabKeyPressed = true;
            }
        });

        // Add blur event listener to trigger change event when the textarea loses focus
        textarea.addEventListener('blur', function () {
            if (textarea.tabKeyPressed) {
                textarea.tabKeyPressed = false;
                const changeEvent = new Event('change', { bubbles: true, cancelable: true });
                textarea.dispatchEvent(changeEvent);
            }
        });

        textarea.tabKeyHandlerAdded = true;
    }
}