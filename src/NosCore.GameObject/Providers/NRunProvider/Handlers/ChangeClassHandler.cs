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
using System.Linq;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Character;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Interaction;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ClientPackets;
using NosCore.Packets.ServerPackets;

namespace NosCore.GameObject.Providers.NRunProvider.Handlers
{
    public class ChangeClassHandler : IHandler<Tuple<IAliveEntity, NrunPacket>, Tuple<IAliveEntity, NrunPacket>>
    {
        public bool Condition(Tuple<IAliveEntity, NrunPacket> item) =>
            item.Item2.Runner == NrunRunnerType.ChangeClass &&
            item.Item2.Type > 0 && item.Item2.Type < 4;

        public void Execute(RequestData<Tuple<IAliveEntity, NrunPacket>> requestData)
        {
            if (requestData.ClientSession.Character.Class != (byte) CharacterClassType.Adventurer)
            {
                requestData.ClientSession.SendPacket(new MsgPacket
                {
                    Message = Language.Instance.GetMessageFromKey(LanguageKey.NOT_ADVENTURER,
                        requestData.ClientSession.Account.Language),
                    Type = MessageType.White
                });
                return;
            }

            if (requestData.ClientSession.Character.Level < 15 || requestData.ClientSession.Character.JobLevel < 20)
            {
                requestData.ClientSession.SendPacket(new MsgPacket
                {
                    Message = Language.Instance.GetMessageFromKey(LanguageKey.TOO_LOW_LEVEL,
                        requestData.ClientSession.Account.Language),
                    Type = MessageType.White
                });
                return;
            }

            if (requestData.ClientSession.Character.Inventory.Any(s => s.Value.Type == PocketType.Wear))
            {
                requestData.ClientSession.SendPacket(new MsgPacket
                {
                    Message = Language.Instance.GetMessageFromKey(LanguageKey.EQ_NOT_EMPTY,
                        requestData.ClientSession.Account.Language),
                    Type = MessageType.White
                });
                return;
            }

            requestData.ClientSession.Character.ChangeClass((CharacterClassType) requestData.Data.Item2.Type);
        }
    }
}