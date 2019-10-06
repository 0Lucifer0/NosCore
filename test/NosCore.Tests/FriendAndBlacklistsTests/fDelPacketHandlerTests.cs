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
using System.Linq.Expressions;
using ChickenAPI.Packets.ClientPackets.Relations;
using ChickenAPI.Packets.Enumerations;
using ChickenAPI.Packets.ServerPackets.UI;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Core;
using NosCore.Core.HttpClients.ChannelHttpClient;
using NosCore.Core.HttpClients.ConnectedAccountHttpClient;
using NosCore.Core.I18N;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.I18N;
using NosCore.Database.DAL;
using NosCore.Database.Entities;
using NosCore.GameObject.HttpClients.FriendHttpClient;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.MasterServer.Controllers;
using NosCore.MasterServer.DataHolders;
using NosCore.PacketHandlers.Friend;
using NosCore.Tests.Helpers;
using Serilog;

namespace NosCore.Tests.PacketHandlerTests
{
    [TestClass]
    public class FDelPacketHandlerTests
    {
        private static readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();
        private Mock<IChannelHttpClient> _channelHttpClient;
        private Mock<IGenericDao<CharacterDto>> _characterDao;
        private IGenericDao<CharacterRelationDto> _characterRelationDao;
        private Mock<IConnectedAccountHttpClient> _connectedAccountHttpClient;
        private FdelPacketHandler _fDelPacketHandler;
        private FriendController _friendController;
        private Mock<IFriendHttpClient> _friendHttpClient;
        private ClientSession _session;

        [TestInitialize]
        public void Setup()
        {
            _characterRelationDao = new GenericDao<CharacterRelation, CharacterRelationDto, Guid>(_logger);
            Broadcaster.Reset();
            TestHelpers.Reset();
            _session = TestHelpers.Instance.GenerateSession();
            _channelHttpClient = TestHelpers.Instance.ChannelHttpClient;
            _connectedAccountHttpClient = TestHelpers.Instance.ConnectedAccountHttpClient;
            _friendHttpClient = TestHelpers.Instance.FriendHttpClient;
            _fDelPacketHandler = new FdelPacketHandler(_friendHttpClient.Object, _channelHttpClient.Object,
                _connectedAccountHttpClient.Object);
            _characterDao = new Mock<IGenericDao<CharacterDto>>();
            _friendController = new FriendController(_logger, _characterRelationDao, _characterDao.Object,
                new FriendRequestHolder(), _connectedAccountHttpClient.Object);
            _friendHttpClient.Setup(s => s.GetListFriends(It.IsAny<long>()))
                .Returns((long id) => _friendController.GetFriends(id));
            _friendHttpClient.Setup(s => s.DeleteFriend(It.IsAny<Guid>()))
                .Callback((Guid id) => _friendController.Delete(id));
        }

        [TestMethod]
        public void Test_Delete_Friend_When_Disconnected()
        {
            var guid = Guid.NewGuid();
            var targetGuid = Guid.NewGuid();
            var list = new List<CharacterDto>
            {
                _session.Character,
                new CharacterDto {CharacterId = 2, Name = "test"}
            };
            _characterDao.Setup(s => s.FirstOrDefault(It.IsAny<Expression<Func<CharacterDto, bool>>>()))
                .Returns((Expression<Func<CharacterDto, bool>> exp) => list.FirstOrDefault(exp.Compile()));
            _characterRelationDao.InsertOrUpdate(new[]
            {
                new CharacterRelationDto
                {
                    CharacterId = 2,
                    CharacterRelationId = guid,
                    RelatedCharacterId = _session.Character.CharacterId,
                    RelationType = CharacterRelationType.Friend
                },
                new CharacterRelationDto
                {
                    RelatedCharacterId = 2,
                    CharacterRelationId = targetGuid,
                    CharacterId = _session.Character.CharacterId,
                    RelationType = CharacterRelationType.Friend
                }
            });
            var fdelPacket = new FdelPacket
            {
                CharacterId = 2
            };

            _fDelPacketHandler.Execute(fdelPacket, _session);

            Assert.IsTrue(_characterRelationDao.LoadAll().Count() == 0);
        }

        [TestMethod]
        public void Test_Delete_Friend()
        {
            var targetSession = TestHelpers.Instance.GenerateSession();
            var guid = Guid.NewGuid();
            var targetGuid = Guid.NewGuid();
            var list = new List<CharacterDto>
            {
                _session.Character,
                targetSession.Character
            };
            _characterDao.Setup(s => s.FirstOrDefault(It.IsAny<Expression<Func<CharacterDto, bool>>>()))
                .Returns((Expression<Func<CharacterDto, bool>> exp) => list.FirstOrDefault(exp.Compile()));
            _characterRelationDao.InsertOrUpdate(new[]
            {
                new CharacterRelationDto
                {
                    CharacterId = targetSession.Character.CharacterId,
                    CharacterRelationId = guid,
                    RelatedCharacterId = _session.Character.CharacterId,
                    RelationType = CharacterRelationType.Friend
                },
                new CharacterRelationDto
                {
                    RelatedCharacterId = targetSession.Character.CharacterId,
                    CharacterRelationId = targetGuid,
                    CharacterId = _session.Character.CharacterId,
                    RelationType = CharacterRelationType.Friend
                }
            });
            var fdelPacket = new FdelPacket
            {
                CharacterId = targetSession.Character.CharacterId
            };

            _fDelPacketHandler.Execute(fdelPacket, _session);

            Assert.IsTrue(_characterRelationDao.LoadAll().Count() == 0);
        }

        [TestMethod]
        public void Test_Delete_Friend_No_Friend()
        {
            var targetSession = TestHelpers.Instance.GenerateSession();
            var guid = Guid.NewGuid();
            var targetGuid = Guid.NewGuid();
            var list = new List<CharacterDto>
            {
                _session.Character,
                targetSession.Character
            };
            _characterDao.Setup(s => s.FirstOrDefault(It.IsAny<Expression<Func<CharacterDto, bool>>>()))
                .Returns((Expression<Func<CharacterDto, bool>> exp) => list.FirstOrDefault(exp.Compile()));

            var fdelPacket = new FdelPacket
            {
                CharacterId = targetSession.Character.CharacterId
            };

            _fDelPacketHandler.Execute(fdelPacket, _session);
            var lastpacket = (InfoPacket) _session.LastPackets.FirstOrDefault(s => s is InfoPacket);
            Assert.AreEqual(Language.Instance.GetMessageFromKey(LanguageKey.NOT_IN_FRIENDLIST,
                _session.Account.Language), lastpacket.Message);
        }
    }
}