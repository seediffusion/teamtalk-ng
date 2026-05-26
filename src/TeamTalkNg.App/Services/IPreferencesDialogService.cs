namespace TeamTalkNg.App.Services;

public interface IPreferencesDialogService
{
    AppSettings? ShowPreferencesDialog(AppSettings currentSettings);
}
