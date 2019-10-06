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

using System.Linq;
using ChickenAPI.Packets.ClientPackets.Inventory;
using ChickenAPI.Packets.Enumerations;
using ChickenAPI.Packets.ServerPackets.UI;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Core;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Providers.InventoryService;
using NosCore.GameObject.Providers.ItemProvider;
using NosCore.PacketHandlers.Inventory;
using NosCore.Tests.Helpers;

namespace NosCore.Tests.PacketHandlerTests
{
    [TestClass]
    public class PutPacketHandlerTests
    {
        private IItemProvider _item;
        private PutPacketHandler _putPacketHandler;
        private ClientSession _session;

        [TestCleanup]
        public void Cleanup()
        {
            SystemTime.Freeze(SystemTime.Now());
        }

        [TestInitialize]
        public void Setup()
        {
            SystemTime.Freeze();
            TestHelpers.Reset();
            _item = TestHelpers.Instance.GenerateItemProvider();
            _session = TestHelpers.Instance.GenerateSession();
            _putPacketHandler = new PutPacketHandler(_session.WorldConfiguration);
        }

        [TestMethod]
        public void Test_PutPartialSlot()
        {
            _session.Character.Inventory.AddItemToPocket(InventoryItemInstance.Create(_item.Create(1012, 999), 0));
            _putPacketHandler.Execute(new PutPacket
            {
                PocketType = PocketType.Main,
                Slot = 0,
                Amount = 500
            }, _session);
            Assert.IsTrue((_session.Character.Inventory.Count == 1) &&
                (_session.Character.Inventory.FirstOrDefault().Value.ItemInstance.Amount == 499));
        }

        [TestMethod]
        public void Test_PutNotDroppable()
        {
            _session.Character.Inventory.AddItemToPocket(InventoryItemInstance.Create(_item.Create(1013, 1), 0));
            _putPacketHandler.Execute(new PutPacket
            {
                PocketType = PocketType.Main,
                Slot = 0,
                Amount = 1
            }, _session);
            var packet = (MsgPacket) _session.LastPackets.FirstOrDefault(s => s is MsgPacket);
            Assert.IsTrue((packet.Message == Language.Instance.GetMessageFromKey(LanguageKey.ITEM_NOT_DROPPABLE,
                _session.Account.Language)) && (packet.Type == 0));
            Assert.IsTrue(_session.Character.Inventory.Count > 0);
        }


        [TestMethod]
        public void Test_Put()
        {
            _session.Character.Inventory.AddItemToPocket(InventoryItemInstance.Create(_item.Create(1012, 1), 0));
            _putPacketHandler.Execute(new PutPacket
            {
                PocketType = PocketType.Main,
                Slot = 0,
                Amount = 1
            }, _session);
            Assert.IsTrue(_session.Character.Inventory.Count == 0);
        }

        [TestMethod]
        public void Test_PutBadPlace()
        {
            _session.Character.PositionX = 2;
            _session.Character.PositionY = 2;
            _session.Character.Inventory.AddItemToPocket(InventoryItemInstance.Create(_item.Create(1012, 1), 0));
            _putPacketHandler.Execute(new PutPacket
            {
                PocketType = PocketType.Main,
                Slot = 0,
                Amount = 1
            }, _session);
            var packet = (MsgPacket) _session.LastPackets.FirstOrDefault(s => s is MsgPacket);
            Assert.IsTrue((packet.Message == Language.Instance.GetMessageFromKey(LanguageKey.ITEM_NOT_DROPPABLE_HERE,
                _session.Account.Language)) && (packet.Type == 0));
            Assert.IsTrue(_session.Character.Inventory.Count > 0);
        }

        [TestMethod]
        public void Test_PutOutOfBounds()
        {
            _session.Character.PositionX = -1;
            _session.Character.PositionY = -1;
            _session.Character.Inventory.AddItemToPocket(InventoryItemInstance.Create(_item.Create(1012, 1), 0));
            _putPacketHandler.Execute(new PutPacket
            {
                PocketType = PocketType.Main,
                Slot = 0,
                Amount = 1
            }, _session);
            var packet = (MsgPacket) _session.LastPackets.FirstOrDefault(s => s is MsgPacket);
            Assert.IsTrue((packet.Message == Language.Instance.GetMessageFromKey(LanguageKey.ITEM_NOT_DROPPABLE_HERE,
                _session.Account.Language)) && (packet.Type == 0));
            Assert.IsTrue(_session.Character.Inventory.Count > 0);
        }
    }
}