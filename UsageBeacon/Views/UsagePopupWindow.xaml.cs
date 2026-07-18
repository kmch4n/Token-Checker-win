using System.Windows;
using System.Windows.Media;
using UsageBeacon.Localization;
using UsageBeacon.Models;
using UsageBeacon.Services;
using UsageBeacon.Utilities;
using UsageBeacon.ViewModels;
using MediaColor = System.Windows.Media.Color;

namespace UsageBeacon.Views;

public partial class UsagePopupWindow : Window
{
    private readonly UsageViewModel _vm;
    private readonly ClaudeStatusLineIntegration _claudeIntegration = new();
    private bool _pickerReady;
    private bool _transparencyPickerReady;
    private bool _languagePickerReady;
    private int _monitorIndex;
    private int _monitorTotal = 1;
    private WidgetPlacement _placement;

    public event Action? MonitorSwitchRequested;
    public event Action? PlacementSwitchRequested;

    public UsagePopupWindow(UsageViewModel vm)
    {
        _vm = vm;
        _placement = vm.WidgetPlacement;
        InitializeComponent();
        ApplySystemAppearance();
        StartupChk.IsChecked = vm.StartupEnabled;
        LocalizationService.LanguageChanged += OnLanguageChanged;
        Closed += (_, _) => LocalizationService.LanguageChanged -= OnLanguageChanged;
        vm.PropertyChanged  += (_, _) => Dispatcher.Invoke(Refresh);

        ApplyLocalization();
        Refresh();
    }

    private void OnLanguageChanged()
        => Dispatcher.Invoke(ApplyLocalization);

    private void ApplyLocalization()
    {
        RefreshBtn.ToolTip = LocalizationService.Get("TooltipRefreshNow");
        CloseBtn.ToolTip = LocalizationService.Get("TooltipClose");
        ClaudeLoginBtn.Content = LocalizationService.Get("CommonLogin");
        CodexLoginBtn.Content = LocalizationService.Get("CommonLogin");
        ClaudeLoading.Text = LocalizationService.Get("StatusLoading");
        CodexLoading.Text = LocalizationService.Get("StatusLoading");
        ClaudeFiveHourLabel.Text = LocalizationService.Get("UsageFiveHour");
        CodexFiveHourLabel.Text = LocalizationService.Get("UsageFiveHour");
        ClaudeWeeklyLabel.Text = LocalizationService.Get("UsageWeekly");
        CodexWeeklyLabel.Text = LocalizationService.Get("UsageWeekly");
        ClaudeSonnetLabel.Text = LocalizationService.Get("UsageWeeklySonnet");
        PollingIntervalLabel.Text = LocalizationService.Get("SettingsPollingInterval");
        PollingIntervalNote.Text = LocalizationService.Get("SettingsPollingNote");
        StartupLabel.Text = LocalizationService.Get("SettingsStartAtLogin");
        TransparencyLabel.Text = LocalizationService.Get("SettingsTransparency");
        MonitorLabel.Text = LocalizationService.Get("SettingsMonitor");
        PositionLabel.Text = LocalizationService.Get("SettingsPosition");
        LanguageLabel.Text = LocalizationService.Get("SettingsLanguage");
        QuitBtn.Content = LocalizationService.Get("CommonExit");

        SetupIntervalPicker();
        SetupTransparencyPicker();
        SetupLanguagePicker();
        SyncClaudeIntegrationButton();
        UpdateMonitorLabel(_monitorIndex, _monitorTotal);
        UpdatePlacementLabel(_placement);
        Refresh();
    }

    private void ApplySystemAppearance()
    {
        Resources["PrimaryText"]   = new SolidColorBrush(MediaColor.FromRgb(0x1A, 0x1A, 0x1A));
        Resources["SecondaryText"] = new SolidColorBrush(MediaColor.FromRgb(0x45, 0x45, 0x45));
        Resources["TertiaryText"]  = new SolidColorBrush(MediaColor.FromRgb(0x70, 0x70, 0x70));
        Resources["BorderBrush2"]  = new SolidColorBrush(MediaColor.FromArgb(0x35, 0x00, 0x00, 0x00));
        Resources["SurfaceBrush"] = new SolidColorBrush(
            MediaColor.FromArgb(_vm.PopupTransparency.ToAlpha(), 0xFF, 0xFF, 0xFF));

        var hover = new SolidColorBrush(MediaColor.FromArgb(0x18, 0x00, 0x00, 0x00));
        Resources["HoverBg"] = hover;
    }

