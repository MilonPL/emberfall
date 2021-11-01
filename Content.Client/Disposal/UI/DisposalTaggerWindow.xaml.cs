using Content.Shared.Disposal.Components;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.XAML;
using static Content.Shared.Disposal.Components.SharedDisposalTaggerComponent;

namespace Content.Client.Disposal.UI
{
    /// <summary>
    /// Client-side UI used to control a <see cref="SharedDisposalTaggerComponent"/>
    /// </summary>
    [GenerateTypedNameReferences]
    public partial class DisposalTaggerWindow : SS14Window
    {
        public DisposalTaggerWindow()
        {
            RobustXamlLoader.Load(this);

            TagInput.IsValid = tag => TagRegex.IsMatch(tag);
        }


        public void UpdateState(DisposalTaggerUserInterfaceState state)
        {
            TagInput.Text = state.Tag;
        }
    }
}
