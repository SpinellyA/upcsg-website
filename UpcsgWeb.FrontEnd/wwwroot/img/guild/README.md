# Guild photos

Photos shown on the drifting rail in the About page's identity section.

## Adding one

1. Drop the image file in this folder.
2. Add an entry to `photos.json`.

```json
[
  { "file": "codesprint-2026.jpg", "caption": "CodeSprint, third round" },
  { "file": "ga-first-sem.jpg",    "caption": "First general assembly of the year" }
]
```

`file` is the filename only — the folder is already known. `caption` is optional but
worth writing: it doubles as the image's alt text, so a photo without one is invisible
to anyone using a screen reader.

The rail renders in the order listed. Leave `photos.json` as `[]` and the rail does not
render at all; the section still reads properly without it.

## What works well here

- **Landscape.** Frames are a fixed height and size to their own aspect ratio, so a
  portrait photo becomes a narrow sliver. Crop to landscape first.
- **Around 1600px wide**, saved as JPEG. Frames are never displayed larger than about
  420px, so anything bigger is bytes the visitor downloads for nothing. These load with
  the page, so keep each one under roughly 300 KB.
- **People doing something.** The rail is the evidence for "students run all of it".
  A photo of a room is not evidence; a photo of someone running a session is.

## Consent

These go on a public page. Get the agreement of the people in a photo before adding it,
and drop any photo on request — delete the file and its entry.
