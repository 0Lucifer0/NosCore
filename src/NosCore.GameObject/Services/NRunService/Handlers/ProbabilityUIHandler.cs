using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using System;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.NRunService.Handlers
{
    public class ProbabilityUIHandler : INrunEventHandler
    {
        public bool Condition(Tuple<IAliveEntity, NrunPacket> datas)
        {
            return datas.Item2.Runner == NrunRunnerType.ProbabilityUIs;
        }

        public Task ExecuteAsync(RequestData<Tuple<IAliveEntity, NrunPacket>> requestData)
        {
            return requestData.ClientSession.SendPacketAsync(
                new WopenPacket
                {
                    Type = (WindowType)(requestData.Data.Item2.Type ?? 0)
                });
        }
    }
}