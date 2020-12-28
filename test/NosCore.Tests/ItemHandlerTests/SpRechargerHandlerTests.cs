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
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.ServerPackets.UI;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Core.Configuration;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.GameObject;
using NosCore.GameObject.Providers.ItemProvider;
using NosCore.GameObject.Providers.ItemProvider.Handlers;
using NosCore.GameObject.Providers.ItemProvider.Item;
using NosCore.GameObject.Services.InventoryService;
using NosCore.Tests.Helpers;
using Serilog;
//TODO stop using obsolete
#pragma warning disable 618

namespace NosCore.Tests.ItemHandlerTests
{
    [TestClass]
    public class SpRechargerEventHandlerTests : UseItemEventHandlerTestsBase
    {
        private ItemProvider? _itemProvider;
        private readonly ILogger _logger = new Mock<ILogger>().Object;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync().ConfigureAwait(false);
            Session = await TestHelpers.Instance.GenerateSessionAsync().ConfigureAwait(false);
            Handler = new SpRechargerEventHandler(Options.Create(new WorldConfiguration {MaxAdditionalSpPoints = 1}));
            var items = new List<ItemDto>
            {
                new Item {VNum = 1, ItemType = ItemType.Special, EffectValue = 1},
            };
            _itemProvider = new ItemProvider(items,
                new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>(), _logger);
        }
        [TestMethod]
        public async Task Test_SpRecharger_When_MaxAsync()
        {
            Session!.Character.SpAdditionPoint = 1;
            var itemInstance = InventoryItemInstance.Create(_itemProvider!.Create(1), Session.Character.CharacterId);
            Session.Character.InventoryService!.AddItemToPocket(itemInstance);
            await ExecuteInventoryItemInstanceEventHandlerAsync(itemInstance).ConfigureAwait(false);
            var lastpacket = (MsgPacket?)Session.LastPackets.FirstOrDefault(s => s is MsgPacket);
            Assert.AreEqual(GameLanguage.Instance.GetMessageFromKey(LanguageKey.SP_ADDPOINTS_FULL, Session.Account.Language), lastpacket?.Message);
            Assert.AreEqual(1, Session.Character.SpAdditionPoint);
            Assert.AreEqual(1, Session.Character.InventoryService.Count);
        }

        [TestMethod]
        public async Task Test_SpRechargerAsync()
        {
            Session!.Character.SpAdditionPoint = 0;
            var itemInstance = InventoryItemInstance.Create(_itemProvider!.Create(1), Session.Character.CharacterId);
            Session.Character.InventoryService!.AddItemToPocket(itemInstance);
            await ExecuteInventoryItemInstanceEventHandlerAsync(itemInstance).ConfigureAwait(false);
            Assert.AreEqual(1, Session.Character.SpAdditionPoint);
            Assert.AreEqual(0, Session.Character.InventoryService.Count);
        }
    }
}