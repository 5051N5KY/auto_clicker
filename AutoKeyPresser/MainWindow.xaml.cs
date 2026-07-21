using System.ComponentModel;
using System.Globalization;
using System.Windows;
using AutoKeyPresser.Models;
using AutoKeyPresser.Services;

namespace AutoKeyPresser;

public partial class MainWindow : Window
{
    private readonly KeyboardHookService _keyboardHook = new();
    private readonly AutoPressService _autoPress = new(new KeyboardInputService());
    private readonly SettingsService _settingsService = new();
    private AppSettings _settings = new();
    private bool _selectingKey;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Closing += OnClosing;
        _keyboardHook.PhysicalKeyDown += OnPhysicalKeyDown;
        _autoPress.DelayStarted += () => Dispatcher.BeginInvoke(() => StatusText.Text = "Starting delay");
        _autoPress.Active += () => Dispatcher.BeginInvoke(() => StatusText.Text = "Active");
        _autoPress.NextPressInChanged += remaining => Dispatcher.BeginInvoke(() =>
            CountdownText.Text = remaining is null ? "--" : FormatCountdown(remaining.Value));
        _autoPress.Failed += ex => Dispatcher.BeginInvoke(() =>
        {
            StatusText.Text = "Key input error";
            MessageBox.Show(ex.Message, "Auto Key Presser error", MessageBoxButton.OK, MessageBoxImage.Error);
        });
        _autoPress.PressCompleted += count => Dispatcher.BeginInvoke(() => CounterText.Text = count.ToString(CultureInfo.InvariantCulture));
        _autoPress.Stopped += reason => Dispatcher.BeginInvoke(() => ApplyStoppedState(reason));
    }

    private static string FormatCountdown(TimeSpan remaining) =>
        remaining.TotalSeconds >= 10
            ? $"{remaining.TotalSeconds:0.0} s"
            : $"{remaining.TotalSeconds:0.00} s";

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        _settings = await _settingsService.LoadAsync();
        ApplySettings();
        try
        {
            _keyboardHook.Start();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Initialization error", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void ApplySettings()
    {
        SelectedKeyText.Text = _settings.KeyName;
        IntervalTextBox.Text = _settings.Interval.ToString(CultureInfo.CurrentCulture);
        IntervalUnitCombo.SelectedIndex = _settings.IntervalInSeconds ? 1 : 0;
        RandomCheckBox.IsChecked = _settings.RandomDeviationEnabled;
        DeviationTextBox.Text = _settings.RandomDeviationMs.ToString();
        StartDelayTextBox.Text = _settings.StartDelaySeconds.ToString();
        LimitModeCombo.SelectedIndex = (int)_settings.LimitMode;
        LimitValueTextBox.Text = _settings.LimitMode == LimitMode.Duration
            ? _settings.DurationLimitMinutes.ToString(CultureInfo.CurrentCulture)
            : _settings.PressCountLimit.ToString();
        StopOnOtherKeyCheckBox.IsChecked = _settings.StopOnOtherKey;
        RandomChanged(this, new RoutedEventArgs());
        LimitModeChanged(this, null!);
    }

    private void SelectKey_Click(object sender, RoutedEventArgs e)
    {
        _selectingKey = true;
        StatusText.Text = "Waiting for a key";
        SelectKeyButton.IsEnabled = false;
    }

    private void OnPhysicalKeyDown(int virtualKey) => Dispatcher.BeginInvoke(() =>
    {
        if (_selectingKey)
        {
            _settings.VirtualKey = virtualKey;
            _settings.KeyName = KeyNameService.GetName(virtualKey);
            SelectedKeyText.Text = _settings.KeyName;
            _selectingKey = false;
            SelectKeyButton.IsEnabled = true;
            StatusText.Text = "Stopped";
            return;
        }
        if (_autoPress.IsRunning && KeyboardEventClassifier.ShouldStop(virtualKey, _settings.VirtualKey, false, StopOnOtherKeyCheckBox.IsChecked == true))
            Stop(StopReason.Keyboard);
    });

    private void Start_Click(object sender, RoutedEventArgs e) => Start();
    private void Stop_Click(object sender, RoutedEventArgs e) => Stop(StopReason.Manual);

    private void Start()
    {
        if (!TryReadOptions(out var options)) return;
        CounterText.Text = "0";
        if (_autoPress.Start(options))
        {
            StatusText.Text = options.StartDelayMs > 0 ? "Starting delay" : "Active";
            ConfigurationPanel.IsEnabled = false;
            StartButton.IsEnabled = false;
            StopButton.IsEnabled = true;
        }
    }

    private bool TryReadOptions(out AutoPressOptions options)
    {
        options = default!;
        if (!double.TryParse(IntervalTextBox.Text, NumberStyles.Float, CultureInfo.CurrentCulture, out var interval) ||
            !AutoPressRules.TryValidateInterval(interval, IntervalUnitCombo.SelectedIndex == 1, out var intervalMs))
            return ShowValidation("The interval must be at least 50 ms.");
        if (!int.TryParse(DeviationTextBox.Text, out var deviation) || deviation < 0 || deviation >= intervalMs)
            return ShowValidation("Deviation must be between 0 and a value lower than the interval.");
        if (!int.TryParse(StartDelayTextBox.Text, out var delay) || delay < 0 || delay > 3600)
            return ShowValidation("Start delay must be between 0 and 3600 seconds.");

        var mode = (LimitMode)LimitModeCombo.SelectedIndex;
        var pressLimit = 1;
        var duration = TimeSpan.FromMinutes(1);
        if (mode == LimitMode.PressCount && (!int.TryParse(LimitValueTextBox.Text, out pressLimit) || pressLimit < 1))
            return ShowValidation("The press limit must be a positive whole number.");
        if (mode == LimitMode.Duration && (!double.TryParse(LimitValueTextBox.Text, NumberStyles.Float, CultureInfo.CurrentCulture, out var minutes) || minutes <= 0 || minutes > 525600))
            return ShowValidation("The time limit must be a positive number of minutes.");
        else if (mode == LimitMode.Duration)
            duration = TimeSpan.FromMinutes(double.Parse(LimitValueTextBox.Text, CultureInfo.CurrentCulture));

        ReadSettings(interval, deviation, delay, mode, pressLimit, duration);
        options = new AutoPressOptions(_settings.VirtualKey, intervalMs, RandomCheckBox.IsChecked == true, deviation, delay * 1000, mode, pressLimit, duration);
        return true;
    }

    private void ReadSettings(double interval, int deviation, int delay, LimitMode mode, int pressLimit, TimeSpan duration)
    {
        _settings.Interval = interval;
        _settings.IntervalInSeconds = IntervalUnitCombo.SelectedIndex == 1;
        _settings.RandomDeviationEnabled = RandomCheckBox.IsChecked == true;
        _settings.RandomDeviationMs = deviation;
        _settings.StartDelaySeconds = delay;
        _settings.LimitMode = mode;
        _settings.PressCountLimit = pressLimit;
        _settings.DurationLimitMinutes = duration.TotalMinutes;
        _settings.StopOnOtherKey = StopOnOtherKeyCheckBox.IsChecked == true;
    }

    private static bool ShowValidation(string message)
    {
        MessageBox.Show(message, "Invalid input", MessageBoxButton.OK, MessageBoxImage.Information);
        return false;
    }

    private void Stop(StopReason reason) => _autoPress.Stop(reason);

    private void ApplyStoppedState(StopReason reason)
    {
        StatusText.Text = reason switch
        {
            StopReason.Keyboard => "Stopped by keyboard",
            StopReason.Limit => "Limit reached",
            StopReason.Error => "Key input error",
            _ => "Stopped"
        };
        CountdownText.Text = "--";
        ConfigurationPanel.IsEnabled = true;
        StartButton.IsEnabled = true;
        StopButton.IsEnabled = false;
    }

    private void RandomChanged(object sender, RoutedEventArgs e)
    {
        if (DeviationTextBox is not null) DeviationTextBox.IsEnabled = RandomCheckBox.IsChecked == true;
    }

    private void LimitModeChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (LimitValuePanel is null) return;
        LimitValuePanel.Visibility = LimitModeCombo.SelectedIndex == 0 ? Visibility.Collapsed : Visibility.Visible;
        LimitUnitText.Text = LimitModeCombo.SelectedIndex == 2 ? " minutes" : " presses";
    }

    private async void OnClosing(object? sender, CancelEventArgs e)
    {
        CaptureSettingsBestEffort();
        await _autoPress.StopAndWaitAsync(StopReason.Closing);
        try { await _settingsService.SaveAsync(_settings); } catch { }
        _keyboardHook.Dispose();
        _autoPress.Dispose();
    }

    private void CaptureSettingsBestEffort()
    {
        if (double.TryParse(IntervalTextBox.Text, NumberStyles.Float, CultureInfo.CurrentCulture, out var interval) && interval > 0) _settings.Interval = interval;
        if (int.TryParse(DeviationTextBox.Text, out var deviation) && deviation >= 0) _settings.RandomDeviationMs = deviation;
        if (int.TryParse(StartDelayTextBox.Text, out var delay) && delay >= 0) _settings.StartDelaySeconds = delay;
        _settings.IntervalInSeconds = IntervalUnitCombo.SelectedIndex == 1;
        _settings.RandomDeviationEnabled = RandomCheckBox.IsChecked == true;
        _settings.LimitMode = (LimitMode)Math.Max(0, LimitModeCombo.SelectedIndex);
        if (_settings.LimitMode == LimitMode.PressCount && int.TryParse(LimitValueTextBox.Text, out var count) && count > 0) _settings.PressCountLimit = count;
        if (_settings.LimitMode == LimitMode.Duration && double.TryParse(LimitValueTextBox.Text, NumberStyles.Float, CultureInfo.CurrentCulture, out var minutes) && minutes > 0) _settings.DurationLimitMinutes = minutes;
        _settings.StopOnOtherKey = StopOnOtherKeyCheckBox.IsChecked == true;
    }
}
