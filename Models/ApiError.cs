namespace Claytree.Risk.Functions.Models;

public sealed record ApiError(string Code, string Message, object? Details = null);
