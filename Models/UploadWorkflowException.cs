using System.Net;

namespace Claytree.Risk.Functions.Models;

public sealed class UploadWorkflowException : Exception
{
    public UploadWorkflowException(HttpStatusCode statusCode, object payload)
    {
        StatusCode = statusCode;
        Payload = payload;
    }

    public HttpStatusCode StatusCode { get; }

    public object Payload { get; }
}
