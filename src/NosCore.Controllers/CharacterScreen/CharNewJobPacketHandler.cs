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

using ChickenAPI.Packets.ClientPackets.CharacterSelectionScreen;
using ChickenAPI.Packets.Enumerations;
using NosCore.Core;
using NosCore.Data.AliveEntities;
using NosCore.Data.Enumerations.Character;
using NosCore.GameObject;
using NosCore.GameObject.Networking.ClientSession;

namespace NosCore.PacketHandlers.CharacterScreen
{
    public class CharNewJobPacketHandler : PacketHandler<CharNewJobPacket>, IWorldPacketHandler
    {
        private readonly IGenericDao<CharacterDto> _characterDao;
        public CharNewJobPacketHandler(IGenericDao<CharacterDto> characterDao)
        {
            _characterDao = characterDao;
        }

        public override void Execute(CharNewJobPacket packet, ClientSession clientSession)
        {
            //TODO add a flag on Account
            if (_characterDao.FirstOrDefault(s =>
                s.Level >= 80 && s.AccountId == clientSession.Account.AccountId && s.State == CharacterState.Active) == null)
            {
                //Needs at least a level 80 to create a martial artist
                //TODO log
                return;
            }

            if (_characterDao.FirstOrDefault(s =>
                s.AccountId == clientSession.Account.AccountId &&
                s.Class == CharacterClassType.MartialArtist && s.State == CharacterState.Active) != null)
            {
                //If already a martial artist, can't create another
                //TODO log
                return;
            }
            //todo add cooldown for recreate 30days

            CreateCharacter(packet);
        }
    }
}