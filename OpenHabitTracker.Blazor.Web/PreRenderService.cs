namespace OpenHabitTracker.Blazor.Web;

public class PreRenderService : IPreRenderService
{
    public bool IsPreRendering { get; private set; }

    public PreRenderService()
    {
    }

    public PreRenderService(IHttpContextAccessor httpContextAccessor)
    {
        if (httpContextAccessor.HttpContext?.Response.HasStarted == true)
        {
            IsPreRendering = false;
        }
        else
        {
            IsPreRendering = true;
        }
    }
}