using System.Threading.Tasks;
using Content.Server.Popups;
using Content.Shared.Construction;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Player;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Construction.Completions;

[DataDefinition]
public class PopupEveryone : IGraphAction
{
    [DataField("text")] public string Text { get; } = string.Empty;

    public void PerformAction(EntityUid uid, EntityUid? userUid, IEntityManager entityManager)
    {
        entityManager.EntitySysManager.GetEntitySystem<PopupSystem>()
                     .PopupEntity(Loc.GetString(Text), uid, Filter.Pvs(uid, entityManager:entityManager));
    }
}