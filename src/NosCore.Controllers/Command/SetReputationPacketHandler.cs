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
using System.Linq;
using ChickenAPI.Packets.ServerPackets.UI;
using NosCore.Configuration;
using NosCore.Core;
using NosCore.Core.I18N;
using NosCore.Core.Networking;
using NosCore.Data.CommandPackets;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;
using NosCore.GameObject;
using NosCore.GameObject.Networking.ClientSession;
using Character = NosCore.Data.WebApi.Character;

namespace NosCore.PacketHandlers.Command
{
    public class SetReputationPacketHandler : PacketHandler<SetReputationPacket>, IWorldPacketHandler
    {
        private readonly IWebApiAccess _webApiAccess;
        public SetReputationPacketHandler(IWebApiAccess webApiAccess)
        {
            _webApiAccess = webApiAccess;
        }

        public override void Execute(SetReputationPacket setReputationPacket, ClientSession session)
        {
            if (setReputationPacket.Name == session.Character.Name || string.IsNullOrEmpty(setReputationPacket.Name))
            {
                session.Character.SetReputation(setReputationPacket.Reputation);
                return;
            }

            var data = new StatData
            {
                ActionType = UpdateStatActionType.UpdateReputation,
                Character = new Character { Name = setReputationPacket.Name },
                Data = setReputationPacket.Reputation
            };

            var receiver = _webApiAccess.GetCharacter(null, setReputationPacket.Name);

            if (receiver.Item2 == null) //TODO: Handle 404 in WebApi
            {
                session.SendPacket(new InfoPacket
                {
                    Message = Language.Instance.GetMessageFromKey(LanguageKey.CANT_FIND_CHARACTER,
                        session.Account.Language)
                });
                return;
            }

            _webApiAccess.Post<StatData>(WebApiRoute.Stat, data, receiver.Item1);
        }
    }

}