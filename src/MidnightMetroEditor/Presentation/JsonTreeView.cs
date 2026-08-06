using System.Text.Json;

namespace MidnightMetroEditor.Presentation;

/// <summary>Collapsible JSON browser — lazy-loads large arrays in chunks.</summary>
public sealed class JsonTreeView : UserControl
{
    const int ArrayChunkSize = 100;

    readonly TreeView _tree;
    readonly HashSet<TreeNode> _filterMatches = new();
    JsonDocument? _document;
    string _filter = "";

    public JsonTreeView()
    {
        _tree = new EllipsisTreeView
        {
            Dock = DockStyle.Fill,
            HideSelection = false
        };
        EditorTheme.StyleTreeView(_tree, DrawJsonNode);
        _tree.BeforeExpand += (_, e) => EnsureExpanded(e.Node);
        Controls.Add(_tree);
    }

    public void LoadJson(string json)
    {
        _document?.Dispose();
        _document = JsonDocument.Parse(json);
        _tree.BeginUpdate();
        _tree.Nodes.Clear();

        var root = _document.RootElement;
        var rootNode = CreateNode(RootLabel(root), root);
        _tree.Nodes.Add(rootNode);
        if (root.ValueKind is JsonValueKind.Object or JsonValueKind.Array)
            AddPlaceholder(rootNode);

        _tree.EndUpdate();
        ApplyFilter(_filter);
    }

    public void ExpandAll()
    {
        foreach (TreeNode node in _tree.Nodes)
            ExpandRecursive(node);
    }

    public void CollapseAll()
    {
        foreach (TreeNode node in _tree.Nodes)
            CollapseRecursive(node);
    }

    public void ApplyFilter(string query)
    {
        _filter = query.Trim();
        _filterMatches.Clear();

        if (string.IsNullOrEmpty(_filter))
        {
            _tree.Invalidate();
            return;
        }

        foreach (TreeNode node in _tree.Nodes)
            ApplyFilterRecursive(node);

        _tree.Invalidate();
    }

    bool ApplyFilterRecursive(TreeNode node)
    {
        var match = node.Text.Contains(_filter, StringComparison.OrdinalIgnoreCase);
        foreach (TreeNode child in node.Nodes)
        {
            if (ApplyFilterRecursive(child))
                match = true;
        }

        if (match)
        {
            _filterMatches.Add(node);
            var parent = node.Parent;
            while (parent != null)
            {
                parent.Expand();
                EnsureExpanded(parent);
                parent = parent.Parent;
            }
        }

        return match;
    }

    void DrawJsonNode(object? sender, DrawTreeNodeEventArgs e)
    {
        if (sender is not TreeView tree || e.Node == null)
            return;

        var selected = (e.State & TreeNodeStates.Selected) != 0;
        var hover = e.Node == tree.SelectedNode && tree.Focused;
        var highlighted = !string.IsNullOrEmpty(_filter) && _filterMatches.Contains(e.Node);
        var back = selected ? EditorTheme.Selection : EditorTheme.Panel;
        var fore = selected
            ? EditorTheme.TextPrimary
            : highlighted
                ? EditorTheme.Accent
                : EditorTheme.TextSecondary;
        if (selected && hover)
            back = Color.FromArgb(68, 86, 118);

        EditorTheme.DrawTreeNodeLabel(e, tree, fore, back, selected);
    }

    void ExpandRecursive(TreeNode node)
    {
        EnsureExpanded(node);
        node.Expand();
        foreach (TreeNode child in node.Nodes)
            ExpandRecursive(child);
    }

    static void CollapseRecursive(TreeNode node)
    {
        foreach (TreeNode child in node.Nodes)
            CollapseRecursive(child);
        node.Collapse();
    }

    void EnsureExpanded(TreeNode? node)
    {
        if (node == null)
            return;

        if (node.Tag is JsonArrayChunk chunk)
        {
            PopulateArrayChunk(node, chunk);
            return;
        }

        if (node.Nodes.Count == 1 && node.Nodes[0].Tag is JsonPlaceholder)
            PopulateNode(node);
    }

