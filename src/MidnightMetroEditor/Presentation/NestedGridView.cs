using MidnightMetroEditor.Services;

namespace MidnightMetroEditor.Presentation;

public sealed class NestedGridView : UserControl
{
    readonly DataGridView _grid;
    readonly Dictionary<string, NestedGridRow> _byId = new(StringComparer.Ordinal);
    List<NestedGridRow> _allRows = new();
    string _filter = "";

    public event Action<object?>? EditTargetSelected;

    public NestedGridView()
    {
        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None
        };
        EditorTheme.StyleDataGridView(_grid);

        _grid.Columns.Add("exp", "");
        _grid.Columns.Add("kind", "Kind");
        _grid.Columns.Add("name", "Name");
        _grid.Columns.Add("detail", "Details");
        _grid.Columns["exp"].Width = 28;
        _grid.Columns["exp"].MinimumWidth = 28;
        _grid.Columns["kind"].Width = 88;
        _grid.Columns["name"].Width = 280;
        _grid.Columns["detail"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

        _grid.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex < 0) return;
            if (_grid.Rows[e.RowIndex].Tag is not string id || !_byId.TryGetValue(id, out var row))
                return;

            if (!row.IsExpandable)
            {
                if (row.EditTarget != null)
                    EditTargetSelected?.Invoke(row.EditTarget);
                return;
            }

            row.Expanded = !row.Expanded;
            RefreshVisibleRows();
        };

        _grid.SelectionChanged += (_, _) =>
        {
            if (_grid.SelectedRows.Count == 0)
            {
                EditTargetSelected?.Invoke(null);
                return;
            }

            if (_grid.SelectedRows[0].Tag is string id && _byId.TryGetValue(id, out var row))
                EditTargetSelected?.Invoke(row.EditTarget);
            else
                EditTargetSelected?.Invoke(null);
        };

        Controls.Add(_grid);
    }

    public void Bind(IReadOnlyList<NestedGridRow> rows)
    {
        _allRows = rows.ToList();
        _byId.Clear();
        foreach (var row in _allRows)
            _byId[row.Id] = row;

        RefreshVisibleRows();
    }

    public void ApplyFilter(string query)
    {
        _filter = query.Trim();
        RefreshVisibleRows();
    }

    void RefreshVisibleRows()
    {
        _grid.Rows.Clear();
        foreach (var row in _allRows)
        {
            if (!IsVisible(row))
                continue;

            if (!string.IsNullOrEmpty(_filter) && !RowMatchesFilter(row))
                continue;

            var indent = new string(' ', row.Depth * 3);
            var exp = row.IsExpandable ? row.Expanded ? "▼" : "▶" : "";
            var idx = _grid.Rows.Add(exp, row.Kind, indent + row.Name, row.Detail);
            _grid.Rows[idx].Tag = row.Id;
        }

        if (_grid.Rows.Count > 0)
        {
            _grid.ClearSelection();
            _grid.Rows[0].Selected = true;
        }
    }

    bool IsVisible(NestedGridRow row)
    {
        if (string.IsNullOrEmpty(row.ParentId))
            return true;

        if (!_byId.TryGetValue(row.ParentId, out var parent))
            return true;

        if (parent.IsExpandable && !parent.Expanded)
            return false;

        return IsVisible(parent);
    }

    bool RowMatchesFilter(NestedGridRow row)
    {
        if (Matches(row.Kind) || Matches(row.Name) || Matches(row.Detail))
            return true;

        foreach (var child in _allRows.Where(r => r.ParentId == row.Id))
        {
            if (RowMatchesFilter(child))
                return true;
        }

        return false;
    }

    bool Matches(string text) =>
        text.Contains(_filter, StringComparison.OrdinalIgnoreCase);
}
