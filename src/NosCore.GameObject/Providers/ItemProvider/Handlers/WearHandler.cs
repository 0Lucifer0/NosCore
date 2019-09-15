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

using ChickenAPI.Packets.ClientPackets.Inventory;
using ChickenAPI.Packets.Enumerations;
using ChickenAPI.Packets.ServerPackets.UI;
using NosCore.Core;
using NosCore.Core.I18N;
using NosCore.Data;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Networking.Group;
using NosCore.GameObject.Providers.InventoryService;
using NosCore.GameObject.Providers.ItemProvider.Item;
using Serilog;
using System;

namespace NosCore.GameObject.Providers.ItemProvider.Handlers
{
    public class WearEventHandler : IEventHandler<Item.Item, Tuple<InventoryItemInstance, UseItemPacket>>
    {
        private readonly ILogger _logger;
        public WearEventHandler(ILogger logger)
        {
            _logger = logger;
        }
        public bool Condition(Item.Item item) => item.ItemType == ItemType.Weapon
            || item.ItemType == ItemType.Jewelery
            || item.ItemType == ItemType.Armor
            || item.ItemType == ItemType.Fashion
            || item.ItemType == ItemType.Specialist;

        public void Execute(RequestData<Tuple<InventoryItemInstance, UseItemPacket>> requestData)
        {
            requestData.ClientSession.SendPacket(requestData.ClientSession.Character.GenerateEff(123));

            var itemInstance = requestData.Data.Item1;
            var packet = requestData.Data.Item2;
            if (requestData.ClientSession.Character.InExchangeOrShop)
            {
                _logger.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.CANT_USE_ITEM_IN_SHOP));
                return;
            }

            if (itemInstance.ItemInstance.BoundCharacterId == null)
            {
                if (packet.Mode == 0 && itemInstance.ItemInstance.Item.RequireBinding)
                {
                    requestData.ClientSession.SendPacket(
                        new QnaPacket
                        {
                            YesPacket = requestData.ClientSession.Character.GenerateUseItem((PocketType)itemInstance.Type,
                                itemInstance.Slot, 1, (byte)packet.Parameter),
                            Question = requestData.ClientSession.GetMessageFromKey(LanguageKey.ASK_BIND)
                        });
                    return;
                }

                if (packet.Mode != 0)
                {
                    itemInstance.ItemInstance.BoundCharacterId = requestData.ClientSession.Character.CharacterId;
                }
            }

            if (itemInstance.ItemInstance.Item.LevelMinimum > (itemInstance.ItemInstance.Item.IsHeroic
                    ? requestData.ClientSession.Character.HeroLevel : requestData.ClientSession.Character.Level)
                || itemInstance.ItemInstance.Item.Sex != 0 &&
                ((itemInstance.ItemInstance.Item.Sex >> (byte)requestData.ClientSession.Character.Gender) & 1) != 1
                || itemInstance.ItemInstance.Item.Class != 0 &&
                ((itemInstance.ItemInstance.Item.Class >> (byte)requestData.ClientSession.Character.Class) & 1) != 1)
            {
                requestData.ClientSession.SendPacket(
                    requestData.ClientSession.Character.GenerateSay(
                        requestData.ClientSession.GetMessageFromKey(LanguageKey.BAD_EQUIPMENT),
                        SayColorType.Yellow));
                return;
            }

            if (requestData.ClientSession.Character.UseSp && itemInstance.ItemInstance.Item.EquipmentSlot == EquipmentType.Fairy)
            {
                var sp = requestData.ClientSession.Character.Inventory.LoadBySlotAndType(
                    (byte)EquipmentType.Sp, NoscorePocketType.Wear);

                if (sp != null && sp.ItemInstance.Item.Element != 0 && itemInstance.ItemInstance.Item.Element != sp.ItemInstance.Item.Element &&
                    itemInstance.ItemInstance.Item.Element != sp.ItemInstance.Item.SecondaryElement)
                {
                    requestData.ClientSession.SendPacket(new MsgPacket
                    {
                        Message = Language.Instance.GetMessageFromKey(LanguageKey.BAD_FAIRY,
                            requestData.ClientSession.Account.Language)
                    });
                    return;
                }
            }

