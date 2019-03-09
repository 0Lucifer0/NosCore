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
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NosCore.Configuration;
using NosCore.Database;
using NosCore.DAL;
using NosCore.Shared.I18N;
using Serilog;
using System.ComponentModel.DataAnnotations;

// ReSharper disable LocalizableElement

namespace NosCore.Parser
{
    public static class Parser
    {
        private const string ConfigurationPath = "../../../configuration";
        private const string Title = "NosCore - Parser";
        private const string ConsoleText = "PARSER - NosCoreIO";
        private static readonly ParserConfiguration ParserConfiguration = new ParserConfiguration();
        private static readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();

        private static void InitializeConfiguration()
        {
            var builder = new ConfigurationBuilder();
            builder.SetBasePath(Directory.GetCurrentDirectory() + ConfigurationPath);
            builder.AddJsonFile("parser.json", false);
            builder.Build().Bind(ParserConfiguration);
            Validator.ValidateObject(ParserConfiguration, new ValidationContext(ParserConfiguration),
                validateAllProperties: true);
            LogLanguage.Language = ParserConfiguration.Language;
            _logger.Information(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.SUCCESSFULLY_LOADED));
        }

        public static void Main(string[] args)
        {
            Console.Title = Title;
            Logger.PrintHeader(ConsoleText);
            InitializeConfiguration();
            try
            {
                var optionsBuilder = new DbContextOptionsBuilder<NosCoreContext>();
                optionsBuilder.UseNpgsql(ParserConfiguration.Database.ConnectionString);
                DataAccessHelper.Instance.Initialize(optionsBuilder.Options);

                try
                {
                    _logger.Warning(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.ENTER_PATH));
                    var folder = string.Empty;
                    var key = default(ConsoleKeyInfo);
                    if (args.Length == 0)
                    {
                        folder = Console.ReadLine();
                        _logger.Information(
                            $"{LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.PARSE_ALL)} [Y/n]");
                        key = Console.ReadKey(true);
                    }
                    else
                    {
                        folder = args.Aggregate(folder, (current, str) => current + str + " ");
                    }

                    var factory = new ImportFactory(folder);
                    factory.ImportPackets();

                    if (key.KeyChar != 'n')
                    {
                        factory.ImportMaps();
                        factory.ImportRespawnMapType();
                        factory.ImportMapType();
                        factory.ImportMapTypeMap();
                        factory.ImportAccounts();
                        factory.ImportPortals();
                        factory.ImportI18N();
                        //factory.ImportScriptedInstances();
                        factory.ImportItems();
                        factory.ImportSkills();
                        factory.ImportCards();
                        factory.ImportNpcMonsters();
                        factory.ImportDrops();
                        //factory.ImportNpcMonsterData();
                        factory.ImportMapNpcs();
                        factory.ImportMapMonsters();
                        factory.ImportShops();
                        //factory.ImportTeleporters();
                        factory.ImportShopItems();
                        //factory.ImportShopSkills();
                        //factory.ImportRecipe();
                        //factory.ImportQuests();
                    }
                    else
                    {
                        _logger.Information(
                            $"{LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.PARSE_MAPS)} [Y/n]");
                        key = Console.ReadKey(true);
                        if (key.KeyChar != 'n')
                        {
                            factory.ImportMaps();
                        }

                        _logger.Information(
                            $"{LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.PARSE_MAPTYPES)} [Y/n]");
                        key = Console.ReadKey(true);
                        if (key.KeyChar != 'n')
                        {
                            factory.ImportRespawnMapType();
                            factory.ImportMapType();
                            factory.ImportMapTypeMap();
                        }

                        _logger.Information(
                            $"{LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.PARSE_ACCOUNTS)} [Y/n]");
                        key = Console.ReadKey(true);
                        if (key.KeyChar != 'n')
                        {
                            factory.ImportAccounts();
                        }

                        _logger.Information(
                            $"{LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.PARSE_PORTALS)} [Y/n]");
                        key = Console.ReadKey(true);
                        if (key.KeyChar != 'n')
                        {
                            factory.ImportPortals();
                        }

                        _logger.Information(
                            $"{LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.PARSE_I18N)} [Y/n]");
                        key = Console.ReadKey(true);
                        if (key.KeyChar != 'n')
                        {
                            factory.ImportI18N();
                        }

                        _logger.Information(
                            $"{LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.PARSE_TIMESPACES)} [Y/n]");
                        key = Console.ReadKey(true);
                        if (key.KeyChar != 'n')
                        {
                            //factory.ImportScriptedInstances();
                        }

                        _logger.Information(
                            $"{LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.PARSE_ITEMS)} [Y/n]");
                        key = Console.ReadKey(true);
                        if (key.KeyChar != 'n')
                        {
                            factory.ImportItems();
                        }

                        _logger.Information(
                            $"{LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.PARSE_NPCMONSTERS)} [Y/n]");
                        key = Console.ReadKey(true);
                        if (key.KeyChar != 'n')
                        {
                            factory.ImportNpcMonsters();
                        }

                        _logger.Information(
                            $"{LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.PARSE_DROPS)} [Y/n]");
                        key = Console.ReadKey(true);
                        if (key.KeyChar != 'n')
                        {
                            factory.ImportDrops();
                        }

                        _logger.Information(
                            $"{LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.PARSE_NPCMONSTERDATA)} [Y/n]");
                        key = Console.ReadKey(true);
                        if (key.KeyChar != 'n')
                        {
                            //factory.ImportNpcMonsterData();
                        }

                        _logger.Information(
                            $"{LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.PARSE_CARDS)} [Y/n]");
                        key = Console.ReadKey(true);
                        if (key.KeyChar != 'n')
                        {
                            factory.ImportCards();
                        }

                        _logger.Information(
                            $"{LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.PARSE_SKILLS)} [Y/n]");
                        key = Console.ReadKey(true);
                        if (key.KeyChar != 'n')
                        {
                            factory.ImportSkills();
                        }

                        _logger.Information(
                            $"{LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.PARSE_MAPNPCS)} [Y/n]");
                        key = Console.ReadKey(true);
                        if (key.KeyChar != 'n')
                        {
                            factory.ImportMapNpcs();
                        }

                        _logger.Information(
                            $"{LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.PARSE_MONSTERS)} [Y/n]");
                        key = Console.ReadKey(true);
                        if (key.KeyChar != 'n')
                        {
                            factory.ImportMapMonsters();
                        }

                        _logger.Information(
                            $"{LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.PARSE_SHOPS)} [Y/n]");
                        key = Console.ReadKey(true);
                        if (key.KeyChar != 'n')
                        {
                            factory.ImportShops();
                        }

                        _logger.Information(
                            $"{LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.PARSE_TELEPORTERS)} [Y/n]");
                        key = Console.ReadKey(true);
                        if (key.KeyChar != 'n')
                        {
                            //factory.ImportTeleporters();
                        }

                        _logger.Information(
                            $"{LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.PARSE_SHOPITEMS)} [Y/n]");
                        key = Console.ReadKey(true);
                        if (key.KeyChar != 'n')
                        {
                            factory.ImportShopItems();
                        }

                        _logger.Information(
                            $"{LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.PARSE_SHOPSKILLS)} [Y/n]");
                        key = Console.ReadKey(true);
                        if (key.KeyChar != 'n')
                        {
                            //factory.ImportShopSkills();
                        }

                        _logger.Information(
                            $"{LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.PARSE_RECIPES)} [Y/n]");
                        key = Console.ReadKey(true);
                        if (key.KeyChar != 'n')
                        {
                            //factory.ImportRecipe();
                        }

                        _logger.Information(
                            $"{LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.PARSE_QUESTS)} [Y/n]");
                        key = Console.ReadKey(true);
                        if (key.KeyChar != 'n')
                        {
                            //factory.ImportQuests();
                        }
                    }

                    _logger.Information(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.DONE));
                    Thread.Sleep(5000);
                }
                catch (FileNotFoundException)
                {
                    _logger.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.AT_LEAST_ONE_FILE_MISSING));
                    Thread.Sleep(5000);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message, ex);
                Console.ReadKey();
            }
        }
    }
}