using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;
using System.Windows.Controls;

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
        FillMattersForClient();
    }

    private static string ComboId(ComboBox box) =>
        (box.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "";

    private void Client_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (Matter is null)
            return;
        FillMattersForClient();
    }

    private void FillMattersForClient()
    {
        Matter.Items.Clear();
        if (ComboId(Client) == "northwind")
        {
            Matter.Items.Add(new ComboBoxItem { Content = "M-1001 Discovery", Tag = "M-1001", IsSelected = true });
            Matter.Items.Add(new ComboBoxItem { Content = "M-1002 Hearing", Tag = "M-1002" });
            Matter.IsEnabled = true;
            return;
        }

        if (ComboId(Client) == "contoso")
        {
            Matter.Items.Add(new ComboBoxItem { Content = "M-2001 Contract review", Tag = "M-2001", IsSelected = true });
            Matter.IsEnabled = true;
            return;
        }

        Matter.Items.Add(new ComboBoxItem { Content = "Select a client first", Tag = "" });
        Matter.SelectedIndex = 0;
        Matter.IsEnabled = false;
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        FormError.Text = "";
        SaveButton.IsEnabled = false;

        var payload = new
        {
            timekeeperId = ComboId(Timekeeper),
            clientId = ComboId(Client),
            matterId = ComboId(Matter),
            durationHours = decimal.TryParse(Duration.Text, out var hours) ? hours : 0m,
            activityCategory = (Category.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "",
            comment = Narrative.Text,
            enteredBy = PaMode.IsChecked == true ? "PA" : "Lawyer",
            source = "Ui",
            idempotencyKey = Guid.NewGuid().ToString()
        };

        try
        {
            var res = await Http.PostAsJsonAsync("/api/entries", payload);
            var body = await res.Content.ReadAsStringAsync();
            if (res.IsSuccessStatusCode)
            {
                TodaysEntries.Items.Insert(0, Narrative.Text.Trim());
                return;
            }

            FormError.Text = body;
        }
        catch (Exception ex)
        {
            FormError.Text = "Server not running: " + ex.Message;
        }
        finally
        {
            SaveButton.IsEnabled = true;
        }
    }
}
