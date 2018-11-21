using Content.Shared.Interfaces;
using SS14.Client;
using SS14.Shared.Map;

namespace Content.Client.Interfaces
{
    public interface IClientNotifyManager : ISharedNotifyManager
    {
        void PopupMessage(ScreenCoordinates coordinates, string message);
        void PopupMessage(string message);
        void FrameUpdate(RenderFrameEventArgs eventArgs);
    }
}
