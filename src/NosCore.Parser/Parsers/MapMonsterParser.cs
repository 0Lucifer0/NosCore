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
using NosCore.Core;
using NosCore.Core.I18N;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.StaticEntities;
using Serilog;

namespace NosCore.Parser.Parsers
{
    public class MapMonsterParser
    {
        private readonly ILogger _logger;
        private readonly IGenericDao<MapMonsterDto> _mapMonsterDao;
        private readonly IGenericDao<NpcMonsterDto> _npcMonsterDao;

        public MapMonsterParser(IGenericDao<MapMonsterDto> mapMonsterDao, IGenericDao<NpcMonsterDto> npcMonsterDao,
            ILogger logger)
        {
            _mapMonsterDao = mapMonsterDao;
            _logger = logger;
            _npcMonsterDao = npcMonsterDao;
        }

        public void InsertMapMonster(List<string[]> packetList)
        {
            short map = 0;
            var mobMvPacketsList = packetList.Where(o => o[0].Equals("mv") && o[1].Equals("3"))
                .Select(currentPacket=>Convert.ToInt32(currentPacket[2])).Distinct().ToList();
            var monsters = new List<MapMonsterDto>();
            var mapMonsterdb = _mapMonsterDao.LoadAll().ToList();
            var npcMonsterdb = _npcMonsterDao.LoadAll().ToList();

            foreach (var currentPacket in packetList.Where(o => o[0].Equals("in") || o[0].Equals("at")))
            {
                if ((currentPacket.Length > 5) && (currentPacket[0] == "at"))
                {
                    map = short.Parse(currentPacket[2]);
                    continue;
                }

                if ((currentPacket.Length <= 7) || (currentPacket[0] != "in") || (currentPacket[1] != "3"))
                {
                    continue;
                }

                var monster = new MapMonsterDto
                {
                    MapId = map,
                    VNum = short.Parse(currentPacket[2]),
                    MapMonsterId = int.Parse(currentPacket[3]),
                    MapX = short.Parse(currentPacket[4]),
                    MapY = short.Parse(currentPacket[5]),
                    Direction = (byte) (currentPacket[6] == string.Empty ? 0 : byte.Parse(currentPacket[6])),
                    IsDisabled = false,
                    IsMoving = mobMvPacketsList.Contains(int.Parse(currentPacket[3]))
                };

                if ((npcMonsterdb.FirstOrDefault(s => s.NpcMonsterVNum.Equals(monster.VNum)) == null)
                    || (mapMonsterdb.FirstOrDefault(s => s.MapMonsterId.Equals(monster.MapMonsterId)) != null)
                    || (monsters.Count(i => i.MapMonsterId == monster.MapMonsterId) != 0))
                {
                    continue;
                }

                monsters.Add(monster);
            }

            _mapMonsterDao.InsertOrUpdate(monsters);
            _logger.Information(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.MONSTERS_PARSED),
                monsters.Count);
        }
    }
}