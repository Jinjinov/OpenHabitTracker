namespace OpenHabitTracker.DeviceTests.Android;

// The raw output of `screencap` without -p: a header of 32-bit fields (width, height, format, and on newer Android a color space),
// then 4 bytes per pixel in RGBA order.
public class ScreenPixels
{
    private readonly byte[] _bytes;
    private readonly int _offset;

    public int Width { get; }

    public int Height { get; }

    public ScreenPixels(byte[] bytes)
    {
        _bytes = bytes;

        Width = BitConverter.ToInt32(bytes, 0);
        Height = BitConverter.ToInt32(bytes, 4);

        // The header length differs between Android versions, so it is whatever precedes the pixels.
        _offset = bytes.Length - Width * Height * 4;

        if (_offset < 12 || _offset > 32)
            throw new InvalidDataException($"screencap returned {bytes.Length} bytes, which is not a {Width}x{Height} RGBA image");
    }

    public (int Red, int Green, int Blue) Pixel(int x, int y)
    {
        int index = _offset + (y * Width + x) * 4;

        return (_bytes[index], _bytes[index + 1], _bytes[index + 2]);
    }

    // Perceived brightness, 0 to 255.
    public double Luminance(int x, int y)
    {
        (int red, int green, int blue) = Pixel(x, y);

        return 0.299 * red + 0.587 * green + 0.114 * blue;
    }

    public double MaxLuminance(int left, int top, int right, int bottom)
    {
        double max = 0;

        for (int y = top; y < bottom; y++)
            for (int x = left; x < right; x++)
                max = Math.Max(max, Luminance(x, y));

        return max;
    }
}
