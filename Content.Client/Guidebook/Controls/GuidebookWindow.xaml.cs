using System.Linq;
using Content.Client.Guidebook.RichText;
using Content.Client.UserInterface.ControlExtensions;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Controls.FancyTree;
using Content.Client.UserInterface.Systems.Info;
using Content.Shared.CCVar;
using Content.Shared.Guidebook;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.Prototypes;

namespace Content.Client.Guidebook.Controls;

[GenerateTypedNameReferences]
public sealed partial class GuidebookWindow : FancyWindow, ILinkClickHandler
{
    [Dependency] private readonly IResourceManager _resourceManager = default!;
    [Dependency] private readonly DocumentParsingManager _parsingMan = default!;

    public Dictionary<ProtoId<GuideEntryPrototype>, GuideEntry> Entries = new();

    public GuidebookWindow()
    {
        RobustXamlLoader.Load(this);
        IoCManager.InjectDependencies(this);

        Tree.OnSelectedItemChanged += OnSelectionChanged;

        SearchBar.OnTextChanged += _ =>
        {
            HandleFilter();
        };
    }

    private void OnSelectionChanged(TreeItem? item)
    {
        if (item != null && item.Metadata is GuideEntry entry)
        {
            ShowGuide(entry);

            var isRulesEntry = entry.RuleEntry;
            ReturnContainer.Visible = isRulesEntry;
            HomeButton.OnPressed += _ => ShowGuide(entry);
        }
        else
            ClearSelectedGuide();
    }

    public void ClearSelectedGuide()
    {
        Placeholder.Visible = true;
        EntryContainer.Visible = false;
        SearchContainer.Visible = false;
        EntryContainer.RemoveAllChildren();
    }

    private void ShowGuide(GuideEntry entry)
    {
        Scroll.SetScrollValue(default);
        Placeholder.Visible = false;
        EntryContainer.Visible = true;
        SearchBar.Text = "";
        EntryContainer.RemoveAllChildren();
        using var file = _resourceManager.ContentFileReadText(entry.Text);

        SearchContainer.Visible = entry.FilterEnabled;

        if (!_parsingMan.TryAddMarkup(EntryContainer, file.ReadToEnd()))
        {
            EntryContainer.AddChild(new Label() { Text = "ERROR: Failed to parse document." });
            Logger.Error($"Failed to parse contents of guide document {entry.Id}.");
        }
    }

    public void UpdateGuides(
        Dictionary<ProtoId<GuideEntryPrototype>, GuideEntry> entries,
        List<ProtoId<GuideEntryPrototype>>? rootEntries = null,
        ProtoId<GuideEntryPrototype>? forceRoot = null,
        ProtoId<GuideEntryPrototype>? selected = null)
    {
        Entries = entries;
        RepopulateTree(rootEntries, forceRoot);
        ClearSelectedGuide();

        Split.State = SplitContainer.SplitState.Auto;
        if (entries.Count == 1)
        {
            TreeBox.Visible = false;
            Split.ResizeMode = SplitContainer.SplitResizeMode.NotResizable;
            selected = entries.Keys.First();
        }
        else
        {
            TreeBox.Visible = true;
            Split.ResizeMode = SplitContainer.SplitResizeMode.RespectChildrenMinSize;
        }

        if (selected != null)
        {
            var item = Tree.Items.FirstOrDefault(x => x.Metadata is GuideEntry entry && entry.Id == selected);
            Tree.SetSelectedIndex(item?.Index);
        }
    }

    private IEnumerable<GuideEntry> GetSortedEntries(List<ProtoId<GuideEntryPrototype>>? rootEntries)
    {
        if (rootEntries == null)
        {
            HashSet<ProtoId<GuideEntryPrototype>> entries = new(Entries.Keys);
            foreach (var entry in Entries.Values)
            {
                if (entry.Children.Count > 0)
                {
                    var sortedChildren = entry.Children
                        .Select(childId => Entries[childId])
                        .OrderBy(childEntry => childEntry.Priority)
                        .ThenBy(childEntry => Loc.GetString(childEntry.Name))
                        .Select(childEntry => new ProtoId<GuideEntryPrototype>(childEntry.Id))
                        .ToList();

                    entry.Children = sortedChildren;
                }
                entries.ExceptWith(entry.Children);
            }
            rootEntries = entries.ToList();
        }

        return rootEntries
            .Select(rootEntryId => Entries[rootEntryId])
            .OrderBy(rootEntry => rootEntry.Priority)
            .ThenBy(rootEntry => Loc.GetString(rootEntry.Name));
    }

    private void RepopulateTree(List<ProtoId<GuideEntryPrototype>>? roots = null, ProtoId<GuideEntryPrototype>? forcedRoot = null)
    {
        Tree.Clear();

        HashSet<ProtoId<GuideEntryPrototype>> addedEntries = new();

        TreeItem? parent = forcedRoot == null ? null : AddEntry(forcedRoot.Value, null, addedEntries);
        foreach (var entry in GetSortedEntries(roots))
        {
            AddEntry(entry.Id, parent, addedEntries);
        }
        Tree.SetAllExpanded(true);
    }

    private TreeItem? AddEntry(ProtoId<GuideEntryPrototype> id, TreeItem? parent, HashSet<ProtoId<GuideEntryPrototype>> addedEntries)
    {
        if (!Entries.TryGetValue(id, out var entry))
            return null;

        if (!addedEntries.Add(id))
        {
            // TODO GUIDEBOOK Maybe allow duplicate entries?
            // E.g., for adding medicine under both chemicals & the chemist job
            Logger.Error($"Adding duplicate guide entry: {id}");
            return null;
        }

        var rulesProto = UserInterfaceManager.GetUIController<InfoUIController>().GetCoreRuleEntry();
        if (entry.RuleEntry && entry.Id != rulesProto.Id)
            return null;

        var item = Tree.AddItem(parent);
        item.Metadata = entry;
        var name = Loc.GetString(entry.Name);
        item.Label.Text = name;

        foreach (var child in entry.Children)
        {
            AddEntry(child, item, addedEntries);
        }

        return item;
    }

    public void HandleClick(string link)
    {
        if (!Entries.TryGetValue(link, out var entry))
            return;

        if (Tree.TryGetIndexFromMetadata(entry, out var index))
        {
            Tree.ExpandParentEntries(index.Value);
            Tree.SetSelectedIndex(index);
        }
        else
        {
            ShowGuide(entry);
        }
    }

    private void HandleFilter()
    {
        var emptySearch = SearchBar.Text.Trim().Length == 0;

        if (Tree.SelectedItem != null && Tree.SelectedItem.Metadata is GuideEntry entry && entry.FilterEnabled)
        {
            var foundElements = EntryContainer.GetSearchableControls();

            foreach (var element in foundElements)
            {
                element.SetHiddenState(true, SearchBar.Text.Trim());
            }
        }

    }
}
