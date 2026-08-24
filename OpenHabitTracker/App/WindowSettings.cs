using YamlDotNet.Serialization;

namespace OpenHabitTracker.App;

// Desktop-only, machine-local window geometry, persisted next to the db as Window.yaml.
// Each host reads/writes in its own native units (DIPs for WPF/MAUI, pixels for Photino/WinForms).
public class WindowSettings
{
    public const string FileName = "Window.yaml";

    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }

    // null means no valid saved geometry (missing or corrupt file):
    // the caller computes its screen-relative first-run default instead.
    public static WindowSettings? Load(string path)
    {
        try
        {
            if (File.Exists(path))
                return new DeserializerBuilder().Build().Deserialize<WindowSettings>(File.ReadAllText(path));
        }
        catch
        {
        }

        return null;
    }

    public void Save(string path)
    {
        try
        {
            File.WriteAllText(path, new SerializerBuilder().Build().Serialize(this));
        }
        catch
        {
        }
    }
}
