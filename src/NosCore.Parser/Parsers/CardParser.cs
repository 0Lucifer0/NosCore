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
using NosCore.Core;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.StaticEntities;
using NosCore.Parser.Parsers.Generic;
using Serilog;

namespace NosCore.Parser.Parsers

{
    public class CardParser
    {
        //  VNUM	CardId
        //  NAME    Name
        //
        //  GROUP	Level	0
        //  STYLE	0	0	BuffType	0	0
        //  EFFECT	0	0
        //  TIME	Duration	Delay
        //  1ST	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0
        //  2ST	0	0	0	0	0	0	0	0	0	0	0	0
        //  LAST	0	0
        //  DESC Description
        //
        //  END
        //#========================================================
        private const string FileCardDat = "\\Card.dat";


        private readonly IGenericDao<BCardDto> _bcardDao;
        private readonly IGenericDao<CardDto> _cardDao;
        private readonly ILogger _logger;

        public CardParser(IGenericDao<CardDto> cardDao, IGenericDao<BCardDto> bcardDao, ILogger logger)
        {
            _cardDao = cardDao;
            _bcardDao = bcardDao;
            _logger = logger;
        }

        public void InsertCards(string folder)
        {
            var actionList = new Dictionary<string, Func<Dictionary<string, string[]>, object>>
            {
                {"CardId", chunk => Convert.ToInt16(chunk["VNUM"][2])},
                {"Name", chunk => chunk["Name"][2]},
                {"Level", chunk => Convert.ToByte(chunk["GROUP"][3])},
                {"EffectId", chunk => Convert.ToInt32(chunk["EFFECT"][2])},
                {"BuffType", chunk => (BCardType.CardType) Convert.ToByte(chunk["STYLE"][3])},
                {"Duration", chunk => Convert.ToInt32(chunk["TIME"][2])},
                {"Delay", chunk => Convert.ToInt32(chunk["TIME"][3])},
                {"BCards", AddBCards},
                {"TimeoutBuff", chunk => Convert.ToInt16(chunk["LAST"][2])},
                {"TimeoutBuffChance", chunk => Convert.ToByte(chunk["LAST"][3])}
            };
            var genericParser = new GenericParser<CardDto>(folder + FileCardDat,
                "#========================================================", 1, actionList, _logger);
            var cards = genericParser.GetDtos();
            _cardDao.InsertOrUpdate(cards);

            _logger.Information(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.CARDS_PARSED), cards.Count);
        }

        public List<BCardDto> AddBCards(Dictionary<string, string[]> chunks)
        {
            var list = new List<BCardDto>();
            for (var j = 0; j < 5; j++)
            {
                var key = (j > 2) ? "2ST" : "1ST";
                var i = (j > 2) ? j - 3 : j;

                if ((chunks[key][2 + i * 6] == "-1") || (chunks[key][2 + i * 6] == "0"))
                {
                    continue;
                }

                var first = int.Parse(chunks[key][i * 6 + 6]);
                list.Add(new BCardDto
                {
                    CardId = Convert.ToInt16(chunks["VNUM"][2]),
                    Type = byte.Parse(chunks[key][2 + i * 6]),
                    SubType = (byte)((Convert.ToByte(chunks[key][3 + i * 6]) + 1) * 10 + 1 + (first < 0 ? 1 : 0)),
                    FirstData = (first > 0 ? first : -first) / 4,
                    SecondData = int.Parse(chunks[key][7 + i * 6]) / 4,
                    ThirdData = int.Parse(chunks[key][5 + i * 6]),
                    IsLevelScaled = Convert.ToBoolean(first % 4),
                    IsLevelDivided = Math.Abs(first % 4) == 2
                });
            }

            return list;
        }
    }
}