namespace TeamTalkNg.App.Services;

public interface IThemeService
{
    void UseTheme(AppTheme theme);

    void UseLightTheme();

    void UseDarkTheme();
}
