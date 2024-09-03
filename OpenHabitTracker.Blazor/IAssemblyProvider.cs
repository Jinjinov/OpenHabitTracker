using System.Reflection;

namespace OpenHabitTracker.Blazor;

public interface IAssemblyProvider
{
    Assembly AppAssembly { get; }

    Assembly[] AdditionalAssemblies { get; }
}
