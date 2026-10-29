using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace DesktopTimer.Views;

/// <summary>
/// タスクトレイのメニュー（WinForms）を、タイマーの右クリックメニューと同じ暗い配色で描く。
/// 色は Themes/Theme.xaml と揃えている。
/// </summary>
public sealed class TrayMenuRenderer : ToolStripProfessionalRenderer
{
    private static readonly Color Background = Color.FromArgb(0x2C, 0x33, 0x37);
    private static readonly Color Stroke = Color.FromArgb(0x4B, 0x57, 0x5C);
    private static readonly Color Separator = Color.FromArgb(0x38, 0x42, 0x46);
    private static readonly Color Highlight = Color.FromArgb(0x1F, 0x5C, 0x5A);
    private static readonly Color Text = Color.FromArgb(0xEE, 0xF4, 0xF5);
    private static readonly Color DisabledText = Color.FromArgb(0x6B, 0x7B, 0x83);

    public TrayMenuRenderer()
        : base(new DarkColorTable())
    {
        RoundedEdges = false;
    }

    /// <summary>メニューに配色と余白を設定する。</summary>
    public static void Apply(ToolStrip menu)
    {
        menu.Renderer = new TrayMenuRenderer();
        menu.BackColor = Background;
        menu.ForeColor = Text;
        menu.Padding = new Padding(2, 4, 2, 4);
    }

    protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
    {
        using var brush = new SolidBrush(Background);
        e.Graphics.FillRectangle(brush, e.AffectedBounds);
    }

    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
    {
        using var pen = new Pen(Stroke);
        var rect = new Rectangle(0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
        e.Graphics.DrawRectangle(pen, rect);
    }

    protected override void OnRenderImageMargin(ToolStripRenderEventArgs e)
    {
        // 左のアイコン用の帯は描かない（背景と同じ色のまま）
    }

    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
    {
        if (!e.Item.Selected || !e.Item.Enabled)
        {
            return;
        }

        // 選択中の項目はティールの四角（角だけ少し丸める）で示す
        var rect = new Rectangle(4, 1, e.Item.Width - 8, e.Item.Height - 2);
        using var path = RoundedRect(rect, 2);
        using var brush = new SolidBrush(Highlight);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.FillPath(brush, path);
    }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        e.TextColor = e.Item.Enabled ? Text : DisabledText;
        base.OnRenderItemText(e);
    }

    protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
    {
        var y = e.Item.Height / 2;
        using var pen = new Pen(Separator);
        e.Graphics.DrawLine(pen, 8, y, e.Item.Width - 8, y);
    }

    private static GraphicsPath RoundedRect(Rectangle rect, int radius)
    {
        var d = radius * 2;
        var path = new GraphicsPath();
        path.AddArc(rect.Left, rect.Top, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Top, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.Left, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    /// <summary>上書きしていない部分（サブメニューの矢印など）の色。</summary>
    private sealed class DarkColorTable : ProfessionalColorTable
    {
        public override Color MenuBorder => Stroke;

        public override Color ToolStripDropDownBackground => Background;

        public override Color ImageMarginGradientBegin => Background;

        public override Color ImageMarginGradientMiddle => Background;

        public override Color ImageMarginGradientEnd => Background;

        public override Color MenuItemSelected => Highlight;

        public override Color MenuItemBorder => Highlight;

        public override Color SeparatorDark => Separator;

        public override Color SeparatorLight => Separator;
    }
}
