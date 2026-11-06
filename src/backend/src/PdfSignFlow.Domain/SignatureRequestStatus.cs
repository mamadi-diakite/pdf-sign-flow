namespace PdfSignFlow.Domain;

public enum SignatureRequestStatus
{
    Draft = 0,
    Sent = 1,
    Completed = 2,
    Declined = 3,
    Cancelled = 4,
}
