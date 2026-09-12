namespace CloudManager.Host.Endpoints;

using CloudManager.Host.Application;
using CloudManager.Host.Mappers;
using CloudManager.Host.Models.Data;

using Microsoft.AspNetCore.Http.HttpResults;

public static class DataEndpoints
{
    //--------------------------------------------------------------------------------
    // Mapping
    //--------------------------------------------------------------------------------

    public static void MapDataEndpoints(this WebApplication app)
    {
        var group = app.MapGroup(ApiRoutes.Data)
            .RequireAuthorization();

        group.MapGet("/", HandleListAsync);
        group.MapGet("/csv", HandleExportCsv);
        group.MapGet("/{id:long}", HandleGetAsync);
        group.MapPost("/", HandleCreateAsync);
        group.MapPut("/{id:long}", HandleUpdateAsync);
        group.MapDelete("/{id:long}", HandleDeleteAsync).RequireAuthorization(Policies.Administrator);
    }

    //--------------------------------------------------------------------------------
    // Handler
    //--------------------------------------------------------------------------------

    private static async ValueTask<IResult> HandleListAsync(
        DataService dataService,
        string? name,
        string? sort,
        CancellationToken cancellationToken,
        bool desc = false,
        [Range(0, Int32.MaxValue)] int page = 0,
        [Range(1, 100)] int size = 20)
    {
        var result = await dataService.QueryPageAsync(name, sort, desc, page, size, cancellationToken);
        return TypedResults.Ok(new DataListResponse(
            result.Total,
            result.Page,
            result.Size,
            result.Items.Select(DataMapper.ToResponse).ToList()));
    }

    private static PushStreamHttpResult HandleExportCsv(DataService dataService, CancellationToken cancellationToken) =>
        TypedResults.Stream(
            async stream =>
            {
                await using var writer = new StreamWriter(stream, new UTF8Encoding(true));
                await using var csv = new CsvHelper.CsvWriter(writer, CultureInfo.InvariantCulture);
                await csv.WriteRecordsAsync(dataService.QueryExportEnumerable(cancellationToken), cancellationToken);
            },
            "text/csv",
            "data.csv");

    private static async ValueTask<IResult> HandleGetAsync(
        DataService dataService,
        long id)
    {
        var entity = await dataService.QueryAsync(id);
        return entity is not null
            ? TypedResults.Ok(entity.ToResponse())
            : TypedResults.NotFound();
    }

    private static async ValueTask<IResult> HandleCreateAsync(
        DataService dataService,
        DataCreateRequest request)
    {
        var id = await dataService.InsertAsync(request.Name, request.Value);
        return id.HasValue
            ? TypedResults.Created($"{ApiRoutes.Data}/{id.Value}", new DataCreateResponse(id.Value))
            : TypedResults.Problem(statusCode: StatusCodes.Status409Conflict, title: "Duplicate name.");
    }

    private static async ValueTask<IResult> HandleUpdateAsync(
        DataService dataService,
        long id,
        DataUpdateRequest request)
    {
        var result = await dataService.UpdateAsync(id, request.Name, request.Value);
        return result switch
        {
            DataWriteStatus.Success => TypedResults.NoContent(),
            DataWriteStatus.NotFound => TypedResults.NotFound(),
            _ => TypedResults.Problem(statusCode: StatusCodes.Status409Conflict, title: "Duplicate name.")
        };
    }

    private static async ValueTask<IResult> HandleDeleteAsync(
        DataService dataService,
        long id)
    {
        var deleted = await dataService.DeleteAsync(id);
        return deleted ? TypedResults.NoContent() : TypedResults.NotFound();
    }
}
