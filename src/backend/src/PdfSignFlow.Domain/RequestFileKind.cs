namespace PdfSignFlow.Domain;

public enum RequestFileKind
{
    /// <summary>A PDF that signers must sign.</summary>
    Document = 0,

    /// <summary>A file sent for information only.</summary>
    Attachment = 1,
}
