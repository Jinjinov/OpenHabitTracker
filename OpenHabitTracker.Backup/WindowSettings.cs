using YamlDotNet.Serialization;

namespace OpenHabitTracker.Backup;

// Desktop-only, machine-local window geometry, persisted next to the db as Window.yaml.
// Each host reads/writes in its own native units (DIPs for WPF/MAUI, pixels for Photino/WinForms).
public class WindowSettings
{
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
            if (System.IO.File.Exists(path))
                return new DeserializerBuilder().Build().Deserialize<WindowSettings>(System.IO.File.ReadAllText(path));
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
            System.IO.File.WriteAllText(path, new SerializerBuilder().Build().Serialize(this));
        }
        catch
        {
        }
    }
}