    // View refresh helpers.

    private void Refresh()
    {
        RefreshClaude();
        RefreshCodex();
        RefreshFooter();
    }

    private void RefreshClaude()
    {
        var snap = _vm.Snapshot;

        if (snap.ClaudeUsage == null && snap.ClaudeError == null)
        {
            ClaudeLoading.Visibility = Visibility.Visible;
            ClaudeContent.Visibility = Visibility.Collapsed;
            ClaudeError.Visibility   = Visibility.Collapsed;
            ClaudeFreshness.Visibility = Visibility.Collapsed;
            return;
        }

        ClaudeLoading.Visibility = Visibility.Collapsed;

        if (snap.ClaudeError != null)
        {
            // Keep the last successful value visible while a retry is pending.
            var showLastValue = snap.ClaudeUsage != null;
            ClaudeContent.Visibility = showLastValue ? Visibility.Visible : Visibility.Collapsed;
            ClaudeError.Visibility   = Visibility.Visible;
            ClaudeErrorTitle.Text    = showLastValue || snap.ClaudeError.Kind == DomainErrorKind.AnthropicRateLimited
                ? LocalizationService.Get("ErrorWaitingTitle")
                : LocalizationService.Get("ErrorFailureTitle");
            ClaudeErrorMsg.Text      = snap.ClaudeError.Kind == DomainErrorKind.AnthropicRateLimited &&
                                       _vm.ClaudeNextRetryAt is { } retryAt
                ? $"{LocalizedText.DomainError(snap.ClaudeError)}\n{LocalizationService.Format("RetryNextAutomatic", retryAt.ToString("t", LocalizationService.Culture))}"
                : LocalizedText.DomainError(snap.ClaudeError);
            if (!showLastValue) return;
        }
        else
        {
            ClaudeContent.Visibility = Visibility.Visible;
            ClaudeError.Visibility   = Visibility.Collapsed;
        }
        var usage = snap.ClaudeUsage!;
        ClaudeFreshness.Visibility = Visibility.Visible;
        ClaudeFreshness.Text = snap.ClaudeFetchedAtUtc is { } fetchedAt &&
                               fetchedAt > DateTime.MinValue
            ? LocalizationService.Format(
                "UsageFetched",
                fetchedAt.ToLocalTime().ToString("g", LocalizationService.Culture),
                ClaudeSourceLabel(snap.ClaudeSource))
            : LocalizationService.Get("UsageFetchedUnknown");

        if (usage.FiveHour is { } fh)
        {
            Claude5hPanel.Visibility = Visibility.Visible;
            Claude5hBar.Value        = fh.Utilization;
            Claude5hPct.Text         = $"{fh.Percent}%";
            Claude5hPct.Foreground   = UtilBrush(fh.Utilization);
            Claude5hReset.Text       = LocalizedText.ResetTime(fh.ResetsAt);
        }
        else { Claude5hPanel.Visibility = Visibility.Collapsed; }

        if (usage.Weekly is { } w)
        {
            ClaudeWeeklyRow.Visibility = Visibility.Visible;
            ClaudeWeeklyPct.Text       = $"{w.Percent}%";
            ClaudeWeeklyPct.Foreground = UtilBrush(w.Utilization);
            ClaudeWeeklyBar.Value      = w.Utilization;
            ClaudeWeeklyReset.Text     = LocalizedText.ResetTime(w.ResetsAt);
        }
        else { ClaudeWeeklyRow.Visibility = Visibility.Collapsed; }

        if (usage.WeeklySonnet is { } ws)
        {
            ClaudeSonnetRow.Visibility = Visibility.Visible;
            ClaudeSonnetPct.Text       = $"{ws.Percent}%";
            ClaudeSonnetPct.Foreground = UtilBrush(ws.Utilization);
        }
        else { ClaudeSonnetRow.Visibility = Visibility.Collapsed; }
    }

