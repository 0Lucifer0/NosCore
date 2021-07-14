//  __  _  __    __   ___ __  ___ ___
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
using System.Threading.Tasks;
using DotNetty.Transport.Channels;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using NosCore.Algorithm.DignityService;
using NosCore.Algorithm.ExperienceService;
using NosCore.Algorithm.HeroExperienceService;
using NosCore.Algorithm.HpService;
using NosCore.Algorithm.JobExperienceService;
using NosCore.Algorithm.MpService;
using NosCore.Algorithm.ReputationService;
using NosCore.Algorithm.SpeedService;
using NosCore.Core.Configuration;
using NosCore.Core.Encryption;
using NosCore.Core.HttpClients.ChannelHttpClients;
using NosCore.Core.HttpClients.ConnectedAccountHttpClients;
using NosCore.Dao;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Character;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.Enumerations.Map;
using NosCore.Data.StaticEntities;
using NosCore.Data.WebApi;
using NosCore.Database;
using NosCore.Database.Entities;
using NosCore.GameObject;
using NosCore.GameObject.Holders;
using NosCore.GameObject.HttpClients.BazaarHttpClient;
using NosCore.GameObject.HttpClients.BlacklistHttpClient;
using NosCore.GameObject.HttpClients.FriendHttpClient;
using NosCore.GameObject.HttpClients.PacketHttpClient;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.EventLoaderService;
using NosCore.GameObject.Services.ExchangeService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.ItemGenerationService.Handlers;
using NosCore.GameObject.Services.MapInstanceAccessService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.GameObject.Services.MapItemGenerationService;
using NosCore.GameObject.Services.MapItemGenerationService.Handlers;
using NosCore.GameObject.Services.MinilandService;
using NosCore.PacketHandlers.Bazaar;
using NosCore.PacketHandlers.CharacterScreen;
using NosCore.PacketHandlers.Friend;
using NosCore.PacketHandlers.Inventory;
using NosCore.Packets.ClientPackets.Drops;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.PathFinder.Heuristic;
using NosCore.PathFinder.Interfaces;
using Serilog;
using Character = NosCore.Database.Entities.Character;
using InventoryItemInstance = NosCore.Database.Entities.InventoryItemInstance;
using Item = NosCore.GameObject.Services.ItemGenerationService.Item.Item;
using ItemInstance = NosCore.Database.Entities.ItemInstance;
using Map = NosCore.GameObject.Map.Map;
using MapMonster = NosCore.Database.Entities.MapMonster;
using MapNpc = NosCore.Database.Entities.MapNpc;
using Miniland = NosCore.Database.Entities.Miniland;
using Portal = NosCore.Database.Entities.Portal;
using ShopItem = NosCore.Database.Entities.ShopItem;

namespace NosCore.Tests.Shared
{
    public class TestHelpers
    {
        private static Lazy<TestHelpers> _lazy =
            new Lazy<TestHelpers>(() => new TestHelpers());

        private IDao<InventoryItemInstanceDto, Guid> _inventoryItemInstanceDao = null!;
        private IDao<IItemInstanceDto?, Guid> _itemInstanceDao = null!;
        private readonly ILogger _logger = new Mock<ILogger>().Object;
        private IDao<MapMonsterDto, int> _mapMonsterDao = null!;
        private IDao<MapNpcDto, int> _mapNpcDao = null!;
        private IDao<PortalDto, int> _portalDao = null!;
        private IDao<ShopItemDto, int> _shopItemDao = null!;
        private IDao<StaticBonusDto, long> _staticBonusDao = null!;
        private int _lastId = 100;
        public Mock<IBlacklistHttpClient> BlacklistHttpClient = new Mock<IBlacklistHttpClient>();
        public Mock<IChannelHttpClient> ChannelHttpClient = new Mock<IChannelHttpClient>();
        public Mock<IConnectedAccountHttpClient> ConnectedAccountHttpClient = new Mock<IConnectedAccountHttpClient>();
        public Mock<IFriendHttpClient> FriendHttpClient = new Mock<IFriendHttpClient>();
        public Mock<IPacketHttpClient> PacketHttpClient = new Mock<IPacketHttpClient>();

        private TestHelpers()
        {
            BlacklistHttpClient.Setup(s => s.GetBlackListsAsync(It.IsAny<long>()))
                .ReturnsAsync(new List<CharacterRelationStatus>());
            FriendHttpClient.Setup(s => s.GetListFriendsAsync(It.IsAny<long>()))
                .ReturnsAsync(new List<CharacterRelationStatus>());
            InitDatabase();
        }

