using System.Net;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using TempTest.Application.SprayEvent;

namespace TempTest.Functions;

public sealed class SprayEvents(IRecordSprayEvent recordSprayEvent, ILogger<SprayEvents> logger)
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);

    [Function("fnPostSprayEvent")]
    public async Task<HttpResponseData> Post(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "spray-events")] HttpRequestData req,
        CancellationToken cancellationToken)
    {
        SprayEventRequest? request;

        try
        {
            request = await JsonSerializer.DeserializeAsync<SprayEventRequest>(
                req.Body,
                JsonSerializerOptions,
                cancellationToken);
        }
        catch (JsonException exception)
        {
            logger.LogWarning(exception, "Invalid spray event JSON payload.");
            return await WriteErrorAsync(req, HttpStatusCode.BadRequest, "Request body must be valid JSON.", cancellationToken);
        }

        if (!TryCreateCommand(request, out RecordSprayEventCommand? command, out string error))
        {
            return await WriteErrorAsync(req, HttpStatusCode.UnprocessableEntity, error, cancellationToken);
        }

        RecordSprayEventResult result = await recordSprayEvent.RecordAsync(command, cancellationToken);

        HttpResponseData response = req.CreateResponse(HttpStatusCode.Created);
        response.Headers.Add("Location", $"/api/spray-events/{result.Id}");
        await response.WriteAsJsonAsync(
            new SprayEventResponse(result.Id, result.CreatedAtUtc),
            cancellationToken);

        return response;
    }

    private static bool TryCreateCommand(
        SprayEventRequest? request,
        [NotNullWhen(true)] out RecordSprayEventCommand? command,
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

        if (request.WaterUsedMilliliters is null)
        {
            error = "WaterUsedMilliliters is required.";
            return false;
        }

        if (request.WaterUsedMilliliters < 0)
        {
            error = "WaterUsedMilliliters must not be negative.";
            return false;
        }

        command = new RecordSprayEventCommand(
            request.StartTemperature.Value,
            request.EndTemperature.Value,
            request.StartHumidity.Value,
            request.EndHumidity.Value,
            request.StartDate.Value,
            request.EndDate.Value,
            request.WaterUsedMilliliters.Value);

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

    private sealed record SprayEventRequest(
        decimal? StartTemperature,
        decimal? EndTemperature,
        decimal? StartHumidity,
        decimal? EndHumidity,
        DateTimeOffset? StartDate,
        DateTimeOffset? EndDate,
        decimal? WaterUsedMilliliters);

    private sealed record SprayEventResponse(
        Guid Id,
        DateTimeOffset CreatedAtUtc);

    private sealed record ErrorResponse(string Error);
}
