/// <summary>
/// Platform-agnostic font resolver interface
/// </summary>
public interface IFontResolver
{
    Font GetFont(string fontFamily, float fontSize);
    Size MeasureString(string text, Font font);
}

/// <summary>
/// Linux-specific font resolver implementation
/// </summary>
public class LinuxFontResolver : IFontResolver
{
    public Font GetFont(string fontFamily, float fontSize)
    {
        // Implement Linux font loading logic
        return new Font(fontFamily, fontSize);
    }

    public Size MeasureString(string text, Font font)
    {
        // Implement Linux-specific text measurement
        return new Size(text.Length * font.Size, font.Size);
    }
}