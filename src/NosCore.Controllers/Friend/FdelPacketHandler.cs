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

using System.Linq;
using ChickenAPI.Packets.ClientPackets.Relations;
using ChickenAPI.Packets.ServerPackets.UI;
using NosCore.Core.HttpClients.ChannelHttpClient;
using NosCore.Core.HttpClients.ConnectedAccountHttpClient;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.HttpClients.FriendHttpClient;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;

namespace NosCore.PacketHandlers.Friend
{
    public class FdelPacketHandler : PacketHandler<FdelPacket>, IWorldPacketHandler
    {
        private readonly IChannelHttpClient _channelHttpClient;
        private readonly IConnectedAccountHttpClient _connectedAccountHttpClient;
        private readonly IFriendHttpClient _friendHttpClient;

        public FdelPacketHandler(IFriendHttpClient friendHttpClient, IChannelHttpClient channelHttpClient,
            IConnectedAccountHttpClient connectedAccountHttpClient)
        {
            _friendHttpClient = friendHttpClient;
            _channelHttpClient = channelHttpClient;
            _connectedAccountHttpClient = connectedAccountHttpClient;
        }

        public override void Execute(FdelPacket fdelPacket, ClientSession session)
        {
            var list = _friendHttpClient.GetListFriends(session.Character.VisualId);
            var idtorem = list.FirstOrDefault(s => s.CharacterId == fdelPacket.CharacterId);
            if (idtorem != null)
            {
                _friendHttpClient.DeleteFriend(idtorem.CharacterRelationId);
                session.SendPacket(new InfoPacket
                {
                    Message = Language.Instance.GetMessageFromKey(LanguageKey.FRIEND_DELETED, session.Account.Language)
                });
                var targetCharacter = Broadcaster.Instance.GetCharacter(s => s.VisualId == fdelPacket.CharacterId);
                if (targetCharacter != null)
                {
                    targetCharacter.SendPacket(targetCharacter.GenerateFinit(_friendHttpClient, _channelHttpClient,
                        _connectedAccountHttpClient));
                }

                session.Character.SendPacket(session.Character.GenerateFinit(_friendHttpClient, _channelHttpClient,
                    _connectedAccountHttpClient));
            }
            else
            {
                session.SendPacket(new InfoPacket
                {
                    Message = Language.Instance.GetMessageFromKey(LanguageKey.NOT_IN_FRIENDLIST,
                        session.Account.Language)
                });
            }
        }
    }
}