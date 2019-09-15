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

using ChickenAPI.Packets.ClientPackets.Inventory;
using Mapster;
using NosCore.Data;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Providers.InventoryService;
using NosCore.GameObject.Providers.ItemProvider.Item;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;

namespace NosCore.GameObject.Providers.ItemProvider
{
    public class ItemProvider : IItemProvider
    {
        private readonly List<IEventHandler<Item.Item, Tuple<InventoryItemInstance, UseItemPacket>>> _handlers;
        private readonly List<ItemDto> _items;

        public ItemProvider(List<ItemDto> items,
            IEnumerable<IEventHandler<Item.Item, Tuple<InventoryItemInstance, UseItemPacket>>> handlers)
        {
            _items = items;
            _handlers = handlers.ToList();
        }

        public IItemInstance Convert(IItemInstanceDto k)
        {
            IItemInstance item;

            if (k is BoxInstanceDto)
            {
                item = k.Adapt<BoxInstance>();
            }
            else if (k is SpecialistInstanceDto)
            {
                item = k.Adapt<SpecialistInstance>();
            }
            else if (k is WearableInstanceDto)
            {
                item = k.Adapt<WearableInstance>();
            }
            else if (k is UsableInstanceDto)
            {
                item = k.Adapt<UsableInstance>();
            }
            else
            {
                item = k.Adapt<ItemInstance>();
            }

            item.Item = _items.Find(s => s.VNum == k.ItemVNum).Adapt<Item.Item>();
            LoadHandlers(item);
            return item;
        }

        public IItemInstance Create(short itemToCreateVNum) =>
            Create(itemToCreateVNum, 1);

        public IItemInstance Create(short itemToCreateVNum, short amount) =>
            Create(itemToCreateVNum, amount, 0);

        public IItemInstance Create(short itemToCreateVNum, short amount, sbyte rare) =>
            Create(itemToCreateVNum, amount, rare, 0);

        public IItemInstance Create(short itemToCreateVNum, short amount, sbyte rare, byte upgrade) => Create(itemToCreateVNum, amount, rare, upgrade, 0);

        public IItemInstance Create(short itemToCreateVNum, short amount, sbyte rare, byte upgrade,
            byte design)
        {
            var item = Generate(itemToCreateVNum, amount, rare, upgrade, design);
            LoadHandlers(item);
            return item;
        }

        private void LoadHandlers(IItemInstance itemInstance)
        {
            var handlersRequest = new Subject<RequestData<Tuple<InventoryItemInstance, UseItemPacket>>>();
            _handlers.ForEach(handler =>
            {
                if (handler.Condition(itemInstance.Item))
                {
                    handlersRequest.Subscribe(handler.Execute);
                }
            });
            itemInstance.Requests = handlersRequest;
        }

        public IItemInstance Generate(short itemToCreateVNum, short amount, sbyte rare, byte upgrade,
            byte design)
        {
            Item.Item itemToCreate = _items.Find(s => s.VNum == itemToCreateVNum).Adapt<Item.Item>();
            switch (itemToCreate.Type)
            {
                case NoscorePocketType.Miniland:
                    return new ItemInstance(itemToCreate)
                    {
                        Amount = amount,
                        DurabilityPoint = itemToCreate.MinilandObjectPoint / 2
                    };

                case NoscorePocketType.Equipment:
                    switch (itemToCreate.ItemType)
                    {
                        case ItemType.Specialist:
                            return new SpecialistInstance(itemToCreate)
                            {
                                SpLevel = 1,
                                Amount = amount,
                                Design = design,
                                Upgrade = upgrade
                            };
                        case ItemType.Box:
                            return new BoxInstance(itemToCreate)
                            {
                                Amount = amount,
                                Upgrade = upgrade,
                                Design = design
                            };
                        default:
                            var wear = new WearableInstance(itemToCreate)
                            {
                                Amount = amount,
                                Rare = rare,
                                Upgrade = upgrade,
                                Design = design
                            };
                            if (wear.Rare > 0)
                            {
                                wear.SetRarityPoint();
                            }

                            return wear;
                    }

                default:
                    return new ItemInstance(itemToCreate)
                    {
                        Amount = amount,
                    };
            }
        }
    }
}