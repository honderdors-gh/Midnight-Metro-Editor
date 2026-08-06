using System.Drawing;

namespace MidnightMetroEditor.Presentation;

/// <summary>Dark Paradox / PDS-Unlimiter inspired palette for the save editor chrome.</summary>
public static class EditorTheme
{
    public static readonly Color Background = Color.FromArgb(30, 31, 34);
    public static readonly Color Panel = Color.FromArgb(37, 38, 43);
    public static readonly Color PanelElevated = Color.FromArgb(47, 49, 54);
    public static readonly Color Border = Color.FromArgb(61, 63, 70);
    public static readonly Color BorderStrong = Color.FromArgb(78, 81, 89);

    public static readonly Color TextPrimary = Color.FromArgb(232, 233, 236);
    public static readonly Color TextSecondary = Color.FromArgb(157, 163, 175);
    public static readonly Color TextMuted = Color.FromArgb(110, 116, 128);

    public static readonly Color Accent = Color.FromArgb(201, 162, 39);
    public static readonly Color AccentSoft = Color.FromArgb(74, 63, 32);
    public static readonly Color Selection = Color.FromArgb(58, 74, 102);
    public static readonly Color SelectionBorder = Color.FromArgb(201, 162, 39);

    public static readonly Color InputBackground = Color.FromArgb(26, 27, 30);
    public static readonly Color RowAlt = Color.FromArgb(33, 34, 38);
    public static readonly Color GridHeader = Color.FromArgb(42, 44, 49);

    public static Font UiFont { get; } = new("Segoe UI", 9f);
    public static Font UiFontSemibold { get; } = new("Segoe UI Semibold", 9f);
    public static Font MonoFont { get; } = new("Cascadia Mono", 9.75f);
    public static Font MonoFontLarge { get; } = new("Cascadia Mono", 10.25f);
    public static Font TitleFont { get; } = new("Segoe UI Semibold", 10.5f);

    public static void ApplyForm(Form form)
    {
        form.BackColor = Background;
        form.ForeColor = TextPrimary;
        form.Font = UiFont;
    }

    public static void StylePanel(Panel panel, bool elevated = false)
    {
        panel.BackColor = elevated ? PanelElevated : Panel;
        panel.ForeColor = TextPrimary;
    }

    public static void StyleLabel(Label label, bool muted = false, bool title = false)
    {
        label.ForeColor = muted ? TextSecondary : TextPrimary;
        if (title)
            label.Font = TitleFont;
    }

    public static void StyleTextBox(TextBox box, bool readOnly = false)
    {
        box.BackColor = readOnly ? Panel : InputBackground;
        box.ForeColor = TextPrimary;
        box.BorderStyle = BorderStyle.FixedSingle;
        if (!readOnly)
            box.Font = UiFont;
    }

    public static void StyleComboBox(ComboBox combo)
    {
        combo.BackColor = InputBackground;
        combo.ForeColor = TextPrimary;
        combo.FlatStyle = FlatStyle.Flat;
    }

    public static void StyleTreeView(TreeView tree, DrawTreeNodeEventHandler? drawHandler = null)
    {
        tree.BackColor = Panel;
        tree.ForeColor = TextPrimary;
        tree.BorderStyle = BorderStyle.None;
        tree.FullRowSelect = true;
        tree.ShowLines = false;
        tree.ShowRootLines = false;
        tree.ItemHeight = 24;
        tree.HideSelection = false;
        tree.ShowNodeToolTips = true;
        tree.DrawMode = TreeViewDrawMode.OwnerDrawText;
        tree.DrawNode -= Tree_DrawNode;
        if (drawHandler != null)
            tree.DrawNode += drawHandler;
        else
            tree.DrawNode += Tree_DrawNode;
    }

    public static Rectangle GetTreeNodeTextBounds(TreeView tree, Rectangle bounds)
    {
        var right = tree.ClientRectangle.Right;
        if (tree.GetNodeCount(true) * Math.Max(tree.ItemHeight, 1) > tree.ClientSize.Height)
            right -= SystemInformation.VerticalScrollBarWidth;

        var width = Math.Max(0, right - bounds.Left - 4);
        return new Rectangle(bounds.Left, bounds.Top, width, bounds.Height);
    }

