﻿////  __  _  __    __   ___ __  ___ ___
//// |  \| |/__\ /' _/ / _//__\| _ \ __|
//// | | ' | \/ |`._`.| \_| \/ | v / _|
//// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//// 
//// Copyright (C) 2019 - NosCore
//// 
//// NosCore is a free software: you can redistribute it and/or modify
//// it under the terms of the GNU General Public License as published by
//// the Free Software Foundation, either version 3 of the License, or any later version.
//// 
//// This program is distributed in the hope that it will be useful,
//// but WITHOUT ANY WARRANTY; without even the implied warranty of
//// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//// GNU General Public License for more details.
//// 
//// You should have received a copy of the GNU General Public License
//// along with this program.  If not, see <http://www.gnu.org/licenses/>.

//using System;
//using System.Collections.Generic;
//using System.Linq;
//using Microsoft.AspNetCore.Mvc;
//using NosCore.Core;
//using NosCore.Core.Networking;
//using NosCore.Shared.Enumerations;

//namespace NosCore.WebApi.Controller
//{
//    [Route("api/[controller]")]
//    [AuthorizeRole(AuthorityType.Root)]
//    public class ChannelController : Microsoft.AspNetCore.Mvc.Controller
//    {
//        // GET api/channel
//        [HttpGet]
//#pragma warning disable CA1822 // Mark members as static
//        public List<ChannelInfo> GetChannels(long? id)
//#pragma warning restore CA1822 // Mark members as static
//        {
//            throw new NotImplementedException();
//            //return id != null ? MasterClientListSingleton.Instance.Channels.Where(s => s.Id == id).ToList() : MasterClientListSingleton.Instance.Channels;
//        }
//    }
//}