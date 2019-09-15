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

using ChickenAPI.Packets.ClientPackets.Drops;
using ChickenAPI.Packets.Enumerations;
using NosCore.Core;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Providers.MapItemProvider;
using NosCore.PathFinder;
using Serilog;
using System;

namespace NosCore.PacketHandlers.Inventory
{
    public class GetPacketHandler : PacketHandler<GetPacket>, IWorldPacketHandler
    {
        private readonly ILogger _logger;

        public GetPacketHandler(ILogger logger)
        {
            _logger = logger;
        }

        public override void Execute(GetPacket getPacket, ClientSession clientSession)
        {
            if (!clientSession.Character.MapInstance.MapItems.ContainsKey(getPacket.VisualId))
            {
                return;
            }

            var mapItem = clientSession.Character.MapInstance.MapItems[getPacket.VisualId];

            var canpick = false;
            switch (getPacket.PickerType)
            {
                case VisualType.Player:
                    canpick = Heuristic.Octile(Math.Abs(clientSession.Character.PositionX - mapItem.PositionX),
                        Math.Abs(clientSession.Character.PositionY - mapItem.PositionY)) < 8;
                    break;

                case VisualType.Npc:
                    return;

                default:
                    _logger.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.UNKNOWN_PICKERTYPE));
                    return;
            }

            if (!canpick)
            {
                return;
            }

            //TODO add group drops
            if (mapItem.OwnerId != null && mapItem.DroppedAt.AddSeconds(30) > SystemTime.Now() &&
                mapItem.OwnerId != clientSession.Character.CharacterId)
            {
                clientSession.SendPacket(clientSession.Character.GenerateSay(
                    Language.Instance.GetMessageFromKey(LanguageKey.NOT_YOUR_ITEM, clientSession.Account.Language),
                    SayColorType.Yellow));
                return;
            }

            mapItem.Requests.OnNext(new RequestData<Tuple<MapItem, GetPacket>>(clientSession,
                new Tuple<MapItem, GetPacket>(mapItem, getPacket)));
        }
    }
}