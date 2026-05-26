using System.Collections.ObjectModel;
using System.Windows.Input;
using TeamTalkNg.App.Services;

namespace TeamTalkNg.App.ViewModels;

public sealed class PreferencesDialogViewModel : ObservableObject
{
    private AppTheme selectedTheme;
    private bool announceChannelMessages;
    private bool announcePrivateMessages;
    private bool announceUserJoinLeave;
    private bool announceSelectionChanges;
    private bool sendAnnouncementsToBraille;

    public PreferencesDialogViewModel(AppSettings settings)
    {
        Themes =
        [
            new ThemeOptionViewModel(AppTheme.Light, "Light"),
            new ThemeOptionViewModel(AppTheme.Dark, "Dark")
        ];

        selectedTheme = settings.Theme;
        announceChannelMessages = settings.AnnounceChannelMessages;
        announcePrivateMessages = settings.AnnouncePrivateMessages;
        announceUserJoinLeave = settings.AnnounceUserJoinLeave;
        announceSelectionChanges = settings.AnnounceSelectionChanges;
        sendAnnouncementsToBraille = settings.SendAnnouncementsToBraille;

        SaveCommand = new RelayCommand(() => RequestClose?.Invoke(this, true));
        CancelCommand = new RelayCommand(() => RequestClose?.Invoke(this, false));
    }

    public event EventHandler<bool>? RequestClose;

    public ObservableCollection<ThemeOptionViewModel> Themes { get; }

    public ICommand SaveCommand { get; }

    public ICommand CancelCommand { get; }

    public AppTheme SelectedTheme
    {
        get => selectedTheme;
        set => SetProperty(ref selectedTheme, value);
    }

    public bool AnnounceChannelMessages
    {
        get => announceChannelMessages;
        set => SetProperty(ref announceChannelMessages, value);
    }

    public bool AnnouncePrivateMessages
    {
        get => announcePrivateMessages;
        set => SetProperty(ref announcePrivateMessages, value);
    }

    public bool AnnounceUserJoinLeave
    {
        get => announceUserJoinLeave;
        set => SetProperty(ref announceUserJoinLeave, value);
    }

    public bool AnnounceSelectionChanges
    {
        get => announceSelectionChanges;
        set => SetProperty(ref announceSelectionChanges, value);
    }

    public bool SendAnnouncementsToBraille
    {
        get => sendAnnouncementsToBraille;
        set => SetProperty(ref sendAnnouncementsToBraille, value);
    }

    public AppSettings ToSettings()
    {
        return new AppSettings
        {
            Theme = SelectedTheme,
            AnnounceChannelMessages = AnnounceChannelMessages,
            AnnouncePrivateMessages = AnnouncePrivateMessages,
            AnnounceUserJoinLeave = AnnounceUserJoinLeave,
            AnnounceSelectionChanges = AnnounceSelectionChanges,
            SendAnnouncementsToBraille = SendAnnouncementsToBraille
        };
    }
}
