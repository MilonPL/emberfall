using Content.Server.Body.Systems;
using Content.Shared.Body.Components;
using Content.Shared.CharacterAppearance.Components;
using Content.Shared.CharacterAppearance.Systems;
using Robust.Server.GameObjects;

namespace Content.Server.CharacterAppearance.Systems
{
    public sealed class HumanoidAppearanceSystem : SharedHumanoidAppearanceSystem
    {
        [Dependency] private readonly BodySystem _bodySystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<HumanoidAppearanceComponent, ChangedHumanoidAppearanceEvent>(UpdateSkinColor);
        }

        private void UpdateSkinColor(EntityUid uid, HumanoidAppearanceComponent component, ChangedHumanoidAppearanceEvent _)
        {
            if (EntityManager.TryGetComponent<SharedBodyComponent>(uid, out SharedBodyComponent?  body))
            {
                foreach (var part in _bodySystem.GetAllParts(uid, body))
                {
                    if (EntityManager.TryGetComponent(part.Owner, out SpriteComponent? sprite))
                    {
                        sprite!.Color = component.Appearance.SkinColor;
                    }
                }
            }
        }
    }
}
