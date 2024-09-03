using OpenHabitTracker.Data;

namespace OpenHabitTracker.Blazor;

public class RuntimeData(JsInterop jsInterop, IPreRenderService preRenderService) : IRuntimeData
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
