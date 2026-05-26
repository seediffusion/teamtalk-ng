using System.Windows;

namespace TeamTalkNg.App.Services;

public sealed class ThemeService : IThemeService
{
    public void UseTheme(AppTheme theme)
    {
        if (theme == AppTheme.Dark)
        {
            UseDarkTheme();
            return;
        }

        UseLightTheme();
    }

    public void UseLightTheme()
    {
        ApplyTheme("Themes/Light.xaml");
    }

    public void UseDarkTheme()
    {
        ApplyTheme("Themes/Dark.xaml");
    }

    private static void ApplyTheme(string resourcePath)
    {
        ResourceDictionary resources = Application.Current.Resources;
        ResourceDictionary dictionary = new()
        {
            Source = new Uri(resourcePath, UriKind.Relative)
        };

        resources.MergedDictionaries.Clear();
        resources.MergedDictionaries.Add(dictionary);
    }
}
