using System.Net;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using TempTest.Application.SensorData;

namespace TempTest.Functions;

public sealed class Crud(IRecordSensorData recordSensorData, ILogger<Crud> logger)
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);

    [Function("fnPost")]
    public async Task<HttpResponseData> Post(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req,
        CancellationToken cancellationToken)
    {
        SensorDataRequest? request;

        try
        {
            request = await JsonSerializer.DeserializeAsync<SensorDataRequest>(
                req.Body,
                JsonSerializerOptions,
                cancellationToken);
        }
        catch (JsonException exception)
        {
            logger.LogWarning(exception, "Invalid sensor data JSON payload.");
            return await WriteErrorAsync(req, HttpStatusCode.BadRequest, "Request body must be valid JSON.", cancellationToken);
        }

        if (!TryCreateCommand(request, out RecordSensorDataCommand? command, out string error))
        {
            return await WriteErrorAsync(req, HttpStatusCode.BadRequest, error, cancellationToken);
        }

        RecordSensorDataResult result = await recordSensorData.RecordAsync(command, cancellationToken);

        HttpResponseData response = req.CreateResponse(HttpStatusCode.Created);
        await response.WriteAsJsonAsync(
            new SensorDataResponse(result.Id, result.CreatedAtUtc),
            cancellationToken);

        return response;
    }

    private static bool TryCreateCommand(
        SensorDataRequest? request,
        [NotNullWhen(true)] out RecordSensorDataCommand? command,
        out string error)
    {
        command = null;

        if (request is null)
        {
            error = "Request body is required.";
            return false;
        }

        if (request.Temperature is null)
        {
            error = "Temperature is required.";
            return false;
        }

        if (request.Humidity is null)
        {
            error = "Humidity is required.";
            return false;
        }

        if (request.Humidity is < 0 or > 100)
        {
            error = "Humidity must be between 0 and 100.";
            return false;
        }

        if (request.Timestamp is null)
        {
            error = "Timestamp is required.";
            return false;
        }

        command = new RecordSensorDataCommand(
            request.Temperature.Value,
            request.Humidity.Value,
            request.Timestamp.Value);

        error = string.Empty;
        return true;
    }

    private static async Task<HttpResponseData> WriteErrorAsync(
        HttpRequestData req,
        HttpStatusCode statusCode,
        string message,
        CancellationToken cancellationToken)
    {
        HttpResponseData response = req.CreateResponse(statusCode);
        await response.WriteAsJsonAsync(new ErrorResponse(message), cancellationToken);
        return response;
    }

    private sealed record SensorDataRequest(
        decimal? Temperature,
        decimal? Humidity,
        DateTimeOffset? Timestamp);

    private sealed record SensorDataResponse(
        Guid Id,
        DateTimeOffset CreatedAtUtc);

    private sealed record ErrorResponse(string Error);
}
