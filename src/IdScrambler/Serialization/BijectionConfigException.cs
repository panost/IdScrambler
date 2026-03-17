namespace IdScrambler.Serialization;

/// <summary>
/// Exception thrown when a bijection chain definition contains invalid configuration.
/// </summary>
public sealed class BijectionConfigException : Exception
{
    /// <summary>The zero-based index of the step that caused the error, or -1 if not step-specific.</summary>
    public int StepIndex { get; }

    public BijectionConfigException(string message, int stepIndex = -1)
        : base(stepIndex >= 0 ? $"Step {stepIndex}: {message}" : message)
    {
        StepIndex = stepIndex;
    }

    public BijectionConfigException(string message, int stepIndex, Exception innerException)
        : base(stepIndex >= 0 ? $"Step {stepIndex}: {message}" : message, innerException)
    {
        StepIndex = stepIndex;
    }
}
