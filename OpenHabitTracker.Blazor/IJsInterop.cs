using Microsoft.AspNetCore.Components;

namespace OpenHabitTracker.Blazor;

public interface IJsInterop
{
    ValueTask ConsoleLog(string message);
    ValueTask SetMode(string mode);
    ValueTask SetLang(string lang);
    ValueTask SetTheme(string theme);
    ValueTask FocusElement(ElementReference element);
    ValueTask SetElementProperty(ElementReference element, string property, object value);
    ValueTask<T> GetElementProperty<T>(ElementReference element, string property);
    Task<Dimensions> GetWindowDimensions();
    Task<Dimensions> GetElementDimensions(ElementReference element);
    ValueTask SaveAsUTF8(string filename, string content);
    ValueTask SetCalculateAutoHeight(ElementReference element);
    ValueTask HandleTabKey(ElementReference element);
}
