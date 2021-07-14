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
using NosCore.GameObject;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ClientPackets.Player;
using Serilog;
using System.Threading.Tasks;
using NosCore.Packets.Enumerations;
using NosCore.GameObject.Services.UpgradeService;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;

namespace NosCore.PacketHandlers.Upgrades
{
    public class UpgradePacketHandler : PacketHandler<UpgradePacket>, IWorldPacketHandler
    {
        private readonly ILogger _logger;
        private readonly ISumUpgradeService _sumUpgradeService;

        public UpgradePacketHandler(ILogger logger, ISumUpgradeService sumUpgradeService)
        {
            _logger = logger;
            _sumUpgradeService = sumUpgradeService;
        }

        public override async Task ExecuteAsync(UpgradePacket upgradePacket, ClientSession session)
        {
            switch (upgradePacket.UpgradeType)
            {
                case UpgradePacketType.SumResistance:
                    if (upgradePacket.Slot2 == null)
                    {
                        _logger.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.UPGRADE_PACKET_SLOT2_NULL));
                        return;
                    }

                    var sourceSlot = session.Character.InventoryService.LoadBySlotAndType(upgradePacket.Slot, (NoscorePocketType)upgradePacket.InventoryType);
                    var targetSlot = session.Character.InventoryService.LoadBySlotAndType((byte)upgradePacket.Slot2, (NoscorePocketType)upgradePacket.InventoryType);

                    await session.SendPacketsAsync(await _sumUpgradeService.SumItemInstanceAsync(session, sourceSlot, targetSlot));
                    break;
            }
        }
    }
}