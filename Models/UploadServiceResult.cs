namespace Claytree.Risk.Functions.Models;

public sealed class UploadServiceResult
{
    public string Message { get; set; } = string.Empty;
    public Guid LoanApplicationId { get; set; }
    public string? ApplicationNumber { get; set; }
    public List<object> Documents { get; set; } = new();
}
