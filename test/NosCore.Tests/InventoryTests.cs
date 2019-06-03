﻿//  __  _  __    __   ___ __  ___ ___  
// |  \| |/__\ /' _/ / _//__\| _ \ __| 
// | | ' | \/ |`._`.| \_| \/ | v / _|  
// |_|\__|\__/ |___/ \__/\__/|_|_\___| 
// 
// Copyright (C) 2019 - NosCore
// 
// NosCore is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Configuration;
using NosCore.Core.I18N;
using NosCore.Data;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject;
using NosCore.GameObject.Providers.InventoryService;
using NosCore.GameObject.Providers.ItemProvider;
using NosCore.GameObject.Providers.ItemProvider.Item;
using Serilog;
using ChickenAPI.Packets.Enumerations;
using ChickenAPI.Packets.ClientPackets.Inventory;

namespace NosCore.Tests
{
    [TestClass]
    public class InventoryTests
    {
        private static readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();
        private ItemProvider _itemProvider;

        private IInventoryService Inventory { get; set; }

        [TestInitialize]
        public void Setup()
        {
            var items = new List<ItemDto>
            {
                new Item {Type = PocketType.Main, VNum = 1012},
                new Item {Type = PocketType.Main, VNum = 1013},
                new Item {Type = PocketType.Equipment, VNum = 1, ItemType = ItemType.Weapon},
                new Item {Type = PocketType.Equipment, VNum = 2, ItemType = ItemType.Weapon},
                new Item {Type = PocketType.Equipment, VNum = 912, ItemType = ItemType.Specialist},
                new Item {Type = PocketType.Equipment, VNum = 924, ItemType = ItemType.Fashion}
            };
            _itemProvider = new ItemProvider(items, new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>());
            Inventory = new InventoryService(items, new WorldConfiguration {BackpackSize = 3, MaxItemAmount = 999}, _logger);
        }

        [TestMethod]
        public void CreateItem()
        {
            var item = Inventory.AddItemToPocket(InventoryItemInstance.Create(_itemProvider.Create(1012), 0)).First();
            Assert.IsTrue(item.ItemInstance.Amount == 1 && item.ItemInstance.ItemVNum == 1012 && item.Type == PocketType.Main);
        }

        [TestMethod]
        public void CreateItemAndStackIt()
        {
            Inventory.AddItemToPocket(InventoryItemInstance.Create(_itemProvider.Create(1012), 0)).First();
            var item = Inventory.AddItemToPocket(InventoryItemInstance.Create(_itemProvider.Create(1012), 0)).First();
            Assert.IsTrue(item.ItemInstance.Amount == 2 && item.ItemInstance.ItemVNum == 1012);
        }

        [TestMethod]
        public void CreateItemWhenSlotMax()
        {
            Inventory.AddItemToPocket(InventoryItemInstance.Create(_itemProvider.Create(1012, 999), 0)).First();
            var items = Inventory.AddItemToPocket(InventoryItemInstance.Create(_itemProvider.Create(1012), 0));
            Assert.IsTrue(items[0].ItemInstance.Amount == 1);
        }

        [TestMethod]
        public void CreateItemWhenSlotFilled()
        {
            Inventory.AddItemToPocket(InventoryItemInstance.Create(_itemProvider.Create(1012, 990), 0)).First();
            var items = Inventory.AddItemToPocket(InventoryItemInstance.Create(_itemProvider.Create(1012, 29), 0));
            Assert.IsTrue(items[0].ItemInstance.Amount == 999 && items.Last().ItemInstance.Amount == 20);
        }

        [TestMethod]
        public void CreateItemAndFillMultiSlot()
        {
            Inventory.AddItemToPocket(InventoryItemInstance.Create(_itemProvider.Create(1012, 990), 0), PocketType.Main, 0);
            Inventory.AddItemToPocket(InventoryItemInstance.Create(_itemProvider.Create(1012, 990), 0), PocketType.Main, 1);
            Inventory.AddItemToPocket(InventoryItemInstance.Create(_itemProvider.Create(1012, 990), 0), PocketType.Main, 2);
            var items = Inventory.AddItemToPocket(InventoryItemInstance.Create(_itemProvider.Create(1012, 27), 0));
            Assert.IsTrue(items.All(item => item.ItemInstance.Amount == 999) && items.Count == 3);
        }