            if (itemInstance.ItemInstance.Item.EquipmentSlot == EquipmentType.Sp)
            {
                double timeSpanSinceLastSpUsage =
                    (SystemTime.Now() - requestData.ClientSession.Character.LastSp).TotalSeconds;
                var sp = requestData.ClientSession.Character.Inventory.LoadBySlotAndType(
                    (byte)EquipmentType.Sp, NoscorePocketType.Wear);
                if (timeSpanSinceLastSpUsage < requestData.ClientSession.Character.SpCooldown && sp != null)
                {
                    requestData.ClientSession.SendPacket(new MsgPacket
                    {
                        Message = string.Format(Language.Instance.GetMessageFromKey(LanguageKey.SP_INLOADING,
                                requestData.ClientSession.Account.Language), requestData.ClientSession.Character.SpCooldown - (int)Math.Round(timeSpanSinceLastSpUsage))
                    });
                    return;
                }

                if (requestData.ClientSession.Character.UseSp)
                {
                    requestData.ClientSession.SendPacket(
                        requestData.ClientSession.Character.GenerateSay(
                            requestData.ClientSession.GetMessageFromKey(LanguageKey.SP_BLOCKED),
                            SayColorType.Yellow));
                    return;
                }

                if (itemInstance.ItemInstance.Rare == -2)
                {
                    requestData.ClientSession.SendPacket(new MsgPacket
                    {
                        Message = Language.Instance.GetMessageFromKey(LanguageKey.CANT_EQUIP_DESTROYED_SP,
                            requestData.ClientSession.Account.Language)
                    });
                    return;
                }
            }

            if (requestData.ClientSession.Character.JobLevel < itemInstance.ItemInstance.Item.LevelJobMinimum)
            {
                requestData.ClientSession.SendPacket(
                    requestData.ClientSession.Character.GenerateSay(
                        requestData.ClientSession.GetMessageFromKey(LanguageKey.LOW_JOB_LVL),
                        SayColorType.Yellow));
                return;
            }

            requestData.ClientSession.Character.Inventory.MoveInPocket(packet.Slot, (NoscorePocketType)packet.Type, NoscorePocketType.Wear,
                (short)itemInstance.ItemInstance.Item.EquipmentSlot, true);
            var newItem =
                requestData.ClientSession.Character.Inventory
                    .LoadBySlotAndType(packet.Slot, (NoscorePocketType)packet.Type);

            requestData.ClientSession.SendPacket(newItem.GeneratePocketChange(packet.Type, packet.Slot));

            requestData.ClientSession.Character.MapInstance.Sessions.SendPacket(requestData.ClientSession.Character
                .GenerateEq());
            requestData.ClientSession.SendPacket(requestData.ClientSession.Character.GenerateEquipment());

            if (itemInstance.ItemInstance.Item.EquipmentSlot == EquipmentType.Sp)
            {
                requestData.ClientSession.SendPacket(requestData.ClientSession.Character.GenerateSpPoint());
            }

            if (itemInstance.ItemInstance.Item.EquipmentSlot == EquipmentType.Fairy)
            {
                requestData.ClientSession.Character.MapInstance.Sessions.SendPacket(
                    requestData.ClientSession.Character.GeneratePairy(itemInstance.ItemInstance as WearableInstance));
            }

            if (itemInstance.ItemInstance.Item.EquipmentSlot == EquipmentType.Amulet)
            {
                requestData.ClientSession.SendPacket(requestData.ClientSession.Character.GenerateEff(39));
            }


            if (itemInstance.ItemInstance.Item.ItemValidTime > 0 && itemInstance.ItemInstance.BoundCharacterId != null)
            {
                itemInstance.ItemInstance.ItemDeleteTime = SystemTime.Now().AddSeconds(itemInstance.ItemInstance.Item.ItemValidTime);
            }

            if (itemInstance.ItemInstance.Item.RequireBinding)
            {
                itemInstance.ItemInstance.BoundCharacterId = requestData.ClientSession.Character.CharacterId;
            }
        }
    }
}