using TeamTalkNg.App.Services;

namespace TeamTalkNg.App.ViewModels;

public sealed class AnnouncementTemplateOptionViewModel : ObservableObject
{
    private string template;

    public AnnouncementTemplateOptionViewModel(AnnouncementTemplateDefinition definition, string? template)
    {
        Definition = definition;
        this.template = string.IsNullOrWhiteSpace(template)
            ? definition.DefaultTemplate
            : template;
    }

    public AnnouncementTemplateDefinition Definition { get; }

    public string Id => Definition.Id;

    public string Name => Definition.Name;

    public string PlaceholderSummary => Definition.PlaceholderSummary;

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

    public string AccessibleName => $"{Name} announcement template. Placeholders: {PlaceholderSummary}. Current template: {Template}";
}