        [TestMethod]
        public void CreateMoreItemThanInventoryPlace()
        {
            Inventory.AddItemToPocket(InventoryItemInstance.Create(_itemProvider.Create(1012, 990), 0), PocketType.Main, 0);
            Inventory.AddItemToPocket(InventoryItemInstance.Create(_itemProvider.Create(1012, 990), 0), PocketType.Main, 1);
            Inventory.AddItemToPocket(InventoryItemInstance.Create(_itemProvider.Create(1012, 990), 0), PocketType.Main, 2);
            var items = Inventory.AddItemToPocket(InventoryItemInstance.Create(_itemProvider.Create(1012, 99), 0));
            Assert.IsTrue(Inventory.Values.All(item => item.ItemInstance.Amount == 990) && items.Count == 0);
        }

        [TestMethod]
        public void CreateStackOnASpecificItem()
        {
            Inventory.AddItemToPocket(InventoryItemInstance.Create(_itemProvider.Create(1012, 990), 0));
            Inventory.AddItemToPocket(InventoryItemInstance.Create(_itemProvider.Create(1013, 990), 0));
            Inventory.AddItemToPocket(InventoryItemInstance.Create(_itemProvider.Create(1013), 0));
            Assert.IsTrue(Inventory.Values.First(item => item.Slot == 1).ItemInstance.Amount == 991);
        }

        [TestMethod]
        public void CreateDoesntStackOnWrongItem()
        {
            Inventory.AddItemToPocket(InventoryItemInstance.Create(_itemProvider.Create(1012, 990), 0));
            Inventory.AddItemToPocket(InventoryItemInstance.Create(_itemProvider.Create(1013, 990), 0));
            Inventory.AddItemToPocket(InventoryItemInstance.Create(_itemProvider.Create(1013, 19), 0));
            Assert.IsTrue(Inventory.Values.First(item => item.Slot == 0).ItemInstance.Amount == 990);
        }

        [TestMethod]
        public void LoadItemOnAnNotEmptySlot()
        {
            Inventory.AddItemToPocket(InventoryItemInstance.Create(_itemProvider.Create(1012, 990), 0));
            var item = Inventory.LoadBySlotAndType(0, PocketType.Main);
            Assert.IsTrue(item.ItemInstance.ItemVNum == 1012 && item.ItemInstance.Amount == 990);
        }

        [TestMethod]
        public void LoadAnNonExistingItem()
        {
            Inventory.AddItemToPocket(InventoryItemInstance.Create(_itemProvider.Create(1012, 990), 0));
            var item = Inventory.LoadBySlotAndType(1, PocketType.Main);
            Assert.IsNull(item);
        }

        [TestMethod]
        public void DeleteFromTypeAndSlot()
        {
            Inventory.AddItemToPocket(InventoryItemInstance.Create(_itemProvider.Create(1012, 990), 0));
            Inventory.AddItemToPocket(InventoryItemInstance.Create(_itemProvider.Create(1012, 990), 0));
            Assert.IsTrue(Inventory.Count == 2);
            var item = Inventory.DeleteFromTypeAndSlot(PocketType.Main, 0);
            Assert.IsNull(item);
            Assert.IsTrue(Inventory.Count == 1);
        }

        [TestMethod]
        public void Delete()
        {
            Inventory.AddItemToPocket(InventoryItemInstance.Create(_itemProvider.Create(1012, 990), 0));
            var items = Inventory.AddItemToPocket(InventoryItemInstance.Create(_itemProvider.Create(1012, 990), 0));
            Assert.IsTrue(Inventory.Count == 2);
            var item = Inventory.DeleteById(items[0].ItemInstanceId);
            Assert.IsNull(item);
            Assert.IsTrue(Inventory.Count == 1);
        }

        [TestMethod]
        public void MoveFullSlot()
        {
            var item = Inventory.AddItemToPocket(InventoryItemInstance.Create(_itemProvider.Create(1012, 999), 0)).First();
            Inventory.TryMoveItem(item.Type, item.Slot, 999, 1, out var originItem, out var destinationItem);
            Assert.IsTrue(originItem == null);
            Assert.IsTrue(destinationItem?.ItemInstance.Amount == 999 && destinationItem.Slot == 1);
        }

        [TestMethod]
        public void MoveHalfSlot()
        {
            var item = Inventory.AddItemToPocket(InventoryItemInstance.Create(_itemProvider.Create(1012, 999), 0)).First();
            Inventory.TryMoveItem(item.Type, item.Slot, 499, 1, out var originItem, out var destinationItem);
            Assert.IsTrue(originItem?.ItemInstance.Amount == 500 && originItem.Slot == 0);
            Assert.IsTrue(destinationItem?.ItemInstance.Amount == 499 && destinationItem.Slot == 1);
        }

