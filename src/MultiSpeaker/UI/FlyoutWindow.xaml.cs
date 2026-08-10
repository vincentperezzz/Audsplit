using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MultiSpeaker.Models;
using MultiSpeaker.Services;

namespace MultiSpeaker.UI;

public partial class FlyoutWindow : Window
{
    private readonly DeviceService _devices;
    private readonly SessionService _sessions;
    private readonly RoutingService _routing;
    private bool _suppressSelectionChanged;

    public ObservableCollection<AppRowViewModel> Apps { get; } = new();
    public ObservableCollection<DeviceChoice> DeviceChoices { get; } = new();

    public FlyoutWindow(DeviceService devices, SessionService sessions, RoutingService routing)
    {
        _devices = devices;
        _sessions = sessions;
        _routing = routing;

        InitializeComponent();
        DataContext = this;
        Deactivated += (_, _) => Hide();
    }

    public void ShowNearCursor()
    {
        Refresh();
        var workArea = SystemParameters.WorkArea;
        Left = Math.Max(0, workArea.Right - Width - 12);
        Top = Math.Max(0, workArea.Bottom - Height - 12);
        Show();
        Activate();
    }

    public void Refresh()
    {
        _suppressSelectionChanged = true;
        try
        {
            DeviceChoices.Clear();
            DeviceChoices.Add(new DeviceChoice { Id = null, Name = "System default" });
            foreach (var d in _devices.GetOutputDevices())
            {
                var label = d.IsDefault ? $"{d.Name} (default)" : d.Name;
                DeviceChoices.Add(new DeviceChoice { Id = d.Id, Name = label });
            }

            Apps.Clear();
            foreach (var session in _sessions.GetAppSessions())
            {
                var selectedId = session.PersistedDeviceId;
                var selected = DeviceChoices.FirstOrDefault(c =>
                                   string.Equals(c.Id, selectedId, StringComparison.OrdinalIgnoreCase))
                               ?? DeviceChoices[0];

                Apps.Add(new AppRowViewModel
                {
                    ProcessId = session.ProcessId,
                    DisplayName = session.DisplayName,
                    ProcessName = session.ProcessName,
                    Icon = session.Icon,
                    SelectedDevice = selected,
                });
            }

            var empty = Apps.Count == 0;
            EmptyText.Visibility = empty ? Visibility.Visible : Visibility.Collapsed;
            AppsList.Visibility = empty ? Visibility.Collapsed : Visibility.Visible;
            SetStatus(empty
                ? "No active apps · hit Refresh after audio starts"
                : $"Refreshed · {Apps.Count} app{(Apps.Count == 1 ? "" : "s")}");
        }
        finally
        {
            _suppressSelectionChanged = false;
        }
    }

    private void OnDeviceSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressSelectionChanged)
        {
            return;
        }

        if (sender is not ComboBox { DataContext: AppRowViewModel row } combo)
        {
            return;
        }

        if (combo.SelectedItem is not DeviceChoice choice)
        {
            return;
        }

        row.SelectedDevice = choice;
        var ok = _routing.SetOutputDevice(row.ProcessId, choice.Id);
        if (!ok)
        {
            SetStatus($"Failed to route {row.DisplayName}");
            return;
        }

        SetStatus(choice.Id is null
            ? $"{row.DisplayName} → system default"
            : $"{row.DisplayName} → {choice.Name}");
    }

    private void OnRefreshClick(object sender, RoutedEventArgs e) => Refresh();

    private void OnResetAllClick(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "Reset all per-app audio routes to the system default?",
            "AUDSPLIT",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        if (_routing.ClearAllRoutes())
        {
            SetStatus("All routes cleared");
            Refresh();
        }
        else
        {
            SetStatus("Failed to clear routes");
        }
    }

    private void OnTitleBarMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            DragMove();
        }
    }

    private void SetStatus(string text) => StatusText.Text = text;
}

public sealed class DeviceChoice
{
    public string? Id { get; init; }
    public required string Name { get; init; }

    public override string ToString() => Name;
}

public sealed class AppRowViewModel : INotifyPropertyChanged
{
    private DeviceChoice? _selectedDevice;

    public uint ProcessId { get; init; }
    public required string DisplayName { get; init; }
    public required string ProcessName { get; init; }
    public ImageSource? Icon { get; init; }

    public DeviceChoice? SelectedDevice
    {
        get => _selectedDevice;
        set
        {
            if (!Equals(_selectedDevice, value))
            {
                _selectedDevice = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
