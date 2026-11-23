using ObjCRuntime;
using System.Runtime.InteropServices;
using UIKit;

namespace OpenHabitTracker.Blazor.Maui;

// WebKit opens <input type="time"> in a UIDatePicker it sets to the wheels style, and the Mac idiom forbids the wheels:
// UIKit throws _UICatalystUnsupportedMacIdiomBehavior and the app terminates. The check is on the picker's own traits,
// and WebKit offers no way to reach the picker, so UIDatePicker's public setPreferredDatePickerStyle: is replaced by one
// that gives a picker asked for wheels the Pad idiom first. Only that picker leaves the Mac idiom.
// Trait overrides need macOS 14; before it the picker gets the inline style, which is tiny but does not terminate the app.
public static class TimePickerStyle
{
    private delegate void SetPreferredDatePickerStyle(IntPtr self, IntPtr selector, nint style);

    [DllImport(Constants.ObjectiveCLibrary)]
    private static extern IntPtr class_getInstanceMethod(IntPtr type, IntPtr selector);

    [DllImport(Constants.ObjectiveCLibrary)]
    private static extern IntPtr method_setImplementation(IntPtr method, IntPtr implementation);

    // Kept in fields so the garbage collector never frees what the runtime calls.
    private static readonly SetPreferredDatePickerStyle _replacement = Replacement;

    private static SetPreferredDatePickerStyle? _original;

    public static void AllowWheels()
    {
        if (UIDevice.CurrentDevice.UserInterfaceIdiom != UIUserInterfaceIdiom.Mac || _original is not null)
            return;

        IntPtr method = class_getInstanceMethod(Class.GetHandle(typeof(UIDatePicker)), Selector.GetHandle("setPreferredDatePickerStyle:"));

        IntPtr original = method_setImplementation(method, Marshal.GetFunctionPointerForDelegate(_replacement));

        _original = Marshal.GetDelegateForFunctionPointer<SetPreferredDatePickerStyle>(original);
    }

    [MonoPInvokeCallback(typeof(SetPreferredDatePickerStyle))]
    private static void Replacement(IntPtr self, IntPtr selector, nint style)
    {
        if (style == (nint)(long)UIDatePickerStyle.Wheels)
        {
            if (OperatingSystem.IsMacCatalystVersionAtLeast(17) && Runtime.GetNSObject<UIDatePicker>(self) is UIDatePicker picker)
                picker.TraitOverrides.UserInterfaceIdiom = UIUserInterfaceIdiom.Pad;
            else
                style = (nint)(long)UIDatePickerStyle.Inline;
        }

        _original!(self, selector, style);
    }
}
