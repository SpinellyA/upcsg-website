// Saves text the browser already has to a file, without a server round trip.
//
// The snapshot is fetched from the API into .NET and handed here as a string, so this
// never re-requests it, and the download works even for content the browser could not
// simply navigate to.
export function saveText(fileName, text, mimeType) {
    const blob = new Blob([text], { type: mimeType || 'application/json' });
    const url = URL.createObjectURL(blob);

    const link = document.createElement('a');
    link.href = url;
    link.download = fileName;

    // Must be in the document for the click to count in Firefox.
    document.body.appendChild(link);
    link.click();
    link.remove();

    // Released on the next tick: revoking synchronously can cancel the download in
    // Safari before it has read the blob.
    setTimeout(() => URL.revokeObjectURL(url), 1000);
}
