// This is a JavaScript module that is loaded on demand.
// It can export any number of functions, and may import other JavaScript modules if required.

export function showPrompt(message) {
  return prompt(message, 'Type anything here');
}

export function focusElement(element) {
    element.focus();
}

export function saveAsFile(filename, bytesBase64) {
    var link = document.createElement('a');
    link.download = filename;
    link.href = "data:application/octet-stream;base64," + bytesBase64;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
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

function calculateAutoHeight(e) {
    if (e && e.target) {
        e.target.style.height = 'auto';
        e.target.style.height = this.scrollHeight + 'px';
        e.target.style.overflowY = 'hidden';
    }
}

export function recalculateAutoHeight(textarea) {
    if (!textarea)
        return;

    // fire input to trigger autosize in case the text is long
    if ("createEvent" in document) {
        let event = document.createEvent("HTMLEvents");
        event.initEvent("input", false, true);
        textarea.dispatchEvent(event);
    }
    else {
        textarea.fireEvent("oninput");
    }
}

export function handleTabKey(textarea) {
    if (textarea) {
        textarea.oninput = calculateAutoHeight;

        // fire input immediatelly to trigger autosize in case the text is long
        if ("createEvent" in document) {
            let event = document.createEvent("HTMLEvents");
            event.initEvent("input", false, true);
            textarea.dispatchEvent(event);
        }
        else {
            textarea.fireEvent("oninput");
        }

        textarea.rows = 2;
    }
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
            }
        });

        textarea.tabKeyHandlerAdded = true;
    }
}