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

using System.Collections.Concurrent;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.Character;
using NosCore.Data.Enumerations.Group;
using NosCore.GameObject;
using NosCore.GameObject.Networking.Group;
using Serilog;

namespace NosCore.Tests
{
    [TestClass]
    public class GroupTests
    {
        private static readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();
        private Group _group;

        [TestInitialize]
        public void Setup()
        {
            _group = new Group(GroupType.Group);

            GroupAccess.Instance.Groups = new ConcurrentDictionary<long, Group>();

            _group.GroupId = GroupAccess.Instance.GetNextGroupId();
        }

        [TestMethod]
        public void Test_Add_Player()
        {
            var entity = new Character(null, null, null, null, null, null, null, _logger, null, null, null, null)
            {
                Name = "TestExistingCharacter",
                Slot = 1,
                AccountId = 1,
                MapId = 1,
                State = CharacterState.Active
            };

            _group.JoinGroup(entity);

            Assert.IsFalse(_group.Count == 2);
        }

        [TestMethod]
        public void Test_Remove_Player()
        {
            var entity = new Character(null, null, null, null, null, null, null, _logger, null, null, null, null)
            {
                Name = "TestExistingCharacter",
                Slot = 1,
                AccountId = 1,
                MapId = 1,
                State = CharacterState.Active
            };

            _group.JoinGroup(entity);

            Assert.IsFalse(_group.Count == 2);

            _group.LeaveGroup(entity);

            Assert.IsTrue(_group.Count == 0);
        }

        [TestMethod]
        public void Test_Monster_Join_Group()
        {
            var entity = new Pet();

            _group.JoinGroup(entity);

            Assert.IsTrue(_group.IsEmpty);
        }

        [TestMethod]
        public void Test_Leader_Change()
        {
            for (var i = 0; i < (long) _group.Type; i++)
            {
                var entity = new Character(null, null, null, null, null, null, null, _logger, null, null, null, null)
                {
                    CharacterId = i + 1,
                    Name = $"TestExistingCharacter{i}",
                    Slot = 1,
                    AccountId = i + 1,
                    MapId = 1,
                    State = CharacterState.Active
                };

                _group.JoinGroup(entity);
            }

            Assert.IsTrue(_group.IsGroupFull && _group.IsGroupLeader(_group.ElementAt(0).Value.Item2.VisualId));

            _group.LeaveGroup(_group.ElementAt(0).Value.Item2);

            Assert.IsTrue(!_group.IsGroupFull && _group.IsGroupLeader(_group.ElementAt(1).Value.Item2.VisualId));
        }
    }
}