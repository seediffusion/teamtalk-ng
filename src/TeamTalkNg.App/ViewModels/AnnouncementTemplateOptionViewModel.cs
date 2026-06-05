using TeamTalkNg.App.Services;

namespace TeamTalkNg.App.ViewModels;

public sealed class AnnouncementTemplateOptionViewModel : ObservableObject
{
    private bool isEnabled;
    private string template;

    public AnnouncementTemplateOptionViewModel(AnnouncementTemplateDefinition definition, bool isEnabled, string? template)
    {
        Definition = definition;
        this.isEnabled = isEnabled;
        this.template = string.IsNullOrWhiteSpace(template)
            ? definition.DefaultTemplate
            : template;
    }

    public AnnouncementTemplateDefinition Definition { get; }

    public string Id => Definition.Id;

    public string Name => Definition.Name;

    public string PlaceholderSummary => Definition.PlaceholderSummary;

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

    public string Template
    {
        get => template;
        set
        {
            string normalized = string.IsNullOrWhiteSpace(value)
                ? Definition.DefaultTemplate
                : value;

            if (SetProperty(ref template, normalized))
            {
                OnPropertyChanged(nameof(AccessibleName));
            }
        }
    }

    public string AccessibleName => $"{Name} announcement, {(IsEnabled ? "enabled" : "disabled")}. Placeholders: {PlaceholderSummary}. Current template: {Template}";
}
