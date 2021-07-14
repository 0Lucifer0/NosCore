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

using Microsoft.Extensions.Options;
using NosCore.Core.Configuration;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace NosCore.GameObject.Services.InventoryService
{
    public class InventoryService : ConcurrentDictionary<Guid, InventoryItemInstance>, IInventoryService
    {
        private readonly IOptions<WorldConfiguration> _configuration;
        private readonly List<ItemDto> _items;
        private readonly ILogger _logger;

        public InventoryService(List<ItemDto> items, IOptions<WorldConfiguration> configuration, ILogger logger)
        {
            _items = items;
            _configuration = configuration;
            _logger = logger;
        }

        private byte GetMaxSlot(NoscorePocketType pocket)
        {
            //TODO make this configurable
            return (byte)(pocket switch
            {
                NoscorePocketType.Miniland => 50 + Expensions[pocket],
                NoscorePocketType.Specialist => 45 + Expensions[pocket],
                NoscorePocketType.Costume => 60 + Expensions[pocket],
                NoscorePocketType.Wear => 17,
                _ => _configuration.Value.BackpackSize + Expensions[pocket]
            });
        }

        public Dictionary<NoscorePocketType, byte> Expensions { get; set; } = new Dictionary<NoscorePocketType, byte>
        {
            { NoscorePocketType.Costume, 0 },
            { NoscorePocketType.Equipment, 0 },
            { NoscorePocketType.Etc, 0 },
            { NoscorePocketType.Miniland, 0 },
            { NoscorePocketType.Main, 0 },
            { NoscorePocketType.Specialist, 0 },
            { NoscorePocketType.Wear, 0 },
        };

        public InventoryItemInstance? LoadBySlotAndType(short slot, NoscorePocketType type)
        {
            InventoryItemInstance? retItem = default;
            try
            {
                retItem = this.Select(s => s.Value)
                    .SingleOrDefault(i => (i.Slot == slot) && (i.Type == type));
            }
            catch (InvalidOperationException ioEx)
            {
                _logger.Error(ioEx.Message, ioEx);
            }

            return retItem;
        }

        public List<InventoryItemInstance?> LoadByVNumAndAmount(short vnum, short amount)
        {
            var result = new List<InventoryItemInstance?>();
            if (CountItem(vnum) < amount)
            {
                return result;
            }
            foreach (var item in this.Select(s => s.Value).OrderByDescending(o => o.Slot).Where(w => w.ItemInstance!.ItemVNum == vnum).ToList())
            {
                result.Add(item);
                amount -= item.ItemInstance!.Amount;
                if (amount <= 0)
                {
                    break;
                }
            }
            return result;
        }

        public bool CanAddItem(short itemVnum)
        {
            var type = _items.Find(item => item.VNum == itemVnum)?.Type;
            return type != null && GetFreeSlot((NoscorePocketType)type).HasValue;
        }

        public int CountItem(int itemVNum)
        {
            return this.Select(s => s.Value).Where(s => s.ItemInstance?.ItemVNum == itemVNum)
                .Sum(i => i.ItemInstance?.Amount ?? 0);
        }

        public int CountItemInAnPocket(NoscorePocketType inv)
        {
            return this.Count(s => s.Value.Type == inv);
        }

        public List<InventoryItemInstance>? AddItemToPocket(InventoryItemInstance newItem)
        {
            return AddItemToPocket(newItem, null, null);
        }

        public List<InventoryItemInstance>? AddItemToPocket(InventoryItemInstance newItem, NoscorePocketType? type)
        {
            return AddItemToPocket(newItem, type, null);
        }

        public List<InventoryItemInstance>? AddItemToPocket(InventoryItemInstance newItem, NoscorePocketType? type,
            short? slot)
        {
            var invlist = new List<InventoryItemInstance>();
            // override type if necessary
            if (type.HasValue)
            {
                newItem.Type = type.Value;
            }

            // check if item can be stapled
            if ((slot == null)
                && ((newItem.Type == NoscorePocketType.Etc) || (newItem.Type == NoscorePocketType.Main)))
            {
                var slotNotFull = this.ToList().Select(s => s.Value).Where(i =>
                    i.ItemInstance!.ItemVNum.Equals(newItem.ItemInstance!.ItemVNum) &&
                    (i.ItemInstance.Amount < _configuration.Value.MaxItemAmount));
                var freeslot = GetMaxSlot(newItem.Type) - this.Count(s => s.Value.Type == newItem.Type);
                IEnumerable<InventoryItemInstance> itemInstances =
                    slotNotFull as IList<InventoryItemInstance> ?? slotNotFull.ToList();
                if (newItem.ItemInstance!.Amount <= freeslot * _configuration.Value.MaxItemAmount
                    + itemInstances.Sum(s => _configuration.Value.MaxItemAmount - s.ItemInstance!.Amount))
                {
                    foreach (var slotToAdd in itemInstances)
                    {
                        var max = slotToAdd.ItemInstance?.Amount + newItem.ItemInstance?.Amount;
                        max = max > _configuration.Value.MaxItemAmount ? _configuration.Value.MaxItemAmount : max;
                        newItem.ItemInstance!.Amount =
                            (short)((slotToAdd.ItemInstance?.Amount + newItem.ItemInstance?.Amount - max) ?? 0);
                        slotToAdd.ItemInstance!.Amount = (short)(max ?? 0);
                        invlist.Add(slotToAdd);
                    }
                }
            }

            if (newItem.ItemInstance == null || newItem.ItemInstance?.Amount <= 0)
            {
                return invlist;
            }

            // create new item
            var freeSlot = newItem.Type == NoscorePocketType.Wear
                ? LoadBySlotAndType((short)newItem.ItemInstance!.Item!.EquipmentSlot, NoscorePocketType.Wear) == null
                    ? (short?)newItem.ItemInstance.Item.EquipmentSlot
                    : null
                : GetFreeSlot(newItem.Type);
            if (!slot.HasValue && !freeSlot.HasValue)
            {
                return invlist;
            }

            newItem.Slot = slot ?? freeSlot ?? 0;

            if (ContainsKey(newItem.ItemInstanceId))
            {
                var e = new InvalidOperationException("Cannot add the same ItemInstance twice to pocket.");
                _logger.Error(e.Message, e);
                return null;
            }

            if (this.Any(s => (s.Value.Slot == newItem.Slot) && (s.Value.Type == newItem.Type))
                || (newItem.Slot >= GetMaxSlot(newItem.Type)))
            {
                return null;
            }

            if ((newItem.Type == NoscorePocketType.Specialist) && !(newItem.ItemInstance is SpecialistInstance))
            {
                var e = new InvalidOperationException(
                    "Cannot add an item of type Specialist without beeing a SpecialistInstance.");
                _logger.Error(e.Message, e);
                return null;
            }

            if (((newItem.Type == NoscorePocketType.Equipment) || (newItem.Type == NoscorePocketType.Wear))
                && !(newItem.ItemInstance is WearableInstance) && !(newItem.ItemInstance is SpecialistInstance))
            {
                var e = new InvalidOperationException(
                    "Cannot add an item of type Equipment or Wear without beeing a WearableInstance or a SpecialistInstance.");
                _logger.Error(e.Message, e);
                return null;
            }

            this[newItem.ItemInstanceId] = newItem;
            invlist.Add(newItem);

            return invlist;
        }

        public InventoryItemInstance? DeleteById(Guid id)
        {
            var inv = this[id];
            if (inv != null)
            {
                if (TryRemove(inv.ItemInstanceId, out var value))
                {
                    return null;
                }

                return value;
            }

            var e = new InvalidOperationException("Expected item wasn't deleted, Type or Slot did not match!");
            _logger.Error(e.Message, e);
            return null;
        }

        public InventoryItemInstance LoadByItemInstanceId(Guid id)
        {
            return this[id];
        }

        public InventoryItemInstance? DeleteFromTypeAndSlot(NoscorePocketType type, short slot)
        {
            var inv = this.Select(s => s.Value).FirstOrDefault(i => i.Slot.Equals(slot) && i.Type.Equals(type));

            if (inv != null)
            {
                if (TryRemove(inv.ItemInstanceId, out var value))
                {
                    return null;
                }

                return value;
            }

            var e = new InvalidOperationException("Expected item wasn't deleted, Type or Slot did not match!");
            _logger.Error(e.Message, e);
            return null;
        }

        public InventoryItemInstance? MoveInPocket(short sourceSlot, NoscorePocketType sourceType,
            NoscorePocketType targetType)
        {
            return MoveInPocket(sourceSlot, sourceType, targetType, null, false);
        }

        public InventoryItemInstance? MoveInPocket(short sourceSlot, NoscorePocketType sourceType,
            NoscorePocketType targetType,
            short? targetSlot, bool swap)
        {
            if ((sourceSlot == targetSlot) && (sourceType == targetType))
            {
                var e = new InvalidOperationException("SourceInstance can't be moved on the same spot");
                _logger.Error(e.Message, e);
                return null;
            }

            var sourceInstance = LoadBySlotAndType(sourceSlot, sourceType);
            if (!(sourceInstance!.ItemInstance is WearableInstance || sourceInstance.ItemInstance is SpecialistInstance))
            {
                var e = new InvalidOperationException("SourceInstance can't be moved between pockets");
                _logger.Error(e.Message, e);
                return null;
            }

            if (sourceInstance.ItemInstance is WearableInstance && (targetType != NoscorePocketType.Equipment) &&
                (targetType != NoscorePocketType.Costume) && (targetType != NoscorePocketType.Wear))
            {
                var e = new InvalidOperationException("WearableInstance can't be move to this inventory");
                _logger.Error(e.Message, e);
                return null;
            }

            if (sourceInstance.ItemInstance is SpecialistInstance && (targetType != NoscorePocketType.Equipment) &&
                (targetType != NoscorePocketType.Specialist) && (targetType != NoscorePocketType.Wear))
            {
                var e = new InvalidOperationException("SpecialistInstance can't be move to this inventory");
                _logger.Error(e.Message, e);
                return null;
            }

            if (targetSlot.HasValue)
            {
                var targetInstance = LoadBySlotAndType(targetSlot.Value, targetType);

                if (swap && (targetInstance != null))
                {
                    // swap

                    sourceInstance.Slot = targetSlot.Value;
                    sourceInstance.Type = targetType;

                    targetInstance.Slot = sourceSlot;
                    targetInstance.Type = sourceType;
                }
                else if (targetInstance == null)
                {
                    sourceInstance.Slot = targetSlot.Value;
                    sourceInstance.Type = targetType;
                }
                else
                {
                    var e = new InvalidOperationException("Source can not be swapped");
                    _logger.Error(e.Message, e);
                    return null;
                }

                return sourceInstance;
            }

            // check for free target slot
            short? nextFreeSlot;
            if (targetType == NoscorePocketType.Wear)
            {
                nextFreeSlot =
                    LoadBySlotAndType((short)sourceInstance.ItemInstance.Item!.EquipmentSlot, targetType) == null
                        ? (short)sourceInstance.ItemInstance.Item.EquipmentSlot
                        : (short)-1;
            }
            else
            {
                nextFreeSlot = GetFreeSlot(targetType);
            }

            if (nextFreeSlot.HasValue)
            {
                sourceInstance.Type = targetType;
                sourceInstance.Slot = nextFreeSlot.Value;
            }
            else
            {
                return null;
            }

            return sourceInstance;
        }

        public bool TryMoveItem(NoscorePocketType sourcetype, short sourceSlot, short amount, short destinationSlot,
            out InventoryItemInstance? sourcePocket, out InventoryItemInstance? destinationPocket)
        {
            // load source and destination slots
            sourcePocket = LoadBySlotAndType(sourceSlot, sourcetype);
            destinationPocket = LoadBySlotAndType(destinationSlot, sourcetype);

            if ((sourceSlot == destinationSlot) || (amount == 0)
                || (destinationSlot > GetMaxSlot(sourcetype)))
            {
                return false;
            }

            // check if the item is a palced MinilandObject
            if (sourcetype == NoscorePocketType.Miniland)
            {
                //todo test this (looks weird)
                if (sourcePocket?.ItemInstance is MinilandObjectDto || destinationPocket?.ItemInstance is MinilandObjectDto)
                {
                    return false;
                }
            }

            if ((sourcePocket != null) && (amount <= sourcePocket.ItemInstance?.Amount))
            {
                switch (destinationPocket)
                {
                    case null when sourcePocket.ItemInstance.Amount == amount:
                        sourcePocket.Slot = destinationSlot;
                        break;

                    case null:
                        var itemDest = (IItemInstance)sourcePocket.ItemInstance.Clone();
                        sourcePocket.ItemInstance.Amount -= amount;
                        itemDest.Amount = amount;
                        itemDest.Id = Guid.NewGuid();
                        AddItemToPocket(new InventoryItemInstance
                        {
                            Id = Guid.NewGuid(),
                            CharacterId = sourcePocket.CharacterId,
                            Slot = sourcePocket.Slot,
                            Type = sourcePocket.Type,
                            ItemInstance = itemDest,
                            ItemInstanceId = itemDest.Id
                        }, sourcetype, destinationSlot);
                        break;

                    default:
                        if ((destinationPocket.ItemInstance?.ItemVNum == sourcePocket.ItemInstance.ItemVNum)
                            && ((sourcePocket.ItemInstance.Item!.Type == NoscorePocketType.Main) ||
                                (sourcePocket.ItemInstance.Item.Type == NoscorePocketType.Etc)))
                        {
                            if (destinationPocket.ItemInstance.Amount + amount > _configuration.Value.MaxItemAmount)
                            {
                                var saveItemCount = destinationPocket.ItemInstance.Amount;
                                destinationPocket.ItemInstance.Amount = _configuration.Value.MaxItemAmount;
                                sourcePocket.ItemInstance.Amount =
                                    (short)(saveItemCount + sourcePocket.ItemInstance.Amount -
                                        _configuration.Value.MaxItemAmount);
                            }
                            else
                            {
                                destinationPocket.ItemInstance.Amount += amount;
                                sourcePocket.ItemInstance.Amount -= amount;

                                // item with amount of 0 should be removed
                                if (sourcePocket.ItemInstance.Amount == 0)
                                {
                                    DeleteFromTypeAndSlot(sourcePocket.Type, sourcePocket.Slot);
                                }
                            }
                        }
                        else
                        {
                            // add and remove save pocket
                            destinationPocket = TakeItem(destinationPocket.Slot, destinationPocket.Type);
                            if (destinationPocket == null)
                            {
                                return true;
                            }

                            destinationPocket.Slot = sourceSlot;
                            destinationPocket.Type = sourcetype;
                            sourcePocket = TakeItem(sourcePocket.Slot, sourcePocket.Type);
                            if (sourcePocket == null)
                            {
                                return true;
                            }

                            sourcePocket.Slot = destinationSlot;
                            this[destinationPocket.ItemInstanceId] = destinationPocket;
                            this[sourcePocket.ItemInstanceId] = sourcePocket;
                        }

                        break;
                }
            }

            sourcePocket = LoadBySlotAndType(sourceSlot, sourcetype);
            destinationPocket = LoadBySlotAndType(destinationSlot, sourcetype);
            return true;
        }

        public bool EnoughPlace(List<IItemInstance> itemInstances, NoscorePocketType type)
        {
            var place = new Dictionary<NoscorePocketType, int>();
            foreach (var itemGroup in itemInstances.GroupBy(s => s.ItemVNum))
            {
                var itemList = this.Select(s => s.Value).Where(i => i.Type == type).ToList();
                if (!place.ContainsKey(type))
                {
                    place.Add(type, GetMaxSlot(type) - itemList.Count);
                }

                var amount = itemGroup.Sum(s => s.Amount);
                var rest = amount % (type == NoscorePocketType.Equipment ? 1 : _configuration.Value.MaxItemAmount);
                var newSlotNeeded = itemList.Where(s => s.ItemInstance?.ItemVNum == itemGroup.Key)
                    .Sum(s => _configuration.Value.MaxItemAmount - s.ItemInstance?.Amount) <= rest;
                place[type] -= amount / (type == NoscorePocketType.Equipment ? 1 : _configuration.Value.MaxItemAmount) +
                    (newSlotNeeded ? 1 : 0);

                if (place[type] < 0)
                {
                    return false;
                }
            }

            return true;
        }

        public InventoryItemInstance? RemoveItemAmountFromInventory(short amount, Guid id)
        {
            var inv = this[id];
            if (inv?.ItemInstance != null)
            {
                inv.ItemInstance.Amount -= amount;
                if (inv.ItemInstance.Amount <= 0)
                {
                    return TryRemove(inv.ItemInstanceId, out _) ? null : inv;
                }

                return inv;
            }

            var e = new InvalidOperationException("Expected item wasn't deleted, Type or Slot did not match!");
            _logger.Error(e.Message, e);
            return null;
        }

        public List<InventoryItemInstance?> RemoveItemAmountFromInventory(short amount, short vnum)
        {
            var result = new List<InventoryItemInstance?>();
            if (CountItem(vnum) < amount)
            {
                return result;
            }
            while (amount != 0)
            {
                var item = this.OrderByDescending(o => o.Value.Slot).Where(w => w.Value.ItemInstance!.ItemVNum == vnum).FirstOrDefault();
                var removedItem = RemoveItemAmountFromInventory(amount, item.Key);
                amount -= removedItem != null ? amount : (short)(item.Value.ItemInstance!.Amount + amount);
                result.Add(removedItem);
            }
            return result;
        }

        private short? GetFreeSlot(NoscorePocketType type)
        {
            var itemInstanceSlotsByType = this.Select(s => s.Value).Where(i => i.Type == type).OrderBy(i => i.Slot)
                .Select(i => (int)i.Slot);
            IEnumerable<int> instanceSlotsByType =
                itemInstanceSlotsByType as int[] ?? itemInstanceSlotsByType.ToArray();
            var nextFreeSlot = instanceSlotsByType.Any()
                ? Enumerable
                    .Range(0, GetMaxSlot(type) + 1)
                    .Except(instanceSlotsByType).FirstOrDefault()
                : 0;
            return (short?)nextFreeSlot < GetMaxSlot(type) ? (short?)nextFreeSlot : null;
        }

        private InventoryItemInstance? TakeItem(short slot, NoscorePocketType type)
        {
            var itemInstance = this.Select(s => s.Value).SingleOrDefault(i => (i.Slot == slot) && (i.Type == type));
            if (itemInstance == null)
            {
                return null;
            }

            TryRemove(itemInstance.ItemInstanceId, out _);
            return itemInstance;
        }
    }
}