using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using PersistentEmpiresLib.Factions;
using TaleWorlds.Core;

namespace PersistentEmpiresLib.NetworkMessages.Server
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class FactionUpdateMarshall : GameNetworkMessage
    {
        public int FactionIndex { get; private set; }
        public NetworkCommunicator TargetPlayer { get; private set; }
        public bool IsMarshall { get; private set; }

        public FactionUpdateMarshall() { }

        public FactionUpdateMarshall(int factionIndex, NetworkCommunicator targetPlayer, bool isMarshall)
        {
            this.FactionIndex = factionIndex;
            this.TargetPlayer = targetPlayer;
            this.IsMarshall = isMarshall;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.Administration;
        }

        protected override string OnGetLogFormat()
        {
            return $"Faction {FactionIndex}: {TargetPlayer.UserName} is now a Marshall: {IsMarshall}";
        }

        protected override bool OnRead()
        {
            bool result = true;
            this.FactionIndex = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(-1, 200, true), ref result);
            this.TargetPlayer = GameNetworkMessage.ReadNetworkPeerReferenceFromPacket(ref result);
            this.IsMarshall = GameNetworkMessage.ReadBoolFromPacket(ref result);
            return result;
        }

        protected override void OnWrite()
        {
            GameNetworkMessage.WriteIntToPacket(this.FactionIndex, new CompressionInfo.Integer(-1, 200, true));
            GameNetworkMessage.WriteNetworkPeerReferenceToPacket(this.TargetPlayer);
            GameNetworkMessage.WriteBoolToPacket(this.IsMarshall);
        }

        public static void BroadcastMarshallUpdate(int factionIndex, NetworkCommunicator targetPlayer, bool isMarshall)
        {
            Faction faction = FactionManager.GetFactionByIndex(factionIndex);
            if (faction == null)
            {
                InformationManager.DisplayMessage(new InformationMessage($"Faction {factionIndex} not found, unable to update Marshall status."));
                return;
            }

            if (isMarshall)
            {
                if (!faction.marshalls.Contains(targetPlayer.VirtualPlayer.ToPlayerId()))
                {
                    faction.marshalls.Add(targetPlayer.VirtualPlayer.ToPlayerId());
                }
            }
            else
            {
                faction.marshalls.Remove(targetPlayer.VirtualPlayer.ToPlayerId());
            }

            GameNetwork.BeginBroadcastModuleEvent();
            GameNetwork.WriteMessage(new FactionUpdateMarshall(factionIndex, targetPlayer, isMarshall));
            GameNetwork.EndBroadcastModuleEvent();

            string statusMessage = isMarshall ? "promoted to Marshall" : "demoted from Marshall";
            InformationManager.DisplayMessage(new InformationMessage($"{targetPlayer.UserName} has been {statusMessage} in faction {faction.name}."));
        }
    }
}
