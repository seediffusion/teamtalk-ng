using System.Reflection;
using TeamTalkNg.Core.Accessibility;

namespace TeamTalkNg.Accessibility;

public sealed class PrismatoidScreenReaderOutput : IScreenReaderOutput
{
    private readonly IDisposable context;
    private readonly IDisposable backend;
    private readonly MethodInfo speakMethod;
    private readonly MethodInfo brailleMethod;
    private readonly MethodInfo outputMethod;

    private PrismatoidScreenReaderOutput(
        IDisposable context,
        IDisposable backend,
        MethodInfo speakMethod,
        MethodInfo brailleMethod,
        MethodInfo outputMethod)
    {
        this.context = context;
        this.backend = backend;
        this.speakMethod = speakMethod;
        this.brailleMethod = brailleMethod;
        this.outputMethod = outputMethod;
    }

    public bool IsAvailable => true;

    public static bool TryCreate(out IScreenReaderOutput output)
    {
        output = new DebugScreenReaderOutput();

        Type? contextType = Type.GetType("Prismatoid.PrismContext, Prismatoid", throwOnError: false);
        if (contextType is null)
        {
            return false;
        }

        object? contextInstance = Activator.CreateInstance(contextType);
        if (contextInstance is not IDisposable context)
        {
            return false;
        }

        try
        {
            MethodInfo? acquireBestBackend = contextType.GetMethod("AcquireBestBackend", Type.EmptyTypes);
            object? backendInstance = acquireBestBackend?.Invoke(context, null);
            if (backendInstance is not IDisposable backend)
            {
                context.Dispose();
                return false;
            }

            Type backendType = backend.GetType();
            MethodInfo? speak = backendType.GetMethod("Speak", [typeof(string), typeof(bool)]);
            MethodInfo? braille = backendType.GetMethod("Braille", [typeof(string)]);
            MethodInfo? outputMethod = backendType.GetMethod("Output", [typeof(string), typeof(bool)]);

            if (speak is null || braille is null || outputMethod is null)
            {
                backend.Dispose();
                context.Dispose();
                return false;
            }

            output = new PrismatoidScreenReaderOutput(context, backend, speak, braille, outputMethod);
            return true;
        }
        catch
        {
            context.Dispose();
            return false;
        }
    }

    public void Speak(string message, bool interrupt = false)
    {
        speakMethod.Invoke(backend, [message, interrupt]);
    }

    public void Braille(string message)
    {
        brailleMethod.Invoke(backend, [message]);
    }

    public void Output(string message, bool interrupt = false)
    {
        outputMethod.Invoke(backend, [message, interrupt]);
    }

    public void Dispose()
    {
        backend.Dispose();
        context.Dispose();
    }
}