    void PopulateNode(TreeNode node)
    {
        if (node.Tag is not JsonNodeRef reference)
            return;

        _tree.BeginUpdate();
        node.Nodes.Clear();

        switch (reference.Element.ValueKind)
        {
            case JsonValueKind.Object:
                PopulateObject(node, reference.Element);
                break;
            case JsonValueKind.Array:
                PopulateArray(node, reference.Element, 0);
                break;
        }

        _tree.EndUpdate();
    }

    void PopulateArrayChunk(TreeNode node, JsonArrayChunk chunk)
    {
        _tree.BeginUpdate();
        node.Nodes.Clear();
        PopulateArray(node, chunk.Array, chunk.StartIndex);
        _tree.EndUpdate();
    }

    void PopulateObject(TreeNode parent, JsonElement obj)
    {
        foreach (var property in obj.EnumerateObject())
        {
            var child = CreateNode(FormatProperty(property.Name, property.Value), property.Value);
            parent.Nodes.Add(child);
            if (property.Value.ValueKind is JsonValueKind.Object or JsonValueKind.Array)
                AddPlaceholder(child);
        }
    }

    void PopulateArray(TreeNode parent, JsonElement array, int startIndex)
    {
        var length = array.GetArrayLength();
        var end = Math.Min(startIndex + ArrayChunkSize, length);
        for (var i = startIndex; i < end; i++)
        {
            var value = array[i];
            var child = CreateNode(FormatIndex(i, value), value);
            parent.Nodes.Add(child);
            if (value.ValueKind is JsonValueKind.Object or JsonValueKind.Array)
                AddPlaceholder(child);
        }

        if (end < length)
        {
            var remaining = length - end;
            var chunkEnd = Math.Min(end + ArrayChunkSize, length) - 1;
            var more = new TreeNode($"[{end}..{chunkEnd}] … {remaining:N0} items")
            {
                Tag = new JsonArrayChunk(array, end)
            };
            more.Nodes.Add(new TreeNode("…") { Tag = new JsonPlaceholder() });
            parent.Nodes.Add(more);
        }
    }

    static TreeNode CreateNode(string text, JsonElement element) =>
        new(text) { Tag = new JsonNodeRef(element) };

    static void AddPlaceholder(TreeNode node) =>
        node.Nodes.Add(new TreeNode("…") { Tag = new JsonPlaceholder() });

    static string RootLabel(JsonElement el) => el.ValueKind switch
    {
        JsonValueKind.Object => $"{{object}} · {CountProperties(el)} keys",
        JsonValueKind.Array => $"[array] · {el.GetArrayLength():N0} items",
        _ => FormatPrimitive(el)
    };

    static string FormatProperty(string name, JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.Object => $"\"{name}\": {{…}} · {CountProperties(value)} keys",
        JsonValueKind.Array => $"\"{name}\": […] · {value.GetArrayLength():N0} items",
        _ => $"\"{name}\": {FormatPrimitive(value)}"
    };

    static string FormatIndex(int index, JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.Object => $"[{index}] {{…}} · {CountProperties(value)} keys",
        JsonValueKind.Array => $"[{index}] […] · {value.GetArrayLength():N0} items",
        _ => $"[{index}]: {FormatPrimitive(value)}"
    };

    static int CountProperties(JsonElement obj)
    {
        var count = 0;
        foreach (var _ in obj.EnumerateObject())
            count++;
        return count;
    }

    static string FormatPrimitive(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.String => $"\"{Truncate(value.GetString() ?? "", 120)}\"",
        JsonValueKind.Number => value.ToString(),
        JsonValueKind.True => "true",
        JsonValueKind.False => "false",
        JsonValueKind.Null => "null",
        _ => value.ToString()
    };

    static string Truncate(string text, int max) =>
        text.Length <= max ? text : text[..max] + "…";

    sealed class JsonNodeRef
    {
        public JsonNodeRef(JsonElement element) => Element = element;
        public JsonElement Element { get; }
    }

    sealed class JsonArrayChunk
    {
        public JsonArrayChunk(JsonElement array, int startIndex)
        {
            Array = array;
            StartIndex = startIndex;
        }

        public JsonElement Array { get; }
        public int StartIndex { get; }
    }

    sealed class JsonPlaceholder
    {
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _document?.Dispose();
        base.Dispose(disposing);
    }
}
