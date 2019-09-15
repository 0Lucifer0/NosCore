﻿using ChickenAPI.Packets.ClientPackets.Login;
using ChickenAPI.Packets.Enumerations;
using ChickenAPI.Packets.ServerPackets.Login;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;
using NosCore.Configuration;
using NosCore.Core;
using NosCore.Core.Controllers;
using NosCore.Core.Encryption;
using NosCore.Core.HttpClients.AuthHttpClient;
using NosCore.Core.HttpClients.ChannelHttpClient;
using NosCore.Core.HttpClients.ConnectedAccountHttpClient;
using NosCore.Data.Enumerations;
using NosCore.Data.WebApi;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Networking.LoginService;
using NosCore.PacketHandlers.Login;
using NosCore.Tests.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using NosCore.Core.I18N;
using NosCore.Core.Networking;

namespace NosCore.Tests.PacketHandlerTests
{
    [TestClass]
    public class NoS0577PacketHandlerTests
    {
        private LoginConfiguration _loginConfiguration;
        private ClientSession _session;
        private NoS0577PacketHandler _noS0577PacketHandler;
        private Mock<IAuthHttpClient> _authHttpClient;
        private Mock<IChannelHttpClient> _channelHttpClient;
        private Mock<IConnectedAccountHttpClient> _connectedAccountHttpClient;
        private static readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();
        [TestInitialize]
        public void Setup()
        {
            TestHelpers.Reset();
            _session = TestHelpers.Instance.GenerateSession();
            _loginConfiguration = new LoginConfiguration
            {
                MasterCommunication = new WebApiConfiguration()
            };
            _authHttpClient = new Mock<IAuthHttpClient>();
            _channelHttpClient = TestHelpers.Instance.ChannelHttpClient;
            _connectedAccountHttpClient = TestHelpers.Instance.ConnectedAccountHttpClient;
            _noS0577PacketHandler = new NoS0577PacketHandler(new LoginService(_loginConfiguration, TestHelpers.Instance.AccountDao,
                _authHttpClient.Object, _channelHttpClient.Object, _connectedAccountHttpClient.Object));
            var authController = new AuthController(_loginConfiguration.MasterCommunication,
                TestHelpers.Instance.AccountDao, _logger);
            SessionFactory.Instance.AuthCodes[_session.Account.Name] = "AA11AA11AA11".ToSha512();
            _authHttpClient.Setup(s => s.IsAwaitingConnection(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<int>())).Returns((string a, string b, int c) => (bool)((OkObjectResult)authController.IsExpectingConnection(a, b, c)).Value);
            SessionFactory.Instance.ReadyForAuth.Clear();
        }

        [TestMethod]
        public void LoginBCrypt()
        {
            _loginConfiguration.MasterCommunication.HashingType = HashingType.BCrypt;
            _channelHttpClient.Setup(s => s.GetChannels()).Returns(new List<ChannelInfo> { new ChannelInfo() });
            _connectedAccountHttpClient.Setup(s => s.GetConnectedAccount(It.IsAny<ChannelInfo>()))
                .Returns(new List<ConnectedAccount>());
            _session.Account.NewAuthSalt = BCrypt.Net.BCrypt.GenerateSalt();
            _session.Account.NewAuthPassword = "AA11AA11AA11".ToBcrypt(_session.Account.NewAuthSalt);

            SessionFactory.Instance.AuthCodes[_session.Account.Name] = "AA11AA11AA11";
            _noS0577PacketHandler.Execute(new NoS0577Packet
            {
                AuthToken = string.Join("", SessionFactory.Instance.AuthCodes[_session.Account.Name].Select(c => ((int)c).ToString("X2"))),
                Username = _session.Account.Name
            }, _session);


            Assert.IsNotNull((NsTestPacket)_session.LastPackets.FirstOrDefault(s => s is NsTestPacket));
        }

        [TestMethod]
        public void LoginPbkdf2()
        {
            _loginConfiguration.MasterCommunication.HashingType = HashingType.Pbkdf2;
            _channelHttpClient.Setup(s => s.GetChannels()).Returns(new List<ChannelInfo> { new ChannelInfo() });
            _connectedAccountHttpClient.Setup(s => s.GetConnectedAccount(It.IsAny<ChannelInfo>()))
                .Returns(new List<ConnectedAccount>());
            _session.Account.NewAuthPassword = "AA11AA11AA11".ToPbkdf2Hash("MY_SUPER_SECRET_HASH");
            _session.Account.NewAuthSalt = "MY_SUPER_SECRET_HASH";
            SessionFactory.Instance.AuthCodes[_session.Account.Name] = "AA11AA11AA11";
            _noS0577PacketHandler.Execute(new NoS0577Packet
            {
                AuthToken = string.Join("", SessionFactory.Instance.AuthCodes[_session.Account.Name].Select(c => ((int)c).ToString("X2"))),
                Username = _session.Account.Name
            }, _session);

            Assert.IsNotNull((NsTestPacket)_session.LastPackets.FirstOrDefault(s => s is NsTestPacket));
        }

        [TestMethod]
        public void LoginOldClient()
        {
            _loginConfiguration.ClientVersion = new ClientVersionSubPacket { Major = 1 };
            _noS0577PacketHandler.Execute(new NoS0577Packet
            {
                AuthToken = "AA11AA11AA11".ToSha512(),
                Username = _session.Account.Name.ToUpperInvariant()
            }, _session);

            Assert.IsTrue(((FailcPacket)_session.LastPackets.FirstOrDefault(s => s is FailcPacket)).Type == LoginFailType.OldClient);
        }

