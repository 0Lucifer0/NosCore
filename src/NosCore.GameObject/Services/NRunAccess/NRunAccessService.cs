﻿//  __  _  __    __   ___ __  ___ ___  
// |  \| |/__\ /' _/ / _//__\| _ \ __| 
// | | ' | \/ |`._`.| \_| \/ | v / _|  
// |_|\__|\__/ |___/ \__/\__/|_|_\___| 
// 
// Copyright (C) 2018 - NosCore
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
using System.Reactive.Subjects;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ClientPackets;

namespace NosCore.GameObject.Services.NRunAccess
{
    public class NrunAccessService
    {
        private List<IHandler<Tuple<MapNpc, NrunPacket>, Tuple<MapNpc, NrunPacket>>> _handlers { get; }

        public NrunAccessService(IEnumerable<IHandler<Tuple<MapNpc, NrunPacket>, Tuple<MapNpc, NrunPacket>>> handlers)
        {
            _handlers = handlers.ToList();
        }

        public void NRunLaunch(ClientSession clientSession, Tuple<MapNpc, NrunPacket> data)
        {
            var handlersRequest = new Subject<RequestData<Tuple<MapNpc, NrunPacket>>>();
            _handlers.ForEach(handler =>
            {
                if (handler.Condition(data))
                {
                    handlersRequest.Subscribe(handler.Execute);
                }
            });
            handlersRequest.OnNext(new RequestData<Tuple<MapNpc, NrunPacket>>(clientSession, data));
        }
    }
}