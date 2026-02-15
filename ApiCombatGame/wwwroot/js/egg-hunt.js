// egg-hunt.js — Easter Egg Hunt client-side renderer
// Checks the current page for hidden eggs and renders them. On click, shows
// a treasure chest animation that reveals a redeemable code.
(function () {
    'use strict';

    var API_CHECK = '/api/eggs/check';
    var API_REDEEM = '/api/eggs/redeem';
    var currentEggCode = null;

    // Don't run on admin pages
    if (location.pathname.startsWith('/Admin')) return;

    document.addEventListener('DOMContentLoaded', function () {
        // Small random delay so it doesn't look scripted
        var delay = 1000 + Math.random() * 2000;
        setTimeout(checkForEgg, delay);
    });

    function checkForEgg() {
        var page = location.pathname;

        fetch(API_CHECK + '?page=' + encodeURIComponent(page), {
            credentials: 'same-origin'
        })
            .then(function (res) {
                if (!res.ok) return null;
                return res.json();
            })
            .then(function (data) {
                if (!data || !data.hasEgg || data.alreadyClaimed) return;
                currentEggCode = data.code;
                placeEgg(data);
            })
            .catch(function () { /* silent — hunt is optional */ });
    }

    function placeEgg(data) {
        // Find the container
        var container = data.selector ? document.querySelector(data.selector) : null;
        if (!container) container = document.querySelector('main') || document.body;

        // Make container position-aware; allow eggs to show through overflow
        var styles = getComputedStyle(container);
        if (styles.position === 'static') container.style.position = 'relative';
        if (styles.overflow === 'hidden') container.style.overflow = 'visible';

        // Create the hidden egg element
        var egg = document.createElement('button');
        egg.className = 'egg-hunt-egg';
        egg.setAttribute('aria-label', 'Hidden treasure! Click to reveal');
        egg.innerHTML = '<span class="material-symbols-outlined">' + (data.icon || 'egg_alt') + '</span>';

        // Position within the container
        egg.style.position = 'absolute';
        egg.style.left = (data.offsetX || 50) + '%';
        egg.style.top = (data.offsetY || 50) + '%';

        egg.addEventListener('click', function (e) {
            e.preventDefault();
            e.stopPropagation();
            egg.classList.add('egg-hunt-egg--found');
            // Brief sparkle, then show the chest dialog
            if (window.Celebrations) {
                Celebrations.burst(egg, { particleCount: 25, startVelocity: 20, duration: 1500 });
            }
            setTimeout(function () {
                egg.remove();
                showChestDialog();
            }, 600);
        });

        container.appendChild(egg);
    }

    function showChestDialog() {
        // Build the chest dialog
        var dialog = document.createElement('dialog');
        dialog.className = 'md3-dialog egg-chest-dialog';
        dialog.innerHTML =
            '<div class="egg-chest-stage">' +
            '  <div class="egg-chest-icon">' +
            '    <span class="material-symbols-outlined" style="font-size: 64px; color: var(--md-sys-color-tertiary);">mystery</span>' +
            '  </div>' +
            '  <h3 class="egg-chest-title">Treasure Found!</h3>' +
            '  <p class="egg-chest-subtitle">You discovered a hidden chest. Open it to reveal your reward!</p>' +
            '</div>' +
            '<div class="egg-chest-actions">' +
            '  <button class="md3-btn-text" id="egg-close">Not now</button>' +
            '  <button class="md3-btn-filled egg-chest-open" id="egg-open">' +
            '    <span class="material-symbols-outlined" style="font-size: 18px; margin-right: 4px;">lock_open</span>' +
            '    Open Chest' +
            '  </button>' +
            '</div>';

        document.body.appendChild(dialog);
        dialog.showModal();

        // Confetti behind the dialog
        if (window.Celebrations) {
            setTimeout(function () { Celebrations.confetti({ particleCount: 60, duration: 2500 }); }, 200);
        }

        var closeBtn = dialog.querySelector('#egg-close');
        var openBtn = dialog.querySelector('#egg-open');

        closeBtn.addEventListener('click', function () {
            dialog.close();
            dialog.remove();
        });

        openBtn.addEventListener('click', function () {
            openBtn.disabled = true;
            openBtn.innerHTML = '<span class="material-symbols-outlined" style="font-size: 18px; margin-right: 4px;">hourglass_top</span> Opening...';
            redeemEgg(dialog);
        });

        dialog.addEventListener('click', function (e) {
            if (e.target === dialog) { dialog.close(); dialog.remove(); }
        });
    }

    function redeemEgg(dialog) {
        if (!currentEggCode) {
            showError(dialog, 'Something went wrong. Try refreshing the page.');
            return;
        }

        fetch(API_REDEEM, {
            method: 'POST',
            credentials: 'same-origin',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ code: currentEggCode })
        })
            .then(function (res) { return res.json(); })
            .then(function (data) {
                if (!data.success) {
                    showError(dialog, data.error || 'This egg has already been claimed.');
                    return;
                }

                // Big confetti burst!
                if (window.Celebrations) {
                    Celebrations.confetti({ particleCount: 150, spread: 100, duration: 4000 });
                }

                var lootIcon = data.lootType === 'Gems' ? 'diamond' : data.lootType === 'XpBoost' ? 'electric_bolt' : 'paid';
                var lootColor = data.lootType === 'Gems' ? 'var(--md-sys-color-tertiary)' : data.lootType === 'XpBoost' ? 'var(--md-sys-color-primary)' : '#facc15';

                dialog.innerHTML =
                    '<div class="egg-chest-stage">' +
                    '  <div class="egg-chest-icon egg-chest-icon--reward">' +
                    '    <span class="material-symbols-outlined" style="font-size: 64px; color: ' + lootColor + ';">' + lootIcon + '</span>' +
                    '  </div>' +
                    '  <h3 class="egg-chest-title">Loot Acquired!</h3>' +
                    '  <p class="egg-chest-reward">' + data.lootDescription + '</p>' +
                    '  <p class="egg-chest-subtitle">Added to your account. Keep exploring to find more!</p>' +
                    '  <div class="egg-chest-api-hint">' +
                    '    <span class="material-symbols-outlined" style="font-size: 14px; vertical-align: text-bottom;">terminal</span> ' +
                    '    Pro tip: You can also redeem eggs via the API:<br>' +
                    '    <code>POST /api/eggs/redeem { "code": "' + (data.code || '???') + '" }</code>' +
                    '  </div>' +
                    '</div>' +
                    '<div class="egg-chest-actions">' +
                    '  <button class="md3-btn-filled" id="egg-done">Awesome!</button>' +
                    '</div>';

                dialog.querySelector('#egg-done').addEventListener('click', function () {
                    dialog.close();
                    dialog.remove();
                });
            })
            .catch(function () {
                showError(dialog, 'Network error. Please try again.');
            });
    }

    function showError(dialog, message) {
        dialog.innerHTML =
            '<div class="egg-chest-stage">' +
            '  <div class="egg-chest-icon" style="background: color-mix(in srgb, var(--md-sys-color-error) 12%, transparent);">' +
            '    <span class="material-symbols-outlined" style="font-size: 48px; color: var(--md-sys-color-error);">error</span>' +
            '  </div>' +
            '  <h3 class="egg-chest-title">' + message + '</h3>' +
            '</div>' +
            '<div class="egg-chest-actions">' +
            '  <button class="md3-btn-text" id="egg-dismiss">OK</button>' +
            '</div>';

        dialog.querySelector('#egg-dismiss').addEventListener('click', function () {
            dialog.close();
            dialog.remove();
        });
    }
})();
