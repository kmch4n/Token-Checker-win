using System.Diagnostics;
using System.Windows;
using UsageBeacon.Localization;
using UsageBeacon.Models;
using UsageBeacon.Services;
using UsageBeacon.Utilities;
using UsageBeacon.ViewModels;

namespace UsageBeacon.Views;

public partial class LoginWindow : Window
{
    private readonly string _cliCommand;
    private readonly string _service;
    private readonly WindowsTokenSource? _tokenSource;
    private readonly UsageViewModel _vm;
    private string? _tokenSnapshot;
    private CancellationTokenSource? _pollCts;
    private string? _statusKey;
    private object?[] _statusArguments = [];

    public LoginWindow(string service, string cliCommand, UsageViewModel vm, WindowsTokenSource? tokenSource = null)
    {
        _service = service;
        _cliCommand  = cliCommand;
        _tokenSource = tokenSource;
        _vm          = vm;

        InitializeComponent();
        LocalizationService.LanguageChanged += OnLanguageChanged;
        Closed += (_, _) => LocalizationService.LanguageChanged -= OnLanguageChanged;
        ApplyLocalization();

        Loaded += async (_, _) =>
        {
            WindowEffects.Apply(this);
            await SnapshotCurrentTokenAsync();
        };
    }

    private void OnLanguageChanged()
        => Dispatcher.Invoke(ApplyLocalization);

    private void ApplyLocalization()
    {
        Title = LocalizationService.Get("LoginWindowTitle");
        TitleLabel.Text = LocalizationService.Format("LoginTitle", _service);
        OpenTerminalBtn.Content = LocalizationService.Format("LoginOpenBrowser", _cliCommand);
        DescLabel.Text = LocalizationService.Get("LoginDescription");
        CopyCommandBtn.Content = LocalizationService.Get("LoginCopyCommand");
        CancelBtn.Content = LocalizationService.Get("CommonCancel");
        DoneBtn.Content = LocalizationService.Get("CommonLoginComplete");
        if (_statusKey != null)
            StatusLabel.Text = LocalizationService.Format(_statusKey, _statusArguments);
    }

    // Initialization.

    private async Task SnapshotCurrentTokenAsync()
    {
        if (_tokenSource == null) return;
        try { _tokenSnapshot = await _tokenSource.ReadAccessTokenAsync(); }
        catch { _tokenSnapshot = null; }
    }

    // Button handlers.

    private void OpenTerminal_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo("cmd.exe", $"/k {_cliCommand}")
            {
                UseShellExecute = true,
            });
        }
        catch
        {
            ShowStatus("LoginTerminalFailed");
            return;
        }

        // Release topmost mode so it does not obstruct the browser or terminal flow.
        Topmost = false;

        OpenTerminalBtn.IsEnabled = false;
        DoneBtn.IsEnabled         = true;
        ShowStatus("LoginBrowserPending");

        if (_tokenSource != null)
            _ = PollForNewTokenAsync();
    }

    private void OpenWsl_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Load both .bashrc and .profile so the CLI is available on the WSL PATH.
            // cmd.exe does not interpret the single quotes, so WSL receives them unchanged.
            Process.Start(new ProcessStartInfo("cmd.exe", $"/k wsl -- bash -il -c '{_cliCommand}'")
            {
                UseShellExecute = true,
            });
        }
        catch
        {
            ShowStatus("LoginWslFailed");
            return;
        }

        Topmost = false;
        OpenTerminalBtn.IsEnabled = false;
        OpenWslBtn.IsEnabled      = false;
        DoneBtn.IsEnabled         = true;
        ShowStatus("LoginWslPending");

        if (_tokenSource != null)
            _ = PollForNewTokenAsync();
    }

    private void CopyCommand_Click(object sender, RoutedEventArgs e)
    {
        try { System.Windows.Clipboard.SetText(_cliCommand); }
        catch { }
        ShowStatus("LoginCommandCopied", _cliCommand);
        DoneBtn.IsEnabled = true;
    }

    private void Done_Click(object sender, RoutedEventArgs e)
    {
        _pollCts?.Cancel();
        _ = _vm.RefreshAsync(force: true);
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        _pollCts?.Cancel();
        Close();
    }

    // Token polling.

    private async Task PollForNewTokenAsync()
    {
        _pollCts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        var ct = _pollCts.Token;

        while (!ct.IsCancellationRequested)
        {
            try { await Task.Delay(3000, ct); }
            catch (OperationCanceledException) { break; }

            try
            {
                var token = await _tokenSource!.ReadAccessTokenAsync(ct);
                if (token != _tokenSnapshot)
                {
                    Dispatcher.Invoke(() => ShowStatus("LoginSucceeded"));
                    await Task.Delay(1400, ct);
                    Dispatcher.Invoke(() =>
                    {
                        _ = _vm.RefreshAsync(force: true);
                        Close();
                    });
                    return;
                }
            }
            catch (DomainError e) when (e.Kind == DomainErrorKind.TokenMissing) { }
            catch (OperationCanceledException) { break; }
            catch { }
        }
    }

    // Helpers.

    private void ShowStatus(string key, params object?[] arguments)
    {
        _statusKey = key;
        _statusArguments = arguments;
        StatusLabel.Text = LocalizationService.Format(key, arguments);
        StatusArea.Visibility = Visibility.Visible;
    }
}
