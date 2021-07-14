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

using NosCore.Algorithm.SumService;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Networking.Group;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.Shop;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Helpers;
using Serilog;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.UpgradeService
{
    public class SumUpgradeService : UpgradeService, ISumUpgradeService
    {
        private readonly ISumService _sumService;
        private const short SandVNum = 1027;

        public SumUpgradeService(ILogger logger, ISumService sumService) : base(logger)
        {
            _sumService = sumService;
        }

        public async Task<List<IPacket>> SumItemInstanceAsync(ClientSession session, InventoryItemInstance? sourceSlot, InventoryItemInstance? targetSlot)
        {
            var result = new List<IPacket>();
            if (sourceSlot == null || targetSlot == null || sourceSlot.ItemInstance is not WearableInstance sourceWearableInstance || targetSlot.ItemInstance is not WearableInstance targetWearableInstance)
            {
                _logger.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.UPGRADE_PACKET_SLOT2_NULL));
                return result;
            }

            if (sourceSlot.ItemInstance!.Upgrade + targetSlot.ItemInstance!.Upgrade > 5)
            {
                result.Add(new InfoiPacket
                {
                    Message = Game18NConstString.CombinationNumExceeded
                });
                return result;
            }

            var levelSum = (byte)(sourceSlot.ItemInstance.Upgrade + targetSlot.ItemInstance.Upgrade);
            var sandCost = _sumService.GetSandCost(levelSum);
            var didSucess = RandomHelper.Instance.RandomNumber() < _sumService.GetSuccessRate(levelSum);
            if (!didSucess)
            {
                session.Character.InventoryService.RemoveItemAmountFromInventory(1, sourceSlot.ItemInstanceId);
                result.AddRange(new IPacket[] {
                    new GuriPacket
                    {
                        Type = GuriPacketType.AfterSumming,
                        Argument = 1,
                        SecondArgument = 0,
                        EntityId = session.Character.CharacterId,
                        Value = 1332
                    },
                    new MsgiPacket
                    {
                        Message = Game18NConstString.CombinationFailed
                    },
                    new Sayi2Packet
                    {
                        Message = Game18NConstString.CombinationItemsDisappeared,
                        Type = SayColorType.Purple
                    }
                });
            }
            else
            {
                sourceWearableInstance.DarkResistance = (short)((sourceWearableInstance.DarkResistance ?? 0) + (targetWearableInstance.DarkResistance ?? 0));
                sourceWearableInstance.LightResistance = (short)((sourceWearableInstance.LightResistance ?? 0) + (targetWearableInstance.LightResistance ?? 0));
                sourceWearableInstance.FireResistance = (short)((sourceWearableInstance.FireResistance ?? 0) + (targetWearableInstance.FireResistance ?? 0));
                sourceWearableInstance.WaterResistance = (short)((sourceWearableInstance.WaterResistance ?? 0) + (targetWearableInstance.WaterResistance ?? 0));
                sourceSlot.ItemInstance.Upgrade += (byte)(targetSlot.ItemInstance.Upgrade + 1);

                result.AddRange(new IPacket[] {
                    new GuriPacket
                    {
                        Type = GuriPacketType.AfterSumming,
                        Argument = 1,
                        SecondArgument = 0,
                        EntityId = session.Character.CharacterId,
                        Value = 1324
                    },
                    new PdtiPacket
                    {
                        Unknow = 10,
                        ItemVnum = sourceSlot.ItemInstance.ItemVNum,
                        RecipeAmount = 1,
                        Unknow3 = 27,
                        ItemUpgrade = sourceSlot.ItemInstance.Upgrade,
                        Unknow4 = 0
                    },
                    new MsgiPacket
                    {
                        Message = Game18NConstString.CombinationSuccessful
                    },
                    new Sayi2Packet
                    {
                        Message = Game18NConstString.CombinationSuccessful,
                        Type = SayColorType.Green
                    }
                });
            }

            result.AddRange(new IPacket[] {
                sourceSlot.GeneratePocketChange(PocketType.Equipment, sourceSlot.Slot),
                ((InventoryItemInstance?)null).GeneratePocketChange(PocketType.Equipment, targetSlot.Slot),
                new ShopEndPacket
                {
                    Type = ShopEndPacketType.CloseSubWindow
                }
            });
            result.AddRange(session.Character.InventoryService.LoadByVNumAndAmount(SandVNum, (short)sandCost).Select(s => s.GeneratePocketChange(PocketType.Main, s!.Slot)));

            session.Character.InventoryService.RemoveItemAmountFromInventory((short)sandCost, SandVNum);
            session.Character.InventoryService.DeleteById(targetSlot.ItemInstanceId);
            await session.Character.RemoveGoldAsync(_sumService.GetPrice(levelSum));
            await session.Character.MapInstance.SendPacketAsync(new GuriPacket
            {
                Type = GuriPacketType.Unknow,
                Argument = 1,
                SecondArgument = 0,
                EntityId = session.Character.CharacterId
            });
            return result;
        }
    }
}