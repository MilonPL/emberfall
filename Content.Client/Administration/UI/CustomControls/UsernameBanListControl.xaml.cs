using System.Linq;
using Content.Client.Administration.Managers;
using Content.Client.UserInterface.Controls;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Input;

namespace Content.Client.Administration.UI.CustomControls;

[GenerateTypedNameReferences]
public sealed partial class UsernameBanListControl : BoxContainer
{
    private readonly IClientUsernameBanCacheManager _usernameBanCache;
    public event Action<int?>? OnSelectionChanged;
    private int? _selectedId;
    public UsernameBanListControl()
    {
        _usernameBanCache = IoCManager.Resolve<IClientUsernameBanCacheManager>();
        RobustXamlLoader.Load(this);
        UsernameBanListContainer.GenerateItem += GenerateUsernameBanButton;
        UsernameBanListContainer.ItemPressed += UsernameBanListItemPressed;
        UsernameBanListContainer.NoItemSelected += UsernameBanNoItemSelected;
        _usernameBanCache.UpdatedCache += PopulateList;
        PopulateList(_usernameBanCache.BanList);
    }

    private void PopulateList(IReadOnlyList<UsernameCacheLine>? banData = null)
    {
        banData ??= _usernameBanCache.BanList;
        UsernameBanListContainer.PopulateList(banData.Select(info => new UsernameBanListData(info)).ToList());
    }

    private void GenerateUsernameBanButton(ListData data, ListContainerButton button)
    {
        if (data is not UsernameBanListData { Info: var info })
        {
            return;
        }

        var entry = new UsernameBanListEntry();
        entry.Setup(info);
        button.AddChild(entry);
        button.AddStyleClass(ListContainer.StyleClassListContainerButton);
    }

    private void UsernameBanListItemPressed(BaseButton.ButtonEventArgs? args, ListData? data)
    {
        if (args == null || data is not UsernameBanListData { Info: var selectedUsernameBan })
        {
            return;
        }

        int selectedId = selectedUsernameBan.Id;

        if (args.Event.Function != EngineKeyFunctions.UIClick)
        {
            return;
        }

        if (selectedId == _selectedId)
        {
            return;
        }

        OnSelectionChanged?.Invoke(selectedId);
        _selectedId = selectedId;
    }


    private void UsernameBanNoItemSelected()
    {
        _selectedId = null;
        OnSelectionChanged?.Invoke(null);
    }
}

public record UsernameBanListData(UsernameCacheLine Info) : ListData;
