//  __  _  __    __   ___ __  ___ ___
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
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Core.Configuration;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Services.EventLoaderService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using Serilog;

namespace NosCore.GameObject.Tests.Services.InventoryService
{
    [TestClass]
    public class InventoryServiceTests
    {
        private static readonly ILogger Logger = new Mock<ILogger>().Object;
        private GameObject.Services.ItemGenerationService.ItemGenerationService? _itemProvider;

        private IInventoryService? Inventory { get; set; }

        [TestInitialize]
        public void Setup()
        {
            var items = new List<ItemDto>
            {
                new Item {Type = NoscorePocketType.Main, VNum = 1012},
                new Item {Type = NoscorePocketType.Main, VNum = 1013},
                new Item {Type = NoscorePocketType.Etc, VNum = 1027},
                new Item {Type = NoscorePocketType.Equipment, VNum = 1, ItemType = ItemType.Weapon},
                new Item {Type = NoscorePocketType.Equipment, VNum = 2, ItemType = ItemType.Weapon},
                new Item {Type = NoscorePocketType.Equipment, VNum = 912, ItemType = ItemType.Specialist},
                new Item {Type = NoscorePocketType.Equipment, VNum = 924, ItemType = ItemType.Fashion}
            };
            _itemProvider = new GameObject.Services.ItemGenerationService.ItemGenerationService(items,
                new EventLoaderService<Item, Tuple<InventoryItemInstance, UseItemPacket>, IUseItemEventHandler>(new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>()), Logger);
            Inventory = new GameObject.Services.InventoryService.InventoryService(items, Options.Create(new WorldConfiguration { BackpackSize = 3, MaxItemAmount = 999 }),
                Logger);
        }

        [TestMethod]
        public void CreateItem()
        {
            var item = Inventory!.AddItemToPocket(InventoryItemInstance.Create(_itemProvider!.Create(1012), 0))!.First();
            Assert.IsTrue((item.ItemInstance?.Amount == 1) && (item.ItemInstance.ItemVNum == 1012) &&
                (item.Type == NoscorePocketType.Main));
        }

        [TestMethod]
        public void CreateItemAndStackIt()
        {
            Inventory!.AddItemToPocket(InventoryItemInstance.Create(_itemProvider!.Create(1012), 0))!.First();
            var item = Inventory.AddItemToPocket(InventoryItemInstance.Create(_itemProvider.Create(1012), 0))!.First();
            Assert.IsTrue((item.ItemInstance?.Amount == 2) && (item.ItemInstance.ItemVNum == 1012));
        }

        [TestMethod]
        public void CreateItemWhenSlotMax()
        {
            Inventory!.AddItemToPocket(InventoryItemInstance.Create(_itemProvider!.Create(1012, 999), 0))!.First();
            var items = Inventory.AddItemToPocket(InventoryItemInstance.Create(_itemProvider.Create(1012), 0));
            Assert.IsTrue(items![0]!.ItemInstance?.Amount == 1);
        }

        [TestMethod]
        public void CreateItemWhenSlotFilled()
        {
            Inventory!.AddItemToPocket(InventoryItemInstance.Create(_itemProvider!.Create(1012, 990), 0))!.First();
            var items = Inventory.AddItemToPocket(InventoryItemInstance.Create(_itemProvider.Create(1012, 29), 0));
            Assert.IsTrue((items![0]!.ItemInstance?.Amount == 999) && (items.Last()!.ItemInstance?.Amount == 20));
        }

        [TestMethod]
        public void CreateItemAndFillMultiSlot()
        {
            Inventory!.AddItemToPocket(InventoryItemInstance.Create(_itemProvider!.Create(1012, 990), 0),
                NoscorePocketType.Main, 0);
            Inventory.AddItemToPocket(InventoryItemInstance.Create(_itemProvider.Create(1012, 990), 0),
                NoscorePocketType.Main, 1);
            Inventory.AddItemToPocket(InventoryItemInstance.Create(_itemProvider.Create(1012, 990), 0),
                NoscorePocketType.Main, 2);
            var items = Inventory.AddItemToPocket(InventoryItemInstance.Create(_itemProvider.Create(1012, 27), 0))!;
            Assert.IsTrue(items.All(item => item.ItemInstance!.Amount == 999) && (items?.Count == 3));
        }

