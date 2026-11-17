using System.Globalization;
using System.Windows;
using System.Windows.Media;
using NetSpeedMonitor.Core.Formatting;

namespace NetSpeedMonitor.App.Controls;

/// <summary>Lightweight real-time download/upload line graph (last 120 one-second samples).</summary>
public sealed class SpeedGraph : FrameworkElement
{
    private const int Capacity = 120;
    private readonly double[] _down = new double[Capacity];
    private readonly double[] _up = new double[Capacity];
    private int _count;
    private long _lastPush;

    public SpeedGraph()
    {
        FlowDirection = FlowDirection.LeftToRight;
        MinHeight = 120;
    }

    public void AddSample(double down, double up)
    {
        // one point per second no matter the refresh interval, so the graph always spans 2 minutes
        long now = System.Diagnostics.Stopwatch.GetTimestamp();
        if (_count > 0 && (now - _lastPush) / (double)System.Diagnostics.Stopwatch.Frequency < 0.95) return;
        _lastPush = now;

        if (_count == Capacity)
        {
            Array.Copy(_down, 1, _down, 0, Capacity - 1);
            Array.Copy(_up, 1, _up, 0, Capacity - 1);
            _count--;
        }
        _down[_count] = down;
        _up[_count] = up;
        _count++;

        if (IsVisible) InvalidateVisual();
    }

    protected override void OnRender(DrawingContext dc)
    {
        double w = ActualWidth, h = ActualHeight;
        if (w < 10 || h < 10) return;

        var line = (Brush)(TryFindResource("LineBrush") ?? Brushes.Gray);
        var subtle = (Brush)(TryFindResource("SubtleBrush") ?? Brushes.Gray);
        var downBrush = (SolidColorBrush)(TryFindResource("DownBrush") ?? Brushes.LimeGreen);
        var upBrush = (SolidColorBrush)(TryFindResource("UpBrush") ?? Brushes.DodgerBlue);

        double peak = 0;
        for (int i = 0; i < _count; i++) peak = Math.Max(peak, Math.Max(_down[i], _up[i]));
        double max = NiceCeiling(Math.Max(peak, 100 * 1024));

        var gridPen = new Pen(line, 1) { DashStyle = DashStyles.Dot };
        gridPen.Freeze();
        for (int g = 0; g <= 4; g++)
        {
            double y = Math.Round(h * g / 4) + 0.5;
            dc.DrawLine(gridPen, new Point(0, y), new Point(w, y));
        }

        DrawSeries(dc, _down, downBrush, w, h, max);
        DrawSeries(dc, _up, upBrush, w, h, max);

        double dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        var label = new FormattedText(SpeedFormatter.FormatSpeed(max, false, 0), CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight, new Typeface("Segoe UI"), 11, subtle, dpi);
        dc.DrawText(label, new Point(4, 2));
    }

    private void DrawSeries(DrawingContext dc, double[] data, SolidColorBrush color, double w, double h, double max)
    {
        if (_count < 2) return;
        double step = w / (Capacity - 1);
        double x0 = w - (_count - 1) * step;

        var fill = new StreamGeometry();
        var stroke = new StreamGeometry();
        using (var f = fill.Open())
        using (var s = stroke.Open())
        {
            var first = new Point(x0, Y(data[0], h, max));
            f.BeginFigure(new Point(x0, h), true, true);
            f.LineTo(first, false, false);
            s.BeginFigure(first, false, false);
            for (int i = 1; i < _count; i++)
            {
                var p = new Point(x0 + i * step, Y(data[i], h, max));
                f.LineTo(p, false, false);
                s.LineTo(p, true, false);
            }
            f.LineTo(new Point(x0 + (_count - 1) * step, h), false, false);
        }
        fill.Freeze();
        stroke.Freeze();

        var fillBrush = new SolidColorBrush(Color.FromArgb(48, color.Color.R, color.Color.G, color.Color.B));
        fillBrush.Freeze();
        var pen = new Pen(color, 1.6) { LineJoin = PenLineJoin.Round };
        pen.Freeze();
        dc.DrawGeometry(fillBrush, null, fill);
        dc.DrawGeometry(null, pen, stroke);
    }

    private static double Y(double v, double h, double max) => h - Math.Min(1, v / max) * (h - 4) - 1;

    private static double NiceCeiling(double v)
    {
        double mag = Math.Pow(2, Math.Ceiling(Math.Log2(v)));
        return mag;
    }
}
