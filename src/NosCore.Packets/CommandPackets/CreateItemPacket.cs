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

using NosCore.Core.Serializing;
using NosCore.Data.Enumerations.Account;

namespace NosCore.Packets.CommandPackets
{
    [PacketHeader("$CreateItem", Authority = AuthorityType.GameMaster)]
    public class CreateItemPacket : PacketDefinition, ICommandPacket
    {
        [PacketIndex(0)]
        public short VNum { get; set; }

        [PacketIndex(1)]
        public short? DesignOrAmount { get; set; }

        [PacketIndex(2)]
        public byte? Upgrade { get; set; }

        public string Help()
        {
            return "$CreateItem ITEMVNUM DESIGN/RARE/AMOUNT/WINGS UPDATE";
        }
    }
}