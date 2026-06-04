using System.Windows.Input;
using TeamTalkNg.App.Services;

namespace TeamTalkNg.App.ViewModels;

public sealed class SoundEventOptionViewModel : ObservableObject
{
    private bool isEnabled;
    private string fileName;
    private readonly Action<SoundEventOptionViewModel> play;

    public SoundEventOptionViewModel(
        SoundEventDefinition definition,
        bool isEnabled,
        string fileName,
        Action<SoundEventOptionViewModel> play)
    {
        Definition = definition;
        this.isEnabled = isEnabled;
        this.fileName = fileName;
        this.play = play;
        PlayCommand = new RelayCommand(() => this.play(this));
    }

    public SoundEventDefinition Definition { get; }

    public SoundEvent Event => Definition.Event;

    public string Id => Definition.Id;

    public string Name => Definition.Name;

    public string OfficialFileName => Definition.OfficialFileName;

    public ICommand PlayCommand { get; }

    public bool IsEnabled
    {
        get => isEnabled;
        set
        {
            if (SetProperty(ref isEnabled, value))
            {
                OnPropertyChanged(nameof(AccessibleName));
            }
        }
    }

    public string FileName
    {
        get => fileName;
        set
        {
            if (SetProperty(ref fileName, value))
            {
                OnPropertyChanged(nameof(AccessibleName));
            }
        }
    }

    public string AccessibleName => $"{Name}, {(IsEnabled ? "enabled" : "disabled")}, sound file {FileName}";

    public override string ToString()
    {
        return AccessibleName;
    }
}