        [TestMethod]
        public void MoveHalfSlotAndMergeThem()
        {
            var item = Inventory.AddItemToPocket(InventoryItemInstance.Create(_itemProvider.Create(1012, 999), 0)).First();
            Inventory.TryMoveItem(item.Type, item.Slot, 499, 1, out _, out _);
            Inventory.TryMoveItem(item.Type, 0, 500, 1, out var originItem, out var destinationItem);
            Assert.IsTrue(originItem == null);
            Assert.IsTrue(destinationItem?.ItemInstance.Amount == 999 && destinationItem.Slot == 1);
        }

        [TestMethod]
        public void MoveHalfSlotAndMergeThemWithOverflow()
        {
            var item = Inventory.AddItemToPocket(InventoryItemInstance.Create(_itemProvider.Create(1012, 999), 0)).First();
            Inventory.AddItemToPocket(InventoryItemInstance.Create(_itemProvider.Create(1012, 500), 0));
            Inventory.TryMoveItem(item.Type, item.Slot, 600, 1, out var originItem, out var destinationItem);
            Assert.IsTrue(originItem?.ItemInstance.Amount == 500 && originItem.Slot == 0);
            Assert.IsTrue(destinationItem?.ItemInstance.Amount == 999 && destinationItem.Slot == 1);
        }

        //TODO RemoveItemAmountFromInventory

        //TODO EnoughPlace

        [TestMethod]
        public void MoveFashionToFashionPocket()
        {
            var fashion = Inventory.AddItemToPocket(InventoryItemInstance.Create(_itemProvider.Create(924), 0)).First();
            var item = Inventory.MoveInPocket(fashion.Slot, fashion.Type, PocketType.Costume);
            Assert.IsTrue(item.Type == PocketType.Costume);
        }

        [TestMethod]
        public void MoveFashionToSpecialistPocket()
        {
            var fashion = Inventory.AddItemToPocket(InventoryItemInstance.Create(_itemProvider.Create(924), 0)).First();
            var item = Inventory.MoveInPocket(fashion.Slot, fashion.Type, PocketType.Specialist);
            Assert.IsNull(item);
        }

        [TestMethod]
        public void MoveSpecialistToFashionPocket()
        {
            var specialist = Inventory.AddItemToPocket(InventoryItemInstance.Create(_itemProvider.Create(912), 0)).First();
            var item = Inventory.MoveInPocket(specialist.Slot, specialist.Type, PocketType.Costume);
            Assert.IsNull(item);
        }

        [TestMethod]
        public void MoveSpecialistToSpecialistPocket()
        {
            var specialist = Inventory.AddItemToPocket(InventoryItemInstance.Create(_itemProvider.Create(912), 0)).First();
            var item = Inventory.MoveInPocket(specialist.Slot, specialist.Type, PocketType.Specialist);
            Assert.IsTrue(item.Type == PocketType.Specialist);
        }

        [TestMethod]
        public void MoveWeaponToPocket()
        {
            var weapon = Inventory.AddItemToPocket(InventoryItemInstance.Create(_itemProvider.Create(1), 0)).First();
            var item = Inventory.MoveInPocket(weapon.Slot, weapon.Type, PocketType.Wear);
            Assert.IsTrue(item.Type == PocketType.Wear);
        }

        [TestMethod]
        public void SwapWithEmpty()
        {
            var weapon = Inventory.AddItemToPocket(InventoryItemInstance.Create(_itemProvider.Create(1), 0)).First();
            var item = Inventory.MoveInPocket(weapon.Slot, weapon.Type, PocketType.Wear,
                (short) EquipmentType.MainWeapon, true);
            Assert.IsTrue(item.Type == PocketType.Wear &&
                Inventory.LoadBySlotAndType(0, PocketType.Equipment) == null);
        }

        [TestMethod]
        public void SwapWithNotEmpty()
        {
            var weapon = Inventory.AddItemToPocket(InventoryItemInstance.Create(_itemProvider.Create(2), 0)).First();
            var weapon2 = Inventory.AddItemToPocket(InventoryItemInstance.Create(_itemProvider.Create(1), 0)).First();
            var item = Inventory.MoveInPocket(weapon.Slot, weapon.Type, PocketType.Wear,
                (short) EquipmentType.MainWeapon, true);
            var item2 = Inventory.MoveInPocket(weapon2.Slot, weapon2.Type, PocketType.Wear,
                (short) EquipmentType.MainWeapon, true);

            Assert.IsTrue(item.Type == PocketType.Equipment && item.Slot == 1 && item2.Type == PocketType.Wear &&
                item2.Slot == (short) EquipmentType.MainWeapon);
        }
    }
}