using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Audsplit.Models;
using Audsplit.Services;

namespace Audsplit.UI;

public partial class FlyoutWindow : Window
{
    private readonly DeviceService _devices;
    private readonly SessionService _sessions;
    private readonly RoutingService _routing;
    private bool _suppressSelectionChanged;
    private bool _suppressVolumeChanged;
    private bool _suppressTabChanged;
    private bool _onSpeakersPage;
    private bool _uiReady;

    public ObservableCollection<AppRowViewModel> Apps { get; } = new();
    public ObservableCollection<DeviceChoice> DeviceChoices { get; } = new();
    public ObservableCollection<DeviceRowViewModel> Devices { get; } = new();

    public FlyoutWindow(DeviceService devices, SessionService sessions, RoutingService routing)
    {
        _devices = devices;
        _sessions = sessions;
        _routing = routing;

        InitializeComponent();
        DataContext = this;
        _uiReady = true;
        Loaded += (_, _) =>
        {
            AutoHideScroll.Attach(AppsScroll);
            AutoHideScroll.Attach(SpeakersScroll);
        };
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
        _suppressVolumeChanged = true;
        try
        {
            var outputs = _devices.GetOutputDevices();

            DeviceChoices.Clear();
            DeviceChoices.Add(new DeviceChoice { Id = null, Name = "System default" });
            foreach (var d in outputs)
            {
                var label = d.IsDefault ? $"{d.Name} (default)" : d.Name;
                DeviceChoices.Add(new DeviceChoice { Id = d.Id, Name = label });
            }

            Devices.Clear();
            foreach (var d in outputs)
            {
                var name = d.IsDefault ? $"{d.Name} (default)" : d.Name;
                Devices.Add(new DeviceRowViewModel
                {
                    Id = d.Id,
                    Name = name,
                    VolumePercent = Math.Clamp(d.Volume * 100.0, 0, 100),
                    IsMuted = d.IsMuted,
                });
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

            UpdatePageChrome();
        }
        finally
        {
            _suppressSelectionChanged = false;
            _suppressVolumeChanged = false;
        }
    }

    private void OnAppsTabChecked(object sender, RoutedEventArgs e)
    {
        if (!_uiReady || _suppressTabChanged)
        {
            return;
        }

        ShowAppsPage();
    }

    private void OnSpeakersTabChecked(object sender, RoutedEventArgs e)
    {
        if (!_uiReady || _suppressTabChanged)
        {
            return;
        }

        ShowSpeakersPage();
    }

    private void OnAppsTabUnchecked(object sender, RoutedEventArgs e)
    {
        if (!_uiReady || _suppressTabChanged)
        {
            return;
        }

        if (SpeakersTab.IsChecked != true)
        {
            _suppressTabChanged = true;
            AppsTab.IsChecked = true;
            _suppressTabChanged = false;
        }
    }

    private void OnSpeakersTabUnchecked(object sender, RoutedEventArgs e)
    {
        if (!_uiReady || _suppressTabChanged)
        {
            return;
        }

        if (AppsTab.IsChecked != true)
        {
            _suppressTabChanged = true;
            SpeakersTab.IsChecked = true;
            _suppressTabChanged = false;
        }
    }

    private void ShowAppsPage()
    {
        _onSpeakersPage = false;
        _suppressTabChanged = true;
        try
        {
            AppsTab.IsChecked = true;
            SpeakersTab.IsChecked = false;
        }
        finally
        {
            _suppressTabChanged = false;
        }

        AppsPage.Visibility = Visibility.Visible;
        SpeakersScroll.Visibility = Visibility.Collapsed;
        ResetAllButton.Visibility = Visibility.Visible;
        UpdatePageChrome();
    }

    private void ShowSpeakersPage()
    {
        _onSpeakersPage = true;
        _suppressTabChanged = true;
        try
        {
            SpeakersTab.IsChecked = true;
            AppsTab.IsChecked = false;
        }
        finally
        {
            _suppressTabChanged = false;
        }

        AppsPage.Visibility = Visibility.Collapsed;
        SpeakersScroll.Visibility = Visibility.Visible;
        ResetAllButton.Visibility = Visibility.Collapsed;
        UpdatePageChrome();
    }

    private void UpdatePageChrome()
    {
        if (_onSpeakersPage)
        {
            SetStatus(Devices.Count == 0
                ? "No output devices found"
                : $"Speakers · {Devices.Count} device{(Devices.Count == 1 ? "" : "s")}");
            return;
        }

        var empty = Apps.Count == 0;
        EmptyText.Visibility = empty ? Visibility.Visible : Visibility.Collapsed;
        AppsScroll.Visibility = empty ? Visibility.Collapsed : Visibility.Visible;
        SetStatus(empty
            ? "No active apps · hit Refresh after audio starts"
            : $"Apps · {Apps.Count} session{(Apps.Count == 1 ? "" : "s")} · pick an output");
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

    private void OnVolumeChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_suppressVolumeChanged)
        {
            return;
        }

        if (sender is not Slider { DataContext: DeviceRowViewModel row })
        {
            return;
        }

        row.VolumePercent = e.NewValue;
        var ok = _devices.SetVolume(row.Id, (float)(e.NewValue / 100.0));
        SetStatus(ok
            ? $"{row.Name} · {(int)Math.Round(e.NewValue)}%"
            : $"Failed to set volume for {row.Name}");
    }

    private void OnMuteChanged(object sender, RoutedEventArgs e)
    {
        if (_suppressVolumeChanged)
        {
            return;
        }

        if (sender is not ToggleButton { DataContext: DeviceRowViewModel row } toggle)
        {
            return;
        }

        var muted = toggle.IsChecked == true;
        row.IsMuted = muted;
        var ok = _devices.SetMute(row.Id, muted);
        SetStatus(ok
            ? $"{row.Name} · {(muted ? "muted" : "unmuted")}"
            : $"Failed to mute {row.Name}");
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

public sealed class DeviceRowViewModel : INotifyPropertyChanged
{
    private double _volumePercent;
    private bool _isMuted;

    public required string Id { get; init; }
    public required string Name { get; init; }

    public double VolumePercent
    {
        get => _volumePercent;
        set
        {
            if (Math.Abs(_volumePercent - value) > 0.01)
            {
                _volumePercent = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(VolumeText));
            }
        }
    }

    public bool IsMuted
    {
        get => _isMuted;
        set
        {
            if (_isMuted != value)
            {
                _isMuted = value;
                OnPropertyChanged();
            }
        }
    }

    public string VolumeText => $"{(int)Math.Round(VolumePercent)}%";

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
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
