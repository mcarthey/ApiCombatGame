// API Combat Game — Client-side utilities (CSS-first M3 version)

// Dark mode toggle (works with any element with data-theme-toggle)
function initThemeToggle() {
    var toggles = document.querySelectorAll('[data-theme-toggle]');
    if (toggles.length === 0) return;

    function setTheme(isDark) {
        document.documentElement.classList.toggle('dark', isDark);
        document.documentElement.setAttribute('data-theme', isDark ? 'dark' : 'light');
        localStorage.setItem('theme', isDark ? 'dark' : 'light');
        toggles.forEach(function (t) {
            var lightIcon = t.querySelector('.icon-light');
            var darkIcon = t.querySelector('.icon-dark');
            if (lightIcon && darkIcon) {
                lightIcon.style.display = isDark ? 'none' : '';
                darkIcon.style.display = isDark ? '' : 'none';
            }
        });
    }

    var isDark = document.documentElement.classList.contains('dark');
    // Set initial icon state
    toggles.forEach(function (t) {
        var lightIcon = t.querySelector('.icon-light');
        var darkIcon = t.querySelector('.icon-dark');
        if (lightIcon && darkIcon) {
            lightIcon.style.display = isDark ? 'none' : '';
            darkIcon.style.display = isDark ? '' : 'none';
        }
        t.addEventListener('click', function () {
            setTheme(!document.documentElement.classList.contains('dark'));
        });
    });
}

document.addEventListener('DOMContentLoaded', initThemeToggle);

// Mobile navigation menu
function initMobileMenu() {
    var btn = document.getElementById('mobile-menu-btn');
    var menu = document.getElementById('mobile-menu');
    var panel = document.getElementById('mobile-menu-panel');
    var backdrop = document.getElementById('mobile-menu-backdrop');
    var closeBtn = document.getElementById('mobile-menu-close');
    var icon = document.getElementById('mobile-menu-icon');
    if (!btn || !menu || !panel) return;

    function openMenu() {
        menu.style.display = '';
        // Trigger reflow for transition
        panel.offsetHeight;
        panel.style.transform = 'translateX(0)';
        btn.setAttribute('aria-expanded', 'true');
        if (icon) icon.textContent = 'close';
        document.body.style.overflow = 'hidden';
    }

    function closeMenu() {
        panel.style.transform = 'translateX(100%)';
        btn.setAttribute('aria-expanded', 'false');
        if (icon) icon.textContent = 'menu';
        document.body.style.overflow = '';
        setTimeout(function () { menu.style.display = 'none'; }, 250);
    }

    btn.addEventListener('click', function () {
        var isOpen = btn.getAttribute('aria-expanded') === 'true';
        if (isOpen) closeMenu(); else openMenu();
    });
    if (closeBtn) closeBtn.addEventListener('click', closeMenu);
    if (backdrop) backdrop.addEventListener('click', closeMenu);
}

document.addEventListener('DOMContentLoaded', initMobileMenu);

// Clipboard
function copyToClipboard(text, button) {
    navigator.clipboard.writeText(text.trim ? text.trim() : text).then(function () {
        if (button) {
            button.classList.add('copy-done');
            setTimeout(function () { button.classList.remove('copy-done'); }, 2000);
        }
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

// Confirm action via native dialog
function confirmAction(message) {
    return Promise.resolve(confirm(message));
}
