// API Combat Game - Client-side utilities

// Dark mode toggle
function initThemeToggle() {
    var toggle = document.getElementById('theme-toggle');
    var sun = document.getElementById('icon-sun');
    var moon = document.getElementById('icon-moon');

    if (!toggle || !sun || !moon) return;

    function updateIcons(isDark) {
        sun.classList.toggle('hidden', !isDark);
        moon.classList.toggle('hidden', isDark);
    }

    function toggleTheme() {
        var isDark = document.documentElement.classList.toggle('dark');
        localStorage.setItem('theme', isDark ? 'dark' : 'light');
        updateIcons(isDark);
    }

    updateIcons(document.documentElement.classList.contains('dark'));
    toggle.addEventListener('click', toggleTheme);
}

document.addEventListener('DOMContentLoaded', initThemeToggle);

function copyToClipboard(text) {
    navigator.clipboard.writeText(text).then(function () {
        showToast('Copied to clipboard!');
    }).catch(function () {
        // Fallback for older browsers
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

function showToast(message) {
    var toast = document.createElement('div');
    toast.className = 'fixed bottom-4 right-4 bg-gray-900 dark:bg-gray-100 text-white dark:text-gray-900 px-4 py-2 rounded-lg shadow-lg text-sm z-50';
    toast.textContent = message;
    document.body.appendChild(toast);
    setTimeout(function () {
        toast.style.opacity = '0';
        toast.style.transition = 'opacity 0.3s';
        setTimeout(function () { document.body.removeChild(toast); }, 300);
    }, 2000);
}

function confirmAction(message) {
    return confirm(message);
}
