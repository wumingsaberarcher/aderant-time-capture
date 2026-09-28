using System.Diagnostics;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
using Microsoft.AspNetCore.Builder;
using TimeCapture;

namespace TimeCapture.Desktop.Tests;

public class DesktopSaveTests
{
    private static WebApplication? _app;
    private static string _url = "";

    [OneTimeSetUp]
    public async Task StartServer()
    {
        _app = TimeCaptureApp.Create([]);
        _app.Urls.Clear();
        _app.Urls.Add("http://127.0.0.1:0");
        await _app.StartAsync();
        _url = _app.Urls.First();
    }

    [OneTimeTearDown]
    public async Task StopServer()
    {
        if (_app is not null)
            await _app.DisposeAsync();
    }

    [Test]
    public void DesktopWindow_SavesThroughTheServer()
    {
        var psi = new ProcessStartInfo(FindExe())
        {
            UseShellExecute = false
        };
        psi.Environment["TIMECAPTURE_URL"] = _url;

        using var automation = new UIA3Automation();
        using var app = Application.Launch(psi);
        var window = app.GetMainWindow(automation, TimeSpan.FromSeconds(10));

        var narrative = window.FindFirstDescendant(cf => cf.ByAutomationId("narrative"))!.AsTextBox();
        var save = window.FindFirstDescendant(cf => cf.ByAutomationId("save-entry"))!.AsButton();
        var list = window.FindFirstDescendant(cf => cf.ByAutomationId("todays-entries"))!;

        narrative.Text = "Reviewed discovery from desktop";
        save.Invoke();

        var seen = false;
        for (var i = 0; i < 25; i++)
        {
            if ((list.Name ?? "").Contains("Reviewed discovery from desktop"))
            {
                seen = true;
                break;
            }
            Thread.Sleep(200);
        }

        Assert.That(seen, Is.True, "Desktop Save should write through the server and show the comment.");
        app.Close();
    }

    private static string FindExe()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var exe = Path.Combine(dir.FullName, "src", "TimeCapture.Desktop", "bin", "Debug", "net10.0-windows", "TimeCapture.Desktop.exe");
            if (File.Exists(exe))
                return exe;
            dir = dir.Parent;
        }

        throw new FileNotFoundException("Build TimeCapture.Desktop first.");
    }
}