        [TestMethod]
        public void LoginNoAccount()
        {
            _noS0577PacketHandler.Execute(new NoS0577Packet
            {
                AuthToken = "AA11AA11AA11".ToSha512(),
                Username = "noaccount"
            }, _session);

            Assert.IsTrue(((FailcPacket)_session.LastPackets.FirstOrDefault(s => s is FailcPacket)).Type == LoginFailType.AccountOrPasswordWrong);
        }

        [TestMethod]
        public void LoginWrongCaps()
        {
            _noS0577PacketHandler.Execute(new NoS0577Packet
            {
                AuthToken = "AA11AA11AA11".ToSha512(),
                Username = _session.Account.Name.ToUpperInvariant()
            }, _session);

            Assert.IsTrue(((FailcPacket)_session.LastPackets.FirstOrDefault(s => s is FailcPacket)).Type == LoginFailType.WrongCaps);
        }

        [TestMethod]
        public void LoginWrongToken()
        {
            SessionFactory.Instance.AuthCodes[_session.Account.Name] = "AA11AA11AA11".ToSha512();
            _noS0577PacketHandler.Execute(new NoS0577Packet
            {
                AuthToken = string.Join("", "adsadasdsa".ToSha512().Select(c => ((int)c).ToString("X2"))),
                Username = _session.Account.Name
            }, _session);

            Assert.IsTrue(((FailcPacket)_session.LastPackets.FirstOrDefault(s => s is FailcPacket)).Type == LoginFailType.AccountOrPasswordWrong);
        }

        [TestMethod]
        public void Login()
        {
            _channelHttpClient.Setup(s => s.GetChannels()).Returns(new List<ChannelInfo> { new ChannelInfo() });
            _connectedAccountHttpClient.Setup(s => s.GetConnectedAccount(It.IsAny<ChannelInfo>()))
                .Returns(new List<ConnectedAccount>());
            SessionFactory.Instance.AuthCodes[_session.Account.Name] = "AA11AA11AA11".ToSha512();
            _noS0577PacketHandler.Execute(new NoS0577Packet
            {
                AuthToken = string.Join("", SessionFactory.Instance.AuthCodes[_session.Account.Name].Select(c => ((int)c).ToString("X2"))),
                Username = _session.Account.Name
            }, _session);

            Assert.IsNotNull((NsTestPacket)_session.LastPackets.FirstOrDefault(s => s is NsTestPacket));
        }

        [TestMethod]
        public void LoginAlreadyConnected()
        {
            _channelHttpClient.Setup(s => s.GetChannels()).Returns(new List<ChannelInfo> { new ChannelInfo() });
            _connectedAccountHttpClient.Setup(s => s.GetConnectedAccount(It.IsAny<ChannelInfo>()))
                .Returns(new List<ConnectedAccount>
            { new ConnectedAccount {Name = _session.Account.Name}});
            SessionFactory.Instance.AuthCodes[_session.Account.Name] = "AA11AA11AA11".ToSha512();
            _noS0577PacketHandler.Execute(new NoS0577Packet
            {
                AuthToken = string.Join("", SessionFactory.Instance.AuthCodes[_session.Account.Name].Select(c => ((int)c).ToString("X2"))),
                Username = _session.Account.Name
            }, _session);
            Assert.IsTrue(((FailcPacket)_session.LastPackets.FirstOrDefault(s => s is FailcPacket)).Type == LoginFailType.AlreadyConnected);
        }

        [TestMethod]
        public void LoginNoServer()
        {
            _channelHttpClient.Setup(s => s.GetChannels()).Returns(new List<ChannelInfo>());
            _connectedAccountHttpClient.Setup(s => s.GetConnectedAccount(It.IsAny<ChannelInfo>()))
                .Returns(new List<ConnectedAccount>());
            SessionFactory.Instance.AuthCodes[_session.Account.Name] = "AA11AA11AA11".ToSha512();
            _noS0577PacketHandler.Execute(new NoS0577Packet
            {
                AuthToken = string.Join("", SessionFactory.Instance.AuthCodes[_session.Account.Name].Select(c => ((int)c).ToString("X2"))),
                Username = _session.Account.Name
            }, _session);
            Assert.IsTrue(((FailcPacket)_session.LastPackets.FirstOrDefault(s => s is FailcPacket)).Type == LoginFailType.CantConnect);
        }

        //[TestMethod]
        //public void LoginBanned()
        //{
        //    _handler.VerifyLogin(new NoS0575Packet
        //    {
        //        Password ="test".Sha512(),
        //        Name = Name,
        //    });
        //    Assert.IsTrue(_session.LastPacket is FailcPacket);
        //    Assert.IsTrue(((FailcPacket) _session.LastPacket).Type == LoginFailType.Banned);
        //}

        //[TestMethod]
        //public void LoginMaintenance()
        //{
        //    _handler.VerifyLogin(new NoS0575Packet
        //    {
        //        Password ="test".Sha512(),
        //        Name = Name,
        //    });
        //    Assert.IsTrue(_session.LastPacket is FailcPacket);
        //    Assert.IsTrue(((FailcPacket)_session.LastPacket.FirstOrDefault(s => s is FailcPacket)).Type == LoginFailType.Maintenance);
        //}
    }
}
