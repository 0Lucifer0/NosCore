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
using ChickenAPI.Packets.ClientPackets.Inventory;
using ChickenAPI.Packets.ServerPackets.Inventory;
using ChickenAPI.Packets.ServerPackets.UI;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Configuration;
using NosCore.Core.I18N;
using NosCore.Data;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.GameObject;
using NosCore.GameObject.Providers.InventoryService;
using NosCore.GameObject.Providers.ItemProvider;
using NosCore.GameObject.Providers.ItemProvider.Handlers;
using NosCore.GameObject.Providers.ItemProvider.Item;
using NosCore.Tests.Helpers;
using Serilog;

namespace NosCore.Tests.ItemHandlerTests
{
    [TestClass]
    public class BackPackTicketHandlerTests : UseItemEventHandlerTests
    {
        private ItemProvider _itemProvider;
        private Mock<ILogger> _logger;

        [TestInitialize]
        public void Setup()
        {
            _logger = new Mock<ILogger>();
            _session = TestHelpers.Instance.GenerateSession();
            _handler = new BackPackTicketHandler(_logger.Object, new WorldConfiguration {MaxAdditionalSpPoints = 1});
            var items = new List<ItemDto>
            {
                new Item {VNum = 1, ItemType = ItemType.Special, EffectValue = 0},
            };
            _itemProvider = new ItemProvider(items,
                new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>());
        }
        [TestMethod]
        public void Test_Can_Not_Stack()
        {
            _session.Character.StaticBonusList.Add(new StaticBonusDto
            {
                CharacterId = _session.Character.CharacterId,
                DateEnd = null,
                StaticBonusType = StaticBonusType.InventoryTicketUpgrade
            });
            var itemInstance = InventoryItemInstance.Create(_itemProvider.Create(1), _session.Character.CharacterId);
            _session.Character.Inventory.AddItemToPocket(itemInstance);
            ExecuteInventoryItemInstanceEventHandler(itemInstance);

            Assert.AreEqual(1, _session.Character.StaticBonusList.Count);
            Assert.AreEqual(1, _session.Character.Inventory.Count);
        }

        [TestMethod]
        public void Test_BackPackTicket()
        {
            var itemInstance = InventoryItemInstance.Create(_itemProvider.Create(1), _session.Character.CharacterId);
            _session.Character.Inventory.AddItemToPocket(itemInstance);
            ExecuteInventoryItemInstanceEventHandler(itemInstance);
            var lastpacket = (ExtsPacket)_session.LastPackets.FirstOrDefault(s => s is ExtsPacket);
            Assert.IsNotNull(lastpacket);
            Assert.AreEqual(1, _session.Character.StaticBonusList.Count);
            Assert.AreEqual(60, _session.Character.Inventory.Expensions[NoscorePocketType.Etc]);
            Assert.AreEqual(60, _session.Character.Inventory.Expensions[NoscorePocketType.Equipment]);
            Assert.AreEqual(60, _session.Character.Inventory.Expensions[NoscorePocketType.Main]);
            Assert.AreEqual(0, _session.Character.Inventory.Count);
        }
    }
}