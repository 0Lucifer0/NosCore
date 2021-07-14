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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Core;
using NosCore.Data.Enumerations;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.EventLoaderService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.UpgradeService;
using NosCore.PacketHandlers.Upgrades;
using NosCore.Packets.ClientPackets.Player;
using NosCore.Packets.Enumerations;
using NosCore.Tests.Shared;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.GameObject;
using NosCore.Data.Enumerations.Items;
using NosCore.Algorithm.SumService;

namespace NosCore.PacketHandlers.Tests.Upgrades
{
    [TestClass]
    public class UpgradePacketHandlerTests
    {
        private static readonly ILogger Logger = new Mock<ILogger>().Object;
        private ItemGenerationService? _itemProvider;
        private ISumUpgradeService? _sumUpgradeService;
        private ClientSession? _session;
        private UpgradePacketHandler? _upgradePacketHandler;

        [TestInitialize]
        public async Task SetupAsync()
        {
            SystemTime.Freeze();
            await TestHelpers.ResetAsync().ConfigureAwait(false);
            var items = new List<ItemDto>
            {
                new Item {Type = NoscorePocketType.Etc, VNum = 1027},
                new Item {Type = NoscorePocketType.Equipment, VNum = 71, ItemType = ItemType.Fashion, LightResistance = 2},
            };
            _itemProvider = new ItemGenerationService(items, new EventLoaderService<Item, Tuple<InventoryItemInstance, UseItemPacket>, IUseItemEventHandler>(new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>()), Logger);
            _sumUpgradeService = new SumUpgradeService(Logger, new SumService());
            _session = await TestHelpers.Instance.GenerateSessionAsync().ConfigureAwait(false);
            _upgradePacketHandler = new UpgradePacketHandler(Logger, _sumUpgradeService);
        }

        [TestMethod]
        public async Task Test_UpgradePacketSum1Async()
        {
            _session!.Character.InventoryService!.AddItemToPocket(InventoryItemInstance.Create(_itemProvider!.Create(1027, 999), 0));
            var glove = _session!.Character.InventoryService!.AddItemToPocket(InventoryItemInstance.Create(_itemProvider!.Create(71, 1, 0, 0), 0))![0];
            _session!.Character.InventoryService!.AddItemToPocket(InventoryItemInstance.Create(_itemProvider!.Create(71, 1, 0, 0), 0));
            await _upgradePacketHandler!.ExecuteAsync(new UpgradePacket
            {
                UpgradeType = UpgradePacketType.SumResistance,
                Slot = 0,
                Slot2 = 1,
                InventoryType = PocketType.Equipment
            }, _session).ConfigureAwait(false);
            Assert.IsTrue((_session.Character.InventoryService.Count == 2) &&
                (_session.Character.InventoryService!.CountItem(1027) == 994) &&
                (glove.ItemInstance is WearableInstance wearableInstance) &&
                wearableInstance.LightResistance == 4 &&
                wearableInstance.Upgrade == 1);
        }
    }
}