    public static void DrawTreeNodeLabel(
        DrawTreeNodeEventArgs e,
        TreeView tree,
        Color fore,
        Color back,
        bool selected)
    {
        if (e.Node == null)
            return;

        using var backBrush = new SolidBrush(back);
        e.Graphics.FillRectangle(backBrush, e.Bounds);

        if (selected)
        {
            using var accentPen = new Pen(Accent, 2f);
            e.Graphics.DrawLine(accentPen, e.Bounds.Left, e.Bounds.Top, e.Bounds.Left, e.Bounds.Bottom - 1);
        }

        var textBounds = GetTreeNodeTextBounds(tree, e.Bounds);
        TextRenderer.DrawText(
            e.Graphics,
            e.Node.Text,
            tree.Font,
            textBounds,
            fore,
            TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
    }

    static void Tree_DrawNode(object? sender, DrawTreeNodeEventArgs e)
    {
        if (sender is not TreeView tree || e.Node == null)
            return;

        var selected = (e.State & TreeNodeStates.Selected) != 0;
        var hover = e.Node == tree.SelectedNode && tree.Focused;
        var back = selected ? Selection : Panel;
        var fore = selected ? TextPrimary : TextSecondary;
        if (selected && hover)
            back = Color.FromArgb(68, 86, 118);

        DrawTreeNodeLabel(e, tree, fore, back, selected);
    }

    public static void StyleDataGridView(DataGridView grid)
    {
        grid.BackgroundColor = Panel;
        grid.GridColor = Border;
        grid.BorderStyle = BorderStyle.None;
        grid.EnableHeadersVisualStyles = false;
        grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        grid.RowHeadersVisible = false;
        grid.RowTemplate.Height = 26;

        grid.DefaultCellStyle.BackColor = InputBackground;
        grid.DefaultCellStyle.ForeColor = TextPrimary;
        grid.DefaultCellStyle.SelectionBackColor = Selection;
        grid.DefaultCellStyle.SelectionForeColor = TextPrimary;
        grid.DefaultCellStyle.Padding = new Padding(8, 2, 6, 2);

        grid.AlternatingRowsDefaultCellStyle.BackColor = RowAlt;
        grid.AlternatingRowsDefaultCellStyle.ForeColor = TextPrimary;
        grid.AlternatingRowsDefaultCellStyle.SelectionBackColor = Selection;
        grid.AlternatingRowsDefaultCellStyle.SelectionForeColor = TextPrimary;

        grid.ColumnHeadersDefaultCellStyle.BackColor = GridHeader;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = TextSecondary;
        grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = GridHeader;
        grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = TextSecondary;
        grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(8, 6, 6, 6);
        grid.ColumnHeadersHeight = 30;
    }

    public static void StylePropertyGrid(PropertyGrid grid)
    {
        grid.BackColor = Panel;
        grid.ViewBackColor = InputBackground;
        grid.ViewForeColor = TextPrimary;
        grid.LineColor = Border;
        grid.CategoryForeColor = Accent;
        grid.CommandsBackColor = Panel;
        grid.CommandsForeColor = TextPrimary;
        grid.HelpBackColor = PanelElevated;
        grid.HelpForeColor = TextSecondary;
        grid.ToolbarVisible = false;
        grid.HelpVisible = false;
    }

    public static void StyleSplitContainer(SplitContainer split)
    {
        split.BackColor = Background;
        split.Panel1.BackColor = Panel;
        split.Panel2.BackColor = Panel;
    }

    public static void StyleTabControl(TabControl tabs)
    {
        tabs.Appearance = TabAppearance.FlatButtons;
        tabs.DrawMode = TabDrawMode.OwnerDrawFixed;
        tabs.SizeMode = TabSizeMode.Fixed;
        tabs.ItemSize = new Size(120, 28);
        tabs.Padding = new Point(12, 4);
        tabs.BackColor = Panel;
        tabs.DrawItem -= Tab_DrawItem;
        tabs.DrawItem += Tab_DrawItem;
    }

    static void Tab_DrawItem(object? sender, DrawItemEventArgs e)
    {
        if (sender is not TabControl tabs || e.Index < 0 || e.Index >= tabs.TabPages.Count)
            return;

        var page = tabs.TabPages[e.Index];
        var selected = tabs.SelectedIndex == e.Index;
        var back = selected ? PanelElevated : Panel;
        var fore = selected ? Accent : TextSecondary;

        using var brush = new SolidBrush(back);
        e.Graphics.FillRectangle(brush, e.Bounds);

        if (selected)
        {
            using var accentPen = new Pen(Accent, 2f);
            e.Graphics.DrawLine(accentPen, e.Bounds.Left + 4, e.Bounds.Bottom - 1, e.Bounds.Right - 4, e.Bounds.Bottom - 1);
        }

        TextRenderer.DrawText(
            e.Graphics,
            page.Text,
            tabs.Font,
            e.Bounds,
            fore,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }

    public static void StyleMenuStrip(MenuStrip menu) =>
        menu.Renderer = new DarkToolStripRenderer();

    public static void StyleToolStrip(ToolStrip strip) =>
        strip.Renderer = new DarkToolStripRenderer();

    public static void StyleStatusStrip(StatusStrip status)
    {
        status.BackColor = PanelElevated;
        status.ForeColor = TextSecondary;
        status.Renderer = new DarkToolStripRenderer();
    }

    public static Button CreateChromeButton(string text, int width = 30)
    {
        var button = new Button
        {
            Text = text,
            Width = width,
            Height = 28,
            FlatStyle = FlatStyle.Flat,
            BackColor = PanelElevated,
            ForeColor = TextPrimary,
            Margin = new Padding(0, 0, 4, 0),
            Cursor = Cursors.Hand
        };
        button.FlatAppearance.BorderColor = Border;
        button.FlatAppearance.MouseOverBackColor = Selection;
        return button;
    }

    public static Button CreateBreadcrumbButton(string text)
    {
        var button = new Button
        {
            Text = text,
            AutoSize = true,
            Height = 28,
            FlatStyle = FlatStyle.Flat,
            BackColor = PanelElevated,
            ForeColor = TextSecondary,
            Margin = new Padding(0, 0, 2, 0),
            Padding = new Padding(8, 2, 8, 2),
            Cursor = Cursors.Hand
        };
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = Selection;
        return button;
    }
}

sealed class DarkToolStripRenderer : ToolStripProfessionalRenderer
{
    public DarkToolStripRenderer() : base(new DarkColorTable()) { }

    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e) { }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        e.TextColor = e.Item.Selected ? EditorTheme.Accent : EditorTheme.TextPrimary;
        base.OnRenderItemText(e);
    }
}

