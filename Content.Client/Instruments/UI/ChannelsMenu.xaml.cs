using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Audio.Midi;
using Robust.Shared.Timing;

namespace Content.Client.Instruments.UI;

[GenerateTypedNameReferences]
public sealed partial class ChannelsMenu : DefaultWindow
{
    private readonly InstrumentClientBoundUserInterface _owner;

    public ChannelsMenu(InstrumentClientBoundUserInterface owner) : base()
    {
        RobustXamlLoader.Load(this);
        _owner = owner;

        ChannelList.OnItemSelected += OnItemSelected;
        ChannelList.OnItemDeselected += OnItemDeselected;
        AllButton.OnPressed += OnAllPressed;
        ClearButton.OnPressed += OnClearPressed;
    }

    private void OnItemSelected(ItemList.ItemListSelectedEventArgs args)
    {
        _owner.Instruments.SetFilteredChannel(_owner.Owner, (int)ChannelList[args.ItemIndex].Metadata!, false);
    }

    private void OnItemDeselected(ItemList.ItemListDeselectedEventArgs args)
    {
        _owner.Instruments.SetFilteredChannel(_owner.Owner, (int)ChannelList[args.ItemIndex].Metadata!, true);
    }

    private void OnAllPressed(BaseButton.ButtonEventArgs obj)
    {
        foreach (var item in ChannelList)
        {
            // TODO: Make this efficient jfc
            item.Selected = true;
        }
    }

    private void OnClearPressed(BaseButton.ButtonEventArgs obj)
    {
        foreach (var item in ChannelList)
        {
            // TODO: Make this efficient jfc
            item.Selected = false;
        }
    }

    public void Populate()
    {
        ChannelList.Clear();

        for (int i = 0; i < RobustMidiEvent.MaxChannels; i++)
        {
            var item = ChannelList.AddItem(_owner.Loc.GetString("instrument-component-channel-name",
                ("number", i)), null, true, i);

            item.Selected = !_owner.Instrument?.FilteredChannels[i] ?? false;
        }
    }
}
