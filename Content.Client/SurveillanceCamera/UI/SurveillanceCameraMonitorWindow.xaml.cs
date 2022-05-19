using System.Linq;
using Content.Client.Viewport;
using Content.Shared.SurveillanceCamera;
using Robust.Client.AutoGenerated;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.XAML;

namespace Content.Client.SurveillanceCamera.UI;

[GenerateTypedNameReferences]
public sealed partial class SurveillanceCameraMonitorWindow : DefaultWindow
{
    public event Action<string>? CameraSelected;
    public event Action<string>? SubnetOpened;
    public event Action? CameraRefresh;
    public event Action? SubnetRefresh;

    private ScalingViewport? _cameraView;
    private FixedEye _defaultEye = new();

    private string? SelectedSubnet
    {
        get
        {
            if (SubnetSelector.ItemCount == 0
                || SubnetSelector.SelectedMetadata == null)
            {
                return null;
            }

            return (string) SubnetSelector.SelectedMetadata;
        }
    }

    public SurveillanceCameraMonitorWindow()
    {
        RobustXamlLoader.Load(this);

        CameraView.ViewportSize = new Vector2i(500, 500);
        CameraView.Eye = _defaultEye; // sure
        CameraViewBackground.Modulate = Color.Blue;

        SubnetList.OnItemSelected += OnSubnetListSelect;

        SubnetSelector.OnItemSelected += args =>
        {
            // piss
            SubnetOpened!((string) args.Button.SelectedMetadata!);
        };
        SubnetRefreshButton.OnPressed += _ => SubnetRefresh!();
        CameraRefreshButton.OnPressed += _ => CameraRefresh!();
    }

    // The UI class should get the eye from the entity, and then
    // pass it here so that the UI can change its view.
    public void UpdateState(IEye? eye, HashSet<string> subnets, string activeSubnet, Dictionary<string, string> cameras)
    {
        SetCameraView(eye);

        if (subnets.Count == 0)
        {
            SubnetSelector.Visible = false;
            return;
        }

        SubnetSelector.Visible = true;

        // That way, we have *a* subnet selected if this is ever opened.
        if (string.IsNullOrEmpty(activeSubnet))
        {
            SubnetOpened!(subnets.First());
            return;
        }

        // if the subnet count is unequal, that means
        // we have to rebuild the subnet selector
        if (SubnetSelector.ItemCount != subnets.Count)
        {
            SubnetSelector.Clear();

            foreach (var subnet in subnets)
            {
                var id = AddSubnet(subnet);
                if (subnet == activeSubnet)
                {
                    SubnetSelector.Select(id);
                }
            }
        }

        PopulateCameraList(cameras);
    }

    private void PopulateCameraList(Dictionary<string, string> cameras)
    {
        SubnetList.Clear();

        foreach (var (address, name) in cameras)
        {
            AddCameraToList(name, address);
        }

        SubnetList.SortItemsByText();
    }

    private void SetCameraView(IEye? eye)
    {
        CameraView.Eye = eye ?? _defaultEye;
        CameraView.Visible = eye != null;
    }

    private int AddSubnet(string subnet)
    {
        SubnetSelector.AddItem(subnet);
        SubnetSelector.SetItemMetadata(SubnetSelector.ItemCount - 1, subnet);

        return SubnetSelector.ItemCount - 1;
    }

    private void AddCameraToList(string name, string address)
    {
        // var button = CreateCameraButton(name, address);
        var item = SubnetList.AddItem($"{name} - {address}");
        item.Metadata = address;
    }

    private void OnSubnetListSelect(ItemList.ItemListSelectedEventArgs args)
    {
        CameraSelected!((string) SubnetList[args.ItemIndex].Metadata!);
    }

    private Button CreateCameraButton(string name, string address)
    {
        var button = new Button()
        {
            Text = $"{name} - {address}"
        };

        button.OnPressed += _ => CameraSelected!(address);

        return button;
    }
}
