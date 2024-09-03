using OpenHabitTracker.Data;

namespace OpenHabitTracker.Blazor;

public class RuntimeData(JsInterop jsInterop) : IRuntimeData
{
    private readonly JsInterop _jsInterop = jsInterop;
    private int? _windowWidth;

    public async Task<int> GetWindowWidth()
    {
        if (!_windowWidth.HasValue)
        {
            Dimensions windowDimensions = await _jsInterop.GetWindowDimensions();
            _windowWidth = windowDimensions.Width;
        }

        return _windowWidth.Value;
    }
}