        public static TestHelpers Instance => _lazy.Value;

        public IDao<AccountDto, long> AccountDao { get; private set; } = null!;
        public IDao<MateDto, long> MateDao { get; private set; } = null!;
        public IDao<CharacterRelationDto, Guid> CharacterRelationDao { get; set; } = null!;
        public IDao<CharacterDto, long> CharacterDao { get; private set; } = null!;
        public IDao<MinilandDto, Guid> MinilandDao { get; private set; } = null!;
        public IDao<MinilandObjectDto, Guid> MinilandObjectDao { get; private set; } = null!;
        public IMapItemGenerationService? MapItemProvider { get; set; }
        public Guid MinilandId { get; set; } = Guid.NewGuid();

        public IOptions<WorldConfiguration> WorldConfiguration { get; } = Options.Create(new WorldConfiguration
        {
            BackpackSize = 2,
            MaxItemAmount = 999,
            MaxSpPoints = 10_000,
            MaxAdditionalSpPoints = 1_000_000,
            MaxGoldAmount = 999_999_999
        });

        public List<ItemDto> ItemList { get; } = new List<ItemDto>
        {
            new Item {Type = NoscorePocketType.Main, VNum = 1012, IsDroppable = true},
            new Item {Type = NoscorePocketType.Main, VNum = 1013},
            new Item {Type = NoscorePocketType.Equipment, VNum = 1, ItemType = ItemType.Weapon},
            new Item
            {
                Type = NoscorePocketType.Equipment, VNum = 2, EquipmentSlot = EquipmentType.Fairy,
                Element = ElementType.Water
            },
            new Item
            {
                Type = NoscorePocketType.Equipment, VNum = 912, ItemType = ItemType.Specialist, ReputationMinimum = 2,
                Element = ElementType.Fire
            },
            new Item {Type = NoscorePocketType.Equipment, VNum = 924, ItemType = ItemType.Fashion},
            new Item
            {
                Type = NoscorePocketType.Main, VNum = 1078, ItemType = ItemType.Special,
                Effect = ItemEffectType.DroppedSpRecharger, EffectValue = 10_000, WaitDelay = 5_000
            }
        };

        public MapInstanceGeneratorService MapInstanceGeneratorService { get; set; } = null!;

        public MapInstanceAccessorService MapInstanceAccessorService { get; set; } = null!;
        public IHeuristic DistanceCalculator { get; set; } = new OctileDistanceHeuristic();

        private async Task GenerateMapInstanceProviderAsync()
        {
            MapItemProvider = new MapItemGenerationService(new EventLoaderService<MapItem, Tuple<MapItem, GetPacket>, IGetMapItemEventHandler>(new List<IEventHandler<MapItem, Tuple<MapItem, GetPacket>>>
                {new DropEventHandler(), new SpChargerEventHandler(), new GoldDropEventHandler(TestHelpers.Instance.WorldConfiguration)}));
            var map = new Map
            {
                MapId = 0,
                NameI18NKey = "testMap",
                Data = new byte[]
                {
                    8, 0, 8, 0,
                    0, 0, 0, 0, 0, 0, 0, 0,
                    0, 1, 1, 1, 0, 0, 0, 0,
                    0, 1, 1, 1, 0, 0, 0, 0,
                    0, 1, 1, 1, 0, 0, 0, 0,
                    0, 0, 0, 0, 0, 0, 0, 0,
                    0, 0, 0, 0, 0, 0, 0, 0
                }
            };

            var mapShop = new Map
            {
                MapId = 1,
                NameI18NKey = "shopMap",
                ShopAllowed = true,
                Data = new byte[]
                {
                    8, 0, 8, 0,
                    0, 0, 0, 0, 0, 0, 0, 0,
                    0, 1, 1, 1, 0, 0, 0, 0,
                    0, 1, 1, 1, 0, 0, 0, 0,
                    0, 1, 1, 1, 0, 0, 0, 0,
                    0, 0, 0, 0, 0, 0, 0, 0,
                    0, 0, 0, 0, 0, 0, 0, 0
                }
            };

            var miniland = new Map
            {
                MapId = 20001,
                NameI18NKey = "miniland",
                ShopAllowed = true,
                Data = new byte[]
                {
                    8, 0, 8, 0,
                    0, 0, 0, 0, 0, 0, 0, 0,
                    0, 1, 1, 1, 0, 0, 0, 0,
                    0, 1, 1, 1, 0, 0, 0, 0,
                    0, 1, 1, 1, 0, 0, 0, 0,
                    0, 0, 0, 0, 0, 0, 0, 0,
                    0, 0, 0, 0, 0, 0, 0, 0
                }
            };
            var npc = new MapNpcDto();
            await _mapNpcDao.TryInsertOrUpdateAsync(npc).ConfigureAwait(false);
            var holder = new MapInstanceHolder();
            MapInstanceAccessorService = new MapInstanceAccessorService(holder);
            var instanceGeneratorService = new MapInstanceGeneratorService(new List<MapDto> { map, mapShop, miniland }, new List<NpcMonsterDto>(), new List<NpcTalkDto>(), new List<ShopDto>(),
                MapItemProvider,
                _mapNpcDao,
                _mapMonsterDao, _portalDao, _shopItemDao, _logger, new EventLoaderService<MapInstance, MapInstance, IMapInstanceEntranceEventHandler>(new List<IEventHandler<MapInstance, MapInstance>>()), holder, MapInstanceAccessorService);
            await instanceGeneratorService.InitializeAsync().ConfigureAwait(false);
            await instanceGeneratorService.AddMapInstanceAsync(new MapInstance(miniland, MinilandId, false,
                MapInstanceType.NormalInstance, MapItemProvider, _logger)).ConfigureAwait(false);
            MapInstanceGeneratorService = instanceGeneratorService;
        }

