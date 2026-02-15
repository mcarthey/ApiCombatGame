// celebrations.js — Lightweight celebration animations for API Combat Game
// Usage: add data-celebrate="confetti" to any element to auto-trigger on page load,
// or call Celebrations.confetti() / Celebrations.burst(element) from JS.
(function () {
    'use strict';

    // M3-inspired palette (works on both light and dark backgrounds)
    const COLORS = ['#6750A4', '#D0BCFF', '#7D5260', '#FFB74D', '#4FC3F7', '#81C784', '#FF8A65', '#BA68C8'];

    let activeCanvas = null;

    function getCanvas() {
        if (activeCanvas && activeCanvas.parentNode) {
            activeCanvas.width = window.innerWidth;
            activeCanvas.height = window.innerHeight;
            return activeCanvas;
        }
        const c = document.createElement('canvas');
        c.style.cssText = 'position:fixed;top:0;left:0;width:100%;height:100%;pointer-events:none;z-index:9999;';
        c.width = window.innerWidth;
        c.height = window.innerHeight;
        document.body.appendChild(c);
        activeCanvas = c;
        return c;
    }

    /**
     * Fire a confetti burst.
     * @param {Object} [opts]
     * @param {number} [opts.particleCount=100]  Number of particles
     * @param {number} [opts.spread=80]          Spread angle in degrees
     * @param {number} [opts.startVelocity=50]   Initial velocity
     * @param {number} [opts.duration=3500]      Animation length in ms
     * @param {number} [opts.originX=0.5]        Horizontal origin (0–1)
     * @param {number} [opts.originY=0.35]       Vertical origin (0–1)
     * @param {string[]} [opts.colors]           Hex color array
     */
    function confetti(opts) {
        const o = Object.assign({
            particleCount: 100,
            spread: 80,
            startVelocity: 50,
            duration: 3500,
            originX: 0.5,
            originY: 0.35,
            gravity: 0.8,
            decay: 0.94,
            colors: COLORS
        }, opts);

        const canvas = getCanvas();
        const ctx = canvas.getContext('2d');
        const particles = [];
        const start = performance.now();

        for (let i = 0; i < o.particleCount; i++) {
            const angle = ((Math.random() * o.spread - o.spread / 2) + 270) * Math.PI / 180;
            const v = o.startVelocity * (0.4 + Math.random() * 0.6);
            particles.push({
                x: canvas.width * o.originX,
                y: canvas.height * o.originY,
                vx: Math.cos(angle) * v * (Math.random() > 0.5 ? 1 : -1),
                vy: Math.sin(angle) * v * -1,
                color: o.colors[Math.floor(Math.random() * o.colors.length)],
                w: Math.random() * 10 + 4,
                h: Math.random() * 6 + 2,
                rotation: Math.random() * 360,
                spin: (Math.random() - 0.5) * 12,
                wobble: Math.random() * 10,
                wobbleSpeed: 0.05 + Math.random() * 0.1,
                opacity: 1
            });
        }

        function frame(now) {
            const elapsed = now - start;
            if (elapsed > o.duration) {
                ctx.clearRect(0, 0, canvas.width, canvas.height);
                if (activeCanvas) { activeCanvas.remove(); activeCanvas = null; }
                return;
            }

            ctx.clearRect(0, 0, canvas.width, canvas.height);
            const progress = elapsed / o.duration;

            particles.forEach(function (p) {
                p.vy += o.gravity;
                p.vx *= o.decay;
                p.vy *= o.decay;
                p.x += p.vx;
                p.y += p.vy;
                p.rotation += p.spin;
                p.wobble += p.wobbleSpeed;
                p.x += Math.sin(p.wobble) * 0.5;

                // Fade out in the last 40%
                p.opacity = progress > 0.6 ? 1 - ((progress - 0.6) / 0.4) : 1;

                ctx.save();
                ctx.translate(p.x, p.y);
                ctx.rotate(p.rotation * Math.PI / 180);
                ctx.globalAlpha = Math.max(0, p.opacity);
                ctx.fillStyle = p.color;
                ctx.fillRect(-p.w / 2, -p.h / 2, p.w, p.h);
                ctx.restore();
            });

            requestAnimationFrame(frame);
        }

        requestAnimationFrame(frame);
    }

    /**
     * Fire a small celebratory burst near an element (e.g. a button or badge).
     * @param {HTMLElement} el  The element to burst from
     * @param {Object} [opts]   Options passed to confetti()
     */
    function burst(el, opts) {
        if (!el) return;
        const rect = el.getBoundingClientRect();
        confetti(Object.assign({
            particleCount: 40,
            spread: 50,
            startVelocity: 30,
            duration: 2000,
            originX: (rect.left + rect.width / 2) / window.innerWidth,
            originY: (rect.top + rect.height / 2) / window.innerHeight
        }, opts));
    }

    // Expose API
    window.Celebrations = { confetti: confetti, burst: burst };

    // Auto-trigger: any element with data-celebrate="confetti" fires on DOMContentLoaded
    document.addEventListener('DOMContentLoaded', function () {
        var trigger = document.querySelector('[data-celebrate="confetti"]');
        if (trigger) {
            setTimeout(function () { confetti(); }, 400);
        }

        // Also support burst: data-celebrate="burst" fires a small burst near that element
        document.querySelectorAll('[data-celebrate="burst"]').forEach(function (el) {
            setTimeout(function () { burst(el); }, 400);
        });
    });
})();
