using System.Net;
using System.Text.Json;
using Claytree.Risk.Functions.Models;
using Microsoft.Azure.Functions.Worker.Http;

namespace Claytree.Risk.Functions.Extensions;

public static class HttpResponseExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static HttpResponseData WriteJson<T>(this HttpResponseData res, HttpStatusCode status, T payload)
    {
        res.StatusCode = status;
        res.Headers.Add("Content-Type", "application/json; charset=utf-8");
        res.WriteString(JsonSerializer.Serialize(payload, JsonOptions));
        return res;
    }

    public static HttpResponseData WriteError(this HttpResponseData res, HttpStatusCode status, string code, string message, object? details = null)
        => res.WriteJson(status, new ApiError(code, message, details));
}
