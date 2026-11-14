namespace OpenHabitTracker.DeviceTests.Android;

[TestFixture]
public class PackagedAssetTests : AndroidTest
{
    // In the packaged app the themes are static web assets inside the APK, not files a dev server serves,
    // and trimming the assets once broke the fonts. Each theme's stylesheet must load and apply.
    [Test]
    public void Themes_EachOne_ItsStylesheetLoadsAndApplies()
    {
        ClearApp();
        LaunchApp();
        SwitchToWebView();

        OpenSidebar("bi-gear");
        IWebElement select = WaitForElement("[data-settings-step-2] select");
        string[] themes = ((IReadOnlyCollection<object>)RunScript("return [...arguments[0].options].map(o => o.value);", select)!).Cast<string>().ToArray();

        Assert.That(themes, Has.Length.EqualTo(26), "Not all 26 themes are offered");

        List<string> failed = new();

        foreach (string theme in themes)
        {
            RunScript("arguments[0].value = arguments[1]; arguments[0].dispatchEvent(new Event('change', { bubbles: true }));", select, theme);

            // Loaded: the link points at the theme, its rules are readable, and Bootstrap's variables resolve.
            bool applied = WaitUntil(() => RunScript("""
                const link = document.getElementById('theme-link');
                return link.getAttribute('href').includes('/' + arguments[0] + '/')
                    && link.sheet !== null && link.sheet.cssRules.length > 0
                    && getComputedStyle(document.documentElement).getPropertyValue('--bs-body-bg').trim() !== '';
                """, theme) is true, DefaultTimeout);

            if (!applied)
                failed.Add(theme);
        }

        Assert.That(failed, Is.Empty, "These themes did not load");
    }
}