        [TestMethod]
        public void CreateMoreItemThanInventoryPlace()
        {
            Inventory!.AddItemToPocket(InventoryItemInstance.Create(_itemProvider!.Create(1012, 990), 0),
                NoscorePocketType.Main, 0);
            Inventory.AddItemToPocket(InventoryItemInstance.Create(_itemProvider.Create(1012, 990), 0),
                NoscorePocketType.Main, 1);
            Inventory.AddItemToPocket(InventoryItemInstance.Create(_itemProvider.Create(1012, 990), 0),
                NoscorePocketType.Main, 2);
            var items = Inventory.AddItemToPocket(InventoryItemInstance.Create(_itemProvider.Create(1012, 99), 0));
            Assert.IsTrue(Inventory.Values.All(item => item.ItemInstance?.Amount == 990) && (items?.Count == 0));
        }

        [TestMethod]
        public void CreateStackOnASpecificItem()
        {
            Inventory!.AddItemToPocket(InventoryItemInstance.Create(_itemProvider!.Create(1012, 990), 0));
            Inventory.AddItemToPocket(InventoryItemInstance.Create(_itemProvider.Create(1013, 990), 0));
            Inventory.AddItemToPocket(InventoryItemInstance.Create(_itemProvider.Create(1013), 0));
            Assert.IsTrue(Inventory.Values.First(item => item.Slot == 1).ItemInstance!.Amount == 991);
        }

        [TestMethod]
        public void CreateDoesntStackOnWrongItem()
        {
            Inventory!.AddItemToPocket(InventoryItemInstance.Create(_itemProvider!.Create(1012, 990), 0));
            Inventory.AddItemToPocket(InventoryItemInstance.Create(_itemProvider.Create(1013, 990), 0));
            Inventory.AddItemToPocket(InventoryItemInstance.Create(_itemProvider.Create(1013, 19), 0));
            Assert.IsTrue(Inventory.Values.First(item => item.Slot == 0).ItemInstance?.Amount == 990);
        }

        [TestMethod]
        public void LoadItemOnAnNotEmptySlot()
        {
            Inventory!.AddItemToPocket(InventoryItemInstance.Create(_itemProvider!.Create(1012, 990), 0));
            var item = Inventory.LoadBySlotAndType(0, NoscorePocketType.Main);
            Assert.IsTrue((item!.ItemInstance?.ItemVNum == 1012) && (item.ItemInstance?.Amount == 990));
        }

        [TestMethod]
        public void LoadAnNonExistingItem()
        {
            Inventory!.AddItemToPocket(InventoryItemInstance.Create(_itemProvider!.Create(1012, 990), 0));
            var item = Inventory.LoadBySlotAndType(1, NoscorePocketType.Main);
            Assert.IsNull(item);
        }

        [TestMethod]
        public void DeleteFromTypeAndSlot()
        {
            Inventory!.AddItemToPocket(InventoryItemInstance.Create(_itemProvider!.Create(1012, 990), 0));
            Inventory.AddItemToPocket(InventoryItemInstance.Create(_itemProvider.Create(1012, 990), 0));
            Assert.IsTrue(Inventory.Count == 2);
            var item = Inventory.DeleteFromTypeAndSlot(NoscorePocketType.Main, 0);
            Assert.IsNull(item);
            Assert.IsTrue(Inventory.Count == 1);
        }

        [TestMethod]
        public void Delete()
        {
            Inventory!.AddItemToPocket(InventoryItemInstance.Create(_itemProvider!.Create(1012, 990), 0));
            var items = Inventory.AddItemToPocket(InventoryItemInstance.Create(_itemProvider.Create(1012, 990), 0));
            Assert.IsTrue(Inventory.Count == 2);
            var item = Inventory.DeleteById(items![0].ItemInstanceId);
            Assert.IsNull(item);
            Assert.IsTrue(Inventory.Count == 1);
        }

        [TestMethod]
        public void MoveFullSlot()
        {
            var item = Inventory!.AddItemToPocket(InventoryItemInstance.Create(_itemProvider!.Create(1012, 999), 0))!
                .First();
            Inventory.TryMoveItem(item.Type, item.Slot, 999, 1, out var originItem, out var destinationItem);
            Assert.IsTrue(originItem == null);
            Assert.IsTrue((destinationItem?.ItemInstance?.Amount == 999) && (destinationItem.Slot == 1));
        }

        [TestMethod]
        public void MoveHalfSlot()
        {
            var item = Inventory!.AddItemToPocket(InventoryItemInstance.Create(_itemProvider!.Create(1012, 999), 0))!
                .First();
            Inventory.TryMoveItem(item.Type, item.Slot, 499, 1, out var originItem, out var destinationItem);
            Assert.IsTrue((originItem?.ItemInstance?.Amount == 500) && (originItem.Slot == 0));
            Assert.IsTrue((destinationItem?.ItemInstance?.Amount == 499) && (destinationItem.Slot == 1));
        }

