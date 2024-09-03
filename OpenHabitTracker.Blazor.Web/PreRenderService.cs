namespace OpenHabitTracker.Blazor.Web;

public class PreRenderService(IHttpContextAccessor httpContextAccessor) : IPreRenderService
{
    public bool IsPreRendering { get; private init; } = !(httpContextAccessor.HttpContext?.Response.HasStarted == true);
}