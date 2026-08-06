using MidnightMetroEditor.Models;
using MidnightMetroEditor.Presentation;
using MidnightMetroEditor.Services;

namespace MidnightMetroEditor;

public sealed class MainForm : Form
{
    readonly SaveDocument _doc = new();
    readonly EllipsisTreeView _tree = new() { Dock = DockStyle.Fill, HideSelection = false, BorderStyle = BorderStyle.None };
    readonly Panel _contentHost = new() { Dock = DockStyle.Fill, Padding = new Padding(8) };
    readonly Label _propertyHeader = new()
    {
        Dock = DockStyle.Top,
        Height = 28,
        Text = "Properties — select a row to edit",
        TextAlign = ContentAlignment.MiddleLeft,
        Padding = new Padding(6, 4, 0, 0)
    };
    readonly PropertyGrid _propertyGrid = new()
    {
        Dock = DockStyle.Fill,
        ToolbarVisible = true,
        HelpVisible = true,
        PropertySort = PropertySort.Categorized,
        LineColor = SystemColors.ControlLight
    };
    readonly Panel _propertyPanel = new() { Dock = DockStyle.Fill, Padding = new Padding(4) };
    readonly SplitContainer _mainSplit = new() { Dock = DockStyle.Fill, SplitterDistance = 300 };
    readonly SplitContainer _rightSplit = new()
    {
        Dock = DockStyle.Fill,
        Orientation = Orientation.Horizontal,
        SplitterDistance = 360
    };
    readonly StatusStrip _status = new();
    readonly ToolStripStatusLabel _statusLabel = new() { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
    readonly DataGridView _grid = CreateGrid();
    readonly TextBox _overviewBox = CreateReadOnlyMultiline();
    readonly TextBox _rawJsonBox = new()
    {
        Dock = DockStyle.Fill,
        Multiline = true,
        ScrollBars = ScrollBars.Both,
        Font = new Font(FontFamily.GenericMonospace, 9f),
        WordWrap = false,
        AcceptsTab = true
    };
    readonly JsonTreeView _jsonTreeView = new() { Dock = DockStyle.Fill };
    Panel? _rawJsonHost;
    bool _rawJsonTextMode;
    readonly EditorBreadcrumbBar _breadcrumbBar = new();
    readonly EditorFilterBar _filterBar = new();
    readonly EditorNavigation _navigation = new();
    readonly Label _sidebarTitle = new();
    readonly Label _sidebarSubtitle = new();
    readonly Panel _propertyHeaderPanel = new();
    bool _suppressTreeSelect;
    CitizenSourceKind? _citizenSourceFilter;
    bool _suppressSourceFilterEvent;
    readonly NumericUpDown _cellX = new() { Minimum = 0, Maximum = 999, Width = 70 };
    readonly NumericUpDown _cellY = new() { Minimum = 0, Maximum = 999, Width = 70 };
    readonly Label _cellSummary = new() { AutoSize = true, Padding = new Padding(8, 6, 0, 0) };

    NestedGridView? _nestedGridView;
    List<CitizenListRow> _citizenRows = new();
    List<WorkplaceListRow> _workplaceRows = new();
    List<LotListRow> _lotRows = new();
    string _currentView = "overview";

    public MainForm()
    {
        Text = "Midnight Metro — Save Editor";
        Width = 1480;
        Height = 920;
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1024, 680);
        TryApplyWindowIcon();

        AutoScaleMode = AutoScaleMode.Font;
        EditorTheme.ApplyForm(this);

        BuildMenu();
        BuildLayout();
        ApplyEditorChrome();
        BuildTree();
        WireEvents();

        _status.Items.Add(_statusLabel);
        Controls.Add(_mainSplit);
        Controls.Add(_status);
        Controls.Add(_toolStrip);
        Controls.Add(_menuStrip);

        Load += (_, _) => ConfigureSplitters();
        Shown += (_, _) => ConfigureSplitters();
        _mainSplit.SizeChanged += (_, _) =>
        {
            if (_mainSplit.Width > 0)
                _tree.Invalidate();
        };
        TryOpenDefaultSave();
        if (string.IsNullOrEmpty(_doc.Path))
            _navigation.Navigate("overview", recordHistory: true);
    }

    MenuStrip _menuStrip = null!;
    ToolStrip _toolStrip = null!;
    ToolStripMenuItem _saveMenuItem = null!;
    ToolStripMenuItem _saveAsMenuItem = null!;
    ToolStripButton _saveToolButton = null!;

