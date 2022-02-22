using Content.Client.Stylesheets;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.HUD.UI
{
    public sealed class NanoHeading : Container
    {
        private readonly Label _label;
        private readonly PanelContainer _panel;

        public NanoHeading()
        {
            _panel = new PanelContainer
            {
                Children = {(_label = new Label
                {
                    StyleClasses = {StyleNano.StyleClassLabelHeading}
                })}
            };
            AddChild(_panel);

            HorizontalAlignment = HAlignment.Left;
        }

        public string? Text
        {
            get => _label.Text;
            set => _label.Text = value;
        }
    }
}
