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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using ChickenAPI.Packets.Enumerations;
using ChickenAPI.Packets.ServerPackets.Groups;
using ChickenAPI.Packets.ServerPackets.Parcel;
using DotNetty.Common.Concurrency;
using DotNetty.Transport.Channels.Groups;
using NosCore.Data.Enumerations.Group;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Networking;

namespace NosCore.GameObject
{
    public class Group : ConcurrentDictionary<Tuple<VisualType, long>, Tuple<int, INamedEntity>>, IBroadcastable
    {
        private int _lastId;

        public Group(GroupType type)
        {
            Type = type;
            GroupId = -1;
            ExecutionEnvironment.TryGetCurrentExecutor(out var executor);
            Sessions = new DefaultChannelGroup(executor);
        }

        public long GroupId { get; set; }

        public GroupType Type { get; set; }

        public bool IsGroupFull => Count == (long) Type;

        public new bool IsEmpty => Keys.Count(s => s.Item1 == VisualType.Player) <= 1;

        public new int Count => Keys.Count(s => s.Item1 == VisualType.Player);

        public IChannelGroup Sessions { get; set; }

        public PinitPacket GeneratePinit()
        {
            var i = 0;

            return new PinitPacket
            {
                GroupSize = Count == 1 ? 0 : Count,
                PinitSubPackets = Values.Select(s => s.Item2.GenerateSubPinit(Count == 1 ? i : ++i)).ToList()
            };
        }

        public List<PstPacket> GeneratePst()
        {
            var i = 0;

            return Values.OrderBy(s => s.Item1).Select(s => s.Item2).Select(member => new PstPacket
            {
                Type = member.VisualType,
                VisualId = member.VisualId,
                GroupOrder = ++i,
                HpLeft = (int) (member.Hp / (float) member.MaxHp * 100),
                MpLeft = (int) (member.Mp / (float) member.MaxMp * 100),
                HpLoad = member.MaxHp,
                MpLoad = member.MaxMp,
                Race = member.Race,
                Gender = (member as ICharacterEntity)?.Gender ?? GenderType.Male,
                Morph = member.Morph,
                BuffIds = null
            }).ToList();
        }

        public bool IsGroupLeader(long visualId)
        {
            var leader = Values.OrderBy(s => s.Item1).FirstOrDefault(s => s.Item2.VisualType == VisualType.Player);
            return (Count > 1) && (leader?.Item2.VisualId == visualId);
        }

        public void JoinGroup(INamedEntity namedEntity)
        {
            if (namedEntity is ICharacterEntity characterEntity && (characterEntity.Channel != null))
            {
                Sessions.Add(characterEntity.Channel);
            }

            TryAdd(new Tuple<VisualType, long>(namedEntity.VisualType, namedEntity.VisualId),
                new Tuple<int, INamedEntity>(++_lastId, namedEntity));
        }

        public void LeaveGroup(INamedEntity namedEntity)
        {
            if (namedEntity is ICharacterEntity characterEntity && (characterEntity.Channel != null))
            {
                Sessions.Remove(characterEntity.Channel);
            }

            TryRemove(new Tuple<VisualType, long>(namedEntity.VisualType, namedEntity.VisualId), out _);
        }
    }
}