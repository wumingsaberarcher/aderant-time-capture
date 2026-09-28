using TimeCapture;

var app = TimeCaptureApp.Create(args);
app.Run();

namespace TimeCapture
{
    public static class TimeCaptureApp
    {
        public static WebApplication Create(string[] args, string? contentRoot = null)
        {
            var options = new WebApplicationOptions
            {
                Args = args,
                ContentRootPath = contentRoot
            };
            var builder = WebApplication.CreateBuilder(options);
            builder.Services.AddSingleton<TimeEntryStore>();
            var app = builder.Build();
            app.UseDefaultFiles();
            app.UseStaticFiles();

            app.MapGet("/api/lookups", () => new
            {
                timekeepers = FirmDirectory.Timekeepers,
                clients = FirmDirectory.Clients,
                matters = FirmDirectory.Matters,
                categories = FirmDirectory.Categories
            });

            app.MapGet("/api/entries", (TimeEntryStore store) => store.All());

            app.MapPost("/api/entries", async (HttpRequest http, TimeEntryStore store) =>
            {
                var request = await http.ReadFromJsonAsync<CreateEntryRequest>()
                              ?? new CreateEntryRequest(null, null, null, 0, null, null, null, null, null);
                var key = http.Headers["Idempotency-Key"].ToString();
                if (!string.IsNullOrWhiteSpace(key))
                {
                    request = request with { IdempotencyKey = key };
                }

                var slow = http.Query["slow"] == "1";
                var (status, body) = store.Create(request, simulateLatency: slow);
                return Results.Json(body, statusCode: status);
            });

            return app;
        }
    }
}
