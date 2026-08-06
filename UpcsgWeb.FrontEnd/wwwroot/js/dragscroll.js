// Click-and-drag horizontal scrolling with inertia.
//
// Mouse only, deliberately. Touch and trackpads already scroll an overflowing element
// with real momentum, tuned per platform; intercepting pointer events for those replaces
// something good with an imitation, and on touch it also fights the browser's own
// gesture handling. So this fills in the one case the browser does not cover — dragging
// with a mouse — and leaves every other input alone.

/**
 * @param {HTMLElement} el a horizontally scrollable element
 * @returns {{dispose: () => void}}
 */
export function attach(el) {
    if (!el) {
        return { dispose() { } };
    }

    // Someone who has asked for less motion should not be given a flywheel. They keep
    // dragging; only the coasting afterwards is dropped.
    const calmed = window.matchMedia('(prefers-reduced-motion: reduce)');

    let dragging = false;
    let startX = 0;
    let startScroll = 0;
    let travelled = 0;

    // Velocity is measured over the last movement rather than across the whole drag, so a
    // slow reposition that ends with a flick still throws, and a fast drag that stops dead
    // does not.
    let velocity = 0;
    let lastX = 0;
    let lastTime = 0;
    let glide = 0;

    /** Movement beyond this is a drag, and the click that follows it is not a choice. */
    const DRAG_THRESHOLD = 10;

    /** Per-frame retention. Higher coasts further; 0.95 is a long, low-friction wheel. */
    const FRICTION = 0.95;

    /** Below this the wheel has effectively stopped, so stop burning frames. */
    const STOP_BELOW = 0.05;

    function onPointerDown(e) {
        if (e.pointerType !== 'mouse' || e.button !== 0) {
            return;
        }

        dragging = true;
        travelled = 0;
        velocity = 0;
        startX = lastX = e.clientX;
        startScroll = el.scrollLeft;
        lastTime = performance.now();

        cancelAnimationFrame(glide);

        // Keeps the drag alive when the cursor leaves the strip, which it will — the
        // strip is short and the gesture is not.
        //
        // Guarded because this throws NotFoundError when the pointer id is not currently
        // active, and an exception here would abort before the drag state below is set,
        // leaving the strip in a half-started drag that never clears.
        try {
            el.setPointerCapture(e.pointerId);
        } catch {
            // Without capture the drag still works; it just ends early if the pointer
            // leaves the element.
        }
        startTime = performance.now();
        el.classList.add('is-dragging');
    }

    function onPointerMove(e) {
        if (!dragging) {
            return;
        }

        const dx = e.clientX - lastX;
        const now = performance.now();
        const dt = now - lastTime;

        if (dt > 0) {
            // px per frame at 60Hz, which is the unit the glide loop below works in.
            velocity = (dx / dt) * 16.667;
        }

        el.scrollLeft = startScroll - (e.clientX - startX);
        travelled = Math.max(travelled, Math.abs(e.clientX - startX));

        lastX = e.clientX;
        lastTime = now;
    }

    function onPointerUp(e) {
        if (!dragging) {
            return;
        }

        dragging = false;
        el.classList.remove('is-dragging');

        try {
            el.releasePointerCapture(e.pointerId);
        } catch {
            // Already released; nothing to undo.
        }

        // A pointer that was held still before release has no throw in it, however fast
        // it was moving earlier.
        if (performance.now() - lastTime > 100) {
            velocity = 0;
        }

        if (!calmed.matches && Math.abs(velocity) > STOP_BELOW) {
            coast();
        }
    }

    function coast() {
        velocity *= FRICTION;
        el.scrollLeft -= velocity;

        // Hitting either end kills the throw rather than letting it grind against the
        // stop for another second.
        const atStart = el.scrollLeft <= 0;
        const atEnd = el.scrollLeft >= el.scrollWidth - el.clientWidth - 1;

        if (Math.abs(velocity) > STOP_BELOW && !atStart && !atEnd) {
            glide = requestAnimationFrame(coast);
        }
    }

    // A drag that ends over an avatar must not also select it. The click fires after
    // pointerup, so this runs in the capture phase and stops it before it reaches the
    // button — without which every throw would change the card underneath.
    function onClickCapture(e) {
        const heldFor = performance.now() - startTime;
        if (travelled > DRAG_THRESHOLD && heldFor > 80) {
            e.preventDefault();
            e.stopPropagation();
        }
        travelled = 0; // also worth always resetting here, not just on the swallow path
    }

    // Native drag-and-drop would otherwise start on the images and hijack the gesture,
    // leaving a ghost portrait attached to the cursor.
    function onDragStart(e) {
        e.preventDefault();
    }

    el.addEventListener('pointerdown', onPointerDown);
    el.addEventListener('pointermove', onPointerMove);
    el.addEventListener('pointerup', onPointerUp);
    el.addEventListener('pointercancel', onPointerUp);
    el.addEventListener('click', onClickCapture, true);
    el.addEventListener('dragstart', onDragStart);

    return {
        dispose() {
            cancelAnimationFrame(glide);
            el.removeEventListener('pointerdown', onPointerDown);
            el.removeEventListener('pointermove', onPointerMove);
            el.removeEventListener('pointerup', onPointerUp);
            el.removeEventListener('pointercancel', onPointerUp);
            el.removeEventListener('click', onClickCapture, true);
            el.removeEventListener('dragstart', onDragStart);
        }
    };
}

/** Brings a face into view when selection changes from outside the strip. */
export function revealChild(el, index) {
    const child = el?.children?.[index];
    if (child) {
        child.scrollIntoView({ behavior: 'smooth', block: 'nearest', inline: 'center' });
    }
}
