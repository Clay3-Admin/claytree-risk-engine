namespace Claytree.Risk.Functions.Models;

public enum DocumentProcessingStatus
{
    Uploaded = 1,
    Validated = 2,
    QueuedForExtraction = 3,
    Extracting = 4,
    Extracted = 5,
    FailedExtraction = 6,
    FraudReview = 7,
    RiskScored = 8
}