    private void RefreshCodex()
    {
        var snap = _vm.Snapshot;

        if (snap.CodexUsage == null && snap.CodexError == null)
        {
            CodexLoading.Visibility = Visibility.Visible;
            CodexContent.Visibility = Visibility.Collapsed;
            CodexError.Visibility   = Visibility.Collapsed;
            return;
        }

        CodexLoading.Visibility = Visibility.Collapsed;

        if (snap.CodexError != null)
        {
            // Keep the last successful value visible while a retry is pending.
            var showLastValue = snap.CodexUsage != null;
            CodexContent.Visibility = showLastValue ? Visibility.Visible : Visibility.Collapsed;
            CodexError.Visibility   = Visibility.Visible;
            CodexErrorTitle.Text    = showLastValue
                ? LocalizationService.Get("ErrorWaitingTitle")
                : LocalizationService.Get("ErrorFailureTitle");
            CodexErrorMsg.Text      = LocalizedText.DomainError(snap.CodexError);
            if (!showLastValue) return;
        }
        else
        {
            CodexContent.Visibility = Visibility.Visible;
            CodexError.Visibility   = Visibility.Collapsed;
        }
        var usage = snap.CodexUsage!;

        if (usage.FiveHour is { } fh)
        {
            Codex5hPanel.Visibility = Visibility.Visible;
            Codex5hBar.Value        = fh.Utilization;
            Codex5hPct.Text         = $"{fh.Percent}%";
            Codex5hPct.Foreground   = UtilBrush(fh.Utilization);
            Codex5hReset.Text       = LocalizedText.ResetTime(fh.ResetsAt);
        }
        else { Codex5hPanel.Visibility = Visibility.Collapsed; }

        if (usage.Weekly is { } w)
        {
            CodexWeeklyRow.Visibility = Visibility.Visible;
            CodexWeeklyPct.Text       = $"{w.Percent}%";
            CodexWeeklyPct.Foreground = UtilBrush(w.Utilization);
            CodexWeeklyBar.Value      = w.Utilization;
            CodexWeeklyReset.Text     = LocalizedText.ResetTime(w.ResetsAt);
        }
        else { CodexWeeklyRow.Visibility = Visibility.Collapsed; }
    }

    private void RefreshFooter()
    {
        if (_vm.Snapshot.FetchedAt > DateTime.MinValue)
            FetchedAtLabel.Text = LocalizationService.Format(
                "FooterLastChecked",
                _vm.Snapshot.FetchedAt.ToString("T", LocalizationService.Culture));

        RefreshBtn.IsEnabled = !_vm.IsLoading;
    }

    // Settings pickers.

    private void SetupIntervalPicker()
    {
        _pickerReady = false;
        IntervalPicker.ItemsSource       = PollingIntervalExtensions.All
            .Select(p => new IntervalItem(p)).ToList();
        IntervalPicker.DisplayMemberPath = "Label";
        SyncIntervalPicker();
        _pickerReady = true;
    }

    private void SyncIntervalPicker()
    {
        var items = (IList<IntervalItem>?)IntervalPicker.ItemsSource;
        IntervalPicker.SelectedItem = items?.FirstOrDefault(i => i.Value == _vm.PollingInterval);
    }

