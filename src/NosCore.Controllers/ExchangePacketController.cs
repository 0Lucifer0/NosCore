﻿//  __  _  __    __   ___ __  ___ ___  
// |  \| |/__\ /' _/ / _//__\| _ \ __| 
// | | ' | \/ |`._`.| \_| \/ | v / _|  
// |_|\__|\__/ |___/ \__/\__/|_|_\___| 
// 
// Copyright (C) 2018 - NosCore
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
using System.Text;
using JetBrains.Annotations;
using NosCore.Configuration;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Services.ExchangeAccess;
using NosCore.GameObject.Services.ItemBuilder;
using NosCore.GameObject.Services.ItemBuilder.Item;
using NosCore.Packets.ClientPackets;
using NosCore.Packets.ServerPackets;
using NosCore.Shared.Enumerations;
using NosCore.Shared.Enumerations.Buff;
using NosCore.Shared.Enumerations.Character;
using NosCore.Shared.Enumerations.Interaction;
using NosCore.Shared.Enumerations.Items;
using NosCore.Shared.I18N;
using Serilog;

namespace NosCore.Controllers
{
    public class ExchangePacketController : PacketController
    {
        private readonly WorldConfiguration _worldConfiguration;
        private readonly ExchangeAccessService _exchangeAccessService;
        private readonly IItemBuilderService _itemBuilderService;
        private readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();
        
        public ExchangePacketController(WorldConfiguration worldConfiguration, ExchangeAccessService exchangeAccessService, IItemBuilderService itemBuilderService)
        {
            _worldConfiguration = worldConfiguration;
            _exchangeAccessService = exchangeAccessService;
            _itemBuilderService = itemBuilderService;
        }

        [UsedImplicitly]
        public ExchangePacketController()
        {

        }

        [UsedImplicitly]
        public void ExchangeList(ExcListPacket packet)
        {
            if (packet.Gold > Session.Character.Gold || packet.BankGold > Session.Account.BankMoney)
            {
                _logger.Error(LogLanguage.Instance.GetMessageFromKey(LanguageKey.NOT_ENOUGH_GOLD));
                return;
            }

            var list = new ServerExcListPacket
            {
                VisualType = VisualType.Player,
                VisualId = Session.Character.VisualId,
                Gold = packet.Gold,
                BankGold = packet.BankGold,
                SubPackets = new List<ServerExcListSubPacket>()
            };
            
            var target = Broadcaster.Instance.GetCharacter(s => s.VisualId == _exchangeAccessService.ExchangeDatas[Session.Character.CharacterId].TargetVisualId && s.MapInstanceId == Session.Character.MapInstanceId) as Character;

            if (packet.SubPackets.Count > 0 && target != null)
            {
                byte i = 0;
                foreach (var value in packet.SubPackets)
                {
                    var item = Session.Character.Inventory.LoadBySlotAndType<IItemInstance>(value.Slot, value.PocketType);

                    if (item == null || item.Amount < value.Amount)
                    {
                        _logger.Error(LogLanguage.Instance.GetMessageFromKey(LanguageKey.NOT_ENOUGH_ITEMS));
                        return;
                    }

                    if (!item.Item.IsTradable)
                    {
                        Session.SendPacket(_exchangeAccessService.CloseExchange(Session.Character.CharacterId, ExchangeCloseType.Failure));
                        target.SendPacket(_exchangeAccessService.CloseExchange(target.VisualId, ExchangeCloseType.Failure));
                        return;
                    }

                    var itemCpy = (IItemInstance)item.Clone();
                    itemCpy.Amount = value.Amount;
                    _exchangeAccessService.ExchangeDatas[Session.Character.CharacterId].ExchangeItems.TryAdd(i, itemCpy);

                    var subPacket = new ServerExcListSubPacket
                    {
                        ExchangeSlot = i,
                        PocketType = value.PocketType,
                        ItemVnum = itemCpy.ItemVNum,
                        Upgrade = itemCpy.Upgrade,
                        AmountOrRare = value.PocketType == PocketType.Equipment ? itemCpy.Rare : itemCpy.Amount
                    };


                    list.SubPackets.Add(subPacket);
                    i++;
                }
            }
            else
            {
                list.SubPackets.Add(new ServerExcListSubPacket { ExchangeSlot = null });
            }

            _exchangeAccessService.ExchangeDatas[Session.Character.CharacterId].Gold = packet.Gold;
            _exchangeAccessService.ExchangeDatas[Session.Character.CharacterId].BankGold = packet.BankGold;
            target?.SendPacket(list);
            _exchangeAccessService.ExchangeDatas[Session.Character.CharacterId].ExchangeListIsValid = true;
        }

