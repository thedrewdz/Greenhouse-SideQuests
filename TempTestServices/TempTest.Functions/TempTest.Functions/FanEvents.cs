using System.Net;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using TempTest.Application.FanEvent;

namespace TempTest.Functions;

public sealed class FanEvents(IRecordFanEvent recordFanEvent, ILogger<FanEvents> logger)
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);

    [Function("fnPostFanEvent")]
    public async Task<HttpResponseData> Post(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "fan-events")] HttpRequestData req,
        CancellationToken cancellationToken)
    {
        FanEventRequest? request;

        try
        {
            request = await JsonSerializer.DeserializeAsync<FanEventRequest>(
                req.Body,
                JsonSerializerOptions,
                cancellationToken);
        }
        catch (JsonException exception)
        {
            logger.LogWarning(exception, "Invalid fan event JSON payload.");
            return await WriteErrorAsync(req, HttpStatusCode.BadRequest, "Request body must be valid JSON.", cancellationToken);
        }

        if (!TryCreateCommand(request, out RecordFanEventCommand? command, out string error))
        {
            return await WriteErrorAsync(req, HttpStatusCode.UnprocessableEntity, error, cancellationToken);
        }

        RecordFanEventResult result = await recordFanEvent.RecordAsync(command, cancellationToken);

        HttpResponseData response = req.CreateResponse(HttpStatusCode.Created);
        response.Headers.Add("Location", $"/api/fan-events/{result.Id}");
        await response.WriteAsJsonAsync(
            new FanEventResponse(result.Id, result.CreatedAtUtc),
            cancellationToken);

        return response;
    }

    private static bool TryCreateCommand(
        FanEventRequest? request,
        [NotNullWhen(true)] out RecordFanEventCommand? command,
        out string error)
    {
        command = null;

        if (request is null)
        {
            error = "Request body is required.";
            return false;
        }

        if (request.StartTemperature is null)
        {
            error = "StartTemperature is required.";
            return false;
        }

        if (request.EndTemperature is null)
        {
            error = "EndTemperature is required.";
            return false;
        }

        if (request.StartHumidity is null)
        {
            error = "StartHumidity is required.";
            return false;
        }

        if (request.StartHumidity is < 0 or > 100)
        {
            error = "StartHumidity must be between 0 and 100.";
            return false;
        }

        if (request.EndHumidity is null)
        {
            error = "EndHumidity is required.";
            return false;
        }

        if (request.EndHumidity is < 0 or > 100)
        {
            error = "EndHumidity must be between 0 and 100.";
            return false;
        }

        if (request.StartDate is null)
        {
            error = "StartDate is required.";
            return false;
        }

        if (request.EndDate is null)
        {
            error = "EndDate is required.";
            return false;
        }

        if (request.EndDate < request.StartDate)
        {
            error = "EndDate must not be before StartDate.";
            return false;
        }

        command = new RecordFanEventCommand(
            request.StartTemperature.Value,
            request.EndTemperature.Value,
            request.StartHumidity.Value,
            request.EndHumidity.Value,
            request.StartDate.Value,
            request.EndDate.Value);

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

    private sealed record FanEventRequest(
        decimal? StartTemperature,
        decimal? EndTemperature,
        decimal? StartHumidity,
        decimal? EndHumidity,
        DateTimeOffset? StartDate,
        DateTimeOffset? EndDate);

    private sealed record FanEventResponse(
        Guid Id,
        DateTimeOffset CreatedAtUtc);

    private sealed record ErrorResponse(string Error);
}