    private void IntervalPicker_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (_pickerReady && IntervalPicker.SelectedItem is IntervalItem item)
            _vm.PollingInterval = item.Value;
    }

    private sealed record IntervalItem(PollingInterval Value)
    {
        public string Label { get; } = LocalizedText.PollingInterval(Value);
    }

    private void SetupTransparencyPicker()
    {
        _transparencyPickerReady = false;
        TransparencyPicker.ItemsSource = PopupTransparencyExtensions.All
            .Select(p => new TransparencyItem(p)).ToList();
        TransparencyPicker.DisplayMemberPath = "Label";
        var items = (IList<TransparencyItem>?)TransparencyPicker.ItemsSource;
        TransparencyPicker.SelectedItem = items?.FirstOrDefault(i => i.Value == _vm.PopupTransparency);
        _transparencyPickerReady = true;
    }

    private void TransparencyPicker_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (!_transparencyPickerReady || TransparencyPicker.SelectedItem is not TransparencyItem item) return;
        _vm.PopupTransparency = item.Value;
        ApplySystemAppearance();
    }

    private sealed record TransparencyItem(PopupTransparency Value)
    {
        public string Label { get; } = LocalizedText.PopupTransparency(Value);
    }

    private void SetupLanguagePicker()
    {
        _languagePickerReady = false;
        LanguagePicker.ItemsSource = LocalizationService.SupportedLanguages;
        LanguagePicker.DisplayMemberPath = nameof(LanguageOption.DisplayName);
        LanguagePicker.SelectedValuePath = nameof(LanguageOption.Code);
        LanguagePicker.SelectedValue = _vm.UiLanguage;
        _languagePickerReady = true;
    }

    private void LanguagePicker_SelectionChanged(
        object sender,
        System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (_languagePickerReady && LanguagePicker.SelectedItem is LanguageOption option)
            _vm.UiLanguage = option.Code;
    }

    // Event handlers.

    private async void Refresh_Click(object sender, RoutedEventArgs e)
        => await _vm.RefreshAsync(force: true);

    private void Close_Click(object sender, RoutedEventArgs e)
        => Hide();

    private void Quit_Click(object sender, RoutedEventArgs e)
    {
        if (System.Windows.MessageBox.Show(
                LocalizationService.Get("QuitPrompt"),
                LocalizationService.Get("CommonConfirm"),
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question) == System.Windows.MessageBoxResult.Yes)
            System.Windows.Application.Current.Shutdown();
    }

    private void ClaudeLogin_Click(object sender, RoutedEventArgs e)
    {
        var win = new LoginWindow("Claude Code", "claude auth login", _vm, new WindowsTokenSource());
        win.Show();
    }

    private void ClaudeIntegration_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_claudeIntegration.IsEnabled)
            {
                if (!_claudeIntegration.Disable())
                {
                    System.Windows.MessageBox.Show(
                        LocalizationService.Get("IntegrationChangedWarning"),
                        "UsageBeacon",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }
            }
            else
            {
                var result = System.Windows.MessageBox.Show(
                    LocalizationService.Get("IntegrationEnablePrompt"),
                    LocalizationService.Get("IntegrationTitle"),
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                if (result != MessageBoxResult.Yes) return;
                _claudeIntegration.Enable();
            }
            SyncClaudeIntegrationButton();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                LocalizationService.Format("IntegrationFailed", ex.Message),
                "UsageBeacon",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private void CodexLogin_Click(object sender, RoutedEventArgs e)
    {
        var win = new LoginWindow("Codex", "codex login", _vm);
        win.Show();
    }

    private void StartupChk_Changed(object sender, RoutedEventArgs e)
        => _vm.StartupEnabled = StartupChk.IsChecked == true;

    private void Monitor_Click(object sender, RoutedEventArgs e)
        => MonitorSwitchRequested?.Invoke();

    private void Placement_Click(object sender, RoutedEventArgs e)
        => PlacementSwitchRequested?.Invoke();

    // Helpers.

    private static SolidColorBrush UtilBrush(double v) => new(
        v < 0.75 ? MediaColor.FromRgb(0x4C, 0xAF, 0x50) :
        v < 0.90 ? MediaColor.FromRgb(0xFF, 0xC1, 0x07) :
                   MediaColor.FromRgb(0xF4, 0x43, 0x36));

    private static string ClaudeSourceLabel(UsageDataSource? source) => source switch
    {
        UsageDataSource.ClaudeCodeStatusLine => "Claude Code",
        UsageDataSource.OAuthApi => LocalizationService.Get("SourceUsageApi"),
        _ => LocalizationService.Get("SourceCache"),
    };

    private void SyncClaudeIntegrationButton()
    {
        ClaudeIntegrationBtn.Content = _claudeIntegration.IsEnabled
            ? LocalizationService.Get("IntegrationDisable")
            : LocalizationService.Get("IntegrationEnable");
    }

    public void UpdateMonitorLabel(int index, int total)
    {
        _monitorIndex = index;
        _monitorTotal = total;
        MonitorBtn.Content = total > 1
            ? LocalizationService.Format("MonitorIndex", index + 1, total)
            : LocalizationService.Get("MonitorSwitch");
    }

    public void UpdatePlacementLabel(WidgetPlacement placement)
    {
        _placement = placement;
        PlacementBtn.Content = placement == WidgetPlacement.Right
            ? LocalizationService.Get("PlacementRight")
            : LocalizationService.Get("PlacementLeft");
    }

}
