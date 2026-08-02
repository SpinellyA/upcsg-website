// Google Identity Services, wired to Blazor.
//
// The library is loaded on demand rather than from a <script> tag in index.html: it is
// only needed on the login page, and pulling a third-party script into every page load
// costs every visitor a request for something most of them never use.

let scriptPromise = null;

function loadLibrary() {
    // Cached, so several calls during one visit share a single network request and a
    // single global initialisation.
    scriptPromise ??= new Promise((resolve, reject) => {
        if (window.google?.accounts?.id) {
            resolve();
            return;
        }

        const script = document.createElement('script');
        script.src = 'https://accounts.google.com/gsi/client';
        script.async = true;
        script.defer = true;
        script.onload = () => resolve();
        script.onerror = () => {
            // Cleared so a later attempt can retry rather than awaiting a promise that
            // is already rejected — a blocked script on one render should not
            // permanently disable sign-in for the session.
            scriptPromise = null;
            reject(new Error('Could not load Google sign-in.'));
        };

        document.head.appendChild(script);
    });

    return scriptPromise;
}

/**
 * Renders Google's own button into `elementId` and hands the credential back to .NET.
 *
 * Google's button has to be rendered by their library — it is not something we can
 * style ourselves, and reimplementing it would break the "Sign in with Google" branding
 * rules as well as the one-tap behaviour.
 */
export async function renderButton(elementId, clientId, dotNetRef) {
    await loadLibrary();

    const target = document.getElementById(elementId);
    if (!target) {
        return;
    }

    window.google.accounts.id.initialize({
        client_id: clientId,

        // The credential never touches JavaScript state: it goes straight to .NET, which
        // posts it to the API for verification. Nothing here trusts it.
        callback: (response) => {
            dotNetRef.invokeMethodAsync('OnGoogleCredential', response.credential);
        },

        // Cancels the prompt if the user taps outside it, rather than leaving a modal
        // the page has no control over.
        cancel_on_tap_outside: true,
    });

    window.google.accounts.id.renderButton(target, {
        theme: 'filled_black',
        size: 'large',
        shape: 'pill',
        text: 'signin_with',
        logo_alignment: 'center',

        // Matches the container so the button does not sit narrower than the panel
        // it lives in. Google caps this at 400.
        width: Math.min(400, target.offsetWidth || 320),
    });
}

/** Clears the session hint so the next sign-in offers the account chooser again. */
export function disableAutoSelect() {
    if (window.google?.accounts?.id) {
        window.google.accounts.id.disableAutoSelect();
    }
}
