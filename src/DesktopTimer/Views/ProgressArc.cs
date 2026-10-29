using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace DesktopTimer.Views;

/// <summary>
/// 円形ゲージの弧。真上から時計回りに、<see cref="Progress"/>（0〜1）の割合だけ描く。
/// 大きさは Width / Height で指定する（線の太さの半分だけ内側に描くので、枠からはみ出さない）。
/// </summary>
public sealed class ProgressArc : Shape
{
    public static readonly DependencyProperty ProgressProperty = DependencyProperty.Register(
        nameof(Progress), typeof(double), typeof(ProgressArc),
        new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>描く割合（0〜1）。</summary>
    public double Progress
    {
        get => (double)GetValue(ProgressProperty);
        set => SetValue(ProgressProperty, value);
    }

    protected override Geometry DefiningGeometry
    {
        get
        {
            var size = Math.Min(Width, Height);
            var progress = Math.Clamp(Progress, 0, 1);
            if (double.IsNaN(size) || progress <= 0)
            {
                return Geometry.Empty;
            }

            var radius = (size - StrokeThickness) / 2;
            var center = new Point(size / 2, size / 2);
            if (progress >= 1)
            {
                return new EllipseGeometry(center, radius, radius);
            }

            // 真上（-90度）から時計回り
            var angle = progress * 2 * Math.PI - Math.PI / 2;
            var start = new Point(center.X, center.Y - radius);
            var end = new Point(center.X + radius * Math.Cos(angle), center.Y + radius * Math.Sin(angle));

            var figure = new PathFigure { StartPoint = start, IsClosed = false };
            figure.Segments.Add(new ArcSegment(
                end, new Size(radius, radius), 0, progress > 0.5, SweepDirection.Clockwise, true));
            var geometry = new PathGeometry();
            geometry.Figures.Add(figure);
            return geometry;
        }
    }
}
