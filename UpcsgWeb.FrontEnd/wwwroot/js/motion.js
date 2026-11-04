// Scroll-triggered motion for the marketing pages.
//
// Hand-rolled rather than AOS/GSAP for two reasons. Blazor renders components after the
// page loads and re-renders them on every state change, so a library that scans the DOM
// once at init silently misses everything the framework adds later. The usual fix is
// re-initialising on each render, which fights the framework. Here each component hands
// its own element over when it mounts, so timing is never in question. Second, this ships
// to GitHub Pages: no CDN, no vendored bundle to audit, ~2 KB instead of ~14 KB.

const reduceMotion = window.matchMedia('(prefers-reduced-motion: reduce)');

// Tells the failsafe in index.html that this module arrived. Without it, the page gives
// up on animation and shows everything. See the comment there for why that matters.
window.__upcsgMotionReady = true;

// One observer for the whole page. One per element would be hundreds of observers on a
// long page, and they all watch the same viewport anyway.
const observer = new IntersectionObserver(
    (entries) => {
        for (const entry of entries) {
            if (!entry.isIntersecting) {
                continue;
            }

            entry.target.classList.add('is-revealed');

            // Entrance animations play once. Leaving elements observed would replay them
            // on every scroll past, which reads as a glitch rather than a flourish.
            observer.unobserve(entry.target);
        }
    },
    {
        // Fires a little before the element reaches the viewport, so the animation is
        // already settling by the time it is properly in view.
        rootMargin: '0px 0px -4% 0px',
        threshold: 0.05,
    });

export function reveal(element) {
    if (!element) {
        return;
    }

    // Someone who asked the OS for less motion gets the finished state immediately,
    // never a blank element that only appears if an observer happens to fire.
    if (reduceMotion.matches) {
        element.classList.add('is-revealed');
        return;
    }

    observer.observe(element);
}

export function release(element) {
    if (element) {
        observer.unobserve(element);
    }
}

/**
 * Counts a number up once its element scrolls into view.
 *
 * The DOM is written directly instead of round-tripping each frame through .NET: a
 * 60 fps interop call per counter is a lot of marshalling to animate one integer.
 */
export function countUp(element, target, durationMs) {
    if (!element) {
        return;
    }

    if (reduceMotion.matches || target <= 0) {
        element.textContent = String(target);
        element.classList.add('is-revealed');
        return;
    }

    const start = performance.now();

    // Decelerating: fast at first, easing into the final value. A linear count looks
    // mechanical and finishes with a jolt.
    const ease = (t) => 1 - Math.pow(1 - t, 3);

    const step = (now) => {
        const progress = Math.min((now - start) / durationMs, 1);
        element.textContent = String(Math.round(ease(progress) * target));

        if (progress < 1) {
            requestAnimationFrame(step);
        }
    };

    const counter = new IntersectionObserver((entries, self) => {
        for (const entry of entries) {
            if (entry.isIntersecting) {
                element.classList.add('is-revealed');
                requestAnimationFrame(step);
                self.disconnect();
            }
        }
    }, { threshold: 0.4 });

    counter.observe(element);
}

/**
 * Publishes scroll depth as a CSS variable on the element, so parallax and fade-on-scroll
 * are expressed in the stylesheet rather than by writing inline styles from script.
 */
export function trackScroll(element) {
    if (!element || reduceMotion.matches) {
        return;
    }

    const update = () => {
        const depth = Math.min(window.scrollY / window.innerHeight, 1);
        element.style.setProperty('--scroll-depth', depth.toFixed(3));
    };

    update();
    window.addEventListener('scroll', update, { passive: true });
}

/**
 * Colour by chapter: the fixed header takes the field of the chapter beneath its
 * bottom edge. Each chapter carries data-field; the page background is never
 * recoloured, only the header follows the edge as it passes.
 */
export function trackChapters(target) {
    const header = typeof target === "string" ? document.querySelector(target) : target;
    if (!header || header.__chapters) {
        return;
    }

    header.__chapters = true;
    let queued = false;

    const update = () => {
        queued = false;
        const line = header.getBoundingClientRect().bottom;
        let field = 'night';

        for (const chapter of document.querySelectorAll('[data-field]')) {
            const box = chapter.getBoundingClientRect();
            if (box.top <= line && box.bottom > line) {
                field = chapter.dataset.field;
            }
        }

        if (header.dataset.field !== field) {
            header.dataset.field = field;
        }
    };

    const queue = () => {
        if (!queued) {
            queued = true;
            requestAnimationFrame(update);
        }
    };

    // Every chapter's parts arrive as it enters: its direct children rise in turn.
    // Parts already wrapped in a Reveal are left to it.
    const enter = () => {
        for (const inner of document.querySelectorAll('.upcsg-chapter__inner, .upcsg-eventhero__inner, .upcsg-article__headinner, .upcsg-oppmast__grid > div, .upcsg-oppdeadline > div, .upcsg-lost, .upcsg-lost__links')) {
            let i = 0;
            for (const part of inner.children) {
                if (part.__entered || part.classList.contains('upcsg-reveal') || part.querySelector(':scope > .upcsg-reveal')) {
                    continue;
                }

                part.__entered = true;
                part.classList.add('upcsg-reveal', 'upcsg-reveal--up');
                part.style.setProperty('--reveal-delay', `${Math.min(i++, 4) * 80}ms`);
                reveal(part);
            }
        }
    };

    const changed = () => {
        queue();
        enter();
    };

    window.addEventListener('scroll', queue, { passive: true });
    window.addEventListener('resize', queue);
    new MutationObserver(changed).observe(document.getElementById('app') || document.body, { childList: true, subtree: true });
    update();
    enter();
}
