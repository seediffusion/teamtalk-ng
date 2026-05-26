using System.Diagnostics;
using TeamTalkNg.Core.Accessibility;

namespace TeamTalkNg.Accessibility;

public sealed class DebugScreenReaderOutput : IScreenReaderOutput
{
    public bool IsAvailable => true;

    public void Speak(string message, bool interrupt = false)
    {
        Debug.WriteLine($"Screen reader speech{(interrupt ? " interrupting" : string.Empty)}: {message}");
    }

    public void Braille(string message)
    {
        Debug.WriteLine($"Screen reader braille: {message}");
    }

    public void Output(string message, bool interrupt = false)
    {
        Speak(message, interrupt);
        Braille(message);
    }

    public void Dispose()
    {
    }
}
