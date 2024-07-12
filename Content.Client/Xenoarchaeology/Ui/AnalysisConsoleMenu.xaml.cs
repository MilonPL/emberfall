using System.Text;
using Content.Client.Message;
using Content.Client.Resources;
using Content.Client.UserInterface.Controls;
using Content.Client.Xenoarchaeology.Artifact;
using Content.Client.Xenoarchaeology.Equipment;
using Content.Shared.NameIdentifier;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Content.Shared.Xenoarchaeology.Equipment.Components;
using Robust.Client.AutoGenerated;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Timing;

namespace Content.Client.Xenoarchaeology.Ui;

[GenerateTypedNameReferences]
public sealed partial class AnalysisConsoleMenu : FancyWindow
{
    [Dependency] private readonly IEntityManager _ent = default!;
    [Dependency] private readonly IResourceCache _resCache = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    private readonly ArtifactAnalyzerSystem _artifactAnalyzer;
    private readonly XenoArtifactSystem _xenoArtifact;

    private Entity<AnalysisConsoleComponent> _owner;
    private Entity<XenoArtifactNodeComponent>? _currentNode;

    public event Action? OnServerSelectionButtonPressed;
    public event Action? OnExtractButtonPressed;

    public AnalysisConsoleMenu(EntityUid owner)
    {
        RobustXamlLoader.Load(this);
        IoCManager.InjectDependencies(this);

        _xenoArtifact = _ent.System<XenoArtifactSystem>();
        _artifactAnalyzer = _ent.System<ArtifactAnalyzerSystem>();

        if (BackPanel.PanelOverride is StyleBoxTexture tex)
            tex.Texture = _resCache.GetTexture("/Textures/Interface/Nano/button.svg.96dpi.png");

        IDLabel.SetMarkup(Loc.GetString("analysis-console-info-id"));
        ClassLabel.SetMarkup(Loc.GetString("analysis-console-info-class"));
        LockedLabel.SetMarkup(Loc.GetString("analysis-console-info-locked"));
        EffectLabel.SetMarkup(Loc.GetString("analysis-console-info-effect"));
        TriggerLabel.SetMarkup(Loc.GetString("analysis-console-info-trigger"));
        DurabilityLabel.SetMarkup(Loc.GetString("analysis-console-info-durability"));

        GraphControl.OnNodeSelected += node =>
        {
            _currentNode = node;
            SetSelectedNode(node);
        };

        ServerButton.OnPressed += _ =>
        {
            OnServerSelectionButtonPressed?.Invoke();
        };

        //TODO: extract button

        var comp = _ent.GetComponent<AnalysisConsoleComponent>(owner);
        _owner = (owner, comp);
        Update((owner, comp));
        SetSelectedNode(null);
    }

    public void Update(Entity<AnalysisConsoleComponent> ent)
    {
        _artifactAnalyzer.TryGetArtifactFromConsole(ent, out var arti);
        ArtifactView.SetEntity(arti);
        GraphControl.SetArtifact(arti);

        ExtractButton.Disabled = arti == null;

        if (arti == null)
            NoneSelectedLabel.Visible = false;

        NoArtiLabel.Visible = true;
        if (!_artifactAnalyzer.TryGetAnalyzer(ent, out _))
        {
            NoArtiLabel.Text = Loc.GetString("analysis-console-info-no-scanner");
        }
        else if (arti == null)
        {
            NoArtiLabel.Text = Loc.GetString("analysis-console-info-no-artifact");
        }
        else
        {
            NoArtiLabel.Visible = false;
        }

        if (_currentNode == null || arti == null || !_xenoArtifact.TryGetIndex((arti.Value, arti.Value), _currentNode.Value, out _))
        {
            SetSelectedNode(null);
        }
    }

    public void SetSelectedNode(Entity<XenoArtifactNodeComponent>? node)
    {
        InfoContainer.Visible = node != null;
        if (!_artifactAnalyzer.TryGetArtifactFromConsole(_owner, out var artifact))
            return;
        NoneSelectedLabel.Visible = node == null;

        if (node == null)
            return;

        IDValueLabel.SetMarkup(Loc.GetString("analysis-console-info-id-value",
            ("id", (_ent.GetComponentOrNull<NameIdentifierComponent>(node.Value)?.Identifier ?? 0).ToString("D3"))));

        // If active, state is 2. else, it is 0 or 1 based on whether or not it is unlocked.
        var lockedState = _xenoArtifact.IsNodeActive(artifact.Value, node.Value)
            ? 2
            : node.Value.Comp.Locked
                ? 0
                : 1;
        LockedValueLabel.SetMarkup(Loc.GetString("analysis-console-info-locked-value",
            ("state", lockedState)));

        var percent = (float) node.Value.Comp.Durability / node.Value.Comp.MaxDurability;
        var color = percent switch
        {
            >= 0.75f => Color.Lime,
            >= 0.50f => Color.Yellow,
            _ => Color.Red
        };
        DurabilityValueLabel.SetMarkup(Loc.GetString("analysis-console-info-durability-value",
            ("color", color),
            ("current", node.Value.Comp.Durability),
            ("max", node.Value.Comp.MaxDurability)));

        var hasInfo = _xenoArtifact.HasUnlockedPredecessor(artifact.Value, node.Value);

        EffectValueLabel.SetMarkup(Loc.GetString("analysis-console-info-effect-value",
            ("state", hasInfo),
            ("info", _ent.GetComponentOrNull<MetaDataComponent>(node.Value)?.EntityDescription ?? string.Empty)));

        var predecessorNodes = _xenoArtifact.GetPredecessorNodes(artifact.Value.Owner, node.Value);
        if (!hasInfo)
        {
            TriggerValueLabel.SetMarkup(Loc.GetString("analysis-console-info-effect-value", ("state", false)));
        }
        else
        {
            var triggerStr = new StringBuilder();
            triggerStr.Append("- ");
            triggerStr.Append(Loc.GetString(node.Value.Comp.TriggerTip));

            foreach (var predecessor in predecessorNodes)
            {
                triggerStr.AppendLine();
                triggerStr.Append("- ");
                triggerStr.Append(Loc.GetString(predecessor.Comp.TriggerTip));
            }
            TriggerValueLabel.SetMarkup(Loc.GetString("analysis-console-info-triggered-value", ("triggers", triggerStr.ToString())));
        }

        ClassValueLabel.SetMarkup(Loc.GetString("analysis-console-info-class-value",
            ("class", Loc.GetString($"artifact-node-class-{Math.Min(6, predecessorNodes.Count + 1)}"))));
    }
}

