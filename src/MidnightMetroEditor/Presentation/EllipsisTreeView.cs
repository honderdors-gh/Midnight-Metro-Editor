namespace MidnightMetroEditor.Presentation;

/// <summary>TreeView that hides horizontal scrolling so owner-draw ellipsis can clip long labels.</summary>
public class EllipsisTreeView : TreeView
{
    const int TvsNoHScroll = 0x8000;

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.Style |= TvsNoHScroll;
            return cp;
        }
    }
}
