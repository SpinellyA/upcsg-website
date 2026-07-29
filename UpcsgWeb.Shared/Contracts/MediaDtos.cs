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
    public string PublicUrl { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
}
