using System.Globalization;
using YamlDotNet.Serialization;

namespace OpenHabitTracker.App;

// Machine-local window geometry, persisted next to the db as Window.yaml.
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
    // Parsed by hand, because YamlDotNet's first use costs about 150 ms of startup on a phone,
    // and the file only ever holds the four values Save writes.
    public static WindowSettings? Load(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                WindowSettings settings = new() { X = double.NaN, Y = double.NaN, Width = double.NaN, Height = double.NaN };

                foreach (string line in File.ReadAllLines(path))
                {
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    int colon = line.IndexOf(':');

                    if (colon < 0)
                        return null;

                    double value = double.Parse(line.AsSpan(colon + 1), CultureInfo.InvariantCulture);

                    switch (line.AsSpan(0, colon))
                    {
                        case nameof(X): settings.X = value; break;
                        case nameof(Y): settings.Y = value; break;
                        case nameof(Width): settings.Width = value; break;
                        case nameof(Height): settings.Height = value; break;
                        default: return null;
                    }
                }

                // A missing value would otherwise open a zero-size window.
                if (double.IsNaN(settings.X) || double.IsNaN(settings.Y) || double.IsNaN(settings.Width) || double.IsNaN(settings.Height))
                    return null;

                return settings;
            }
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
