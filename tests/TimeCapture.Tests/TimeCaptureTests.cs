using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace TimeCapture.Tests;

[NonParallelizable]
public class TimeCaptureTests : PageTest
{
    private static WebApplication? _app;
    private static string _url = "";

    public override BrowserNewContextOptions ContextOptions() => new();

    [OneTimeSetUp]
    public async Task StartHost()
    {
        _app = TimeCaptureApp.Create([], FindWebRoot());
        _app.Urls.Clear();
        _app.Urls.Add("http://127.0.0.1:0");
        await _app.StartAsync();
        _url = _app.Urls.First();
    }

    [OneTimeTearDown]
    public async Task StopHost()
    {
        if (_app is not null)
            await _app.DisposeAsync();
    }

    [Test]
    public async Task Lawyer_CanSaveMatterTime_AndSeeItInUtc()
    {
        await OpenForm();
        await Page.GetByTestId("client-search").FillAsync("Northwind Legal");
        await Page.GetByTestId("matter-select").SelectOptionAsync("M-1001");
        await Page.GetByTestId("narrative").FillAsync("Reviewed discovery bundle");
        await Page.GetByTestId("save-entry").ClickAsync();

        await Expect(Page.GetByTestId("todays-entries")).ToContainTextAsync("M-1001 Discovery");
        await Expect(Page.GetByTestId("todays-entries")).ToContainTextAsync("Alice Chen");
        await Expect(Page.GetByTestId("todays-entries")).ToContainTextAsync("UTC");
        await Expect(Page.GetByTestId("todays-entries")).ToContainTextAsync("0.5h");
    }

    [Test]
    public async Task Save_WithoutMatter_ShowsError()
    {
        await OpenForm();
        await Page.GetByTestId("client-search").FillAsync("Northwind Legal");
        await Page.GetByTestId("narrative").FillAsync("Forgot the matter");
        await Page.GetByTestId("save-entry").ClickAsync();
        await Expect(Page.GetByTestId("form-error")).ToContainTextAsync("matter");
    }

    [Test]
    public async Task Save_DisablesButton_WhileServerWorks()
    {
        await OpenForm();
        await Page.GetByTestId("client-search").FillAsync("Northwind Legal");
        await Page.GetByTestId("matter-select").SelectOptionAsync("M-1002");
        await Page.GetByTestId("narrative").FillAsync("Hearing prep");
        var save = Page.GetByTestId("save-entry");
        await save.ClickAsync(new() { NoWaitAfter = true });
        await Expect(save).ToBeDisabledAsync();
        await Expect(Page.GetByTestId("todays-entries")).ToContainTextAsync("M-1002 Hearing");
    }

    [Test]
    public async Task Pa_CannotRecordForUnauthorisedLawyer()
    {
        await OpenForm();
        await Page.GetByTestId("pa-mode").CheckAsync();
        await Page.GetByTestId("timekeeper").SelectOptionAsync("john");
        await Page.GetByTestId("client-search").FillAsync("Contoso Holdings");
        await Page.GetByTestId("matter-select").SelectOptionAsync("M-2001");
        await Page.GetByTestId("narrative").FillAsync("PA tried to bill John");
        await Page.GetByTestId("save-entry").ClickAsync();
        await Expect(Page.GetByTestId("form-error")).ToContainTextAsync("not allowed");
    }

    [Test]
    public async Task Integration_UnknownTimekeeper_IsRejected()
    {
        using var client = new HttpClient { BaseAddress = new Uri(_url) };
        var payload = new
        {
            timekeeperId = "not-a-lawyer",
            clientId = "northwind",
            matterId = "M-1001",
            durationHours = 0.2m,
            activityCategory = "Research",
            comment = "Imported from another system",
            enteredBy = "Lawyer",
            source = "Integration",
            idempotencyKey = Guid.NewGuid().ToString()
        };
        var res = await client.PostAsJsonAsync("/api/entries", payload);
        Assert.That(res.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var body = await res.Content.ReadAsStringAsync();
        Assert.That(body, Does.Contain("Unknown timekeeper"));
    }

    [Test]
    public async Task SameIdempotencyKey_DoesNotCreateTwoEntries()
    {
        using var client = new HttpClient { BaseAddress = new Uri(_url) };
        var key = "same-click-" + Guid.NewGuid();
        var payload = new Dictionary<string, object?>
        {
            ["timekeeperId"] = "alice",
            ["clientId"] = "northwind",
            ["matterId"] = "M-1001",
            ["durationHours"] = 0.3m,
            ["activityCategory"] = "Phone call",
            ["comment"] = "Called client about filing",
            ["enteredBy"] = "Lawyer",
            ["source"] = "Ui",
            ["idempotencyKey"] = key
        };
        using var a = await client.PostAsJsonAsync("/api/entries?slow=1", payload);
        using var b = await client.PostAsJsonAsync("/api/entries?slow=1", payload);
        Assert.That(a.IsSuccessStatusCode);
        Assert.That(b.IsSuccessStatusCode);
        var list = await client.GetFromJsonAsync<JsonElement[]>("/api/entries");
        var matches = list!.Count(e => e.GetProperty("comment").GetString() == "Called client about filing");
        Assert.That(matches, Is.EqualTo(1));
    }

    private async Task OpenForm()
    {
        await Page.GotoAsync(_url + "/");
        await Expect(Page.GetByTestId("timekeeper")).ToHaveValueAsync("alice");
    }

    private static string FindWebRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "src", "TimeCapture.Web");
            if (Directory.Exists(Path.Combine(candidate, "wwwroot")))
                return candidate;
            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Could not find TimeCapture.Web wwwroot.");
    }
}