        [TestMethod]
        public void MoveHalfSlotAndMergeThem()
        {
            var item = Inventory!.AddItemToPocket(InventoryItemInstance.Create(_itemProvider!.Create(1012, 999), 0))!
                .First();
            Inventory.TryMoveItem(item.Type, item.Slot, 499, 1, out _, out _);
            Inventory.TryMoveItem(item.Type, 0, 500, 1, out var originItem, out var destinationItem);
            Assert.IsTrue(originItem == null);
            Assert.IsTrue((destinationItem?.ItemInstance?.Amount == 999) && (destinationItem.Slot == 1));
        }

        [TestMethod]
        public void MoveHalfSlotAndMergeThemWithOverflow()
        {
            var item = Inventory!.AddItemToPocket(InventoryItemInstance.Create(_itemProvider!.Create(1012, 999), 0))!
                .First();
            Inventory.AddItemToPocket(InventoryItemInstance.Create(_itemProvider.Create(1012, 500), 0));
            Inventory.TryMoveItem(item.Type, item.Slot, 600, 1, out var originItem, out var destinationItem);
            Assert.IsTrue((originItem?.ItemInstance?.Amount == 500) && (originItem.Slot == 0));
            Assert.IsTrue((destinationItem?.ItemInstance?.Amount == 999) && (destinationItem.Slot == 1));
        }

        [TestMethod]
        public void RemoveItemAmountFromInventoryByGuidWithoutRemove()
        {
            var item = Inventory!.AddItemToPocket(InventoryItemInstance.Create(_itemProvider!.Create(1027, 999), 0))!.First();
            Inventory!.RemoveItemAmountFromInventory(499, item.ItemInstanceId);
            Assert.IsTrue(item.ItemInstance?.Amount == 500);
        }

        [TestMethod]
        public void RemoveItemAmountFromInventoryByGuidWithRemove()
        {
            var item = Inventory!.AddItemToPocket(InventoryItemInstance.Create(_itemProvider!.Create(1027, 999), 0))!.First();
            Inventory!.RemoveItemAmountFromInventory(999, item.ItemInstanceId);
            Assert.IsTrue(Inventory.LoadBySlotAndType(item.Slot, NoscorePocketType.Etc) == null);
        }

        [TestMethod]
        public void RemoveItemAmountFromInventoryByVNum()
        {
            var itemSlot0 = Inventory!.AddItemToPocket(InventoryItemInstance.Create(_itemProvider!.Create(1027, 5), 0), NoscorePocketType.Etc, 0)!.First();
            var itemSlot1 = Inventory!.AddItemToPocket(InventoryItemInstance.Create(_itemProvider!.Create(1027, 3), 0), NoscorePocketType.Etc, 1)!.First();
            var itemSlot2 = Inventory!.AddItemToPocket(InventoryItemInstance.Create(_itemProvider!.Create(1027, 10), 0), NoscorePocketType.Etc, 2)!.First();
            Inventory!.RemoveItemAmountFromInventory(15, 1027);
            Assert.IsTrue(Inventory.LoadBySlotAndType(itemSlot2.Slot, NoscorePocketType.Etc)?.ItemInstance == null);
            Assert.IsTrue(Inventory.LoadBySlotAndType(itemSlot1.Slot, NoscorePocketType.Etc)?.ItemInstance == null);
            Assert.IsTrue(Inventory.LoadBySlotAndType(itemSlot0.Slot, NoscorePocketType.Etc)?.ItemInstance?.Amount == 3);
        }

        [TestMethod]
        public void RemoveItemAmountFromInventoryByVNumWithoutItemOrAmount()
        {
            Assert.IsTrue(Inventory?.RemoveItemAmountFromInventory(15, 1027).Count == 0);
            Assert.IsTrue(Inventory?.RemoveItemAmountFromInventory(0, 1027).Count == 0);
            Inventory?.AddItemToPocket(InventoryItemInstance.Create(_itemProvider!.Create(1027, 10), 0), NoscorePocketType.Etc, 0)?.First();
            Assert.IsTrue(Inventory?.RemoveItemAmountFromInventory(15, 1027).Count == 0);
        }

        [TestMethod]
        public void LoadByVNumAndAmount()
        {
            Inventory?.AddItemToPocket(InventoryItemInstance.Create(_itemProvider!.Create(1027, 5), 0), NoscorePocketType.Etc, 0)?.First();
            Inventory?.AddItemToPocket(InventoryItemInstance.Create(_itemProvider!.Create(1027, 3), 0), NoscorePocketType.Etc, 1)?.First();
            Assert.IsTrue(Inventory?.LoadByVNumAndAmount(1027, 3).Count == 1);
            Assert.IsTrue(Inventory?.LoadByVNumAndAmount(1027, 6).Count == 2);
        }

