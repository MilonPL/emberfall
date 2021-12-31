#nullable enable
using Content.Client.Administration.UI.CustomControls;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.XAML;

namespace Content.Client.Administration.UI
{
    [GenerateTypedNameReferences]
    public partial class BwoinkedWindow : SS14Window
    {
        public BwoinkPanel? Bwoink = default!;

        public BwoinkedWindow(BwoinkPanel bp)
        {
            RobustXamlLoader.Load(this);
            Bwoink = bp;
            ContentsContainer.AddChild(bp);
        }
    }
}
