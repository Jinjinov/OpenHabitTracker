using System.Reflection;

namespace Ididit.Blazor;

public interface IAssemblyProvider
{
    Assembly AppAssembly { get; }

    Assembly[] AdditionalAssemblies { get; }
}
