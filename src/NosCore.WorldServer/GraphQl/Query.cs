﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NosCore.Core;
using NosCore.Core.Networking;
using NosCore.Data.Enumerations.Account;
using NosCore.Data.WebApi;
using NosCore.GameObject.Networking;

namespace NosCore.WorldServer.GraphQl
{
    public class Query
    {
        //[AuthorizeRole(AuthorityType.GameMaster)]
        public IEnumerable<ConnectedAccount> GetConnectedAccounts(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return Broadcaster.Instance.ConnectedAccounts().Where(s=>s.Name == name);
            }
            return Broadcaster.Instance.ConnectedAccounts();
        }
    }
}
