using MidnightMetroEditor.Services;

namespace MidnightMetroEditor.Presentation;

public sealed class EditorBreadcrumbBar : Panel
{
    readonly FlowLayoutPanel _flow;
    readonly Button _backButton;
    readonly Button _forwardButton;
    readonly Label _pathLabel;
    string _rootLabel = "Save";

    public event Action<string>? SegmentClicked;

    public EditorBreadcrumbBar()
    {
        Dock = DockStyle.Top;
        Height = 40;
        Padding = new Padding(10, 6, 10, 6);
        EditorTheme.StylePanel(this, elevated: true);

        _backButton = EditorTheme.CreateChromeButton("◀", 32);
        _forwardButton = EditorTheme.CreateChromeButton("▶", 32);
        _backButton.Click += (_, _) => BackRequested?.Invoke();
        _forwardButton.Click += (_, _) => ForwardRequested?.Invoke();

        _pathLabel = new Label
        {
            AutoSize = true,
            Text = "Navigate the save structure",
            Padding = new Padding(8, 6, 0, 0)
        };
        EditorTheme.StyleLabel(_pathLabel, muted: true);

        _flow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            WrapContents = false,
            AutoScroll = true,
            Padding = new Padding(0),
            Margin = new Padding(0)
        };
        _flow.BackColor = PanelElevated;

        var left = new FlowLayoutPanel
        {
            AutoSize = true,
            WrapContents = false,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };
        left.BackColor = PanelElevated;
        left.Controls.Add(_backButton);
        left.Controls.Add(_forwardButton);

        var host = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };
        host.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        host.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        host.Controls.Add(left, 0, 0);
        host.Controls.Add(_flow, 1, 0);
        Controls.Add(host);
    }

    static Color PanelElevated => EditorTheme.PanelElevated;

    public event Action? BackRequested;
    public event Action? ForwardRequested;

    public void SetRootLabel(string label) => _rootLabel = string.IsNullOrWhiteSpace(label) ? "Save" : label.Trim();

    public void SetPath(IReadOnlyList<(string viewName, string label)> segments)
    {
        _flow.SuspendLayout();
        _flow.Controls.Clear();

        var rootButton = EditorTheme.CreateBreadcrumbButton(_rootLabel);
        rootButton.ForeColor = segments.Count == 0 ? EditorTheme.Accent : EditorTheme.TextSecondary;
        rootButton.Click += (_, _) => SegmentClicked?.Invoke("overview");
        _flow.Controls.Add(rootButton);

        for (var i = 0; i < segments.Count; i++)
        {
            _flow.Controls.Add(MakeSeparator());
            var (viewName, label) = segments[i];
            var button = EditorTheme.CreateBreadcrumbButton(label);
            button.ForeColor = i == segments.Count - 1 ? EditorTheme.Accent : EditorTheme.TextSecondary;
            var captured = viewName;
            button.Click += (_, _) => SegmentClicked?.Invoke(captured);
            _flow.Controls.Add(button);
        }

        if (segments.Count == 0)
            _flow.Controls.Add(_pathLabel);

        _flow.ResumeLayout();
    }

    public void SetNavState(bool canBack, bool canForward)
    {
        _backButton.Enabled = canBack;
        _forwardButton.Enabled = canForward;
        _backButton.ForeColor = canBack ? EditorTheme.TextPrimary : EditorTheme.TextMuted;
        _forwardButton.ForeColor = canForward ? EditorTheme.TextPrimary : EditorTheme.TextMuted;
    }

    static Label MakeSeparator() => new()
    {
        AutoSize = true,
        Text = "›",
        ForeColor = EditorTheme.TextMuted,
        Padding = new Padding(2, 6, 2, 0),
        Margin = new Padding(0)
    };
}

public sealed class EditorFilterBar : Panel
{
    readonly Label _filterLabel;
    readonly TextBox _filterBox;
    readonly ComboBox _sourceCombo;
    readonly Label _countLabel;

    public EditorFilterBar()
    {
        Dock = DockStyle.Bottom;
        Height = 40;
        Padding = new Padding(10, 6, 10, 6);
        EditorTheme.StylePanel(this, elevated: true);

        _filterLabel = new Label
        {
            Text = "Filter",
            AutoSize = true,
            Padding = new Padding(0, 6, 8, 0)
        };
        EditorTheme.StyleLabel(_filterLabel, muted: true);

        _filterBox = new TextBox
        {
            Width = 280,
            PlaceholderText = "Type to filter rows…"
        };
        EditorTheme.StyleTextBox(_filterBox);

        _sourceCombo = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 170,
            Visible = false
        };
        EditorTheme.StyleComboBox(_sourceCombo);

        _countLabel = new Label
        {
            AutoSize = true,
            Padding = new Padding(12, 6, 0, 0)
        };
        EditorTheme.StyleLabel(_countLabel, muted: true);

        var flow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            WrapContents = false
        };
        flow.BackColor = EditorTheme.PanelElevated;
        flow.Controls.Add(_filterLabel);
        flow.Controls.Add(_filterBox);
        flow.Controls.Add(_sourceCombo);
        flow.Controls.Add(_countLabel);
        Controls.Add(flow);
    }

    public TextBox FilterBox => _filterBox;
    public ComboBox SourceCombo => _sourceCombo;
    public Label CountLabel => _countLabel;

    public void Configure(EditorFilterMode mode, string? placeholder = null)
    {
        _filterBox.PlaceholderText = placeholder ?? mode switch
        {
            EditorFilterMode.Citizens => "Filter citizens by name, roster id, occupation…",
            EditorFilterMode.Grid => "Filter visible rows…",
            EditorFilterMode.Json => "Filter JSON keys and values…",
            EditorFilterMode.None => "Filter not available in this view",
            _ => "Type to filter rows…"
        };

        _filterBox.Enabled = mode != EditorFilterMode.None;
        _sourceCombo.Visible = mode == EditorFilterMode.Citizens;
        if (mode == EditorFilterMode.None)
            _filterBox.Clear();
    }
}

public enum EditorFilterMode
{
    None,
    Citizens,
    Grid,
    Json
}

public static class EditorGridFilter
{
    public static void Apply(DataGridView grid, string query)
    {
        query = query.Trim();
        foreach (DataGridViewRow row in grid.Rows)
        {
            if (string.IsNullOrEmpty(query))
            {
                row.Visible = true;
                continue;
            }

            var visible = false;
            foreach (DataGridViewCell cell in row.Cells)
            {
                var text = cell.Value?.ToString() ?? "";
                if (text.Contains(query, StringComparison.OrdinalIgnoreCase))
                {
                    visible = true;
                    break;
                }
            }

            row.Visible = visible;
        }
    }
}
