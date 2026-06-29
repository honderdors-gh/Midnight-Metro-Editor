using MidnightMetroEditor.Models;
using MidnightMetroEditor.Services;

namespace MidnightMetroEditor;

public sealed class MainForm : Form
{
    readonly SaveDocument _doc = new();
    readonly TreeView _tree = new() { Dock = DockStyle.Fill, HideSelection = false, BorderStyle = BorderStyle.None, ItemHeight = 22 };
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
    readonly SplitContainer _mainSplit = new() { Dock = DockStyle.Fill, SplitterDistance = 260 };
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
    readonly TextBox _searchBox = new() { Width = 220, PlaceholderText = "Search citizens..." };
    readonly ComboBox _sourceFilterCombo = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 170 };
    readonly Label _citizenCountLabel = new() { AutoSize = true, Padding = new Padding(8, 8, 0, 0) };
    CitizenSourceKind? _citizenSourceFilter;
    bool _suppressSourceFilterEvent;
    readonly NumericUpDown _cellX = new() { Minimum = 0, Maximum = 999, Width = 70 };
    readonly NumericUpDown _cellY = new() { Minimum = 0, Maximum = 999, Width = 70 };
    readonly Label _cellSummary = new() { AutoSize = true, Padding = new Padding(8, 6, 0, 0) };

    List<CitizenListRow> _citizenRows = new();
    string _currentView = "overview";

    public MainForm()
    {
        Text = "Midnight Metro Save Editor";
        Width = 1440;
        Height = 900;
        StartPosition = FormStartPosition.CenterScreen;

        AutoScaleMode = AutoScaleMode.Font;
        Font = new Font("Segoe UI", 9f);

        BuildMenu();
        BuildLayout();
        BuildTree();
        WireEvents();

        _status.Items.Add(_statusLabel);
        Controls.Add(_mainSplit);
        Controls.Add(_status);
        Controls.Add(_toolStrip);
        Controls.Add(_menuStrip);

        Load += (_, _) => ConfigureSplitters();
        Shown += (_, _) => ConfigureSplitters();
        TryOpenDefaultSave();
        ShowOverview();
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
            RowHeadersVisible = false,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
            AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None,
            ScrollBars = ScrollBars.Both,
            RowTemplate = { Height = 24 },
            DefaultCellStyle = { WrapMode = DataGridViewTriState.False, Padding = new Padding(4, 2, 4, 2) },
            ColumnHeadersDefaultCellStyle = { WrapMode = DataGridViewTriState.False, Padding = new Padding(4, 4, 4, 4) },
            BackgroundColor = SystemColors.Window,
            BorderStyle = BorderStyle.FixedSingle
        };
        return grid;
    }

    static TextBox CreateReadOnlyMultiline() => new()
    {
        Dock = DockStyle.Fill,
        Multiline = true,
        ReadOnly = true,
        ScrollBars = ScrollBars.Vertical,
        Font = new Font(FontFamily.GenericMonospace, 10f),
        BorderStyle = BorderStyle.None,
        BackColor = SystemColors.Window
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
        var treePanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(6, 8, 4, 4) };
        treePanel.Controls.Add(_tree);

        _propertyPanel.Controls.Add(_propertyHeader);
        _propertyPanel.Controls.Add(_propertyGrid);

        _mainSplit.Panel1.Controls.Add(treePanel);
        _rightSplit.Panel1.Controls.Add(_contentHost);
        _rightSplit.Panel2.Controls.Add(_propertyPanel);
        _mainSplit.Panel2.Controls.Add(_rightSplit);
    }

    void ConfigureSplitters()
    {
        try
        {
            _mainSplit.Panel1MinSize = 160;
            _mainSplit.Panel2MinSize = 320;
            if (_mainSplit.Width > 0)
            {
                var mainSplit = Math.Clamp(260, _mainSplit.Panel1MinSize, _mainSplit.Width - _mainSplit.Panel2MinSize);
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
        var citizens = _tree.Nodes.Add("citizens", "Citizens");
        citizens.Nodes.Add("citizens_all", "All");
        citizens.Nodes.Add("citizens_residents", "Residents");
        citizens.Nodes.Add("citizens_agents", "Agents");
        citizens.Nodes.Add("citizens_deceased", "Deceased");
        citizens.Nodes.Add("citizens_legacy", "Legacy v1");
        _tree.Nodes.Add("cases", "Criminal Cases");
        _tree.Nodes.Add("honor", "Honor Wall");
        _tree.Nodes.Add("grid", "Grid Cell Inspector");
        _tree.Nodes.Add("metrics", "Metrics History");
        _tree.Nodes.Add("raw", "Raw JSON");
        _tree.SelectedNode = _tree.Nodes[0];
    }

    void WireEvents()
    {
        _tree.AfterSelect += (_, e) =>
        {
            if (e.Node?.Name != null)
                ShowView(e.Node.Name);
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
            if (_currentView == "raw")
                MarkDirty();
        };

        _searchBox.TextChanged += (_, _) => ApplyCitizenFilter();
        _sourceFilterCombo.SelectedIndexChanged += (_, _) =>
        {
            if (_suppressSourceFilterEvent) return;
            if (_currentView is not ("citizens" or "citizens_all" or "citizens_residents" or "citizens_agents" or "citizens_deceased" or "citizens_legacy"))
                return;
            if (_sourceFilterCombo.SelectedIndex < 0) return;
            _citizenSourceFilter = _sourceFilterCombo.SelectedIndex switch
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
            case "honor":
                ShowHonorWall();
                break;
            case "grid":
                ShowGridInspector();
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
            _contentHost.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = "Edit session fields in the property panel.\r\n\r\nCommon edits: day, cityName, randomSeed, treasury, metroNetworkResetPending.",
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
        _citizenRows = CitizenIndex.Build(_doc.File, _doc.Names);
        PopulateSourceFilterCombo();
        SetSourceFilterCombo(_citizenSourceFilter);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 3,
            ColumnCount = 1
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        var top = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            WrapContents = false,
            Padding = new Padding(0, 2, 0, 2)
        };
        top.Controls.Add(new Label { Text = "Filter:", AutoSize = true, Padding = new Padding(0, 8, 4, 0) });
        top.Controls.Add(_sourceFilterCombo);
        top.Controls.Add(new Label { Text = "Search:", AutoSize = true, Padding = new Padding(12, 8, 4, 0) });
        _searchBox.Width = 240;
        top.Controls.Add(_searchBox);
        top.Controls.Add(_citizenCountLabel);

        layout.Controls.Add(top, 0, 0);
        layout.Controls.Add(new Label
        {
            Text = "Filter by population type or use the sidebar. Select a row to edit fields below; double-click for a larger editor.",
            AutoSize = true,
            MaximumSize = new Size(900, 0),
            Padding = new Padding(0, 4, 0, 8)
        }, 0, 1);
        layout.Controls.Add(_grid, 0, 2);

        _contentHost.Controls.Add(layout);
        ApplyCitizenFilter();
    }

    void PopulateSourceFilterCombo()
    {
        _sourceFilterCombo.Items.Clear();
        _sourceFilterCombo.Items.Add($"All ({_citizenRows.Count})");
        _sourceFilterCombo.Items.Add($"{CitizenSourceLabels.Label(CitizenSourceKind.Resident)} ({CitizenSourceLabels.Count(_citizenRows, CitizenSourceKind.Resident)})");
        _sourceFilterCombo.Items.Add($"{CitizenSourceLabels.Label(CitizenSourceKind.Agent)} ({CitizenSourceLabels.Count(_citizenRows, CitizenSourceKind.Agent)})");
        _sourceFilterCombo.Items.Add($"{CitizenSourceLabels.Label(CitizenSourceKind.Deceased)} ({CitizenSourceLabels.Count(_citizenRows, CitizenSourceKind.Deceased)})");
        _sourceFilterCombo.Items.Add($"{CitizenSourceLabels.Label(CitizenSourceKind.Legacy)} ({CitizenSourceLabels.Count(_citizenRows, CitizenSourceKind.Legacy)})");
    }

    void SetSourceFilterCombo(CitizenSourceKind? filter)
    {
        _suppressSourceFilterEvent = true;
        try
        {
            _sourceFilterCombo.SelectedIndex = filter switch
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
        var q = _searchBox.Text.Trim();
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
        _citizenCountLabel.Text = $"{list.Count} {filterLabel} shown";

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
            _propertyHeader.Text = "Properties — select a row to edit";
            return;
        }

        var target = _grid.SelectedRows[0].Tag;
        _propertyGrid.SelectedObject = target;
        _propertyHeader.Text = target != null
            ? $"Properties — {target.GetType().Name}"
            : "Properties — select a row to edit";
        _propertyGrid.Refresh();
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
                ? "Inspect/edit one grid cell from the game save columnar grid."
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
        _rawJsonBox.ReadOnly = false;
        _rawJsonBox.Text = _doc.IsGameSave && _doc.GameFile != null
            ? MetroSaveJson.SerializePretty(_doc.GameFile)
            : SaveJson.SerializePretty(_doc.File);
        _contentHost.Controls.Add(_rawJsonBox);
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
            if (_currentView == "raw" && TryApplyRawJson())
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
            if (_currentView == "raw")
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
