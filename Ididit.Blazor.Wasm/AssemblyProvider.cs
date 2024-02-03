using System.Reflection;

namespace Ididit.Blazor.Wasm;

public class AssemblyProvider : IAssemblyProvider
{
    public Assembly AppAssembly { get; } = typeof(IAssemblyProvider).Assembly;

    public Assembly[] AdditionalAssemblies { get; } = [typeof(AssemblyProvider).Assembly];
}
