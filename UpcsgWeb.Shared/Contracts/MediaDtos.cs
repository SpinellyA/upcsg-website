namespace UpcsgWeb.Shared.Contracts;

public class UploadGrantRequest
{
    /// <summary>merch, events, members or achievements.</summary>
    public string Folder { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
}

public class UploadGrantDto
{
    public string Key { get; set; } = string.Empty;
    public string UploadUrl { get; set; } = string.Empty;
    public string PublicUrl { get; set; } = string.Empty;

    /// <summary>PUT straight to the bucket, or POST to the API's local fallback.</summary>
    public string Method { get; set; } = "PUT";
}

public class ConfirmUploadRequest
{
    public string Key { get; set; } = string.Empty;
}

public class ConfirmUploadDto
{
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Empty for an object in the private bucket, which by definition has no public URL.
    /// Anything being saved should use <see cref="StoredReference"/> instead.
    /// </summary>
    public string PublicUrl { get; set; } = string.Empty;

    /// <summary>
    /// What the caller persists on the record: the public URL for site content, the
    /// storage key for a private object such as a receipt.
    ///
    /// The distinction matters because a private object's URL is presigned and expires. A
    /// receipt row holding one would render a broken image within the hour, and the
    /// officer looking at it could not tell that from an upload that never arrived.
    /// </summary>
    public string StoredReference { get; set; } = string.Empty;

    public long SizeBytes { get; set; }
}
