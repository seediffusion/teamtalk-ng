using System.Collections.ObjectModel;
using System.Windows.Input;

namespace TeamTalkNg.App.ViewModels;

public sealed class MoveUserDialogViewModel : ObservableObject
{
    private MoveUserDestinationViewModel? selectedDestination;

    public MoveUserDialogViewModel(string userName, IEnumerable<MoveUserDestinationViewModel> destinations)
    {
        UserName = userName;
        Destinations = new ObservableCollection<MoveUserDestinationViewModel>(
            destinations.OrderBy(destination => destination.Path, StringComparer.CurrentCultureIgnoreCase));
        selectedDestination = Destinations.FirstOrDefault();
        MoveCommand = new RelayCommand(() => RequestClose?.Invoke(this, true), () => SelectedDestination is not null);
        CancelCommand = new RelayCommand(() => RequestClose?.Invoke(this, false));
    }

    public event EventHandler<bool>? RequestClose;

    public string UserName { get; }

    public ObservableCollection<MoveUserDestinationViewModel> Destinations { get; }

    public MoveUserDestinationViewModel? SelectedDestination
    {
        get => selectedDestination;
        set
        {
            if (SetProperty(ref selectedDestination, value) && MoveCommand is RelayCommand command)
            {
                command.RaiseCanExecuteChanged();
            }
        }
    }

    public ICommand MoveCommand { get; }

    public ICommand CancelCommand { get; }
}
