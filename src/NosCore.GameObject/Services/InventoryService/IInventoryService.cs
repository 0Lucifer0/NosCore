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

using NosCore.Data.Enumerations;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using System;
using System.Collections.Generic;

namespace NosCore.GameObject.Services.InventoryService
{
    public interface IInventoryService : IDictionary<Guid, InventoryItemInstance>
    {
        Dictionary<NoscorePocketType, byte> Expensions { get; set; }

        List<InventoryItemInstance>? AddItemToPocket(InventoryItemInstance newItem);

        List<InventoryItemInstance>? AddItemToPocket(InventoryItemInstance newItem, NoscorePocketType? type);

        List<InventoryItemInstance>?
            AddItemToPocket(InventoryItemInstance newItem, NoscorePocketType? type, short? slot);

        bool CanAddItem(short itemVnum);

        int CountItem(int itemVNum);

        int CountItemInAnPocket(NoscorePocketType inv);

        InventoryItemInstance? DeleteById(Guid id);

        InventoryItemInstance? DeleteFromTypeAndSlot(NoscorePocketType type, short slot);

        InventoryItemInstance? LoadByItemInstanceId(Guid id);

        InventoryItemInstance? LoadBySlotAndType(short slot, NoscorePocketType type);

        List<InventoryItemInstance?> LoadByVNumAndAmount(short vnum, short amount);

        InventoryItemInstance? MoveInPocket(short sourceSlot, NoscorePocketType sourceType,
            NoscorePocketType targetType);

        InventoryItemInstance? MoveInPocket(short sourceSlot, NoscorePocketType sourceType, NoscorePocketType targetType,
            short? targetSlot, bool swap);

        bool TryMoveItem(NoscorePocketType sourcetype, short sourceSlot, short amount, short destinationSlot,
            out InventoryItemInstance? sourcePocket, out InventoryItemInstance? destinationPocket);

        bool EnoughPlace(List<IItemInstance> itemInstances, NoscorePocketType type);

        InventoryItemInstance? RemoveItemAmountFromInventory(short amount, Guid id);

        List<InventoryItemInstance?> RemoveItemAmountFromInventory(short amount, short vnum);
    }
}