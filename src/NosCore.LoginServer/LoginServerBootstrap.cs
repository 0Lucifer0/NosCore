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
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using AutofacSerilogIntegration;
using NosCore.Packets;
using NosCore.Packets.Interfaces;
using DotNetty.Buffers;
using DotNetty.Codecs;
using FastExpressionCompiler;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.Threading;
using NosCore.Core;
using NosCore.Core.Configuration;
using NosCore.Core.Encryption;
using NosCore.Core.HttpClients.AuthHttpClients;
using NosCore.Core.HttpClients.ChannelHttpClients;
using NosCore.Core.HttpClients.ConnectedAccountHttpClients;
using NosCore.Core.I18N;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.I18N;
using NosCore.Database;
using NosCore.Database.Entities;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.HttpClients.BlacklistHttpClient;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Networking.LoginService;
using NosCore.PacketHandlers.Login;
using Serilog;
using ILogger = Serilog.ILogger;
using NosCore.Dao;
using NosCore.Shared.Configuration;
using NosCore.Shared.Enumerations;

namespace NosCore.LoginServer
{
    public static class LoginServerBootstrap
    {
        private const string Title = "NosCore - LoginServer";
        private const string ConsoleText = "LOGIN SERVER - NosCoreIO";
        private static ILogger _logger = null!;

        private static DataAccessHelper _dataAccess = null!;

        private static void InitializeConfiguration(string[] args)
        {
            _logger = Shared.I18N.Logger.GetLoggerConfiguration().CreateLogger();
            Shared.I18N.Logger.PrintHeader(ConsoleText);

            var optionsBuilder = new DbContextOptionsBuilder<NosCoreContext>();
            var loginConfiguration = new LoginConfiguration();
            ConfiguratorBuilder.InitializeConfiguration(args, new[] { "logger.yml", "login.yml" }).Bind(loginConfiguration);
            optionsBuilder.UseNpgsql(loginConfiguration.Database!.ConnectionString);
            _dataAccess = new DataAccessHelper();
            _dataAccess.Initialize(optionsBuilder.Options, _logger);
        }

        private static void InitializeContainer(ContainerBuilder containerBuilder)
        {
            containerBuilder.Register<IDbContextBuilder>(c => _dataAccess).AsImplementedInterfaces().SingleInstance();
            containerBuilder.RegisterLogger();
            containerBuilder.RegisterType<Dao<Account, AccountDto, long>>().As<IDao<AccountDto, long>>()
                .SingleInstance();
            containerBuilder.RegisterType<LoginDecoder>().As<MessageToMessageDecoder<IByteBuffer>>();
            containerBuilder.RegisterType<LoginEncoder>().As<MessageToMessageEncoder<IEnumerable<IPacket>>>();
            containerBuilder.RegisterType<LoginServer>().PropertiesAutowired();
            containerBuilder.RegisterType<ClientSession>();

            containerBuilder.RegisterType<NetworkManager>();
            containerBuilder.RegisterType<PipelineFactory>();
            containerBuilder.RegisterType<LoginService>().AsImplementedInterfaces();
            containerBuilder.RegisterType<AuthHttpClient>().AsImplementedInterfaces();
            containerBuilder.RegisterType<ChannelHttpClient>().SingleInstance().AsImplementedInterfaces();
            containerBuilder.RegisterType<ConnectedAccountHttpClient>().AsImplementedInterfaces();
            containerBuilder.RegisterAssemblyTypes(typeof(BlacklistHttpClient).Assembly)
                .Where(t => t.Name.EndsWith("HttpClient"))
                .AsImplementedInterfaces()
                .PropertiesAutowired();
            containerBuilder.Register(c =>
            {
                var conf = c.Resolve<IOptions<LoginConfiguration>>();
                return new Channel
                {
                    MasterCommunication = conf.Value!.MasterCommunication,
                    ClientType = ServerType.LoginServer,
                    ClientName = $"{ServerType.LoginServer}",
                    Port = conf.Value.Port,
                    Host = conf.Value.Host!
                };
            });
            foreach (var type in typeof(NoS0575PacketHandler).Assembly.GetTypes())
            {
                if (typeof(IPacketHandler).IsAssignableFrom(type) && typeof(ILoginPacketHandler).IsAssignableFrom(type))
                {
                    containerBuilder.RegisterType(type)
                        .AsImplementedInterfaces()
                        .PropertiesAutowired();
                }
            }

            var listofpacket = typeof(IPacket).Assembly.GetTypes()
                .Where(p => ((p.Namespace == "NosCore.Packets.ServerPackets.Login") ||
                        (p.Namespace == "NosCore.Packets.ClientPackets.Login"))
                    && p.GetInterfaces().Contains(typeof(IPacket)) && p.IsClass && !p.IsAbstract).ToList();
            containerBuilder.Register(c => new Deserializer(listofpacket))
                .AsImplementedInterfaces()
                .SingleInstance();
            containerBuilder.Register(c => new Serializer(listofpacket))
                .AsImplementedInterfaces()
                .SingleInstance();
        }

        public static async Task Main(string[] args)
        {
            try
            {
                await BuildHost(args).RunAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.EXCEPTION), ex.Message);
            }
        }

        private static IHost BuildHost(string[] args)
        {
            return new HostBuilder()
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddSerilog();
                })
                .UseConsoleLifetime()
                .UseServiceProviderFactory(new AutofacServiceProviderFactory())
                .ConfigureServices((hostContext, services) =>
                {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        Console.Title = Title;
                    }

                    var configuration =
                        ConfiguratorBuilder.InitializeConfiguration(args, new[] {"logger.yml", "login.yml"});
                    services.AddOptions<LoginConfiguration>().Bind(configuration).ValidateDataAnnotations();
                    services.AddOptions<ServerConfiguration>().Bind(configuration).ValidateDataAnnotations();
                    InitializeConfiguration(args);

                    services.AddLogging(builder => builder.AddFilter("Microsoft", LogLevel.Warning));
                    services.AddHttpClient();
                    services.RemoveAll<IHttpMessageHandlerBuilderFilter>();
                    services.Configure<ConsoleLifetimeOptions>(o => o.SuppressStatusMessages = true);
                    services.AddDbContext<NosCoreContext>();
                    var containerBuilder = new ContainerBuilder();
                    InitializeContainer(containerBuilder);
                    containerBuilder.Populate(services);
                    var container = containerBuilder.Build();

                    Task.Run(container.Resolve<LoginServer>().RunAsync).Forget();
                    TypeAdapterConfig.GlobalSettings.ForDestinationType<IInitializable>()
                       .AfterMapping(dest => Task.Run(dest.InitializeAsync));
                    TypeAdapterConfig.GlobalSettings.EnableJsonMapping();
                    TypeAdapterConfig.GlobalSettings.Compiler = exp => exp.CompileFast();
                })
                .Build();
        }
    }
}