namespace MidnightMetroEditor.Services;

using System.Windows.Forms;
using MidnightMetroEditor.Presentation;

public sealed class EditorTreeItem
{
    public string Label { get; init; } = "";
    public object? EditTarget { get; init; }
    public List<EditorTreeItem> Children { get; init; } = new();
}

public static class EditorTreeViewHelper
{
    public static TreeView CreateEditorTree()
    {
        var tree = new EllipsisTreeView
        {
            Dock = DockStyle.Fill,
            HideSelection = false,
            BorderStyle = BorderStyle.None
        };
        EditorTheme.StyleTreeView(tree);
        return tree;
    }

    public static void Populate(TreeView tree, IReadOnlyList<EditorTreeItem> roots)
    {
        tree.BeginUpdate();
        tree.Nodes.Clear();
        foreach (var root in roots)
            tree.Nodes.Add(ToNode(root));
        tree.EndUpdate();

        if (tree.Nodes.Count > 0)
            tree.SelectedNode = tree.Nodes[0];
    }

    static TreeNode ToNode(EditorTreeItem item)
    {
        var node = new TreeNode(item.Label)
        {
            Tag = item.EditTarget
        };

        foreach (var child in item.Children)
            node.Nodes.Add(ToNode(child));

        return node;
    }
}