        public IItemGenerationService GenerateItemProvider()
        {
            return new ItemGenerationService(ItemList, new EventLoaderService<Item,
                Tuple<GameObject.Services.InventoryService.InventoryItemInstance, UseItemPacket>, IUseItemEventHandler>(
                new List<IEventHandler<Item,
                    Tuple<GameObject.Services.InventoryService.InventoryItemInstance, UseItemPacket>>>
                {
                    new SpRechargerEventHandler(WorldConfiguration),
                    new VehicleEventHandler(_logger),
                    new WearEventHandler(_logger)
                }), _logger);
        }

        public void InitDatabase()
        {
            var optionsBuilder = new DbContextOptionsBuilder<NosCoreContext>().UseInMemoryDatabase(
                Guid.NewGuid().ToString());
            DbContext ContextBuilder() => new NosCoreContext(optionsBuilder.Options);
            CharacterRelationDao = new Dao<Database.Entities.CharacterRelation, CharacterRelationDto, Guid>(_logger, ContextBuilder);
            AccountDao = new Dao<Account, AccountDto, long>(_logger, ContextBuilder);
            MateDao = new Dao<Mate, MateDto, long>(_logger, ContextBuilder);
            _portalDao = new Dao<Portal, PortalDto, int>(_logger, ContextBuilder);
            _mapMonsterDao = new Dao<MapMonster, MapMonsterDto, int>(_logger, ContextBuilder);
            _mapNpcDao = new Dao<MapNpc, MapNpcDto, int>(_logger, ContextBuilder);
            MinilandDao = new Dao<Miniland, MinilandDto, Guid>(_logger, ContextBuilder);
            MinilandObjectDao = new Dao<MinilandObject, MinilandObjectDto, Guid>(_logger, ContextBuilder);
            _shopItemDao = new Dao<ShopItem, ShopItemDto, int>(_logger, ContextBuilder);
            CharacterDao = new Dao<Character, CharacterDto, long>(_logger, ContextBuilder);
            _itemInstanceDao = new Dao<ItemInstance, IItemInstanceDto?, Guid>(_logger, ContextBuilder);
            _inventoryItemInstanceDao = new Dao<InventoryItemInstance, InventoryItemInstanceDto, Guid>(_logger, ContextBuilder);
            _staticBonusDao = new Dao<StaticBonus, StaticBonusDto, long>(_logger, ContextBuilder);
            TypeAdapterConfig.GlobalSettings.AllowImplicitSourceInheritance = false;
            TypeAdapterConfig.GlobalSettings.ForDestinationType<IPacket>().Ignore(s => s.ValidationResult!);
            TypeAdapterConfig<MapNpcDto, GameObject.MapNpc>.NewConfig()
                .ConstructUsing(src => new GameObject.MapNpc(GenerateItemProvider(), _logger, TestHelpers.Instance.DistanceCalculator));
            TypeAdapterConfig<MapMonsterDto, GameObject.MapMonster>.NewConfig()
                .ConstructUsing(src => new GameObject.MapMonster(_logger, TestHelpers.Instance.DistanceCalculator));
        }

