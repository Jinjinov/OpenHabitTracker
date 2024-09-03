namespace OpenHabitTracker.Blazor;

public interface IPreRenderService
{
    bool IsPreRendering { get; }
}

public class PreRenderService : IPreRenderService
{
    public bool IsPreRendering => false;
}