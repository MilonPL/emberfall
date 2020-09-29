﻿using System.Collections.Generic;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.Utility;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.GameObjects.Components.PipeDispenser
{
    public class PipeDispenserMenu : SS14Window
    {

        private readonly ItemList _itemList;
        private readonly Label _selectedLabel;
        private readonly LineEdit _searchBar;
        protected override Vector2? CustomSize => (400, 450);

        public PipeDispenserBoundUserInterface Owner { get; set; }

        private List<Item> _items = new List<Item>();
        private List<Item> _shownItems = new List<Item>();
        private Item _selectedItem;

        public PipeDispenserMenu(PipeDispenserBoundUserInterface owner = null)
        {
            IoCManager.InjectDependencies(this);

            Owner = owner;

            Title = "Pipe Dispenser Menu";

            var margin = new MarginContainer()
            {
                SizeFlagsVertical = SizeFlags.FillExpand,
                SizeFlagsHorizontal = SizeFlags.FillExpand,
            };

            var margin1 = new MarginContainer()
            {
                SizeFlagsVertical = SizeFlags.FillExpand,
                SizeFlagsHorizontal = SizeFlags.FillExpand,
            };

            var margin2 = new MarginContainer()
            {
                SizeFlagsVertical = SizeFlags.FillExpand,
                SizeFlagsHorizontal = SizeFlags.FillExpand,
            };

            var vBox = new VBoxContainer()
            {
                SizeFlagsVertical = SizeFlags.FillExpand,
                SeparationOverride = 5,
            };

            var buttonRow = new HBoxContainer()
            {
                SizeFlagsHorizontal = SizeFlags.FillExpand,
                SeparationOverride = 5,
            };

            var hBoxButtons = new HBoxContainer()
            {
                SizeFlagsHorizontal = SizeFlags.FillExpand,
                SizeFlagsVertical = SizeFlags.FillExpand,
                SizeFlagsStretchRatio = 1,
                Align = BoxContainer.AlignMode.Center,
            };

            var hBoxSelected = new HBoxContainer()
            {
                SizeFlagsHorizontal = SizeFlags.FillExpand,
                SizeFlagsVertical = SizeFlags.FillExpand,
                SizeFlagsStretchRatio = 1,
            };

            var hBoxFilter = new HBoxContainer()
            {
                SizeFlagsHorizontal = SizeFlags.FillExpand,
                SizeFlagsVertical = SizeFlags.FillExpand,
                SizeFlagsStretchRatio = 1
            };

            _searchBar = new LineEdit()
            {
                PlaceHolder = "Search",
                SizeFlagsHorizontal = SizeFlags.FillExpand,
                SizeFlagsStretchRatio = 3
            };

            _searchBar.OnTextChanged += Populate;

            var filterButton = new Button()
            {
                Text = "Filter",
                TextAlign = Label.AlignMode.Center,
                SizeFlagsHorizontal = SizeFlags.Fill,
                SizeFlagsStretchRatio = 1,
                Disabled = true,
            };

            var ejectOne = new Button()
            {
                Text = "1",
                TextAlign = Label.AlignMode.Center,
                SizeFlagsHorizontal = SizeFlags.Fill,
                SizeFlagsStretchRatio = 1
            };

            ejectOne.OnButtonUp += (_) => { Eject(1); };

            var ejectFive = new Button()
            {
                Text = "5",
                TextAlign = Label.AlignMode.Center,
                SizeFlagsHorizontal = SizeFlags.Fill,
                SizeFlagsStretchRatio = 1
            };

            ejectFive.OnButtonUp += (_) => { Eject(5); };

            var ejectTen = new Button()
            {
                Text = "10",
                TextAlign = Label.AlignMode.Center,
                SizeFlagsHorizontal = SizeFlags.Fill,
                SizeFlagsStretchRatio = 1
            };

            ejectTen.OnButtonUp += (_) => { Eject(10); };

            _itemList = new ItemList()
            {
                SizeFlagsStretchRatio = 8,
                SizeFlagsVertical = SizeFlags.FillExpand,
                SelectMode = ItemList.ItemListSelectMode.Button,
            };

            _itemList.OnItemSelected += ItemSelected;

            _selectedLabel = new Label()
            {
                Text = " - "
            };

            var marginFixed1 = new MarginContainer()
            {
                SizeFlagsVertical = SizeFlags.FillExpand,
                MarginLeftOverride = 10,
                MarginRightOverride = 10
            };

            var marginFixed2 = new MarginContainer()
            {
                SizeFlagsVertical = SizeFlags.FillExpand,
                MarginLeftOverride = 10,
                MarginRightOverride = 10
            };

            var marginFixed3 = new MarginContainer()
            {
                SizeFlagsVertical = SizeFlags.FillExpand,
                MarginRightOverride = 3
            };

            hBoxSelected.AddChild(_selectedLabel);

            hBoxButtons.AddChild(marginFixed1);
            hBoxButtons.AddChild(ejectOne);
            hBoxButtons.AddChild(margin1);
            hBoxButtons.AddChild(ejectFive);
            hBoxButtons.AddChild(margin2);
            hBoxButtons.AddChild(ejectTen);
            hBoxButtons.AddChild(marginFixed2);

            hBoxFilter.AddChild(_searchBar);
            hBoxFilter.AddChild(marginFixed3);
            hBoxFilter.AddChild(filterButton);

            vBox.AddChild(hBoxFilter);
            vBox.AddChild(_itemList);
            vBox.AddChild(hBoxSelected);
            vBox.AddChild(hBoxButtons);

            margin.AddChild(vBox);

            Contents.AddChild(margin);
        }

        public void ItemSelected(ItemList.ItemListSelectedEventArgs args)
        {
            _selectedItem = _shownItems[args.ItemIndex];
            _selectedLabel.Text = _shownItems[args.ItemIndex].Name;
        }
        
        /// <summary>
        ///     Adds shown items to the ItemList control.
        /// </summary>
        public void PopulateList()
        {
            _itemList.Clear();
            foreach (var item in _shownItems)
            {
                    _itemList.AddItem(item.Name, item.Icon);
            }
        }

        /// <summary>
        ///     Populates the list of items that will actually be shown, using the current filters.
        /// </summary>
        public void Populate()
        {
            _shownItems.Clear();

            foreach (var prototype in _items)
            {
                if (_searchBar.Text.Trim().Length != 0)
                {
                    if (prototype.Name.ToLowerInvariant().Contains(_searchBar.Text.Trim().ToLowerInvariant()))
                        _shownItems.Add(prototype);
                    continue;
                }

                _shownItems.Add(prototype);
            }

            PopulateList();
        }

        /// <inheritdoc cref="Populate"/>
        public void Populate(LineEdit.LineEditEventArgs args)
        {
            Populate();
        }

        /// <summary>
        /// Adds a new Item to the list of items inside the dispenser menu 
        /// </summary>
        public void AddItem(string id, string name, Texture icon)
        {
            _items.Add(new Item(id, name, icon));
            Populate();
        }

        private void Eject(uint amount)
        {
            if (_selectedItem == null)
                return;

            Owner.Eject(_selectedItem.ID, amount);
        }

        /// <summary>
        /// REEEEEEE!
        /// </summary>
        private class Item
        {
            public string ID;
            public string Name;
            public Texture Icon;
            public Item(string id, string name, Texture icon)
            {
                ID = id;
                Name = name;
                Icon = icon;
            }
        }
    }
}
