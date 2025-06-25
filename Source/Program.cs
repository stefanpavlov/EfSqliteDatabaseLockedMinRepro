using Microsoft.EntityFrameworkCore;

Directory.SetCurrentDirectory(AppContext.BaseDirectory);

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(serverOptions => serverOptions.ListenLocalhost(9538));

builder.Services
    .AddDbContext<ApplicationDbContext>(o =>
        o
        .UseSqlite("Data Source=./TapeService.db;Default Timeout=86400")
        .EnableSensitiveDataLogging(true)
        .EnableDetailedErrors(true));

builder.Services
    .AddHttpClient("self")
    .ConfigureHttpClient(c =>
    {
        c.BaseAddress = new Uri("http://localhost:9538");
        c.Timeout = TimeSpan.FromHours(1);
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapDelete("api/files/{id}", async (Guid id, ApplicationDbContext appDbContext, CancellationToken ct) =>
{
    var timer = System.Diagnostics.Stopwatch.StartNew();
    var file = await appDbContext.Files
        .FirstOrDefaultAsync(t => t.Id == id, ct);
    if (file is null)
    {
        Environment.FailFast($"File {id} not found in database. Cannot delete.");
    }

    if (file.Status == FileStatus.Staged || file.Status == FileStatus.TapeGone)
    {
        appDbContext.Files.Remove(file);
    }
    else
    {
        Environment.FailFast($"File {id} has status {file.Status}, expected Staged or TapeGone. Cannot delete.");
    }

    try
    {
        await appDbContext.SaveChangesAsync(ct);
    }
    catch (Exception ex)
    {
        app.Logger.LogError(
            ex,
            "Failed to delete file {FileId} in {ElapsedMilliseconds} ms",
            id,
            timer.ElapsedMilliseconds);
        return Results.Problem("Failed to delete file", statusCode: 500);
    }

    app.Logger.LogInformation(
        "File {FileId} deleted in {ElapsedMilliseconds} ms",
        id,
        timer.ElapsedMilliseconds);
    return Results.NoContent();
})
.WithTags("BuggyEndpoint");

app.MapPost("TestParallelDelete", async (ApplicationDbContext appDbContext, IHttpClientFactory httpClientFactory, CancellationToken ct) =>
{
    var timer = System.Diagnostics.Stopwatch.StartNew();
    var fileIds = await appDbContext.Files
        .Where(f => f.Status == FileStatus.Staged || f.Status == FileStatus.TapeGone)
        .Select(f => f.Id)
        .Take(1000)
        .ToListAsync(ct);

    var successes = 0;
    var fails = 0;

    var res = fileIds.Select(async id =>
    {
        using var client = httpClientFactory.CreateClient("self");

        var response = await client.DeleteAsync($"http://localhost:9538/api/files/{id}");
        if (response.IsSuccessStatusCode)
        {
            Console.WriteLine($"Successfully deleted file {id}");
            Interlocked.Increment(ref successes);
        }
        else
        {
            Console.WriteLine($"Failed to delete file {id}: {response.StatusCode}");
            Interlocked.Increment(ref fails);
        }
    })
    .ToList();

    Console.WriteLine("Waiting for tasks to complete");

    await Task.WhenAll(res);

    Console.WriteLine($"All tasks completed. Successes: {successes}, Fails: {fails}, Duration: {timer.ElapsedMilliseconds} ms");

    return Results.Ok(new
    {
        Successes = successes,
        Fails = fails,
        Duration = timer.ElapsedMilliseconds
    });
})
.WithTags("IssueReproduction");

app.MapPost("TestSequentialDelete", async (ApplicationDbContext appDbContext, IHttpClientFactory httpClientFactory, CancellationToken ct) =>
{
    var timer = System.Diagnostics.Stopwatch.StartNew();
    var fileIds = await appDbContext.Files
        .Where(f => f.Status == FileStatus.Staged || f.Status == FileStatus.TapeGone)
        .Select(f => f.Id)
        .Take(1000)
        .ToListAsync(ct);

    var successes = 0;
    var fails = 0;

    foreach (var id in fileIds)
    {
        using var client = httpClientFactory.CreateClient("self");
        var response = await client.DeleteAsync($"http://localhost:9538/api/files/{id}");
        if (response.IsSuccessStatusCode)
        {
            Console.WriteLine($"Successfully deleted file {id}");
            successes++;
        }
        else
        {
            Console.WriteLine($"Failed to delete file {id}: {response.StatusCode}");
            fails++;
        }
    }

    Console.WriteLine($"All tasks completed. Successes: {successes}, Fails: {fails}, Duration: {timer.ElapsedMilliseconds} ms");

    return Results.Ok(new
    {
        Successes = successes,
        Fails = fails,
        Duration = timer.ElapsedMilliseconds
    });
})
.WithTags("NoReproduction");

app.Run();