        [UsedImplicitly]
        public void RequestExchange(ExchangeRequestPacket packet)
        {
            var target = Broadcaster.Instance.GetCharacter(s => s.VisualId == packet.VisualId && s.MapInstanceId == Session.Character.MapInstanceId) as Character;

            if (target == null && (packet.RequestType == RequestExchangeType.Requested || packet.RequestType == RequestExchangeType.List))
            {
                _logger.Error(LogLanguage.Instance.GetMessageFromKey(LanguageKey.CANT_FIND_CHARACTER));
                return;
            }

            switch (packet.RequestType)
            {
                case RequestExchangeType.Requested:
                    _exchangeAccessService.RequestExchange(Session, target.Session);
                    return;

                case RequestExchangeType.List:
                    if (target.InExchangeOrShop)
                    {
                        _logger.Error(LogLanguage.Instance.GetMessageFromKey(LanguageKey.ALREADY_EXCHANGE));
                        return;
                    }
                    
                    _exchangeAccessService.OpenExchange(Session, target.Session);
                    return;
                case RequestExchangeType.Declined:
                    Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey(LanguageKey.EXCHANGE_REFUSED, Session.Account.Language), SayColorType.Yellow));
                    target?.SendPacket(target.GenerateSay(target.GetMessageFromKey(LanguageKey.EXCHANGE_REFUSED), SayColorType.Yellow));
                    return;
                case RequestExchangeType.Confirmed:
                    target = Broadcaster.Instance.GetCharacter(s => s.VisualId == _exchangeAccessService.ExchangeDatas[Session.Character.CharacterId].TargetVisualId && s.MapInstanceId == Session.Character.MapInstanceId) as Character;

                    if (target == null)
                    {
                        _logger.Error(Language.Instance.GetMessageFromKey(LanguageKey.CANT_FIND_CHARACTER, Session.Account.Language));
                        return;
                    }

                    if (!_exchangeAccessService.ExchangeDatas[Session.Character.CharacterId].ExchangeListIsValid || !_exchangeAccessService.ExchangeDatas[target.CharacterId].ExchangeListIsValid)
                    {
                        _logger.Error(LogLanguage.Instance.GetMessageFromKey(LanguageKey.INVALID_EXCHANGE_LIST));
                        return;
                    }

                    _exchangeAccessService.ExchangeDatas[Session.Character.CharacterId].ExchangeConfirmed = true;

                    if (!_exchangeAccessService.ExchangeDatas[Session.Character.CharacterId].ExchangeConfirmed || !_exchangeAccessService.ExchangeDatas[target.CharacterId].ExchangeConfirmed)
                    {
                        Session.SendPacket(new InfoPacket { Message = Language.Instance.GetMessageFromKey(LanguageKey.IN_WAITING_FOR, Session.Account.Language) });
                        return;
                    }

                    var exchangeInfo = _exchangeAccessService.ExchangeDatas[Session.Character.CharacterId];
                    var targetInfo = _exchangeAccessService.ExchangeDatas[target.CharacterId];
                    
                    if (exchangeInfo.Gold + target.Gold > _worldConfiguration.MaxGoldAmount)
                    {
                        Session.SendPacket(_exchangeAccessService.CloseExchange(Session.Character.CharacterId, ExchangeCloseType.Failure));
                        target.SendPacket(_exchangeAccessService.CloseExchange(target.VisualId, ExchangeCloseType.Failure));
                        target.SendPacket(new InfoPacket { Message = Language.Instance.GetMessageFromKey(LanguageKey.MAX_GOLD, target.Account.Language)});
                        return;
                    }

