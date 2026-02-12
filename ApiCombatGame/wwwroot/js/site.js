// API Combat Game — Client-side utilities (M3 version)

// Dark mode toggle (works with <md-icon-button toggle>)
function initThemeToggle() {
    var toggles = document.querySelectorAll('[data-theme-toggle]');
    if (toggles.length === 0) return;

    function setTheme(isDark) {
        document.documentElement.classList.toggle('dark', isDark);
        document.documentElement.setAttribute('data-theme', isDark ? 'dark' : 'light');
        localStorage.setItem('theme', isDark ? 'dark' : 'light');
        toggles.forEach(function (t) {
            if (t.tagName === 'MD-ICON-BUTTON') {
                t.selected = isDark;
            }
        });
    }

    var isDark = document.documentElement.classList.contains('dark');
    toggles.forEach(function (t) {
        if (t.tagName === 'MD-ICON-BUTTON') {
            t.selected = isDark;
            t.addEventListener('input', function () {
                setTheme(this.selected);
            });
        } else {
            t.addEventListener('click', function () {
                setTheme(!document.documentElement.classList.contains('dark'));
            });
        }
    });
}

document.addEventListener('DOMContentLoaded', initThemeToggle);

// Clipboard
function copyToClipboard(text) {
    navigator.clipboard.writeText(text).then(function () {
        showToast('Copied to clipboard!');
    }).catch(function () {
        var textArea = document.createElement('textarea');
        textArea.value = text;
        textArea.style.position = 'fixed';
        textArea.style.left = '-9999px';
        document.body.appendChild(textArea);
        textArea.select();
        document.execCommand('copy');
        document.body.removeChild(textArea);
        showToast('Copied to clipboard!');
    });
}

// Toast notification (M3-styled)
function showToast(message) {
    var toast = document.createElement('div');
    toast.style.cssText = [
        'position:fixed', 'bottom:1.5rem', 'right:1.5rem',
        'padding:0.75rem 1.25rem',
        'border-radius:var(--md-sys-shape-corner-small, 8px)',
        'background:var(--md-sys-color-inverse-surface, #313033)',
        'color:var(--md-sys-color-inverse-on-surface, #f4eff4)',
        'font-size:0.875rem',
        'box-shadow:var(--md-sys-elevation-3, 0 4px 8px 3px rgba(0,0,0,0.15))',
        'z-index:50'
    ].join(';');
    toast.textContent = message;
    document.body.appendChild(toast);
    setTimeout(function () {
        toast.style.opacity = '0';
        toast.style.transition = 'opacity 0.3s';
        setTimeout(function () { document.body.removeChild(toast); }, 300);
    }, 2000);
}

// Confirm action via M3 dialog
function confirmAction(message) {
    return new Promise(function (resolve) {
        var dialog = document.createElement('md-dialog');
        dialog.innerHTML =
            '<div slot="headline">Confirm Action</div>' +
            '<div slot="content"><p>' + message + '</p></div>' +
            '<div slot="actions">' +
            '<md-text-button form="dialog" value="cancel">Cancel</md-text-button>' +
            '<md-filled-button form="dialog" value="confirm">Confirm</md-filled-button>' +
            '</div>';
        dialog.addEventListener('close', function () {
            resolve(dialog.returnValue === 'confirm');
            dialog.remove();
        });
        document.body.appendChild(dialog);
        dialog.show();
    });
}
