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

using Microsoft.Extensions.Options;
using NosCore.Core.Configuration;
using NosCore.Core.I18N;
using NosCore.Data.CommandPackets;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using Serilog;
using System.Collections.Generic;
using System.Threading.Tasks;

//TODO stop using obsolete
#pragma warning disable 618

namespace NosCore.PacketHandlers.Command
{
    public class CreateItemPackettHandler : PacketHandler<CreateItemPacket>, IWorldPacketHandler
    {
        private readonly IItemGenerationService _itemProvider;
        private readonly List<ItemDto> _items;
        private readonly ILogger _logger;
        private readonly IOptions<WorldConfiguration> _worldConfiguration;

        public CreateItemPackettHandler(ILogger logger, List<ItemDto> items, IOptions<WorldConfiguration> worldConfiguration,
            IItemGenerationService itemProvider)
        {
            _logger = logger;
            _items = items;
            _itemProvider = itemProvider;
            _worldConfiguration = worldConfiguration;
        }

        public override async Task ExecuteAsync(CreateItemPacket createItemPacket, ClientSession session)
        {
            var vnum = createItemPacket.VNum;
            sbyte rare = 0;
            byte upgrade = 0;
            byte design = 0;
            short amount = 1;
            if (vnum == 1046)
            {
                return; // cannot create gold as item, use $Gold instead
            }

            var iteminfo = _items.Find(item => item.VNum == vnum);
            if (iteminfo == null)
            {
                await session.SendPacketAsync(new MsgPacket
                {
                    Message = GameLanguage.Instance.GetMessageFromKey(LanguageKey.NO_ITEM, session.Account.Language),
                    Type = 0
                }).ConfigureAwait(false);
                return;
            }

            if (iteminfo.IsColored || (iteminfo.Effect == ItemEffectType.BoxEffect))
            {
                if (createItemPacket.DesignOrAmount.HasValue)
                {
                    design = (byte)createItemPacket.DesignOrAmount.Value;
                }

                rare = createItemPacket.Upgrade.HasValue && (iteminfo.Effect == ItemEffectType.BoxEffect)
                    ? (sbyte)createItemPacket.Upgrade.Value : rare;
            }
            else if (iteminfo.Type == NoscorePocketType.Equipment)
            {
                if (createItemPacket.Upgrade.HasValue)
                {
                    if (iteminfo.EquipmentSlot != EquipmentType.Sp)
                    {
                        upgrade = createItemPacket.Upgrade.Value;
                    }
                    else
                    {
                        design = createItemPacket.Upgrade.Value;
                    }

                    if ((iteminfo.EquipmentSlot != EquipmentType.Sp) && (upgrade == 0)
                        && (iteminfo.BasicUpgrade != 0))
                    {
                        upgrade = iteminfo.BasicUpgrade;
                    }
                }

                if (createItemPacket.DesignOrAmount.HasValue)
                {
                    if (iteminfo.EquipmentSlot == EquipmentType.Sp)
                    {
                        upgrade = (byte)createItemPacket.DesignOrAmount.Value;
                    }
                    else if (iteminfo.EquipmentSlot == EquipmentType.Armor || iteminfo.EquipmentSlot == EquipmentType.MainWeapon || iteminfo.EquipmentSlot == EquipmentType.SecondaryWeapon)
                    {
                        rare = (sbyte)createItemPacket.DesignOrAmount.Value;
                    }
                }
            }
            else if (createItemPacket.DesignOrAmount.HasValue && !createItemPacket.Upgrade.HasValue)
            {
                amount = createItemPacket.DesignOrAmount.Value > _worldConfiguration.Value.MaxItemAmount
                    ? _worldConfiguration.Value.MaxItemAmount : createItemPacket.DesignOrAmount.Value;
            }

            var inv = session.Character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(_itemProvider.Create(
                vnum,
                amount, rare, upgrade, design), session.Character.CharacterId));

            if (inv == null || inv.Count <= 0)
            {
                await session.SendPacketAsync(new MsgiPacket
                {
                    Message = Game18NConstString.NotEnoughSpace,
                    Type = 0
                }).ConfigureAwait(false);
                return;
            }

            await session.SendPacketAsync(inv.GeneratePocketChange()).ConfigureAwait(false);
            var firstItem = inv[0];

            if (session.Character.InventoryService.LoadBySlotAndType(firstItem.Slot,
                    firstItem.Type)!.ItemInstance is WearableInstance wearable)
            {
                switch (wearable.Item!.EquipmentSlot)
                {
                    case EquipmentType.Armor:
                    case EquipmentType.MainWeapon:
                    case EquipmentType.SecondaryWeapon:
                        wearable.SetRarityPoint();
                        break;

                    case EquipmentType.Boots:
                    case EquipmentType.Gloves:
                        wearable.FireResistance = (short)(wearable.Item.FireResistance * (upgrade + 1));
                        wearable.DarkResistance = (short)(wearable.Item.DarkResistance * (upgrade + 1));
                        wearable.LightResistance = (short)(wearable.Item.LightResistance * (upgrade + 1));
                        wearable.WaterResistance = (short)(wearable.Item.WaterResistance * (upgrade + 1));
                        break;

                    default:
                        _logger.Debug(
                            LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.NO_SPECIAL_PROPERTIES_WEARABLE));
                        break;
                }
            }

            await session.SendPacketAsync(session.Character.GenerateSay(
                $"{GameLanguage.Instance.GetMessageFromKey(LanguageKey.ITEM_ACQUIRED, session.Account.Language)}: {iteminfo.Name[session.Account.Language]} x {amount}",
                SayColorType.Green)).ConfigureAwait(false);
        }
    }
}