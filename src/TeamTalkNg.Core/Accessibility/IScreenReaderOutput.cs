namespace TeamTalkNg.Core.Accessibility;

public interface IScreenReaderOutput : IDisposable
{
    bool IsAvailable { get; }

    void Speak(string message, bool interrupt = false);

    void Braille(string message);

    void Output(string message, bool interrupt = false);
}
