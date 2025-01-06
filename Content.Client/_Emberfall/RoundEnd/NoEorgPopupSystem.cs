using Content.Shared._Emberfall.CCVar;
using Content.Shared.GameTicking;
using Robust.Shared.Configuration;

namespace Content.Client._Emberfall.RoundEnd;

public sealed class NoEorgPopupSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private NoEorgPopup? _window;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<RoundEndMessageEvent>(OnRoundEnd);
    }

    private void OnRoundEnd(RoundEndMessageEvent ev)
    {
        if (_cfg.GetCVar(ECCVars.SkipRoundEndNoEorgPopup) || _cfg.GetCVar(ECCVars.RoundEndNoEorgPopup) == false)
            return;

        OpenNoEorgPopup();
    }

    private void OpenNoEorgPopup()
    {
        if (_window != null)
            return;

        _window = new NoEorgPopup();
        _window.OpenCentered();
        _window.OnClose += () => _window = null;
    }
}