sealed class DarkColorTable : ProfessionalColorTable
{
    public override Color ToolStripGradientBegin => EditorTheme.PanelElevated;
    public override Color ToolStripGradientMiddle => EditorTheme.PanelElevated;
    public override Color ToolStripGradientEnd => EditorTheme.PanelElevated;
    public override Color MenuStripGradientBegin => EditorTheme.Panel;
    public override Color MenuStripGradientEnd => EditorTheme.Panel;
    public override Color MenuBorder => EditorTheme.Border;
    public override Color MenuItemBorder => EditorTheme.Border;
    public override Color MenuItemSelected => EditorTheme.Selection;
    public override Color MenuItemSelectedGradientBegin => EditorTheme.Selection;
    public override Color MenuItemSelectedGradientEnd => EditorTheme.Selection;
    public override Color MenuItemPressedGradientBegin => EditorTheme.AccentSoft;
    public override Color MenuItemPressedGradientEnd => EditorTheme.AccentSoft;
    public override Color ToolStripDropDownBackground => EditorTheme.PanelElevated;
    public override Color ImageMarginGradientBegin => EditorTheme.PanelElevated;
    public override Color ImageMarginGradientMiddle => EditorTheme.PanelElevated;
    public override Color ImageMarginGradientEnd => EditorTheme.PanelElevated;
    public override Color SeparatorDark => EditorTheme.Border;
    public override Color SeparatorLight => EditorTheme.BorderStrong;
    public override Color StatusStripGradientBegin => EditorTheme.PanelElevated;
    public override Color StatusStripGradientEnd => EditorTheme.PanelElevated;
    public override Color ButtonSelectedBorder => EditorTheme.Accent;
    public override Color ButtonSelectedGradientBegin => EditorTheme.Selection;
    public override Color ButtonSelectedGradientEnd => EditorTheme.Selection;
    public override Color ButtonPressedGradientBegin => EditorTheme.AccentSoft;
    public override Color ButtonPressedGradientEnd => EditorTheme.AccentSoft;
}
