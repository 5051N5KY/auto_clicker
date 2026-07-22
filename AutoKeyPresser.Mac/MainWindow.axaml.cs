using System.Diagnostics;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using AutoKeyPresser.Mac.Services;
using AutoKeyPresser.Models;
using AutoKeyPresser.Services;

namespace AutoKeyPresser.Mac;

public sealed partial class MainWindow : Window
{
    private readonly MacKeyboardHookService _hook = new();
    private readonly MacAutoPressService _press = new();
    private readonly SettingsService _settingsService = new();
    private AppSettings _settings = new();
    private bool _selecting;
    private bool _sanitizing;
    private KeyModifiers _pendingModifiers;

    public MainWindow()
    {
        InitializeComponent();
        Opened += OnOpened;
        Closing += OnClosing;
        _hook.KeyDown += OnPhysicalKeyDown;
        _hook.KeyUp += OnPhysicalKeyUp;
        _press.DelayStarted += () => Ui(() => StatusText.Text = "Starting delay");
        _press.Active += () => Ui(() => StatusText.Text = "Active");
        _press.PressCompleted += count => Ui(() => CounterText.Text = count.ToString(CultureInfo.InvariantCulture));
        _press.NextPressInChanged += time => Ui(() => CountdownText.Text = time is null ? "--" : $"{time.Value.TotalSeconds:0.00} s");
        _press.Failed += ex => Ui(async () => await ShowMessage("Input error", ex.Message));
        _press.Stopped += reason => Ui(() => ApplyStopped(reason));
    }

    private static void Ui(Action action) => Dispatcher.UIThread.Post(action);

    private async void OnOpened(object? sender, EventArgs e)
    {
        _settings = await _settingsService.LoadAsync();
        ApplySettings();
        if (!_hook.EnsurePermissions())
        {
            StatusText.Text = "Permissions required";
            await ShowMessage("Permissions required", "Enable Auto Key Presser in System Settings → Privacy & Security → Accessibility and Input Monitoring, then restart the app.");
        }
        _hook.Start();
    }

    private void ApplySettings()
    {
        SelectedKeyText.Text = MacKeyService.Display((ushort)_settings.VirtualKey, _settings.Modifiers);
        IntervalTextBox.Text = ((int)_settings.Interval).ToString();
        IntervalUnitCombo.SelectedIndex = _settings.IntervalInSeconds ? 1 : 0;
        RandomCheckBox.IsChecked = _settings.RandomDeviationEnabled;
        DeviationTextBox.Text = _settings.RandomDeviationMs.ToString();
        StartDelayTextBox.Text = _settings.StartDelaySeconds.ToString();
        LimitModeCombo.SelectedIndex = (int)_settings.LimitMode;
        LimitValueTextBox.Text = (_settings.LimitMode == LimitMode.Duration ? (int)_settings.DurationLimit : _settings.PressCountLimit).ToString();
        LimitTimeUnitCombo.SelectedIndex = _settings.DurationLimitInSeconds ? 1 : 0;
        StopOnOtherKeyCheckBox.IsChecked = _settings.StopOnOtherKey;
        UpdateConditionalControls();
    }

    private void SelectKey_Click(object? sender, RoutedEventArgs e)
    {
        _selecting = true;
        _pendingModifiers = KeyModifiers.None;
        SelectedKeyText.Text = "Press a key...";
        StatusText.Text = "Waiting for a key";
        SelectKeyButton.IsEnabled = false;
        CancelKeyButton.IsVisible = true;
    }

    private void CancelKey_Click(object? sender, RoutedEventArgs e)
    {
        _selecting = false;
        _pendingModifiers = KeyModifiers.None;
        SelectedKeyText.Text = MacKeyService.Display((ushort)_settings.VirtualKey, _settings.Modifiers);
        SelectKeyButton.IsEnabled = true;
        CancelKeyButton.IsVisible = false;
        StatusText.Text = "Stopped";
    }

    private void OnPhysicalKeyDown(ushort key) => Ui(() =>
    {
        if (_selecting)
        {
            if (MacKeyService.TryGetModifier(key, out var modifier))
            {
                _pendingModifiers |= modifier;
                SelectedKeyText.Text = ModifierName(_pendingModifiers) + " + ...";
                StatusText.Text = "Now press the main key";
                return;
            }
            _settings.VirtualKey = key;
            _settings.Modifiers = _pendingModifiers;
            _settings.KeyName = MacKeyService.Display(key, _pendingModifiers);
            SelectedKeyText.Text = _settings.KeyName;
            _selecting = false;
            SelectKeyButton.IsEnabled = true;
            CancelKeyButton.IsVisible = false;
            StatusText.Text = "Stopped";
            return;
        }
        if (_press.IsRunning && StopOnOtherKeyCheckBox.IsChecked == true && !IsCombinationKey(key))
            _press.Stop(StopReason.Keyboard);
    });

    private void OnPhysicalKeyUp(ushort key) => Ui(() =>
    {
        if (!_selecting || !MacKeyService.TryGetModifier(key, out var modifier)) return;
        _pendingModifiers &= ~modifier;
        SelectedKeyText.Text = _pendingModifiers == KeyModifiers.None ? "Press a key..." : ModifierName(_pendingModifiers) + " + ...";
        StatusText.Text = _pendingModifiers == KeyModifiers.None ? "Waiting for a key" : "Now press the main key";
    });

