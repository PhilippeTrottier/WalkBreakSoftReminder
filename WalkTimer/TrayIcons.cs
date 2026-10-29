using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace WalkTimer;

// Create the small set of animation frames once, and release all icon handles on exit.
internal sealed class TrayIcons : IDisposable
{
    public Icon Seated { get; } = Create(Color.SlateGray);
    public Icon NearlyDone { get; } = Create(Color.DodgerBlue);
    public Icon[] Warning { get; } = CreatePulse(Color.Red);
    public Icon[] Walking { get; } = CreatePulse(Color.Gold);

    private static Icon[] CreatePulse(Color color) => Enumerable.Range(0, 20)
        .Select(frame =>
        {
            var white = (1 - Math.Cos(frame * 2 * Math.PI / 20)) / 2;
            return Create(Color.FromArgb(
                (int)(color.R + (255 - color.R) * white),
                (int)(color.G + (255 - color.G) * white),
                (int)(color.B + (255 - color.B) * white)));
        }).ToArray();

    private static Icon Create(Color color)
    {
        using var bitmap = new Bitmap(32, 32);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var fill = new SolidBrush(color);
        using var border = new Pen(Color.FromArgb(60, 60, 60), 2);
        graphics.FillEllipse(fill, 3, 3, 26, 26);
        graphics.DrawEllipse(border, 3, 3, 26, 26);

        var handle = bitmap.GetHicon();
        try
        {
            using var borrowed = Icon.FromHandle(handle);
            return (Icon)borrowed.Clone();
        }
        finally
        {
            DestroyIcon(handle);
        }
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(IntPtr handle);

    public void Dispose()
    {
        Seated.Dispose();
        NearlyDone.Dispose();
        foreach (var icon in Warning.Concat(Walking))
            icon.Dispose();
    }
}
