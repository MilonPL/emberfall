﻿using System.Text;
using Content.Server.Popups;
using Content.Shared.Censor;
using Content.Shared.Popups;
using JetBrains.Annotations;
using Robust.Shared.Player;

namespace Content.Server.Censor.Actions;

[UsedImplicitly]
public sealed class CensorActionWarningPopup : ICensorAction
{
    [DataField]
    public string Reason = "censor-action-warning-popup-reason";

    public bool SkipCensor(string fullText, Dictionary<string, int> matchedText)
    {
        return false;
    }

    public bool RunAction(ICommonSession session,
        string fullText,
        Dictionary<string, int> matchedText,
        CensorFilter censor,
        IEntityManager entMan)
    {
        entMan.System<PopupSystem>()
            .PopupCursor(Loc.GetString(Reason,
                    ("matches", new StringBuilder().AppendJoin(", ", matchedText.Keys)),
                    ("censorName", censor.DisplayName)),
                session,
                PopupType.LargeCaution);

        return true;
    }
}