    static DataGridView CreateGrid()
    {
        var grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None,
            ScrollBars = ScrollBars.Both,
            DefaultCellStyle = { WrapMode = DataGridViewTriState.False }
        };
        EditorTheme.StyleDataGridView(grid);
        return grid;
    }

    static TextBox CreateReadOnlyMultiline() => new()
    {
        Dock = DockStyle.Fill,
        Multiline = true,
        ReadOnly = true,
        ScrollBars = ScrollBars.Vertical,
        Font = EditorTheme.MonoFontLarge,
        BorderStyle = BorderStyle.None
    };

    void BuildMenu()
    {
        var menu = new MenuStrip();
        var file = new ToolStripMenuItem("&File");
        file.DropDownItems.Add("&Open...", null, (_, _) => OpenSave());
        _saveMenuItem = new ToolStripMenuItem("&Save", null, (_, _) => SaveCurrent());
        file.DropDownItems.Add(_saveMenuItem);
        _saveAsMenuItem = new ToolStripMenuItem("Save &As...", null, (_, _) => SaveAs());
        file.DropDownItems.Add(_saveAsMenuItem);
        file.DropDownItems.Add("&Reload", null, (_, _) => ReloadCurrent());
        file.DropDownItems.Add(new ToolStripSeparator());
        file.DropDownItems.Add("Export &JSON...", null, (_, _) => ExportJson());
        file.DropDownItems.Add("Import JSON...", null, (_, _) => ImportJson());
        file.DropDownItems.Add(new ToolStripSeparator());
        file.DropDownItems.Add("E&xit", null, (_, _) => Close());

        var settings = new ToolStripMenuItem("&Settings");
        settings.DropDownItems.Add("Set &Names Database...", null, (_, _) => PickNamesPath());
        settings.DropDownItems.Add("Open Save &Folder", null, (_, _) => OpenSaveFolder());

        var tools = new ToolStripMenuItem("&Tools");
        tools.DropDownItems.Add("Reset &metro network...", null, (_, _) => ResetMetroNetwork());

        var help = new ToolStripMenuItem("&Help");
        help.DropDownItems.Add("&About", null, (_, _) =>
            MessageBox.Show(
                "Midnight Metro Save Editor\n\n" +
                "Edits Midnight Metro game saves (Unity gzip JSON) and legacy prototype saves.\n" +
                "Run scripts/sync-save-schema.ps1 when the game save version changes.\n\n" +
                "Always back up saves before editing. A .bak copy is created on save.",
                "About",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information));

        menu.Items.Add(file);
        menu.Items.Add(tools);
        menu.Items.Add(settings);
        menu.Items.Add(help);
        _menuStrip = menu;
        MainMenuStrip = menu;

        _toolStrip = new ToolStrip();
        _toolStrip.Items.Add(new ToolStripButton("Open", null, (_, _) => OpenSave()) { DisplayStyle = ToolStripItemDisplayStyle.Text });
        _saveToolButton = new ToolStripButton("Save", null, (_, _) => SaveCurrent()) { DisplayStyle = ToolStripItemDisplayStyle.Text };
        _toolStrip.Items.Add(_saveToolButton);
        _toolStrip.Items.Add(new ToolStripSeparator());
        _toolStrip.Items.Add(new ToolStripLabel("Quick day:"));
        var dayBox = new ToolStripTextBox { Width = 60 };
        dayBox.KeyDown += (_, e) =>
        {
            if (e.KeyCode != Keys.Enter) return;
            if (int.TryParse(dayBox.Text, out var day))
                ApplyQuickDay(day);
        };
        _toolStrip.Items.Add(dayBox);
        _toolStrip.Items.Add(new ToolStripButton("Apply Day", null, (_, _) =>
        {
            if (int.TryParse(dayBox.Text, out var day))
                ApplyQuickDay(day);
        }));
    }

    void ApplyQuickDay(int day)
    {
        if (_doc.IsGameSave)
        {
            if (_doc.GameFile == null) return;
            _doc.GameFile.session.day = day;
        }
        else
        {
            if (_doc.File.session == null) return;
            _doc.File.session.day = day;
        }

        MarkDirty();
        RefreshTitle();
        _overviewBox.Text = _doc.GetOverviewText() + (_doc.Path != null ? $"\r\n\r\nFile: {_doc.Path}" : "");
        _statusLabel.Text = $"Day set to {day}.";
    }

    void BuildLayout()
    {
        _sidebarTitle.Text = "MIDNIGHT METRO";
        _sidebarTitle.Dock = DockStyle.Top;
        _sidebarTitle.AutoSize = false;
        _sidebarTitle.AutoEllipsis = true;
        _sidebarTitle.Height = 26;
        _sidebarTitle.Padding = new Padding(10, 8, 6, 0);
        EditorTheme.StyleLabel(_sidebarTitle, title: true);
        _sidebarTitle.ForeColor = EditorTheme.Accent;

        _sidebarSubtitle.Text = "Save Editor";
        _sidebarSubtitle.Dock = DockStyle.Top;
        _sidebarSubtitle.AutoSize = false;
        _sidebarSubtitle.AutoEllipsis = true;
        _sidebarSubtitle.Height = 22;
        _sidebarSubtitle.Padding = new Padding(10, 0, 6, 8);
        EditorTheme.StyleLabel(_sidebarSubtitle, muted: true);

        var sidebarDivider = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = EditorTheme.Border };

        EditorTheme.StyleTreeView(_tree);

        var treePanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(4, 4, 2, 8) };
        EditorTheme.StylePanel(treePanel);
        treePanel.Controls.Add(_tree);
        treePanel.Controls.Add(sidebarDivider);
        treePanel.Controls.Add(_sidebarSubtitle);
        treePanel.Controls.Add(_sidebarTitle);

        _propertyHeaderPanel.Dock = DockStyle.Top;
        _propertyHeaderPanel.Height = 32;
        _propertyHeaderPanel.Padding = new Padding(10, 0, 0, 0);
        EditorTheme.StylePanel(_propertyHeaderPanel, elevated: true);
        _propertyHeader.Dock = DockStyle.Fill;
        _propertyHeader.TextAlign = ContentAlignment.MiddleLeft;
        EditorTheme.StyleLabel(_propertyHeader);

        _propertyHeaderPanel.Controls.Add(_propertyHeader);

        _propertyPanel.Controls.Add(_propertyGrid);
        _propertyPanel.Controls.Add(_propertyHeaderPanel);
        _propertyPanel.Padding = new Padding(0);
        EditorTheme.StylePanel(_propertyPanel);

        var contentShell = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0) };
        EditorTheme.StylePanel(contentShell);
        contentShell.Controls.Add(_contentHost);
        contentShell.Controls.Add(_filterBar);

        var editorColumn = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0) };
        EditorTheme.StylePanel(editorColumn);
        editorColumn.Controls.Add(contentShell);
        editorColumn.Controls.Add(_breadcrumbBar);

        _mainSplit.Panel1.Controls.Add(treePanel);
        _rightSplit.Panel1.Controls.Add(editorColumn);
        _rightSplit.Panel2.Controls.Add(_propertyPanel);
        _mainSplit.Panel2.Controls.Add(_rightSplit);

        EditorTheme.StyleSplitContainer(_mainSplit);
        EditorTheme.StyleSplitContainer(_rightSplit);
        EditorTheme.StylePropertyGrid(_propertyGrid);
        EditorTheme.StyleTextBox(_overviewBox, readOnly: true);
        EditorTheme.StyleTextBox(_rawJsonBox);
        _rawJsonBox.Font = EditorTheme.MonoFont;
    }

    void TryApplyWindowIcon()
    {
        try
        {
            var exeIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            if (exeIcon != null)
                Icon = (Icon)exeIcon.Clone();
        }
        catch
        {
            // optional branding asset
        }
    }

    void ApplyEditorChrome()
    {
        EditorTheme.StyleMenuStrip(_menuStrip);
        EditorTheme.StyleToolStrip(_toolStrip);
        EditorTheme.StyleStatusStrip(_status);
        _contentHost.Padding = new Padding(10, 8, 10, 4);
        EditorTheme.StylePanel(_contentHost);
    }

    void ConfigureSplitters()
    {
        try
        {
            _mainSplit.Panel1MinSize = 240;
            _mainSplit.Panel2MinSize = 320;
            if (_mainSplit.Width > 0)
            {
                var mainSplit = Math.Clamp(300, _mainSplit.Panel1MinSize, _mainSplit.Width - _mainSplit.Panel2MinSize);
                _mainSplit.SplitterDistance = mainSplit;
            }

            _rightSplit.Panel1MinSize = 120;
            _rightSplit.Panel2MinSize = 160;
            BalanceRightSplit();
        }
        catch (Exception ex)
        {
            _statusLabel.Text = $"Layout warning: {ex.Message}";
        }
    }

    void BalanceRightSplit()
    {
        var available = _rightSplit.Height;
        if (available <= _rightSplit.Panel1MinSize + _rightSplit.Panel2MinSize + _rightSplit.SplitterWidth)
            return;

        var top = (int)(available * 0.58);
        top = Math.Clamp(top, _rightSplit.Panel1MinSize, available - _rightSplit.Panel2MinSize);
        _rightSplit.SplitterDistance = top;
    }

    void BuildTree()
    {
        _tree.Nodes.Clear();
        _tree.Nodes.Add("overview", "Overview");
        _tree.Nodes.Add("session", "Session");
        _tree.Nodes.Add("budget", "Budget & Treasury");
        _tree.Nodes.Add("playerAgency", "Player Agency");
        _tree.Nodes.Add("gangs", "Gangs");
        _tree.Nodes.Add("news", "News");
        _tree.Nodes.Add("elections", "Elections");
        var crime = _tree.Nodes.Add("crime", "Gangs & Crime");
        crime.Nodes.Add("crime_gangs", "Gangs");
        crime.Nodes.Add("crime_ledger", "Criminal Ledger");
        crime.Nodes.Add("crime_cases", "Justice Cases");
        crime.Nodes.Add("crime_members", "Gang Members");
        var citizens = _tree.Nodes.Add("citizens", "Citizens");
        citizens.Nodes.Add("citizens_all", "All");
        citizens.Nodes.Add("citizens_residents", "Residents");
        citizens.Nodes.Add("citizens_agents", "Agents");
        citizens.Nodes.Add("citizens_deceased", "Deceased");
        citizens.Nodes.Add("citizens_legacy", "Legacy v1");
        _tree.Nodes.Add("cases", "Criminal Cases");
        _tree.Nodes.Add("civic", "Civic Offices");
        _tree.Nodes.Add("honor", "Honor Wall");
        _tree.Nodes.Add("grid", "Grid Cell Inspector");
        _tree.Nodes.Add("lots", "Lots");
        _tree.Nodes.Add("workplaces", "Workplaces");
        _tree.Nodes.Add("metrics", "Metrics History");
        _tree.Nodes.Add("raw", "Raw JSON");
        _tree.SelectedNode = _tree.Nodes[0];
        ApplySidebarTreeTooltips(_tree.Nodes);
    }

    static void ApplySidebarTreeTooltips(TreeNodeCollection nodes)
    {
        foreach (TreeNode node in nodes)
        {
            node.ToolTipText = node.Text;
            ApplySidebarTreeTooltips(node.Nodes);
        }
    }

    void WireEvents()
    {
        _navigation.NavigateRequested += viewName =>
        {
            _suppressTreeSelect = true;
            try
            {
                SelectTreeNode(viewName);
                ShowView(viewName);
                UpdateBreadcrumbFromView(viewName);
            }
            finally
            {
                _suppressTreeSelect = false;
            }
        };

        _breadcrumbBar.BackRequested += () => _navigation.GoBack();
        _breadcrumbBar.ForwardRequested += () => _navigation.GoForward();
        _breadcrumbBar.SegmentClicked += viewName => _navigation.Navigate(viewName);

        _tree.AfterSelect += (_, e) =>
        {
            if (_suppressTreeSelect || e.Node?.Name == null)
                return;

            _navigation.Navigate(e.Node.Name);
        };

        _grid.SelectionChanged += (_, _) => UpdatePropertyGridFromSelection();

        _grid.CellDoubleClick += (_, _) =>
        {
            if (_grid.SelectedRows.Count == 0) return;
            if (_grid.SelectedRows[0].Tag != null)
                ShowCitizenDetail(_grid.SelectedRows[0].Tag);
        };

        _propertyGrid.PropertyValueChanged += (_, _) => MarkDirty();
        _rawJsonBox.TextChanged += (_, _) =>
        {
            if (_currentView == "raw" && _rawJsonTextMode)
                MarkDirty();
        };

        _filterBar.FilterBox.TextChanged += (_, _) => ApplyActiveFilter();
        _filterBar.SourceCombo.SelectedIndexChanged += (_, _) =>
        {
            if (_suppressSourceFilterEvent) return;
            if (_currentView is not ("citizens" or "citizens_all" or "citizens_residents" or "citizens_agents" or "citizens_deceased" or "citizens_legacy"))
                return;
            if (_filterBar.SourceCombo.SelectedIndex < 0) return;
            _citizenSourceFilter = _filterBar.SourceCombo.SelectedIndex switch
            {
                0 => null,
                1 => CitizenSourceKind.Resident,
                2 => CitizenSourceKind.Agent,
                3 => CitizenSourceKind.Deceased,
                4 => CitizenSourceKind.Legacy,
                _ => null
            };
            SyncCitizenTreeSelection();
            ApplyCitizenFilter();
        };
        FormClosing += (_, e) =>
        {
            if (!ConfirmDiscard()) e.Cancel = true;
        };
    }

    void ApplyActiveFilter()
    {
        if (_nestedGridView != null)
        {
            _nestedGridView.ApplyFilter(_filterBar.FilterBox.Text);
            return;
        }

        if (_currentView is "citizens" or "citizens_all" or "citizens_residents" or "citizens_agents" or "citizens_deceased" or "citizens_legacy")
            ApplyCitizenFilter();
        else if (UsesGridFilter(_currentView))
            EditorGridFilter.Apply(_grid, _filterBar.FilterBox.Text);
        else if (_currentView == "raw" && !_rawJsonTextMode)
            _jsonTreeView.ApplyFilter(_filterBar.FilterBox.Text);
    }

    static bool UsesGridFilter(string view) => view is
        "news" or "civic" or "crime" or "crime_gangs" or "crime_ledger" or "crime_cases" or "crime_members"
        or "grid";

    void UpdateBreadcrumbFromView(string viewName)
    {
        var node = _tree.Nodes.Find(viewName, true).FirstOrDefault();
        UpdateBreadcrumb(node);
    }

    void UpdateBreadcrumb(TreeNode? node)
    {
        var segments = new List<(string viewName, string label)>();
        for (var current = node; current != null; current = current.Parent)
            segments.Insert(0, (current.Name, current.Text));

        _breadcrumbBar.SetRootLabel(GetBreadcrumbRootLabel());
        _breadcrumbBar.SetPath(segments);
        _breadcrumbBar.SetNavState(_navigation.CanGoBack, _navigation.CanGoForward);
    }

    string GetBreadcrumbRootLabel()
    {
        if (_doc.IsGameSave && _doc.GameFile != null && !string.IsNullOrWhiteSpace(_doc.GameFile.session.cityName))
            return _doc.GameFile.session.cityName.Trim();
        if (!string.IsNullOrWhiteSpace(_doc.File.session?.cityName))
            return _doc.File.session.cityName.Trim();
        return "Save";
    }

    void ConfigureFilterBarForView(string view)
    {
        if (view is "citizens" or "citizens_all" or "citizens_residents" or "citizens_agents" or "citizens_deceased" or "citizens_legacy")
            _filterBar.Configure(EditorFilterMode.Citizens);
        else if (view is "lots" or "workplaces")
            _filterBar.Configure(EditorFilterMode.Grid, "Filter lots, workplaces, households, job slots…");
        else if (UsesGridFilter(view))
            _filterBar.Configure(EditorFilterMode.Grid);
        else if (view == "raw")
            _filterBar.Configure(EditorFilterMode.Json);
        else
            _filterBar.Configure(EditorFilterMode.None);
    }

    void TryOpenDefaultSave()
    {
        var candidates = new List<string>();

        var recent = GameSavePaths.GetMostRecentSavePath();
        if (!string.IsNullOrWhiteSpace(recent))
            candidates.Add(recent);

        candidates.Add(GameSavePaths.PrimarySavePath);

        var settings = EditorSettings.Load();
        if (!string.IsNullOrWhiteSpace(settings.LastSaveDirectory))
            candidates.Add(Path.Combine(settings.LastSaveDirectory, "citysim_save.json"));

        foreach (var path in candidates)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                continue;

            try
            {
                _doc.Load(path);
                RefreshAfterLoad();
                return;
            }
            catch
            {
                // ignore auto-open failures
            }
        }
    }

    void ShowView(string view)
    {
        _currentView = view;
        _nestedGridView = null;
        ConfigureFilterBarForView(view);
        _contentHost.Controls.Clear();
        _propertyGrid.SelectedObject = null;
        _grid.DataSource = null;
        _grid.Columns.Clear();

        switch (view)
        {
            case "overview":
                ShowOverview();
                break;
            case "session":
                ShowSession();
                break;
            case "budget":
                ShowBudget();
                break;
            case "playerAgency":
                ShowPlayerAgency();
                break;
            case "gangs":
                ShowGangs();
                break;
            case "citizens":
            case "citizens_all":
            case "citizens_residents":
            case "citizens_agents":
            case "citizens_deceased":
            case "citizens_legacy":
                _citizenSourceFilter = CitizenSourceLabels.FromTreeNodeName(view);
                ShowCitizens();
                break;
            case "cases":
                ShowCases();
                break;
            case "civic":
                ShowCivicOffices();
                break;
            case "news":
                ShowNews();
                break;
            case "elections":
                ShowElections();
                break;
            case "crime":
            case "crime_gangs":
                ShowCrimeGangs();
                break;
            case "crime_ledger":
                ShowCrimeLedger();
                break;
            case "crime_cases":
                ShowCrimeCases();
                break;
            case "crime_members":
                ShowCrimeMembers();
                break;
            case "honor":
                ShowHonorWall();
                break;
            case "grid":
                ShowGridInspector();
                break;
            case "lots":
                ShowLots();
                break;
            case "workplaces":
                ShowWorkplaces();
                break;
            case "metrics":
                ShowMetrics();
                break;
            case "raw":
                ShowRawJson();
                break;
        }
    }

    void ShowOverview()
    {
        _overviewBox.BackColor = EditorTheme.InputBackground;
        _overviewBox.ForeColor = EditorTheme.TextPrimary;
        _overviewBox.Text = _doc.Path != null
            ? _doc.GetOverviewText() + $"\r\n\r\nFile: {_doc.Path}"
            : "Open a save file to begin.";
        _contentHost.Controls.Add(_overviewBox);
    }

    void ShowSession()
    {
        if (_doc.IsGameSave)
        {
            if (_doc.GameFile == null) return;
            _propertyGrid.SelectedObject = _doc.GameFile.session;

            var session = _doc.GameFile.session;
            var geoText = $"Latitude: {session.latitude:F4}°\r\nLongitude: {session.longitude:F4}°\r\n";
            if (_doc.Names.TryResolveCityPack(session.cityName, out var packLat, out var packLon, out var packLabel))
                geoText += $"City name pack: {packLabel} (center {packLat:F2}°, {packLon:F2}°)\r\n";

            _contentHost.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text =
                    "Edit session fields in the property panel.\r\n\r\n" +
                    "Common edits: day, cityName, randomSeed, treasury, latitude, longitude, metroNetworkResetPending.\r\n\r\n" +
                    geoText +
                    $"\r\nName pools: {_doc.Names.Packs.SourceSummary}",
                AutoSize = false,
                Padding = new Padding(4)
            });
            return;
        }

        EnsureSession();
        _propertyGrid.SelectedObject = _doc.File.session;
        _contentHost.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = "Edit session fields in the property panel below.\r\n\r\nCommon edits: day, cityName, randomSeed, nextCitizenRosterId.",
            AutoSize = false,
            Padding = new Padding(4)
        });
    }

    void ShowBudget()
    {
        EnsureBudget();
        _propertyGrid.SelectedObject = _doc.File.budget;
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.Controls.Add(new Label
        {
            Text = "Treasury, tax splits, staffing lists, and budget history. Select staffing/upgrades rows in the property grid expandable lists.",
            AutoSize = true,
            Padding = new Padding(0, 0, 0, 8)
        }, 0, 0);
        var quick = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = false };
        quick.Controls.Add(new Label { Text = "Quick treasury:", AutoSize = true, Padding = new Padding(0, 6, 0, 0) });
        var treasury = new NumericUpDown { Maximum = 999_999_999, Minimum = -999_999_999, DecimalPlaces = 0, Width = 140, Value = (decimal)_doc.File.budget!.balance };
        treasury.ValueChanged += (_, _) =>
        {
            _doc.File.budget!.balance = (float)treasury.Value;
            MarkDirty();
        };
        quick.Controls.Add(treasury);
        panel.Controls.Add(quick, 0, 1);
        _contentHost.Controls.Add(panel);
    }

    void ShowPlayerAgency()
    {
        EnsurePlayerAgency();
        _propertyGrid.SelectedObject = _doc.File.playerAgency;
        _contentHost.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = "Mayor action points, auto-dispatch settings, kidnap missions, and investigation clues.",
            AutoSize = false
        });
    }

    void ShowGangs()
    {
        EnsureGangs();
        var gangs = _doc.File.gangs!.gangs;
        PopulateGrid(
            gangs.Select(g => new
            {
                g.id,
                g.nameId,
                g.tier,
                g.memberCount,
                g.collectedWealth,
                hq = g.hqBuilding != null ? $"{g.hqBuilding.x},{g.hqBuilding.y}" : "",
                buildings = g.buildings.Count
            }).ToList(),
            gangs.Cast<object>().ToList());
        _contentHost.Controls.Add(WrapGridWithHint("Double-click a gang to focus it in the property panel. Edit wars/reclaims via the Gangs root in property grid."));
    }

    void ShowCitizens()
    {
        _citizenRows = _doc.IsGameSave && _doc.GameFile != null
            ? MetroGameCitizenIndex.Build(_doc.GameFile, _doc.Names)
            : CitizenIndex.Build(_doc.File, _doc.Names);
        PopulateSourceFilterCombo();
        SetSourceFilterCombo(_citizenSourceFilter);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1,
            BackColor = EditorTheme.Panel
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        layout.Controls.Add(new Label
        {
            Text = _doc.IsGameSave
                ? "Resident roster — names resolve from built-in and workshop packs. Select a row to edit; double-click for a larger editor."
                : "Legacy citizen roster. Select a row to edit; double-click for a larger editor.",
            AutoSize = true,
            MaximumSize = new Size(920, 0),
            Padding = new Padding(0, 0, 0, 8),
            ForeColor = EditorTheme.TextSecondary
        }, 0, 0);
        layout.Controls.Add(_grid, 0, 1);

        _contentHost.Controls.Add(layout);
        ApplyCitizenFilter();
    }

    void PopulateSourceFilterCombo()
    {
        _filterBar.SourceCombo.Items.Clear();
        _filterBar.SourceCombo.Items.Add($"All ({_citizenRows.Count})");
        _filterBar.SourceCombo.Items.Add($"{CitizenSourceLabels.Label(CitizenSourceKind.Resident)} ({CitizenSourceLabels.Count(_citizenRows, CitizenSourceKind.Resident)})");
        _filterBar.SourceCombo.Items.Add($"{CitizenSourceLabels.Label(CitizenSourceKind.Agent)} ({CitizenSourceLabels.Count(_citizenRows, CitizenSourceKind.Agent)})");
        _filterBar.SourceCombo.Items.Add($"{CitizenSourceLabels.Label(CitizenSourceKind.Deceased)} ({CitizenSourceLabels.Count(_citizenRows, CitizenSourceKind.Deceased)})");
        _filterBar.SourceCombo.Items.Add($"{CitizenSourceLabels.Label(CitizenSourceKind.Legacy)} ({CitizenSourceLabels.Count(_citizenRows, CitizenSourceKind.Legacy)})");
    }

    void SetSourceFilterCombo(CitizenSourceKind? filter)
    {
        _suppressSourceFilterEvent = true;
        try
        {
            _filterBar.SourceCombo.SelectedIndex = filter switch
            {
                CitizenSourceKind.Resident => 1,
                CitizenSourceKind.Agent => 2,
                CitizenSourceKind.Deceased => 3,
                CitizenSourceKind.Legacy => 4,
                _ => 0
            };
        }
        finally
        {
            _suppressSourceFilterEvent = false;
        }
    }

    void SyncCitizenTreeSelection()
    {
        var nodeName = CitizenSourceLabels.TreeNodeName(_citizenSourceFilter);
        var citizensNode = _tree.Nodes["citizens"];
        if (citizensNode == null) return;

        var target = citizensNode.Nodes[nodeName == "citizens" ? "citizens_all" : nodeName]
                     ?? citizensNode.Nodes[nodeName];
        if (target != null && _tree.SelectedNode != target)
            _tree.SelectedNode = target;
    }

    void ApplyCitizenFilter()
    {
        var q = _filterBar.FilterBox.Text.Trim();
        IEnumerable<CitizenListRow> rows = _citizenRows;

        if (_citizenSourceFilter != null)
            rows = rows.Where(r => r.Source == _citizenSourceFilter);

        if (!string.IsNullOrEmpty(q))
        {
            rows = rows.Where(r =>
                r.DisplayName.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                r.RosterId.ToString().Contains(q, StringComparison.OrdinalIgnoreCase) ||
                r.Occupation.Contains(q, StringComparison.OrdinalIgnoreCase));
        }

        var list = rows.ToList();
        var filterLabel = _citizenSourceFilter == null ? "people" : CitizenSourceLabels.Label(_citizenSourceFilter.Value).ToLowerInvariant();
        _filterBar.CountLabel.Text = $"{list.Count} {filterLabel} shown";

        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
        _grid.DataSource = null;
        _grid.Columns.Clear();
        AddCitizenColumns(showSource: _citizenSourceFilter == null);
        foreach (var row in list)
        {
            if (_citizenSourceFilter == null)
            {
                var idx = _grid.Rows.Add(row.RosterId, row.DisplayName, row.Occupation, row.AgeYears.ToString("0.0"), row.PersonalWealth, row.GangId, row.Source);
                _grid.Rows[idx].Tag = row.EditTarget;
            }
            else
            {
                var idx = _grid.Rows.Add(row.RosterId, row.DisplayName, row.Occupation, row.AgeYears.ToString("0.0"), row.PersonalWealth, row.GangId);
                _grid.Rows[idx].Tag = row.EditTarget;
            }
        }

        if (_grid.Rows.Count > 0)
        {
            _grid.ClearSelection();
            _grid.Rows[0].Selected = true;
        }
        else
            UpdatePropertyGridFromSelection();
    }

    void AddCitizenColumns(bool showSource)
    {
        _grid.Columns.Add("rosterId", "Roster");
        _grid.Columns.Add("name", "Name");
        _grid.Columns.Add("occupation", "Occupation");
        _grid.Columns.Add("age", "Age");
        _grid.Columns.Add("wealth", "Wealth");
        _grid.Columns.Add("gang", "Gang");
        if (showSource)
            _grid.Columns.Add("source", "Source");

        _grid.Columns["rosterId"].Width = 64;
        _grid.Columns["rosterId"].MinimumWidth = 64;
        _grid.Columns["occupation"].Width = 110;
        _grid.Columns["occupation"].MinimumWidth = 90;
        _grid.Columns["age"].Width = 56;
        _grid.Columns["age"].MinimumWidth = 56;
        _grid.Columns["wealth"].Width = 72;
        _grid.Columns["wealth"].MinimumWidth = 72;
        _grid.Columns["gang"].Width = 56;
        _grid.Columns["gang"].MinimumWidth = 56;
        if (showSource)
        {
            _grid.Columns["source"].Width = 88;
            _grid.Columns["source"].MinimumWidth = 80;
        }
        _grid.Columns["name"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        _grid.Columns["name"].MinimumWidth = 280;
    }

    void UpdatePropertyGridFromSelection()
    {
        if (_grid.SelectedRows.Count == 0)
        {
            _propertyGrid.SelectedObject = null;
            _propertyHeader.Text = "Properties · select a row to edit";
            return;
        }

        var target = _grid.SelectedRows[0].Tag;
        _propertyGrid.SelectedObject = target;
        _propertyHeader.Text = target != null
            ? $"Properties · {target.GetType().Name}"
            : "Properties · select a row to edit";
        _propertyGrid.Refresh();
    }

    void ShowCivicOffices()
    {
        if (!_doc.IsGameSave || _doc.GameFile == null)
        {
            _contentHost.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = "Civic offices are only available for Midnight Metro game saves.",
                AutoSize = false,
                Padding = new Padding(8)
            });
            return;
        }

        var rows = MetroCivicOfficeQuery.Scan(_doc.GameFile, _doc.Names);
        var elections = _doc.GameFile.elections;

        var header = new Label
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            MaximumSize = new Size(900, 0),
            Padding = new Padding(0, 0, 0, 8),
            Text =
                "Seated officials from elections block + residents with civic jobs (mayor, council, judge, prosecutor, police command).\r\n" +
                $"Mayor id: {(elections.mayorRosterId > 0 ? elections.mayorRosterId.ToString() : "vacant")} · " +
                $"Police chief id: {(elections.policeChiefRosterId > 0 ? elections.policeChiefRosterId.ToString() : "vacant")} · " +
                $"DA id: {(elections.districtAttorneyRosterId > 0 ? elections.districtAttorneyRosterId.ToString() : "vacant")} · " +
                $"Council slate: {elections.councilSlateMembers?.Length ?? 0} entries (size {elections.councilSlateSize}).\r\n" +
                $"Calendar — next mayor day {elections.nextMayorElectionDay} · next council day {elections.nextCouncilElectionDay} · " +
                $"active race {elections.activeRace} · election day {elections.electionDay} · ballot voters {elections.ballotVotersCollected}.\r\n" +
                "Select a row to edit the resident; double-click for property panel focus."
        };

        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
        _grid.DataSource = null;
        _grid.Columns.Clear();
        _grid.Columns.Add("office", "Office");
        _grid.Columns.Add("rosterId", "Roster");
        _grid.Columns.Add("name", "Name");
        _grid.Columns.Add("occupation", "Occupation");
        _grid.Columns.Add("workplace", "Workplace");
        _grid.Columns.Add("source", "Source");
        _grid.Columns.Add("notes", "Notes");
        _grid.Columns["office"].Width = 120;
        _grid.Columns["rosterId"].Width = 64;
        _grid.Columns["name"].Width = 180;
        _grid.Columns["occupation"].Width = 160;
        _grid.Columns["workplace"].Width = 110;
        _grid.Columns["source"].Width = 180;
        _grid.Columns["notes"].Width = 200;

        foreach (var row in rows)
        {
            var idx = _grid.Rows.Add(
                row.Office,
                row.RosterId,
                row.Name,
                row.Occupation,
                row.Workplace,
                row.Source,
                row.Notes ?? "");
            _grid.Rows[idx].Tag = row.EditTarget;
        }

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        layout.Controls.Add(header, 0, 0);
        layout.Controls.Add(_grid, 0, 1);
        _contentHost.Controls.Add(layout);

        if (_grid.Rows.Count > 0)
        {
            _grid.ClearSelection();
            _grid.Rows[0].Selected = true;
        }
        else
            UpdatePropertyGridFromSelection();
    }

    bool RequireGameSave(string featureLabel)
    {
        if (_doc.IsGameSave && _doc.GameFile != null)
            return true;

        _contentHost.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = $"{featureLabel} is only available for Midnight Metro game saves.",
            AutoSize = false,
            Padding = new Padding(8)
        });
        return false;
    }

    void ShowNews()
    {
        if (!RequireGameSave("News articles"))
            return;

        var file = _doc.GameFile!;
        var rows = MetroNewsIndex.Build(file, _doc.Names);
        var header = new Label
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            MaximumSize = new Size(900, 0),
            Padding = new Padding(0, 0, 0, 8),
            Text =
                $"News archive — {rows.Count} articles · next id {file.news.nextId} · last daily briefing day {file.news.lastDailyBriefingDay}.\r\n" +
                "Select a row to edit headline/body/flags; double-click for property panel focus."
        };

        PrepareManualGrid();
        _grid.Columns.Add("id", "Id");
        _grid.Columns.Add("day", "Day");
        _grid.Columns.Add("hour", "Hour");
        _grid.Columns.Add("category", "Category");
        _grid.Columns.Add("outlet", "Outlet");
        _grid.Columns.Add("headline", "Headline");
        _grid.Columns.Add("subhead", "Subhead");
        _grid.Columns.Add("subject", "Subject");
        _grid.Columns.Add("victim", "Victim");
        _grid.Columns.Add("lot", "Lot");
        _grid.Columns.Add("read", "Read");
        _grid.Columns.Add("bolo", "BOLO");
        _grid.Columns["id"].Width = 52;
        _grid.Columns["day"].Width = 52;
        _grid.Columns["hour"].Width = 52;
        _grid.Columns["category"].Width = 72;
        _grid.Columns["headline"].Width = 220;

        foreach (var row in rows)
        {
            var idx = _grid.Rows.Add(
                row.Id,
                row.Day,
                row.Hour.ToString("0.0"),
                row.Category,
                row.Outlet,
                row.Headline,
                row.Subhead,
                row.Subject,
                row.Victim,
                row.Lot,
                row.Read,
                row.Bolo);
            _grid.Rows[idx].Tag = row.EditTarget;
        }

        AddHeaderGridLayout(header);
        _propertyGrid.SelectedObject = file.news;
        _propertyHeader.Text = "Properties — MetroSaveNews";
        FinalizeManualGridSelection();
    }

    void ShowElections()
    {
        if (!RequireGameSave("Elections"))
            return;

        var file = _doc.GameFile!;
        var summary = new Label
        {
            Dock = DockStyle.Fill,
            AutoSize = false,
            Padding = new Padding(0, 0, 0, 8),
            Text = MetroElectionsIndex.BuildSummary(file, _doc.Names) +
                   "\r\n\r\nEdit calendar fields in the property panel (MetroSaveElections). Select honor rows below to edit terms."
        };

        var tabs = new TabControl { Dock = DockStyle.Fill };
        EditorTheme.StyleTabControl(tabs);
        tabs.TabPages.Add(BuildElectionRosterTab(
            "Mayor candidates",
            MetroElectionsIndex.BuildMayorCandidates(file, _doc.Names)));
        tabs.TabPages.Add(BuildElectionRosterTab(
            "Council slate",
            MetroElectionsIndex.BuildCouncilSlate(file, _doc.Names)));
        tabs.TabPages.Add(BuildHonorTab(file));

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1 };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        layout.Controls.Add(summary, 0, 0);
        layout.Controls.Add(tabs, 0, 1);
        _contentHost.Controls.Add(layout);

        _propertyGrid.SelectedObject = file.elections;
        _propertyHeader.Text = "Properties — MetroSaveElections";
    }

    TabPage BuildElectionRosterTab(string title, List<ElectionRosterRow> rows)
    {
        var page = new TabPage(title) { Padding = new Padding(4) };
        var grid = CreateGrid();
        grid.Columns.Add("slot", "Slot");
        grid.Columns.Add("rosterId", "Roster");
        grid.Columns.Add("name", "Name");
        grid.Columns.Add("vote", "Vote ‰");
        grid.Columns.Add("ballot", "Ballot");
        grid.Columns.Add("context", "Context");

        foreach (var row in rows)
            grid.Rows.Add(row.Slot, row.RosterId, row.Name, row.VotePermille, row.BallotTotal, row.Context);

        page.Controls.Add(grid);
        return page;
    }

    TabPage BuildHonorTab(MetroSaveFile file)
    {
        var page = new TabPage("Mayor honor") { Padding = new Padding(4) };
        var grid = CreateGrid();
        grid.Columns.Add("rosterId", "Roster");
        grid.Columns.Add("name", "Name");
        grid.Columns.Add("termStart", "Term start");
        grid.Columns.Add("termEnd", "Term end");
        grid.Columns.Add("terms", "Terms");
        grid.Columns.Add("interim", "Interim");

        foreach (var row in MetroElectionsIndex.BuildMayorHonor(file, _doc.Names))
        {
            var idx = grid.Rows.Add(row.RosterId, row.Name, row.TermStartDay, row.TermEndDay, row.ConsecutiveTerms, row.Interim);
            grid.Rows[idx].Tag = row.EditTarget;
        }

        grid.SelectionChanged += (_, _) =>
        {
            if (_currentView != "elections" || grid.SelectedRows.Count == 0)
                return;
            var target = grid.SelectedRows[0].Tag;
            if (target != null)
            {
                _propertyGrid.SelectedObject = target;
                _propertyHeader.Text = $"Properties — {target.GetType().Name}";
            }
        };

        page.Controls.Add(grid);
        return page;
    }

    void ShowCrimeGangs() => ShowCrimeGrid(
        "crime_gangs",
        "Gang organizations — HQ, boss, notoriety, rivalry, turf chunks.",
        MetroCrimeIndex.BuildGangs(_doc.GameFile!, _doc.Names),
        r => _grid.Rows.Add(r.GangId, r.Name, r.BossRosterId, r.Boss, r.Hq, r.MemberCount, r.MembersRecruited, r.Notoriety, r.Activity, r.RivalGangId, r.RivalryScore, r.TurfChunks),
        ("gangId", "Gang"), ("name", "Name"), ("bossId", "Boss id"), ("boss", "Boss"), ("hq", "HQ"),
        ("members", "Members"), ("recruited", "Recruited"), ("notoriety", "Notoriety"), ("activity", "Activity"),
        ("rival", "Rival"), ("rivalry", "Rivalry"), ("turf", "Turf chunks"));

    void ShowCrimeLedger() => ShowCrimeGrid(
        "crime_ledger",
        "Per-citizen criminal ledger — hidden crimes, proven flags, gang links.",
        MetroCrimeIndex.BuildLedger(_doc.GameFile!, _doc.Names),
        r => _grid.Rows.Add(r.RosterId, r.Name, r.Day, r.CrimeKind, r.Proven, r.GangId, r.GangName, r.Weight),
        ("rosterId", "Roster"), ("name", "Name"), ("day", "Day"), ("crime", "Crime"), ("proven", "Proven"),
        ("gangId", "Gang id"), ("gang", "Gang"), ("weight", "Weight"));

    void ShowCrimeCases() => ShowCrimeGrid(
        "crime_cases",
        "Active and closed prosecution cases — trial pipeline stages.",
        MetroCrimeIndex.BuildCases(_doc.GameFile!, _doc.Names),
        r => _grid.Rows.Add(r.CaseId, r.OffenderRosterId, r.Offender, r.Victim, r.Crime, r.Stage, r.Verdict, r.OpenedDay, r.TrialDay, r.Incident),
        ("caseId", "Case"), ("offenderId", "Offender id"), ("offender", "Offender"), ("victim", "Victim"),
        ("crime", "Crime"), ("stage", "Stage"), ("verdict", "Verdict"), ("opened", "Opened"), ("trial", "Trial"), ("incident", "Incident"));

    void ShowCrimeMembers() => ShowCrimeGrid(
        "crime_members",
        "Residents with gang membership — role, level, convictions.",
        MetroCrimeIndex.BuildGangMembers(_doc.GameFile!, _doc.Names),
        r => _grid.Rows.Add(r.RosterId, r.Name, r.GangId, r.GangName, r.Role, r.Level, r.Standing, r.Occupation, r.CriminalRecord),
        ("rosterId", "Roster"), ("name", "Name"), ("gangId", "Gang id"), ("gang", "Gang"), ("role", "Role"),
        ("level", "Level"), ("standing", "Standing"), ("occupation", "Occupation"), ("record", "Convictions"));

    void ShowCrimeGrid<T>(
        string viewName,
        string hint,
        List<T> rows,
        Func<T, int> addRow,
        params (string name, string header)[] columns) where T : class
    {
        if (!RequireGameSave("Gangs & crime"))
            return;

        _currentView = viewName;
        var header = new Label
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            MaximumSize = new Size(900, 0),
            Padding = new Padding(0, 0, 0, 8),
            Text = hint + "\r\nSelect a row to edit; double-click residents for a larger editor."
        };

        PrepareManualGrid();
        foreach (var (name, headerText) in columns)
            _grid.Columns.Add(name, headerText);

        foreach (var row in rows)
        {
            var idx = addRow(row);
            _grid.Rows[idx].Tag = row switch
            {
                GangListRow g => g.EditTarget,
                CriminalLedgerRow l => l.EditTarget,
                JusticeCaseRow c => c.EditTarget,
                GangMemberRow m => m.EditTarget,
                _ => null
            };
        }

        AddHeaderGridLayout(header);
        _propertyGrid.SelectedObject = viewName == "crime_gangs" ? _doc.GameFile!.gangs : null;
        _propertyHeader.Text = viewName == "crime_gangs"
            ? "Properties — MetroSaveGangs"
            : "Properties — select a row to edit";
        FinalizeManualGridSelection();
    }

    void PrepareManualGrid()
    {
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
        _grid.DataSource = null;
        _grid.Columns.Clear();
    }

    void AddHeaderGridLayout(Control header)
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        layout.Controls.Add(header, 0, 0);
        layout.Controls.Add(_grid, 0, 1);
        _contentHost.Controls.Add(layout);
    }

    void FinalizeManualGridSelection()
    {
        if (_grid.Rows.Count > 0)
        {
            _grid.ClearSelection();
            _grid.Rows[0].Selected = true;
        }
        else
            UpdatePropertyGridFromSelection();

        if (UsesGridFilter(_currentView))
            EditorGridFilter.Apply(_grid, _filterBar.FilterBox.Text);
    }

    void ShowCases()
    {
        var cases = _doc.File.criminalCases;
        PopulateGrid(
            cases.Select(c => new
            {
                c.suspectRosterId,
                suspect = c.suspectDisplayName,
                c.crimeTier,
                c.filedDay,
                c.incarcerated,
                c.convicted,
                c.crimeTypeLabel
            }).ToList(),
            cases.Cast<object>().ToList());
        _contentHost.Controls.Add(WrapGridWithHint("Criminal case files — select a row to edit full case details below."));
    }

    void ShowHonorWall()
    {
        var entries = _doc.File.honorWall;
        PopulateGrid(
            entries.Select(h => new
            {
                h.rosterId,
                h.displayName,
                occupation = OccupationLabels.Label(h.occupation),
                h.dayFallen,
                h.trainingRank
            }).ToList(),
            entries.Cast<object>().ToList());
        _contentHost.Controls.Add(WrapGridWithHint("Fallen service members memorial wall."));
    }

    void ShowGridInspector()
    {
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3 };
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var controls = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = false };
        controls.Controls.Add(new Label { Text = "X:", AutoSize = true, Padding = new Padding(0, 6, 0, 0) });
        controls.Controls.Add(_cellX);
        controls.Controls.Add(new Label { Text = "Y:", AutoSize = true, Padding = new Padding(8, 6, 0, 0) });
        controls.Controls.Add(_cellY);
        var loadBtn = new Button { Text = "Load Cell", AutoSize = true };
        loadBtn.Click += (_, _) => LoadGridCell();
        controls.Controls.Add(loadBtn);
        controls.Controls.Add(_cellSummary);
        panel.Controls.Add(controls, 0, 0);
        panel.Controls.Add(new Label
        {
            Text = _doc.IsGameSave
                ? "Inspect/edit any grid cell. For lot footprints use Lots; for jobs/fulfillment use Workplaces."
                : "Inspect/edit one grid cell from the v2 columnar grid. Legacy v1 per-cell list is not shown here.",
            AutoSize = true,
            Padding = new Padding(0, 8, 0, 8)
        }, 0, 1);

        var gridList = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            AllowUserToAddRows = false
        };
        if (_doc.IsGameSave)
        {
            if (_doc.GameFile?.grid == null) return;
            var samples = new List<object>();
            var g = _doc.GameFile.grid;
            for (var y = 0; y < g.height; y++)
            for (var x = 0; x < g.width; x++)
            {
                var idx = y * g.width + x;
                if (g.type == null || g.type[idx] == 0 || g.type[idx] == 1)
                    continue;
                samples.Add(new
                {
                    x,
                    y,
                    type = g.type[idx],
                    zone = g.zone?[idx] ?? 0,
                    lot = $"{g.lotWidth?[idx] ?? 0}x{g.lotHeight?[idx] ?? 0}",
                    anchor = g.lotAnchorX != null && g.lotAnchorY != null && g.lotAnchorX[idx] == x && g.lotAnchorY[idx] == y,
                    pop = g.population?[idx] ?? 0,
                    wealth = g.wealth?[idx] ?? 0
                });
                if (samples.Count >= 500) break;
            }

            gridList.DataSource = samples;
            gridList.CellDoubleClick += (_, e) =>
            {
                if (e.RowIndex < 0) return;
                _cellX.Value = Convert.ToDecimal(gridList.Rows[e.RowIndex].Cells[0].Value);
                _cellY.Value = Convert.ToDecimal(gridList.Rows[e.RowIndex].Cells[1].Value);
                LoadGridCell();
            };
        }
        else if (_doc.File.grid != null)
        {
            var samples = new List<object>();
            var g = _doc.File.grid;
            for (var y = 0; y < g.height; y++)
            for (var x = 0; x < g.width; x++)
            {
                var idx = y * g.width + x;
                if (g.type == null || g.type[idx] == (int)CellType.Street || g.type[idx] == (int)CellType.Empty)
                    continue;
                samples.Add(new
                {
                    x,
                    y,
                    type = OccupationLabels.CellTypeLabel(g.type[idx]),
                    zone = OccupationLabels.ZoneLabel(g.zone?[idx] ?? 0),
                    pop = g.population?[idx] ?? 0,
                    gang = g.gangId?[idx] ?? 0
                });
                if (samples.Count >= 500) break;
            }

            gridList.DataSource = samples;
            gridList.CellDoubleClick += (_, e) =>
            {
                if (e.RowIndex < 0) return;
                _cellX.Value = Convert.ToDecimal(gridList.Rows[e.RowIndex].Cells[0].Value);
                _cellY.Value = Convert.ToDecimal(gridList.Rows[e.RowIndex].Cells[1].Value);
                LoadGridCell();
            };
        }

        panel.Controls.Add(gridList, 0, 2);
        _contentHost.Controls.Add(panel);
    }

    void ShowLots()
    {
        if (!_doc.IsGameSave || _doc.GameFile == null)
        {
            _contentHost.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = "Lot editing is available for Midnight Metro game saves only.",
                Padding = new Padding(8),
                ForeColor = EditorTheme.TextSecondary
            });
            return;
        }

        _lotRows = MetroLotIndex.Build(_doc.GameFile);
        ShowNestedGridView(
            NestedGridIndex.BuildLotRows(_doc.GameFile, _doc.Names),
            $"{_lotRows.Count} lots — ▶/▼ expand rows; double-click a lot, workplace, household, or job slot to edit.");
    }

    void ShowNestedGridView(IReadOnlyList<NestedGridRow> rows, string headerText)
    {
        var nested = new NestedGridView { Dock = DockStyle.Fill };
        _nestedGridView = nested;
        nested.EditTargetSelected += target =>
        {
            _propertyGrid.SelectedObject = target;
            _propertyHeader.Text = target != null
                ? $"Properties · {target.GetType().Name}"
                : "Properties · select a row to edit";
            _propertyGrid.Refresh();
        };
        nested.Bind(rows);
        nested.ApplyFilter(_filterBar.FilterBox.Text);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            BackColor = EditorTheme.Panel
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        layout.Controls.Add(new Label
        {
            Text = headerText,
            AutoSize = true,
            MaximumSize = new Size(920, 0),
            Padding = new Padding(0, 0, 0, 8),
            ForeColor = EditorTheme.TextSecondary
        }, 0, 0);
        layout.Controls.Add(nested, 0, 1);
        _contentHost.Controls.Add(layout);
    }

    void WireEditorTree(TreeView tree)
    {
        tree.AfterSelect += (_, e) =>
        {
            var target = e.Node?.Tag;
            _propertyGrid.SelectedObject = target;
            _propertyHeader.Text = target != null
                ? $"Properties — {target.GetType().Name}"
                : "Properties — select a tree node to edit";
            _propertyGrid.Refresh();
        };

        if (tree.SelectedNode?.Tag != null)
        {
            _propertyGrid.SelectedObject = tree.SelectedNode.Tag;
            _propertyHeader.Text = $"Properties — {tree.SelectedNode.Tag.GetType().Name}";
        }
    }

    static DataGridView CreateSelectableListGrid() => new()
    {
        Dock = DockStyle.Fill,
        ReadOnly = true,
        AllowUserToAddRows = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        MultiSelect = false,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        RowHeadersVisible = false
    };

    void WireListGridToPropertyGrid(DataGridView listGrid)
    {
        listGrid.SelectionChanged += (_, _) =>
        {
            if (listGrid.SelectedRows.Count == 0)
            {
                _propertyGrid.SelectedObject = null;
                return;
            }

            _propertyGrid.SelectedObject = listGrid.SelectedRows[0].Tag;
            _propertyGrid.Refresh();
        };

        if (listGrid.Rows.Count > 0)
        {
            listGrid.ClearSelection();
            listGrid.Rows[0].Selected = true;
        }
    }

    void ShowWorkplaces()
    {
        if (!_doc.IsGameSave || _doc.GameFile == null)
        {
            _contentHost.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = "Workplace editing is available for Midnight Metro game saves only.",
                Padding = new Padding(8),
                ForeColor = EditorTheme.TextSecondary
            });
            return;
        }

        _workplaceRows = MetroWorkplaceIndex.Build(_doc.GameFile);
        var file = _doc.GameFile;
        var storageNote = MetroWorkplaceStorage.LayerDescription(file.version);
        var hint = file.version >= MetroWorkplaceStorage.WorkplaceSaveVersion
            ? "v56+: workplaces table with per-workplace wealth and nested job slots."
            : "Legacy: grid job arrays; v51–54 vice in nightlife rows.";

        ShowNestedGridView(
            NestedGridIndex.BuildWorkplaceRows(_doc.GameFile, _doc.Names),
            $"Workplace storage: {storageNote}\r\n{_workplaceRows.Count} workplaces — {hint} Expand job slots with ▶/▼.");
    }

    void LoadGridCell()
    {
        var x = (int)_cellX.Value;
        var y = (int)_cellY.Value;

        if (_doc.IsGameSave)
        {
            if (_doc.GameFile == null || !MetroGameGridHelper.TryGetCellView(_doc.GameFile, x, y, out var gameView) || gameView == null)
            {
                _cellSummary.Text = "Invalid coordinates or no game grid.";
                _propertyGrid.SelectedObject = null;
                return;
            }

            _cellSummary.Text = gameView.Summary;
            _propertyGrid.SelectedObject = gameView;
            return;
        }

        if (!GridHelper.TryGetCellView(_doc.File, x, y, out var view) || view == null)
        {
            _cellSummary.Text = "Invalid coordinates or no v2 grid.";
            _propertyGrid.SelectedObject = null;
            return;
        }

        _cellSummary.Text = view.Summary;
        _propertyGrid.SelectedObject = view;
    }

    void ShowMetrics()
    {
        PopulateGrid(_doc.File.metricsHistory, _doc.File.metricsHistory.Cast<object>().ToList());
        _contentHost.Controls.Add(WrapGridWithHint("Daily city metrics history (population, staffing, treasury snapshots)."));
    }

    void ShowRawJson()
    {
        var json = _doc.IsGameSave && _doc.GameFile != null
            ? MetroSaveJson.SerializePretty(_doc.GameFile)
            : SaveJson.SerializePretty(_doc.File);

        _rawJsonTextMode = false;
        _rawJsonBox.ReadOnly = false;
        _rawJsonBox.Text = json;
        _jsonTreeView.LoadJson(json);

        _rawJsonHost = new Panel { Dock = DockStyle.Fill };
        _rawJsonHost.Controls.Add(_jsonTreeView);

        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1 };
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        panel.Controls.Add(BuildRawJsonToolbar(), 0, 0);
        panel.Controls.Add(_rawJsonHost, 0, 1);
        _contentHost.Controls.Add(panel);
        ApplyActiveFilter();
    }

    Control BuildRawJsonToolbar()
    {
        var flow = new FlowLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            WrapContents = false,
            Padding = new Padding(0, 0, 0, 6)
        };
        flow.BackColor = EditorTheme.Panel;

        var expandAll = EditorTheme.CreateChromeButton("Expand all");
        expandAll.Click += (_, _) => _jsonTreeView.ExpandAll();

        var collapseAll = EditorTheme.CreateChromeButton("Collapse all");
        collapseAll.Click += (_, _) => _jsonTreeView.CollapseAll();

        var toggleText = EditorTheme.CreateChromeButton("Edit as text");
        toggleText.Click += (_, _) => ToggleRawJsonTextMode(toggleText);

        flow.Controls.Add(expandAll);
        flow.Controls.Add(collapseAll);
        flow.Controls.Add(toggleText);
        return flow;
    }

    void ToggleRawJsonTextMode(Button toggleButton)
    {
        if (_rawJsonHost == null)
            return;

        if (!_rawJsonTextMode)
        {
            _rawJsonTextMode = true;
            _rawJsonHost.Controls.Clear();
            _rawJsonHost.Controls.Add(_rawJsonBox);
            toggleButton.Text = "Browse tree";
            return;
        }

        try
        {
            _jsonTreeView.LoadJson(_rawJsonBox.Text);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"JSON is invalid and cannot be shown as a tree:\n{ex.Message}",
                "JSON Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        _rawJsonTextMode = false;
        _rawJsonHost.Controls.Clear();
        _rawJsonHost.Controls.Add(_jsonTreeView);
        toggleButton.Text = "Edit as text";
        ApplyActiveFilter();
    }

    void PopulateGrid(object dataSource, List<object> tagObjects)
    {
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;
        _grid.DataSource = dataSource;
        _grid.Tag = tagObjects;
        _grid.DataBindingComplete += OnGridBindingComplete;
    }

    void OnGridBindingComplete(object? sender, DataGridViewBindingCompleteEventArgs e)
    {
        if (sender is not DataGridView grid || grid.Tag is not List<object> tags) return;
        grid.DataBindingComplete -= OnGridBindingComplete;
        for (var i = 0; i < grid.Rows.Count && i < tags.Count; i++)
            grid.Rows[i].Tag = tags[i];

        if (grid.Columns.Count > 0)
        {
            var last = grid.Columns[grid.Columns.Count - 1];
            last.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            last.MinimumWidth = 120;
        }

        if (grid.Rows.Count > 0)
        {
            grid.ClearSelection();
            grid.Rows[0].Selected = true;
        }
    }

    Control WrapGridWithHint(string hint)
    {
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.Controls.Add(new Label { Text = hint, AutoSize = true, Padding = new Padding(0, 0, 0, 6) }, 0, 0);
        panel.Controls.Add(_grid, 0, 1);
        return panel;
    }

    void ShowCitizenDetail(object editTarget)
    {
        using var dlg = new Form
        {
            Text = $"Edit — {editTarget.GetType().Name}",
            Width = 760,
            Height = 760,
            StartPosition = FormStartPosition.CenterParent
        };
        var grid = new PropertyGrid
        {
            Dock = DockStyle.Fill,
            SelectedObject = editTarget,
            HelpVisible = true,
            PropertySort = PropertySort.Categorized
        };
        grid.PropertyValueChanged += (_, _) => MarkDirty();
        dlg.Controls.Add(grid);
        dlg.ShowDialog(this);
        if (_currentView == "citizens")
            ApplyCitizenFilter();
    }

    void OpenSave()
    {
        if (!ConfirmDiscard()) return;
        using var dlg = new OpenFileDialog
        {
            Filter = "Midnight Metro saves (*.json)|*.json|All files (*.*)|*.*",
            Title = "Open Midnight Metro save"
        };
        var settings = EditorSettings.Load();
        if (!string.IsNullOrWhiteSpace(settings.LastSaveDirectory) && Directory.Exists(settings.LastSaveDirectory))
            dlg.InitialDirectory = settings.LastSaveDirectory;
        else if (Directory.Exists(GameSavePaths.SavesRoot))
            dlg.InitialDirectory = GameSavePaths.SavesRoot;
        else if (Directory.Exists(GameSavePaths.PersistentDataPath))
            dlg.InitialDirectory = GameSavePaths.PersistentDataPath;

        if (dlg.ShowDialog() != DialogResult.OK) return;
        try
        {
            _doc.Load(dlg.FileName);
            RefreshAfterLoad();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to open save:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    void SaveCurrent()
    {
        if (_doc.Path == null)
        {
            SaveAs();
            return;
        }

        try
        {
            if (_currentView == "raw" && _rawJsonTextMode && TryApplyRawJson())
                SaveDocumentToDisk();
            else
                SaveDocumentToDisk();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to save:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    void SaveAs()
    {
        using var dlg = new SaveFileDialog
        {
            Filter = "Midnight Metro saves (*.json)|*.json|All files (*.*)|*.*",
            Title = "Save Midnight Metro save",
            FileName = _doc.IsGameSave ? "midnight_metro_save.json" : "citysim_save.json"
        };
        if (dlg.ShowDialog() != DialogResult.OK) return;
        try
        {
            if (_currentView == "raw" && _rawJsonTextMode)
                TryApplyRawJson();
            _doc.Save(dlg.FileName);
            RefreshTitle();
            _statusLabel.Text = $"Saved {dlg.FileName}";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to save:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    void SaveDocumentToDisk()
    {
        _doc.Save();
        RefreshTitle();
        _statusLabel.Text = $"Saved {_doc.Path}";
    }

    bool TryApplyRawJson()
    {
        try
        {
            if (_doc.IsGameSave)
            {
                var file = MetroSaveJson.Deserialize(_rawJsonBox.Text);
                _doc.ReplaceGameFile(file, _rawJsonBox.Text);
                return true;
            }

            _doc.ReplaceFile(SaveJson.Deserialize(_rawJsonBox.Text));
            return true;
        }
        catch (Exception ex)
        {
            var result = MessageBox.Show(
                $"Raw JSON is invalid:\n{ex.Message}\n\nSave anyway using last parsed state?",
                "JSON Error",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);
            return result == DialogResult.Yes;
        }
    }

    void ReloadCurrent()
    {
        if (_doc.Path == null) return;
        if (!ConfirmDiscard()) return;
        try
        {
            _doc.Reload();
            RefreshAfterLoad();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to reload:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    void ExportJson()
    {
        using var dlg = new SaveFileDialog
        {
            Filter = "JSON (*.json)|*.json",
            FileName = "citysim_export.json"
        };
        if (dlg.ShowDialog() != DialogResult.OK) return;
        var exportText = _doc.IsGameSave && _doc.GameFile != null
            ? MetroSaveJson.SerializePretty(_doc.GameFile)
            : SaveJson.SerializePretty(_doc.File);
        File.WriteAllText(dlg.FileName, exportText);
        _statusLabel.Text = $"Exported {dlg.FileName}";
    }

    void ImportJson()
    {
        if (!ConfirmDiscard()) return;
        using var dlg = new OpenFileDialog
        {
            Filter = "JSON (*.json)|*.json"
        };
        if (dlg.ShowDialog() != DialogResult.OK) return;
        _doc.ImportFromJson(File.ReadAllText(dlg.FileName));
        RefreshAfterLoad();
    }

    void PickNamesPath()
    {
        using var dlg = new OpenFileDialog
        {
            Filter = "Names JSON|citizen_names.json;*.json",
            Title = "Select citizen_names.json"
        };
        if (dlg.ShowDialog() != DialogResult.OK) return;
        var settings = EditorSettings.Load();
        settings.NamesJsonPath = dlg.FileName;
        EditorSettings.Save(settings);
        _doc.RefreshNames();
        if (_currentView is "citizens" or "citizens_all" or "citizens_residents" or "citizens_agents" or "citizens_deceased" or "citizens_legacy")
            ShowCitizens();
        _statusLabel.Text = "Names database updated.";
    }

    void ResetMetroNetwork()
    {
        using var dlg = new OpenFileDialog
        {
            Filter = "Midnight Metro save|*.json",
            Title = "Select game save to reset metro network"
        };
        if (!string.IsNullOrWhiteSpace(_doc.Path) && File.Exists(_doc.Path))
            dlg.InitialDirectory = Path.GetDirectoryName(_doc.Path);
        else if (Directory.Exists(GameSavePaths.SavesRoot))
            dlg.InitialDirectory = GameSavePaths.SavesRoot;
        else
            dlg.InitialDirectory = GameSavePaths.PersistentDataPath;
        if (dlg.ShowDialog() != DialogResult.OK)
            return;

        var path = dlg.FileName;

        var answer = MessageBox.Show(
            "Clear all metro tunnels, stations, and line catalog in this save?\n\n" +
            "A .bak_metro_reset backup is created. On load the game auto-expands a fresh network for 5 sim days.\n\n" +
            $"File: {path}",
            "Reset metro network",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);
        if (answer != DialogResult.Yes)
            return;

        try
        {
            MetroSaveRepair.ResetMetroNetwork(path, createBackup: true);
            _statusLabel.Text = "Metro network cleared — load save in game.";
            MessageBox.Show(
                "Metro data cleared.\n\nLoad this save in Midnight Metro. The planner will rebuild stations/tunnels over the next few sim days.",
                "Reset complete",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Reset failed:\n{ex.Message}", "Reset metro", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    void OpenSaveFolder()
    {
        var dir = _doc.Path != null
            ? Path.GetDirectoryName(_doc.Path)
            : GameSavePaths.PersistentDataPath;
        if (dir == null) return;
        dir = Path.GetFullPath(dir);
        Directory.CreateDirectory(dir);
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = dir,
            UseShellExecute = true
        });
    }

    bool ConfirmDiscard()
    {
        if (!_doc.IsDirty) return true;
        var result = MessageBox.Show(
            "Discard unsaved changes?",
            "Unsaved Changes",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);
        return result == DialogResult.Yes;
    }

    void MarkDirty()
    {
        _doc.IsDirty = true;
        RefreshTitle();
    }

    void RefreshTitle()
    {
        Text = $"Midnight Metro Save Editor — {_doc.Title}";
    }

    void RefreshAfterLoad()
    {
        if (!_doc.IsGameSave)
            EnsureNestedObjects();

        _citizenRows = _doc.IsGameSave && _doc.GameFile != null
            ? MetroGameCitizenIndex.Build(_doc.GameFile, _doc.Names)
            : CitizenIndex.Build(_doc.File, _doc.Names);

        RefreshTitle();
        RebuildTreeForDocument();

        _overviewBox.Text = _doc.GetOverviewText() + $"\r\n\r\nFile: {_doc.Path}";
        _statusLabel.Text = _doc.IsGameSave
            ? $"Loaded game save v{_doc.GameFile?.version} — {_doc.Path}"
            : $"Loaded {_doc.Path}";
        _breadcrumbBar.SetRootLabel(GetBreadcrumbRootLabel());
        _navigation.Reset(_currentView);
        UpdateBreadcrumbFromView(_currentView);
        ShowView(_currentView);
    }

    void RebuildTreeForDocument()
    {
        var selected = _tree.SelectedNode?.Name ?? "overview";
        BuildTree();
        if (_doc.IsGameSave)
        {
            HideTreeNode("budget");
            HideTreeNode("playerAgency");
            HideTreeNode("gangs");
            HideTreeNode("cases");
            HideTreeNode("honor");
            HideTreeNode("metrics");
            HideTreeNode("citizens_agents");
            HideTreeNode("citizens_legacy");
        }
        else
        {
            HideTreeNode("lots");
            HideTreeNode("workplaces");
            HideTreeNode("civic");
            HideTreeNode("news");
            HideTreeNode("elections");
            HideTreeNode("crime");
        }

        SelectTreeNode(selected);
    }

    void HideTreeNode(string name)
    {
        var node = _tree.Nodes.Find(name, true).FirstOrDefault();
        if (node != null)
            node.Remove();
    }

    void SelectTreeNode(string name)
    {
        var node = _tree.Nodes.Find(name, true).FirstOrDefault();
        if (node != null)
            _tree.SelectedNode = node;
        else if (_tree.Nodes.Count > 0)
            _tree.SelectedNode = _tree.Nodes[0];
    }

    void EnsureNestedObjects()
    {
        EnsureSession();
        EnsureBudget();
        EnsureGangs();
        EnsurePlayerAgency();
    }

    void EnsureSession()
    {
        _doc.File.session ??= new CitySimSaveSession();
    }

    void EnsureBudget()
    {
        _doc.File.budget ??= new CitySimSaveBudget();
    }

    void EnsureGangs()
    {
        _doc.File.gangs ??= new CitySimSaveGangs();
    }

    void EnsurePlayerAgency()
    {
        _doc.File.playerAgency ??= new CitySimSavePlayerAgency();
    }
}
