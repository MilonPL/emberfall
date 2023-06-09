using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;

namespace Content.Client.UserInterface.Controls
{
    [GenerateTypedNameReferences]
    public partial class ForegroundImageContainer : Container
    {
        public ForegroundImageContainer()
        {
            RobustXamlLoader.Load(this);
            XamlChildren = ContentsContainer.Children;
        }
    }
}
