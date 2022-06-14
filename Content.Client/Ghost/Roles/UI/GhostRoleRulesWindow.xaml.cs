using System;
using Content.Shared.Ghost.Roles;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Localization;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

using Content.Shared.CCVar;
using Robust.Shared.Configuration;

namespace Content.Client.Ghost.Roles.UI
{
    [GenerateTypedNameReferences]
    public sealed partial class GhostRoleRulesWindow : DefaultWindow
    {
        [Dependency] private readonly IConfigurationManager _cfg = IoCManager.Resolve<IConfigurationManager>();
        private float _timer = 3.0f;

        public GhostRoleRulesWindow(string rules, Action<BaseButton.ButtonEventArgs> requestAction)
        {

            RobustXamlLoader.Load(this);
            TopBanner.SetMessage(FormattedMessage.FromMarkupPermissive(rules + Loc.GetString("ghost-roles-window-rules-footer")));
            RequestButton.OnPressed += requestAction;

            _cfg.OnValueChanged(CCVars.GhostRoleTime, value => _timer = value, true);
        }


        protected override void FrameUpdate(FrameEventArgs args)
        {
            base.FrameUpdate(args);
            if (!RequestButton.Disabled) return;
            if (_timer > 0.0)
            {
                _timer -= args.DeltaSeconds;
            }
            else
            {
                RequestButton.Disabled = false;
            }
        }
    }
}
