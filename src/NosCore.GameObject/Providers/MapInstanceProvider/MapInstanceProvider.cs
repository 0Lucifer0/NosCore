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
using System.Reactive.Linq;
using System.Threading.Tasks;
using Mapster;
using NosCore.Core;
using NosCore.Core.I18N;
using NosCore.Data.AliveEntities;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Map;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Providers.MapInstanceProvider.Handlers;
using NosCore.GameObject.Providers.MapItemProvider;
using Serilog;

namespace NosCore.GameObject.Providers.MapInstanceProvider
{
    public class MapInstanceProvider : IMapInstanceProvider
    {
        private readonly ILogger _logger;
        private readonly IMapItemProvider _mapItemProvider;
        private readonly IGenericDao<MapMonsterDto> _mapMonsters;
        private readonly IGenericDao<PortalDto> _portalDao;
        private readonly List<MapDto> _maps;
        private readonly IGenericDao<MapNpcDto> _mapNpcs;
        private ConcurrentDictionary<Guid, MapInstance> MapInstances =
            new ConcurrentDictionary<Guid, MapInstance>();

        public MapInstanceProvider(List<MapDto> maps,
            IMapItemProvider mapItemProvider, IGenericDao<MapNpcDto> mapNpcs,
            IGenericDao<MapMonsterDto> mapMonsters, IGenericDao<PortalDto> portalDao, ILogger logger)
        {
            _mapItemProvider = mapItemProvider;
            _mapMonsters = mapMonsters;
            _portalDao = portalDao;
            _maps = maps;
            _mapNpcs = mapNpcs;
            _logger = logger;
        }

        public void AddMapInstance(MapInstance mapInstance)
        {
            MapInstances.TryAdd(mapInstance.MapInstanceId, mapInstance);
            LoadPortals(mapInstance, _portalDao.Where(s => s.SourceMapId == mapInstance.Map.MapId).ToList());
        }

        private void LoadPortals(MapInstance mapInstance, List<PortalDto> portals)
        {
            var partitioner = Partitioner.Create(
                portals.Where(s => s.SourceMapId == mapInstance.Map.MapId).Adapt<List<Portal>>(),
                EnumerablePartitionerOptions.None);
            var portalList = new ConcurrentDictionary<int, Portal>();
            Parallel.ForEach(partitioner, portal =>
            {
                portal.SourceMapInstanceId = mapInstance.MapInstanceId;
                if (portal.DestinationMapInstanceId == default)
                {
                    portal.DestinationMapInstanceId = GetBaseMapInstanceIdByMapId(portal.DestinationMapId);
                }
                portalList[portal.PortalId] = portal;
            });
            mapInstance.Portals.AddRange(portalList.Select(s => s.Value));
        }

        public void RemoveMap(Guid mapInstanceId)
        {
            MapInstances.TryRemove(mapInstanceId, out var mapInstance);
            mapInstance?.Kick();
        }

        public void Initialize()
        {
            _logger.Information(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.LOADING_MAPINSTANCES));

            var monsters = _mapMonsters.LoadAll().Adapt<IEnumerable<MapMonster>>().GroupBy(u => u.MapId).ToDictionary(group => group.Key, group => group.ToList());
            var npcs = _mapNpcs.LoadAll().Adapt<IEnumerable<MapNpc>>().GroupBy(u => u.MapId).ToDictionary(group => group.Key, group => group.ToList());
            var portals = _portalDao.LoadAll().ToList();

            var mapsdic = _maps.ToDictionary(x => x.MapId, x => Guid.NewGuid());
            MapInstances = new ConcurrentDictionary<Guid, MapInstance>(_maps.Adapt<List<Map.Map>>().ToDictionary(
                map => mapsdic[map.MapId],
                map =>
                {
                    var mapinstance = CreateMapInstance(map, mapsdic[map.MapId], map.ShopAllowed,
                        MapInstanceType.BaseMapInstance, new List<IMapInstanceEventHandler>());

                    if (monsters.ContainsKey(map.MapId))
                    {
                        mapinstance.LoadMonsters(monsters[map.MapId]);
                    }
                    if (npcs.ContainsKey(map.MapId))
                    {
                        mapinstance.LoadNpcs(npcs[map.MapId]);
                    }
                    mapinstance.StartLife();
                    return mapinstance;
                }));

            var mapInstancePartitioner = Partitioner.Create(MapInstances.Values, EnumerablePartitionerOptions.NoBuffering);
            Parallel.ForEach(mapInstancePartitioner, mapInstance =>
            {
                LoadPortals(mapInstance, portals);
            });
        }

        public Guid GetBaseMapInstanceIdByMapId(short mapId)
        {
            return MapInstances.FirstOrDefault(s =>
                s.Value?.Map.MapId == mapId && s.Value.MapInstanceType == MapInstanceType.BaseMapInstance).Key;
        }

        public MapInstance GetMapInstance(Guid id)
        {
            return MapInstances.ContainsKey(id) ? MapInstances[id] : null;
        }

        public MapInstance GetBaseMapById(short mapId)
        {
            return MapInstances.FirstOrDefault(s =>
                s.Value?.Map.MapId == mapId && s.Value.MapInstanceType == MapInstanceType.BaseMapInstance).Value;
        }

        public MapInstance CreateMapInstance(Map.Map map, Guid guid, bool shopAllowed, MapInstanceType normalInstance, List<IMapInstanceEventHandler> mapInstanceEventHandler)
        {
           return new MapInstance(map, guid, shopAllowed, normalInstance, _mapItemProvider, _logger, mapInstanceEventHandler);
        }
    }
}