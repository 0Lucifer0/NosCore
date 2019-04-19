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

using ChickenAPI.Packets.ClientPackets.Shops;
using ChickenAPI.Packets.Enumerations;
using NosCore.Configuration;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using Serilog;

namespace NosCore.PacketHandlers.Shops
{
    public class BuyPacketHandler : PacketHandler<BuyPacket>, IWorldPacketHandler
    {
        private readonly ILogger _logger;
        private readonly WorldConfiguration _worldConfiguration;

        public BuyPacketHandler(WorldConfiguration worldConfiguration, ILogger logger)
        {
            _worldConfiguration = worldConfiguration;
            _logger = logger;
        }

        public override void Execute(BuyPacket buyPacket, ClientSession clientSession)
        {
            if (clientSession.Character.InExchangeOrTrade)
            {
                //TODO log
                return;
            }

            if (buyPacket.Amount > _worldConfiguration.MaxItemAmount)
            {
                //TODO log
                return;
            }

            IAliveEntity aliveEntity;
            switch (buyPacket.VisualType)
            {
                case VisualType.Player:
                    aliveEntity = Broadcaster.Instance.GetCharacter(s => s.VisualId == buyPacket.VisualId);
                    break;
                case VisualType.Npc:
                    aliveEntity = clientSession.Character.MapInstance.Npcs.Find(s => s.VisualId == buyPacket.VisualId);
                    break;

                default:
                    _logger.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.VISUALTYPE_UNKNOWN),
                        buyPacket.VisualType);
                    return;
            }

            if (aliveEntity == null)
            {
                _logger.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.VISUALENTITY_DOES_NOT_EXIST));
                return;
            }

            clientSession.Character.Buy(aliveEntity.Shop, buyPacket.Slot, buyPacket.Amount);
        }
    }
}