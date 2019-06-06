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

using System.Collections.Generic;
using ChickenAPI.Packets.ClientPackets.Exchanges;
using ChickenAPI.Packets.Enumerations;
using ChickenAPI.Packets.ServerPackets.Exchanges;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Providers.ExchangeProvider;
using NosCore.GameObject.Providers.ItemProvider.Item;
using Serilog;

namespace NosCore.PacketHandlers.Exchange
{
    public class ExcListPacketHandler : PacketHandler<ExcListPacket>, IWorldPacketHandler
    {
        private readonly IExchangeProvider _exchangeProvider;
        private readonly ILogger _logger;

        public ExcListPacketHandler(IExchangeProvider exchangeProvider, ILogger logger)
        {
            _exchangeProvider = exchangeProvider;
            _logger = logger;
        }

        public override void Execute(ExcListPacket packet, ClientSession clientSession)
        {
            if (packet.Gold > clientSession.Character.Gold || packet.BankGold > clientSession.Account.BankMoney)
            {
                _logger.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.NOT_ENOUGH_GOLD));
                return;
            }

            var subPacketList = new List<ServerExcListSubPacket>();

            var target = Broadcaster.Instance.GetCharacter(s =>
                s.VisualId == _exchangeProvider.GetTargetId(clientSession.Character.VisualId) &&
                s.MapInstanceId == clientSession.Character.MapInstanceId) as Character;

            if (packet.SubPackets.Count > 0 && target != null)
            {
                byte i = 0;
                foreach (var value in packet.SubPackets)
                {
                    var item = clientSession.Character.Inventory.LoadBySlotAndType(value.Slot,
                        value.NoscorePocketType);

                    if (item == null || item.ItemInstance.Amount < value.Amount)
                    {
                        var closeExchange =
                            _exchangeProvider.CloseExchange(clientSession.Character.VisualId, ExchangeResultType.Failure);
                        clientSession.SendPacket(closeExchange);
                        target.SendPacket(closeExchange);
                        _logger.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.INVALID_EXCHANGE_LIST));
                        return;
                    }

                    if (!item.ItemInstance.Item.IsTradable)
                    {
                        clientSession.SendPacket(_exchangeProvider.CloseExchange(clientSession.Character.CharacterId,
                            ExchangeResultType.Failure));
                        target.SendPacket(_exchangeProvider.CloseExchange(target.VisualId, ExchangeResultType.Failure));
                        _logger.Error(
                            LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.CANNOT_TRADE_NOT_TRADABLE_ITEM));
                        return;
                    }

                    _exchangeProvider.AddItems(clientSession.Character.CharacterId, item, value.Amount);

                    var subPacket = new ServerExcListSubPacket
                    {
                        ExchangeSlot = i,
                        NoscorePocketType = value.NoscorePocketType,
                        ItemVnum = item.ItemInstance.ItemVNum,
                        Upgrade = item.ItemInstance.Upgrade,
                        AmountOrRare = value.NoscorePocketType == NoscorePocketType.Equipment ? item.ItemInstance.Rare : value.Amount
                    };


                    subPacketList.Add(subPacket);
                    i++;
                }
            }
            else
            {
                subPacketList.Add(new ServerExcListSubPacket { ExchangeSlot = null });
            }

            _exchangeProvider.SetGold(clientSession.Character.CharacterId, packet.Gold, packet.BankGold);
            target?.SendPacket(
                clientSession.Character.GenerateServerExcListPacket(packet.Gold, packet.BankGold, subPacketList));
        }
    }

}