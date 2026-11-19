using ObjCRuntime;
using System.Runtime.InteropServices;
using UIKit;

namespace OpenHabitTracker.Blazor.Maui;

// WebKit opens <input type="time"> in a UIDatePicker it sets to the wheels style, and the Mac idiom forbids the wheels:
// UIKit throws _UICatalystUnsupportedMacIdiomBehavior and the app terminates. WebKit offers no way to choose the style,
// so under the Mac idiom UIDatePicker's public setPreferredDatePickerStyle: is replaced by one that turns wheels into inline,
// which the Mac idiom supports. Every other style, and every other idiom, is passed through unchanged.
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

    public static void UseInlineInsteadOfWheels()
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
        if (style == (nint)UIDatePickerStyle.Wheels)
            style = (nint)UIDatePickerStyle.Inline;

        _original!(self, selector, style);
    }
}
