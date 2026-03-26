using MAAUnified.App.ViewModels.Infrastructure;

namespace MAAUnified.App.ViewModels.Settings;

public enum HotkeyScopePresentation
{
    Global = 0,
    WindowScoped = 1,
    Unsupported = 2,
}

public sealed class HotkeySettingItemViewModel : ObservableObject
{
    private string _title = string.Empty;
    private string _gestureStorage = string.Empty;
    private string _displayGesture = string.Empty;
    private string _scopeLabel = string.Empty;
    private string _warningMessage = string.Empty;
    private string _errorMessage = string.Empty;
    private string _unboundText = "Unbound";
    private string _capturePromptText = "Press shortcut...";
    private string _recordText = "Record";
    private string _reRecordText = "Re-record";
    private string _capturingText = "Listening...";
    private string _clearText = "Clear";
    private bool _isCapturing;
    private HotkeyScopePresentation _scopeKind = HotkeyScopePresentation.Unsupported;

    public HotkeySettingItemViewModel(string name)
    {
        Name = name;
    }

    public string Name { get; }

    public string Title
    {
        get => _title;
        private set => SetProperty(ref _title, value);
    }

    public string GestureStorage
    {
        get => _gestureStorage;
        private set
        {
            if (SetProperty(ref _gestureStorage, value))
            {
                OnPropertyChanged(nameof(IsBound));
                OnPropertyChanged(nameof(InputText));
                OnPropertyChanged(nameof(CaptureButtonText));
            }
        }
    }

    public string DisplayGesture
    {
        get => _displayGesture;
        private set
        {
            if (SetProperty(ref _displayGesture, value))
            {
                OnPropertyChanged(nameof(InputText));
            }
        }
    }

    public bool IsBound => !string.IsNullOrWhiteSpace(GestureStorage);

    public bool IsCapturing
    {
        get => _isCapturing;
        private set
        {
            if (SetProperty(ref _isCapturing, value))
            {
                OnPropertyChanged(nameof(InputText));
                OnPropertyChanged(nameof(CaptureButtonText));
            }
        }
    }

    public string InputText => IsCapturing
        ? _capturePromptText
        : IsBound
            ? DisplayGesture
            : _unboundText;

    public string CaptureButtonText => IsCapturing
        ? _capturingText
        : IsBound
            ? _reRecordText
            : _recordText;

    public string ClearButtonText
    {
        get => _clearText;
        private set => SetProperty(ref _clearText, value);
    }

    public HotkeyScopePresentation ScopeKind => _scopeKind;

    public string ScopeLabel
    {
        get => _scopeLabel;
        private set => SetProperty(ref _scopeLabel, value);
    }

    public string WarningMessage
    {
        get => _warningMessage;
        private set
        {
            if (SetProperty(ref _warningMessage, value))
            {
                OnPropertyChanged(nameof(HasWarningMessage));
            }
        }
    }

    public bool HasWarningMessage => !string.IsNullOrWhiteSpace(WarningMessage);

    public string ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            if (SetProperty(ref _errorMessage, value))
            {
                OnPropertyChanged(nameof(HasErrorMessage));
            }
        }
    }

    public bool HasErrorMessage => !string.IsNullOrWhiteSpace(ErrorMessage);

    public void UpdateLocalization(
        string title,
        string unboundText,
        string capturePromptText,
        string recordText,
        string reRecordText,
        string capturingText,
        string clearText,
        string scopeLabel)
    {
        Title = title;
        _unboundText = unboundText;
        _capturePromptText = capturePromptText;
        _recordText = recordText;
        _reRecordText = reRecordText;
        _capturingText = capturingText;
        ClearButtonText = clearText;
        ScopeLabel = scopeLabel;
        OnPropertyChanged(nameof(InputText));
        OnPropertyChanged(nameof(CaptureButtonText));
    }

    public void SetGesture(string gestureStorage, string displayGesture)
    {
        GestureStorage = gestureStorage;
        DisplayGesture = displayGesture;
    }

    public void SetScope(HotkeyScopePresentation scopeKind, string scopeLabel)
    {
        _scopeKind = scopeKind;
        ScopeLabel = scopeLabel;
    }

    public void BeginCapture()
    {
        WarningMessage = string.Empty;
        ErrorMessage = string.Empty;
        IsCapturing = true;
    }

    public void EndCapture()
    {
        IsCapturing = false;
    }

    public void SetWarning(string message)
    {
        WarningMessage = message;
    }

    public void SetError(string message)
    {
        ErrorMessage = message;
    }

    public void ClearFeedback()
    {
        WarningMessage = string.Empty;
        ErrorMessage = string.Empty;
    }
}