    private bool IsCombinationKey(ushort key) => key == _settings.VirtualKey || MacKeyService.TryGetModifier(key, out var modifier) && _settings.Modifiers.HasFlag(modifier);
    private static string ModifierName(KeyModifiers m) => string.Join(" + ", new[] { m.HasFlag(KeyModifiers.Control) ? "Ctrl" : null, m.HasFlag(KeyModifiers.Shift) ? "Shift" : null, m.HasFlag(KeyModifiers.Alt) ? "Option" : null }.Where(x => x is not null));

    private void Start_Click(object? sender, RoutedEventArgs e)
    {
        if (!TryOptions(out var options)) return;
        CounterText.Text = "0";
        if (_press.Start(options))
        {
            ConfigurationPanel.IsEnabled = false;
            StartButton.IsEnabled = false;
            StopButton.IsEnabled = true;
        }
    }

    private void Stop_Click(object? sender, RoutedEventArgs e) => _press.Stop(StopReason.Manual);

    private bool TryOptions(out AutoPressOptions options)
    {
        options = default!;
        if (!int.TryParse(IntervalTextBox.Text, out var interval) || !AutoPressRules.TryValidateInterval(interval, IntervalUnitCombo.SelectedIndex == 1, out var intervalMs)) return Error("The interval must be at least 50 ms.");
        if (!int.TryParse(DeviationTextBox.Text, out var deviation) || deviation < 0 || deviation >= intervalMs) return Error("Deviation must be lower than the interval.");
        if (!int.TryParse(StartDelayTextBox.Text, out var startDelay) || startDelay < 0 || startDelay > 3600) return Error("Start delay must be between 0 and 3600 seconds.");
        var mode = (LimitMode)LimitModeCombo.SelectedIndex;
        var count = 1;
        var duration = TimeSpan.FromSeconds(1);
        if (mode == LimitMode.PressCount && (!int.TryParse(LimitValueTextBox.Text, out count) || count < 1)) return Error("The press limit must be a positive whole number.");
        if (mode == LimitMode.Duration)
        {
            if (!int.TryParse(LimitValueTextBox.Text, out var value) || value < 1) return Error("The time limit must be a positive whole number.");
            duration = LimitTimeUnitCombo.SelectedIndex == 1 ? TimeSpan.FromSeconds(value) : TimeSpan.FromMilliseconds(value);
            if (!AutoPressRules.IsDurationValid(duration, intervalMs)) return Error("The time limit must be at least as long as the interval.");
        }
        _settings.Interval = interval;
        _settings.IntervalInSeconds = IntervalUnitCombo.SelectedIndex == 1;
        _settings.RandomDeviationEnabled = RandomCheckBox.IsChecked == true;
        _settings.RandomDeviationMs = deviation;
        _settings.StartDelaySeconds = startDelay;
        _settings.LimitMode = mode;
        _settings.PressCountLimit = count;
        _settings.DurationLimit = LimitTimeUnitCombo.SelectedIndex == 1 ? duration.TotalSeconds : duration.TotalMilliseconds;
        _settings.DurationLimitInSeconds = LimitTimeUnitCombo.SelectedIndex == 1;
        _settings.StopOnOtherKey = StopOnOtherKeyCheckBox.IsChecked == true;
        options = new AutoPressOptions(_settings.VirtualKey, _settings.Modifiers, intervalMs, _settings.RandomDeviationEnabled, deviation, startDelay * 1000, mode, count, duration);
        return true;
    }

    private bool Error(string message) { _ = ShowMessage("Invalid input", message); return false; }

    private void ApplyStopped(StopReason reason)
    {
        StatusText.Text = reason switch { StopReason.Keyboard => "Stopped by keyboard", StopReason.Limit => "Limit reached", StopReason.Error => "Input error", _ => "Stopped" };
        ConfigurationPanel.IsEnabled = true;
        StartButton.IsEnabled = true;
        StopButton.IsEnabled = false;
        CountdownText.Text = "--";
    }

    private void NumericTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_sanitizing || sender is not TextBox box) return;
        var cleaned = new string((box.Text ?? "").Where(char.IsDigit).ToArray());
        if (cleaned == box.Text) return;
        _sanitizing = true; box.Text = cleaned; box.CaretIndex = cleaned.Length; _sanitizing = false;
    }

    private void RandomChanged(object? sender, RoutedEventArgs e) => DeviationTextBox.IsEnabled = RandomCheckBox.IsChecked == true;
    private void LimitModeChanged(object? sender, SelectionChangedEventArgs e) => UpdateConditionalControls();
    private void UpdateConditionalControls()
    {
        if (LimitValuePanel is null) return;
        LimitValuePanel.IsVisible = LimitModeCombo.SelectedIndex > 0;
        LimitTimeUnitCombo.IsVisible = LimitModeCombo.SelectedIndex == 2;
        LimitUnitText.IsVisible = LimitModeCombo.SelectedIndex == 1;
    }

    private void Coffee_Click(object? sender, RoutedEventArgs e) => Process.Start(new ProcessStartInfo("https://buycoffee.to/sosinsky") { UseShellExecute = true });

    private async Task ShowMessage(string title, string message)
    {
        var close = new Button { Content = "OK", HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center, Width = 90 };
        var dialog = new Window { Title = title, Width = 440, Height = 190, WindowStartupLocation = WindowStartupLocation.CenterOwner, Content = new StackPanel { Margin = new Thickness(24), Spacing = 20, Children = { new TextBlock { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap }, close } } };
        close.Click += (_, _) => dialog.Close();
        await dialog.ShowDialog(this);
    }

    private async void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        await _press.StopAndWaitAsync(StopReason.Closing);
        try { await _settingsService.SaveAsync(_settings); } catch { }
        _hook.Dispose();
        _press.Dispose();
    }
}