                    if (targetInfo.Gold + Session.Character.Gold > _worldConfiguration.MaxGoldAmount)
                    {
                        Session.SendPacket(_exchangeAccessService.CloseExchange(Session.Character.CharacterId, ExchangeCloseType.Failure));
                        target.SendPacket(_exchangeAccessService.CloseExchange(target.VisualId, ExchangeCloseType.Failure));
                        Session.SendPacket(new InfoPacket { Message = Language.Instance.GetMessageFromKey(LanguageKey.MAX_GOLD, Session.Account.Language) });
                        return;
                    }

                    if (exchangeInfo.BankGold + target.Account.BankMoney > _worldConfiguration.MaxBankGoldAmount)
                    {
                        Session.SendPacket(_exchangeAccessService.CloseExchange(Session.Character.CharacterId, ExchangeCloseType.Failure));
                        target.SendPacket(_exchangeAccessService.CloseExchange(target.VisualId, ExchangeCloseType.Failure));
                        target.SendPacket(new InfoPacket { Message = Language.Instance.GetMessageFromKey(LanguageKey.BANK_FULL, Session.Account.Language) });
                        return;
                    }

                    if (targetInfo.BankGold + Session.Account.BankMoney > _worldConfiguration.MaxBankGoldAmount)
                    {
                        Session.SendPacket(_exchangeAccessService.CloseExchange(Session.Character.CharacterId, ExchangeCloseType.Failure));
                        target.SendPacket(_exchangeAccessService.CloseExchange(target.VisualId, ExchangeCloseType.Failure));
                        Session.SendPacket(new InfoPacket { Message = Language.Instance.GetMessageFromKey(LanguageKey.BANK_FULL, Session.Account.Language) });
                        return;
                    }

                    if (exchangeInfo.ExchangeItems.Values.Any(s => !s.Item.IsTradable))
                    {
                        Session.SendPacket(_exchangeAccessService.CloseExchange(Session.Character.CharacterId, ExchangeCloseType.Failure));
                        target.SendPacket(_exchangeAccessService.CloseExchange(target.VisualId, ExchangeCloseType.Failure));
                        Session.SendPacket(new InfoPacket { Message = Language.Instance.GetMessageFromKey(LanguageKey.ITEM_NOT_TRADABLE, Session.Account.Language) });
                        return;
                    }

                    if (!Session.Character.Inventory.EnoughPlace(targetInfo.ExchangeItems.Values.ToList()) || !target.Inventory.EnoughPlace(exchangeInfo.ExchangeItems.Values.ToList()))
                    {
                        Session.SendPacket(new InfoPacket { Message = Language.Instance.GetMessageFromKey(LanguageKey.INVENTORY_FULL, Session.Account.Language) });
                        target.SendPacket(new InfoPacket { Message = Language.Instance.GetMessageFromKey(LanguageKey.INVENTORY_FULL, target.Account.Language) });
                        Session.SendPacket(_exchangeAccessService.CloseExchange(Session.Character.CharacterId, ExchangeCloseType.Failure));
                        target.SendPacket(_exchangeAccessService.CloseExchange(target.VisualId, ExchangeCloseType.Failure));
                        return;
                    }

                    _exchangeAccessService.ProcessExchange(Session, target.Session);
                    Session.SendPacket(_exchangeAccessService.CloseExchange(Session.Character.CharacterId, ExchangeCloseType.Success));
                    target.SendPacket(_exchangeAccessService.CloseExchange(target.VisualId, ExchangeCloseType.Success));
                    return;
                case RequestExchangeType.Cancelled:
                    target = (Character)Broadcaster.Instance.GetCharacter(s => s.VisualId == _exchangeAccessService.ExchangeDatas[Session.Character.CharacterId].TargetVisualId);

                    target?.SendPacket(_exchangeAccessService.CloseExchange(target.VisualId, ExchangeCloseType.Success));
                    Session.SendPacket(_exchangeAccessService.CloseExchange(Session.Character.CharacterId, ExchangeCloseType.Success));
                    return;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