        public async Task<ClientSession> GenerateSessionAsync(List<IPacketHandler>? packetHandlers = null)
        {
            _lastId++;
            var acc = new AccountDto
            { AccountId = _lastId, Name = "AccountTest" + _lastId, Password = new Sha512Hasher().Hash("test") };
            acc = await AccountDao.TryInsertOrUpdateAsync(acc).ConfigureAwait(false);
            var minilandProvider = new Mock<IMinilandService>();
            var session = new ClientSession(WorldConfiguration,
                MapInstanceAccessorService,
                new Mock<IExchangeService>().Object,
                _logger,
                packetHandlers ?? new List<IPacketHandler>
                {
                    new CharNewPacketHandler(CharacterDao, MinilandDao, new Mock<IItemGenerationService>().Object, new Mock<IDao<QuicklistEntryDto, Guid>>().Object,
                            new Mock<IDao<IItemInstanceDto?, Guid>>().Object, new Mock<IDao<InventoryItemInstanceDto, Guid>>().Object, new HpService(), new MpService(), WorldConfiguration),
                    new BlInsPackettHandler(BlacklistHttpClient.Object, _logger),
                    new UseItemPacketHandler(),
                    new FinsPacketHandler(FriendHttpClient.Object, ChannelHttpClient.Object,
                        ConnectedAccountHttpClient.Object),
                    new SelectPacketHandler(CharacterDao, _logger, new Mock<IItemGenerationService>().Object, MapInstanceAccessorService,
                        _itemInstanceDao, _inventoryItemInstanceDao, _staticBonusDao, new Mock<IDao<QuicklistEntryDto, Guid>>().Object, new Mock<IDao<TitleDto, Guid>>().Object, new Mock<IDao<CharacterQuestDto, Guid>>().Object,
                        new Mock<IDao<ScriptDto, Guid>>().Object, new List<QuestDto>(), new List<QuestObjectiveDto>(),WorldConfiguration),
                    new CSkillPacketHandler(),
                    new CBuyPacketHandler(new Mock<IBazaarHttpClient>().Object, new Mock<IItemGenerationService>().Object, _logger, _itemInstanceDao),
                    new CRegPacketHandler(WorldConfiguration, new Mock<IBazaarHttpClient>().Object, _itemInstanceDao, _inventoryItemInstanceDao),
                    new CScalcPacketHandler(WorldConfiguration, new Mock<IBazaarHttpClient>().Object, new Mock<IItemGenerationService>().Object, _logger, _itemInstanceDao)
                },
                FriendHttpClient.Object,
                new Mock<ISerializer>().Object,
                PacketHttpClient.Object,
                minilandProvider.Object,
                MapInstanceGeneratorService)
            {
                SessionId = _lastId
            };

            var chara = new GameObject.Character(new InventoryService(ItemList, WorldConfiguration, _logger),
                new ExchangeService(new Mock<IItemGenerationService>().Object, WorldConfiguration, _logger, new ExchangeRequestHolder()), new Mock<IItemGenerationService>().Object, CharacterDao, new Mock<IDao<IItemInstanceDto?, Guid>>().Object, new Mock<IDao<InventoryItemInstanceDto, Guid>>().Object, AccountDao,
                _logger, new Mock<IDao<StaticBonusDto, long>>().Object, new Mock<IDao<QuicklistEntryDto, Guid>>().Object, new Mock<IDao<MinilandDto, Guid>>().Object, minilandProvider.Object, new Mock<IDao<TitleDto, Guid>>().Object, new Mock<IDao<CharacterQuestDto, Guid>>().Object
                , new HpService(), new MpService(), new ExperienceService(), new JobExperienceService(), new HeroExperienceService(), new SpeedService(), new ReputationService(), new DignityService(), TestHelpers.Instance.WorldConfiguration)
            {
                CharacterId = _lastId,
                Name = "TestExistingCharacter" + _lastId,
                Slot = 1,
                AccountId = acc.AccountId,
                MapId = 1,
                State = CharacterState.Active,
                Level = 1,
                JobLevel = 1,
                StaticBonusList = new List<StaticBonusDto>(),
                Titles = new List<TitleDto>()
            };
            await CharacterDao.TryInsertOrUpdateAsync(chara).ConfigureAwait(false);
            session.InitializeAccount(acc);
            await session.SetCharacterAsync(chara).ConfigureAwait(false);
            session.Character.MapInstance = MapInstanceAccessorService.GetBaseMapById(0);
            session.Account = acc;
            session.RegisterChannel(new Mock<IChannel>().Object);
            Broadcaster.Instance.RegisterSession(session);
            return session;
        }

        public static async Task ResetAsync()
        {
            _lazy = new Lazy<TestHelpers>(() => new TestHelpers());
            Instance.InitDatabase();
            await Instance.GenerateMapInstanceProviderAsync().ConfigureAwait(false);
        }
    }
}