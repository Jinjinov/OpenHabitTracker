using System.Reflection;

namespace OpenHabitTracker.Blazor;

public class AssemblyProvider : IAssemblyProvider
{
    public Assembly AppAssembly { get; } = typeof(IAssemblyProvider).Assembly;

    public Assembly[] AdditionalAssemblies { get; } = [];
}
