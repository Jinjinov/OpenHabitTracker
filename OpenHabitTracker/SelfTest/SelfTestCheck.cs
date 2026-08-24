namespace OpenHabitTracker.SelfTest;

/// <summary>
/// One named check. Returning normally is a pass, and the returned string is optional detail.
/// Throwing is a failure, and the exception message is the reason.
/// </summary>
public sealed class SelfTestCheck(string name, Func<Task<string>> run)
{
    public string Name { get; } = name;

    public Func<Task<string>> Run { get; } = run;
}
