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
using System.Collections.Generic;
using System.Linq;
using ChickenAPI.Packets.ClientPackets.Relations;
using ChickenAPI.Packets.Enumerations;
using ChickenAPI.Packets.Interfaces;
using ChickenAPI.Packets.ServerPackets.Relations;
using ChickenAPI.Packets.ServerPackets.UI;
using Microsoft.AspNetCore.Mvc;
using NosCore.Core;
using NosCore.Core.I18N;
using NosCore.Core.Networking;
using NosCore.Data;
using NosCore.Data.Enumerations.Account;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Interaction;
using NosCore.Data.WebApi;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Networking;
using Serilog;

namespace NosCore.FriendServer.Controllers
{
    [Route("api/[controller]")]
    [AuthorizeRole(AuthorityType.GameMaster)]
    public class StatusController : Controller
    {
        private readonly FriendRequestHolder _friendRequestHolder;
        private readonly ISerializer _packetSerializer;
        public StatusController(FriendRequestHolder friendRequestHolder, ISerializer packetSerializer)
        {
            _friendRequestHolder = friendRequestHolder;
            _packetSerializer = packetSerializer;
        }

        [HttpPost]
        public IActionResult SendStatus([FromBody] StatusRequest statusRequest)
        {
            var friendRequest = _friendRequestHolder.FriendRequestCharacters.Where(s =>
                s.Value.Item1 == statusRequest.CharacterId || s.Value.Item2 == statusRequest.CharacterId).ToList();
            foreach (var characterRelation in friendRequest)
            {
                long id = characterRelation.Value.Item1 == statusRequest.CharacterId ? characterRelation.Value.Item2
                    : characterRelation.Value.Item1;
                ICharacterEntity targetCharacter = Broadcaster.Instance.GetCharacter(s => s.VisualId == id);
                if (targetCharacter != null)
                {
                    WebApiAccess.Instance.BroadcastPacket(new PostedPacket
                    {
                        Packet = _packetSerializer.Serialize(new[]
                        {
                            new FinfoPacket
                            {
                                FriendList = new List<FinfoSubPackets>
                                {
                                    new FinfoSubPackets
                                    {
                                        CharacterId = statusRequest.CharacterId,
                                        IsConnected = statusRequest.Status
                                    }
                                }
                            }
                        }),
                        ReceiverType = ReceiverType.OnlySomeone,
                        SenderCharacter = new Data.WebApi.Character
                        { Id = statusRequest.CharacterId, Name = statusRequest.Name },
                        ReceiverCharacter = new Data.WebApi.Character
                        {
                            Id = targetCharacter.VisualId,
                            Name = targetCharacter.Name
                        }
                    });
                }
            }

            return Ok();
        }
    }
}