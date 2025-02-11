using OpenHabitTracker.App;

namespace OpenHabitTracker.Blazor;

public class RuntimeClientData(JsInterop jsInterop, IPreRenderService preRenderService) : IRuntimeClientData
{
    private readonly JsInterop _jsInterop = jsInterop;
    private readonly IPreRenderService _preRenderService = preRenderService;
    private int? _windowWidth;

    public async Task<int> GetWindowWidth()
    {
        if (!_windowWidth.HasValue && !_preRenderService.IsPreRendering)
        {
            Dimensions windowDimensions = await _jsInterop.GetWindowDimensions();
            _windowWidth = windowDimensions.Width;
        }

        return _windowWidth ?? 0;
    }
}
