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
using System.Net.Http;
using Mapster;
using Newtonsoft.Json;
using NosCore.Core;
using NosCore.Core.HttpClients;
using NosCore.Core.HttpClients.ChannelHttpClient;
using NosCore.Data;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Miniland;
using NosCore.Data.WebApi;
using NosCore.GameObject.Providers.ItemProvider;
using NosCore.GameObject.Providers.ItemProvider.Item;

namespace NosCore.GameObject.HttpClients.WarehouseHttpClient
{
    public class WarehouseHttpClient : MasterServerHttpClient, IWarehouseHttpClient
    {
        private readonly IGenericDao<IItemInstanceDto> _itemInstanceDao;
        private readonly IItemProvider _itemProvider;

        public WarehouseHttpClient(IHttpClientFactory httpClientFactory, Channel channel,
            IChannelHttpClient channelHttpClient, IItemProvider itemProvider,
            IGenericDao<IItemInstanceDto> itemInstanceDao)
            : base(httpClientFactory, channel, channelHttpClient)
        {
            ApiUrl = "api/warehouse";
            RequireConnection = true;
            _itemProvider = itemProvider;
            _itemInstanceDao = itemInstanceDao;
        }

        public List<WarehouseItem> GetWarehouseItems(long characterId, WarehouseType warehouse)
        {
            var client = Connect();
            var response = client
                .GetAsync($"{ApiUrl}?id=null&ownerId={characterId}&warehouseType={warehouse}&slot=null")
                .Result;
            if (response.IsSuccessStatusCode)
            {
                var warehouseItems = new List<WarehouseItem>();
                var warehouselinks =
                    JsonConvert.DeserializeObject<List<WarehouseLink>>(response.Content.ReadAsStringAsync().Result);
                foreach (var warehouselink in warehouselinks)
                {
                    var warehouseItem = warehouselink.Warehouse.Adapt<WarehouseItem>();
                    var itemInstance = _itemInstanceDao.FirstOrDefault(s => s.Id == warehouselink.ItemInstance.Id);
                    warehouseItem.ItemInstance = _itemProvider.Convert(itemInstance);
                    warehouseItems.Add(warehouseItem);
                }
            }

            throw new ArgumentException();
        }

        public bool DepositItem(long characterId, WarehouseType warehouse, IItemInstance itemInstance, short slot)
        {
            return Post<bool>(new WareHouseDepositRequest
            {
                OwnerId = characterId,
                WarehouseType = warehouse,
                ItemInstance = itemInstance.Adapt<ItemInstanceDto>(),
                Slot = slot
            });
        }

        public void DeleteWarehouseItem(long characterId, WarehouseType warehouse, short slot)
        {
            throw new NotImplementedException();
        }

        public List<WarehouseItem> MoveWarehouseItem(long characterId, WarehouseType warehouse, short slot,
            short destinationSlot)
        {
            throw new NotImplementedException();
        }
    }
}