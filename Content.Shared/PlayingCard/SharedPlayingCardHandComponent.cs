using Robust.Shared.Serialization;

namespace Content.Shared.PlayingCard;

public abstract class SharedPlayingCardHandComponent : Component, ISerializationHooks
{
    public string CardDeckID = string.Empty;
    [ViewVariables]
    [DataField("cardPrototype")]
    public string CardPrototype = string.Empty;
    [ViewVariables]
    [DataField("cardList")]
    public List<string> CardList = new();
}

[Serializable]
[NetSerializable]
public sealed class PlayingCardHandBoundUserInterfaceState : BoundUserInterfaceState
{
    public List<string> CardList { get; }

    public PlayingCardHandBoundUserInterfaceState(List<string> cardList)
    {
        CardList = cardList;
    }
}

[Serializable, NetSerializable]
public sealed class PickSingleCardMessage : BoundUserInterfaceMessage
{
    public readonly int ID;
    public PickSingleCardMessage(int id)
    {
        ID = id;
    }
}

[Serializable, NetSerializable]
public enum PlayingCardHandUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class CardListMessage : BoundUserInterfaceMessage
{
    public readonly List<String> Cards;
    public CardListMessage(List<String> cards)
    {
        Cards = cards;
    }
}
