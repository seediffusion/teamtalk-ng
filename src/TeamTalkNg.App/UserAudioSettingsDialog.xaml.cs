using System.Windows;

namespace TeamTalkNg.App;

public partial class UserAudioSettingsDialog : Window
{
    public UserAudioSettingsDialog()
    {
        InitializeComponent();
        Loaded += (_, _) => VoiceVolumeSlider.Focus();
    }
}
