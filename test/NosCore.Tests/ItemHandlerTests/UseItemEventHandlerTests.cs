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
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Providers.InventoryService;
using NosCore.GameObject.Providers.ItemProvider.Handlers;
using System;
using NosCore.GameObject;
using NosCore.GameObject.Providers.ItemProvider.Item;

namespace NosCore.Tests.ItemHandlerTests
{
    public abstract class UseItemEventHandlerTests
    {
        protected IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>> _handler;
        protected ClientSession _session;
        protected readonly UseItemPacket _useItem = new UseItemPacket();

        protected void ExecuteInventoryItemInstanceEventHandler(InventoryItemInstance item)
        {
            _handler.Execute(
                new RequestData<Tuple<InventoryItemInstance, UseItemPacket>>(
                    _session,
                    new Tuple<InventoryItemInstance, UseItemPacket>(
                        item, _useItem
                    )));
        }
    }
}