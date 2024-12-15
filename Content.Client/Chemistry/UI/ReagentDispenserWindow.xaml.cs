using Content.Client.Chemistry.EntitySystems;
using Content.Client.Stylesheets;
using Content.Client.UserInterface.Controls;
using Content.Shared.Chemistry;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Prototypes;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client.Chemistry.UI
{
    /// <summary>
    /// Client-side UI used to control a <see cref="ReagentDispenserComponent"/>.
    /// </summary>
    [GenerateTypedNameReferences]
    public sealed partial class ReagentDispenserWindow : FancyWindow
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        private readonly ChemistryRegistrySystem _chemistryRegistry;
        public event Action<string>? OnDispenseReagentButtonPressed;
        public event Action<string>? OnEjectJugButtonPressed;

        /// <summary>
        /// Create and initialize the dispenser UI client-side. Creates the basic layout,
        /// actual data isn't filled in until the server sends data about the dispenser.
        /// </summary>
        public ReagentDispenserWindow()
        {
            RobustXamlLoader.Load(this);
            IoCManager.InjectDependencies(this);
            _chemistryRegistry = _entityManager.System<ChemistryRegistrySystem>();
        }

        /// <summary>
        /// Update the button grid of reagents which can be dispensed.
        /// </summary>
        /// <param name="inventory">Reagents which can be dispensed by this dispenser</param>
        public void UpdateReagentsList(List<ReagentInventoryItem> inventory)
        {
            if (ReagentList == null)
                return;

            ReagentList.Children.Clear();
            //Sort inventory by reagentLabel
            inventory.Sort((x, y) => x.ReagentLabel.CompareTo(y.ReagentLabel));

            foreach (var item in inventory)
            {
                var card = new ReagentCardControl(item);
                card.OnPressed += OnDispenseReagentButtonPressed;
                card.OnEjectButtonPressed += OnEjectJugButtonPressed;
                ReagentList.Children.Add(card);
            }
        }

        /// <summary>
        /// Update the UI state when new state data is received from the server.
        /// </summary>
        /// <param name="state">State data sent by the server.</param>
        public void UpdateState(BoundUserInterfaceState state)
        {
            var castState = (ReagentDispenserBoundUserInterfaceState) state;
            UpdateContainerInfo(castState);
            UpdateReagentsList(castState.Inventory);

            _entityManager.TryGetEntity(castState.OutputContainerEntity, out var outputContainerEnt);
            View.SetEntity(outputContainerEnt);

            // Disable the Clear & Eject button if no beaker
            ClearButton.Disabled = castState.OutputContainer is null;
            EjectButton.Disabled = castState.OutputContainer is null;

            AmountGrid.Selected = ((int)castState.SelectedDispenseAmount).ToString();
        }

        /// <summary>
        /// Update the fill state and list of reagents held by the current reagent container, if applicable.
        /// <para>Also highlights a reagent if it's dispense button is being mouse hovered.</para>
        /// </summary>
        /// <param name="state">State data for the dispenser.</param>
        /// or null if no button is being hovered.</param>
        public void UpdateContainerInfo(ReagentDispenserBoundUserInterfaceState state)
        {
            ContainerInfo.Children.Clear();

            if (state.OutputContainer is null)
            {
                ContainerInfoName.Text = "";
                ContainerInfoFill.Text = "";
                ContainerInfo.Children.Add(new Label { Text = Loc.GetString("reagent-dispenser-window-no-container-loaded-text") });
                return;
            }

            // Set Name of the container and its fill status (Ex: 44/100u)
            ContainerInfoName.Text = state.OutputContainer.DisplayName;
            ContainerInfoFill.Text = state.OutputContainer.CurrentVolume + "/" + state.OutputContainer.MaxVolume;

            foreach (var (reagent, quantity) in state.OutputContainer.Reagents!)
            {
                // Try get to the prototype for the given reagent. This gives us its name.
                var localizedName = reagent.IsValid
                    ? reagent.Entity.Comp.LocalizedName
                    : Loc.GetString("reagent-dispenser-window-reagent-name-not-found-text");

                var nameLabel = new Label { Text = $"{localizedName}: " };
                var quantityLabel = new Label
                {
                    Text = Loc.GetString("reagent-dispenser-window-quantity-label-text", ("quantity", quantity)),
                    StyleClasses = { StyleNano.StyleClassLabelSecondaryColor },
                };

                ContainerInfo.Children.Add(new BoxContainer
                {
                    Orientation = LayoutOrientation.Horizontal,
                    Children =
                    {
                        nameLabel,
                        quantityLabel,
                    }
                });
            }
        }
    }
}
