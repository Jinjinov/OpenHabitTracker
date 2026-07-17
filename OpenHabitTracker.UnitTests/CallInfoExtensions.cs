using NSubstitute.Core;

namespace OpenHabitTracker.UnitTests;

internal static class CallInfoExtensions
{
    // NSubstitute 6 annotates CallInfo.Arg<T>() as T? because a substitute can receive null.
    // The mocks in these tests never expect null - fail loudly instead of propagating it.
    internal static T RequiredArg<T>(this CallInfo callInfo) where T : class =>
        callInfo.Arg<T>() ?? throw new InvalidOperationException($"Substitute received a null {typeof(T).Name} argument.");
}
