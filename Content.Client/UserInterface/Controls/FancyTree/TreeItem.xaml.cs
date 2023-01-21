using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;

namespace Content.Client.UserInterface.Controls.FancyTree;

/// <summary>
///     Element of a <see cref="FancyTree"/>
/// </summary>
[GenerateTypedNameReferences]
public sealed partial class TreeItem : PanelContainer
{
    public const string StyleClassSelected = "selected";
    public const string StyleIdentifierTreeButton = "tree-button";
    public const string StyleClassEvenRow = "even-row";
    public const string StyleClassOddRow = "odd-row";

    public object? Metadata;
    public int Index;
    public FancyTree Tree = default!;
    public event Action<TreeItem>? OnSelected;
    public event Action<TreeItem>? OnDeselected;

    public bool Expanded { get; private set; } = false;

    public TreeItem()
    {
        RobustXamlLoader.Load(this);
        Button.StyleIdentifier = StyleIdentifierTreeButton;
        Body.OnChildAdded += OnItemAdded;
        Body.OnChildRemoved += OnItemRemoved;
    }

    private void OnItemRemoved(Control obj)
    {
        Tree.QueueRowStyleUpdate();

        if (Body.ChildCount == 0)
        {
            Body.Visible = false;
            UpdateIcon();
        }
    }

    private void OnItemAdded(Control obj)
    {
        Tree.QueueRowStyleUpdate();

        if (Body.ChildCount == 1)
        {
            Body.Visible = Expanded && Body.ChildCount != 0;
            UpdateIcon();
        }
    }

    public void SetExpanded(bool value)
    {
        if (Expanded == value)
            return;

        Expanded = value;
        Body.Visible = Expanded && Body.ChildCount > 0;
        UpdateIcon();
        Tree.QueueRowStyleUpdate();
    }

    public void SetSelected(bool value)
    {
        if (value)
        {
            OnSelected?.Invoke(this);
            Button.AddStyleClass(StyleClassSelected);
        }
        else
        {
            OnDeselected?.Invoke(this);
            Button.RemoveStyleClass(StyleClassSelected);
        }
    }

    public void UpdateIcon()
    {
        if (Body.ChildCount == 0)
            Icon.Texture = Tree.IconNoChildren;
        else
            Icon.Texture = Expanded ? Tree.IconExpanded : Tree.IconCollapsed;

        Icon.Modulate = Tree.IconColor;
        Icon.Visible = Icon.Texture != null || !Tree.HideEmptyIcon;
    }
}
