using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;

namespace TimeCapture.Desktop;

public partial class MainWindow : Window
{
    private static readonly HttpClient Http = new()
    {
        BaseAddress = new Uri(
            Environment.GetEnvironmentVariable("TIMECAPTURE_URL") ?? "http://localhost:5288")
    };

    public MainWindow()
    {
        InitializeComponent();
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        var payload = new
        {
            timekeeperId = "alice",
            clientId = "northwind",
            matterId = "M-1001",
            durationHours = 0.5m,
            activityCategory = "Research",
            comment = Narrative.Text,
            enteredBy = "Lawyer",
            source = "Ui",
            idempotencyKey = Guid.NewGuid().ToString()
        };

        try
        {
            var res = await Http.PostAsJsonAsync("/api/entries", payload);
            var body = await res.Content.ReadAsStringAsync();
            TodaysEntries.Text = res.IsSuccessStatusCode
                ? Narrative.Text.Trim()
                : body;
        }
        catch (Exception ex)
        {
            TodaysEntries.Text = "Server not running: " + ex.Message;
        }
    }
}
