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

using System.Collections.Generic;
using Mapster;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Core;
using NosCore.Core.I18N;
using NosCore.Data;
using NosCore.Data.Dto;
using NosCore.Data.StaticEntities;
using NosCore.Database;
using NosCore.Database.DAL;
using NosCore.Database.Entities;
using NosCore.GameObject.Mapping;
using Serilog;
using BoxInstance = NosCore.GameObject.Providers.ItemProvider.Item.BoxInstance;
using Item = NosCore.GameObject.Providers.ItemProvider.Item.Item;
using MapMonster = NosCore.GameObject.MapMonster;
using SpecialistInstance = NosCore.GameObject.Providers.ItemProvider.Item.SpecialistInstance;
using UsableInstance = NosCore.GameObject.Providers.ItemProvider.Item.UsableInstance;
using WearableInstance = NosCore.GameObject.Providers.ItemProvider.Item.WearableInstance;

namespace NosCore.Tests
{
    [TestClass]
    public class MapperTests
    {
        private static readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();
        private readonly IGenericDao<AccountDto> _accountDao = new GenericDao<Account, AccountDto, long>(_logger);
        private readonly MapsterMapper.Mapper _mapper = new MapsterMapper.Mapper();

        private readonly IGenericDao<CharacterDto> _characterDao =
            new GenericDao<Character, CharacterDto, long>(_logger);

        private readonly IGenericDao<IItemInstanceDto> _itemInstanceDao = new ItemInstanceDao(_logger);

        [TestInitialize]
        public void Setup()
        {
            TypeAdapterConfig<CharacterDto, GameObject.Character>.NewConfig().ConstructUsing(src =>
                new GameObject.Character(null, null, null, _characterDao, _itemInstanceDao, null, _accountDao, _logger,
                    null, null, null, null, null));
            TypeAdapterConfig<MapMonsterDto, MapMonster>.NewConfig()
                .ConstructUsing(src => new MapMonster(new List<NpcMonsterDto>(), _logger));
            new Mapper();
        }

        [TestMethod]
        public void GoToDtoMappingWorks()
        {
            var monsterGo = new MapMonster(new List<NpcMonsterDto>(), _logger);
            var monsterDto = _mapper.Map<MapMonsterDto>(monsterGo);
            Assert.IsNotNull(monsterDto);
        }

        [TestMethod]
        public void DtoToGoMappingWorks()
        {
            var monsterDto = new MapMonsterDto();
            var monsterGo = _mapper.Map<MapMonster>(monsterDto);
            Assert.IsNotNull(monsterGo);
        }

        [TestMethod]
        public void GoToEntityMappingWorks()
        {
            var monsterGo = new MapMonster(new List<NpcMonsterDto>(), _logger);
            var monsterEntity = _mapper.Map<Database.Entities.MapMonster>(monsterGo);
            Assert.IsNotNull(monsterEntity);
        }

        [TestMethod]
        public void DtoToEntityMappingWorks()
        {
            var monsterDto = new MapMonsterDto();
            var monsterEntity = _mapper.Map<Database.Entities.MapMonster>(monsterDto);
            Assert.IsNotNull(monsterEntity);
        }

        [TestMethod]
        public void EntityToDtoMappingWorks()
        {
            var monsterEntity = new Database.Entities.MapMonster();
            var monsterDto = _mapper.Map<MapMonsterDto>(monsterEntity);
            Assert.IsNotNull(monsterDto);
        }

        [TestMethod]
        public void WearableInstanceToItemInstanceEntity()
        {
            var wearableInstanceGo = new WearableInstance(new Item());
            var itemInstanceEntity = _mapper.Map<ItemInstance>(wearableInstanceGo);
            Assert.IsNotNull(itemInstanceEntity);
        }

        [TestMethod]
        public void WearableInstanceToItemInstanceGo()
        {
            var wearableInstanceGo = new WearableInstance(new Item());
            var itemInstanceGo =
                _mapper.Map<GameObject.Providers.ItemProvider.Item.ItemInstance>(wearableInstanceGo);
            Assert.IsNotNull(itemInstanceGo);
        }

        [TestMethod]
        public void WearableInstanceToItemInstanceDto()
        {
            var wearableInstanceGo = new WearableInstance(new Item());
            var itemInstancedto = _mapper.Map<ItemInstanceDto>(wearableInstanceGo);
            Assert.IsNotNull(itemInstancedto);
        }

        [TestMethod]
        public void ItemInstanceToItemInstanceEntity()
        {
            var itemInstanceGo = new GameObject.Providers.ItemProvider.Item.ItemInstance(new Item());
            var itemInstanceEntity = _mapper.Map<ItemInstance>(itemInstanceGo);
            Assert.IsNotNull(itemInstanceEntity);
        }


        [TestMethod]
        public void ItemInstanceDtoToItemInstanceEntity()
        {
            var itemInstanceDto = new ItemInstanceDto();
            var itemInstanceEntity = _mapper.Map<ItemInstance>(itemInstanceDto);
            Assert.IsNotNull(itemInstanceEntity);
        }

        [TestMethod]
        public void WearableDtoToSpecialistDtoShouldFail()
        {
            var wearableInstanceDto = new WearableInstanceDto();
            var specialistInstanceDto = _mapper.Map<SpecialistInstanceDto>(wearableInstanceDto);
            Assert.IsNull(specialistInstanceDto);
        }

        [TestMethod]
        public void WearableToSpecialistDtoShouldFail()
        {
            var wearableInstance = new WearableInstance();
            var specialistInstanceDto = _mapper.Map<SpecialistInstanceDto>(wearableInstance);
            Assert.IsNull(specialistInstanceDto);
        }

        [TestMethod]
        public void SpecialistDtoToSpecialistShouldPass()
        {
            var specialistInstanceDto = new SpecialistInstanceDto();
            var specialistInstance = _mapper.Map<SpecialistInstance>(specialistInstanceDto);
            Assert.IsNotNull(specialistInstance);
        }

        [TestMethod]
        public void BoxDtoToBoxShouldPass()
        {
            var boxInstanceDto = new BoxInstanceDto();
            var boxInstance = _mapper.Map<BoxInstance>(boxInstanceDto);
            Assert.IsNotNull(boxInstance);
        }

        [TestMethod]
        public void UsableDtoToUsableShouldPass()
        {
            var usableInstanceDto = new UsableInstanceDto();
            var usableInstance = _mapper.Map<UsableInstance>(usableInstanceDto);
            Assert.IsNotNull(usableInstance);
        }
    }
}