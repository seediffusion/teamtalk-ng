using TeamTalkNg.Core.Accessibility;

namespace TeamTalkNg.Accessibility;

public sealed class CompositeScreenReaderOutput : IScreenReaderOutput
{
    private readonly IReadOnlyList<IScreenReaderOutput> outputs;

    public CompositeScreenReaderOutput(params IScreenReaderOutput[] outputs)
    {
        this.outputs = outputs;
    }

    public bool IsAvailable => outputs.Any(output => output.IsAvailable);

    public void Speak(string message, bool interrupt = false)
    {
        foreach (IScreenReaderOutput output in outputs.Where(output => output.IsAvailable))
        {
            TryOutput(() => output.Speak(message, interrupt));
        }
    }

    public void Braille(string message)
    {
        foreach (IScreenReaderOutput output in outputs.Where(output => output.IsAvailable))
        {
            TryOutput(() => output.Braille(message));
        }
    }

    public void Output(string message, bool interrupt = false)
    {
        foreach (IScreenReaderOutput output in outputs.Where(output => output.IsAvailable))
        {
            TryOutput(() => output.Output(message, interrupt));
        }
    }

    public void Dispose()
    {
        foreach (IScreenReaderOutput output in outputs)
        {
            output.Dispose();
        }
    }

    private static void TryOutput(Action outputAction)
    {
        try
        {
            outputAction();
        }
        catch (Exception)
        {
        }
    }
}