        [TestMethod]
        public void LoadByVNumAndAmountWithoutItemOrAmount()
        {
            Assert.IsTrue(Inventory?.LoadByVNumAndAmount(1027, 3).Count == 0);
            Inventory?.AddItemToPocket(InventoryItemInstance.Create(_itemProvider!.Create(1027, 5), 0), NoscorePocketType.Etc, 0)?.First();
            Assert.IsTrue(Inventory?.LoadByVNumAndAmount(1027, 3).Count == 1);
            Assert.IsTrue(Inventory?.LoadByVNumAndAmount(1027, 6).Count == 0);
            Inventory?.AddItemToPocket(InventoryItemInstance.Create(_itemProvider!.Create(1027, 10), 0), NoscorePocketType.Etc, 1)?.First();
            Assert.IsTrue(Inventory?.LoadByVNumAndAmount(1027, 12).Count == 2);
        }

        //TODO EnoughPlace

        [TestMethod]
        public void MoveFashionToFashionPocket()
        {
            var fashion = Inventory!.AddItemToPocket(InventoryItemInstance.Create(_itemProvider!.Create(924), 0))!.First();
            var item = Inventory.MoveInPocket(fashion.Slot, fashion.Type, NoscorePocketType.Costume);
            Assert.IsTrue(item?.Type == NoscorePocketType.Costume);
        }

        [TestMethod]
        public void MoveFashionToSpecialistPocket()
        {
            var fashion = Inventory!.AddItemToPocket(InventoryItemInstance.Create(_itemProvider!.Create(924), 0))!.First();
            var item = Inventory.MoveInPocket(fashion.Slot, fashion.Type, NoscorePocketType.Specialist);
            Assert.IsNull(item);
        }

        [TestMethod]
        public void MoveSpecialistToFashionPocket()
        {
            var specialist = Inventory!.AddItemToPocket(InventoryItemInstance.Create(_itemProvider!.Create(912), 0))!
                .First();
            var item = Inventory.MoveInPocket(specialist.Slot, specialist.Type, NoscorePocketType.Costume);
            Assert.IsNull(item);
        }

        [TestMethod]
        public void MoveSpecialistToSpecialistPocket()
        {
            var specialist = Inventory!.AddItemToPocket(InventoryItemInstance.Create(_itemProvider!.Create(912), 0))!
                .First();
            var item = Inventory.MoveInPocket(specialist.Slot, specialist.Type, NoscorePocketType.Specialist);
            Assert.IsTrue(item?.Type == NoscorePocketType.Specialist);
        }

        [TestMethod]
        public void MoveWeaponToPocket()
        {
            var weapon = Inventory!.AddItemToPocket(InventoryItemInstance.Create(_itemProvider!.Create(1), 0))!.First();
            var item = Inventory.MoveInPocket(weapon.Slot, weapon.Type, NoscorePocketType.Wear);
            Assert.IsTrue(item?.Type == NoscorePocketType.Wear);
        }

        [TestMethod]
        public void SwapWithEmpty()
        {
            var weapon = Inventory!.AddItemToPocket(InventoryItemInstance.Create(_itemProvider!.Create(1), 0))!.First();
            var item = Inventory.MoveInPocket(weapon.Slot, weapon.Type, NoscorePocketType.Wear,
                (short)EquipmentType.MainWeapon, true);
            Assert.IsTrue((item?.Type == NoscorePocketType.Wear) &&
                (Inventory.LoadBySlotAndType(0, NoscorePocketType.Equipment) == null));
        }

        [TestMethod]
        public void SwapWithNotEmpty()
        {
            var weapon = Inventory!.AddItemToPocket(InventoryItemInstance.Create(_itemProvider!.Create(2), 0))!.First();
            var weapon2 = Inventory.AddItemToPocket(InventoryItemInstance.Create(_itemProvider.Create(1), 0))!.First();
            var item = Inventory.MoveInPocket(weapon.Slot, weapon.Type, NoscorePocketType.Wear,
                (short)EquipmentType.MainWeapon, true);
            var item2 = Inventory.MoveInPocket(weapon2.Slot, weapon2.Type, NoscorePocketType.Wear,
                (short)EquipmentType.MainWeapon, true);

            Assert.IsTrue((item?.Type == NoscorePocketType.Equipment) && (item.Slot == 1) &&
                (item2?.Type == NoscorePocketType.Wear) &&
                (item2.Slot == (short)EquipmentType.MainWeapon));
        }
    }
}