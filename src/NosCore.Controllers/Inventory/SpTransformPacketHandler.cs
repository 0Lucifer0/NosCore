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
using ChickenAPI.Packets.ClientPackets.Specialists;
using ChickenAPI.Packets.Enumerations;
using ChickenAPI.Packets.ServerPackets.UI;
using NosCore.Core;
using NosCore.Core.I18N;
using NosCore.Data;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Networking.Group;
using NosCore.GameObject.Providers.ItemProvider.Item;

namespace NosCore.PacketHandlers.Inventory
{
    public class SpTransformPacketHandler : PacketHandler<SpTransformPacket>, IWorldPacketHandler
    {
        public override void Execute(SpTransformPacket spTransformPacket, ClientSession clientSession)
        {
            var specialistInstance =
                clientSession.Character.Inventory.LoadBySlotAndType((byte) EquipmentType.Sp,
                    NoscorePocketType.Wear)?.ItemInstance as SpecialistInstance;

            if (spTransformPacket.Type == SlPacketType.ChangePoints)
            {
                //TODO set points
            }
            else
            {
                if (clientSession.Character.IsSitting)
                {
                    return;
                }

                if (specialistInstance == null)
                {
                    clientSession.SendPacket(new MsgPacket
                    {
                        Message = Language.Instance.GetMessageFromKey(LanguageKey.NO_SP, clientSession.Account.Language)
                    });

                    return;
                }

                if (clientSession.Character.IsVehicled)
                {
                    clientSession.SendPacket(new MsgPacket
                    {
                        Message = Language.Instance.GetMessageFromKey(LanguageKey.REMOVE_VEHICLE,
                            clientSession.Account.Language)
                    });
                    return;
                }

                var currentRunningSeconds = (SystemTime.Now() - clientSession.Character.LastSp).TotalSeconds;

                if (clientSession.Character.UseSp)
                {
                    clientSession.Character.LastSp = SystemTime.Now();
                    clientSession.Character.RemoveSp();
                }
                else
                {
                    if ((clientSession.Character.SpPoint == 0) && (clientSession.Character.SpAdditionPoint == 0))
                    {
                        clientSession.SendPacket(new MsgPacket
                        {
                            Message = Language.Instance.GetMessageFromKey(LanguageKey.SP_NOPOINTS,
                                clientSession.Account.Language)
                        });
                        return;
                    }

                    if (currentRunningSeconds >= clientSession.Character.SpCooldown)
                    {
                        if (spTransformPacket.Type == SlPacketType.WearSpAndTransform)
                        {
                            clientSession.Character.ChangeSp();
                        }
                        else
                        {
                            clientSession.SendPacket(new DelayPacket
                            {
                                Type = 3,
                                Delay = 5000,
                                Packet = new SpTransformPacket {Type = SlPacketType.WearSp}
                            });
                            clientSession.Character.MapInstance.Sessions.SendPacket(new GuriPacket
                            {
                                Type = GuriPacketType.Unknow,
                                Value = 1,
                                EntityId = clientSession.Character.CharacterId
                            });
                        }
                    }
                    else
                    {
                        clientSession.SendPacket(new MsgPacket
                        {
                            Message = string.Format(Language.Instance.GetMessageFromKey(LanguageKey.SP_INLOADING,
                                    clientSession.Account.Language),
                                clientSession.Character.SpCooldown - (int) Math.Round(currentRunningSeconds))
                        });
                    }
                }
            }
        }
    }
}