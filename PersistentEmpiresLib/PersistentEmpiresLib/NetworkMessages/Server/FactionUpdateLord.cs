using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using PersistentEmpiresLib.Factions;
using TaleWorlds.Core;

namespace PersistentEmpiresLib.NetworkMessages.Server
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class FactionUpdateLord : GameNetworkMessage
    {
        public int FactionIndex { get; private set; }
        public NetworkCommunicator Player { get; private set; }

        public FactionUpdateLord() { }

        public FactionUpdateLord(int factionIndex, NetworkCommunicator player)
        {
            this.FactionIndex = factionIndex;
            this.Player = player;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.Administration;
        }

        protected override string OnGetLogFormat()
        {
            return $"Faction {FactionIndex} has a new lord: {Player.VirtualPlayer.Id}";
        }

        protected override bool OnRead()
        {
            bool result = true;
            this.FactionIndex = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(-1, 200, true), ref result);
            this.Player = GameNetworkMessage.ReadNetworkPeerReferenceFromPacket(ref result);
            return result;
        }

        protected override void OnWrite()
        {
            GameNetworkMessage.WriteIntToPacket(this.FactionIndex, new CompressionInfo.Integer(-1, 200, true));
            GameNetworkMessage.WriteNetworkPeerReferenceToPacket(this.Player);
        }

        public static void BroadcastNewLord(int factionIndex, NetworkCommunicator newLord)
        {
            Faction faction = FactionManager.GetFactionByIndex(factionIndex);
            if (faction == null)
            {
                InformationManager.DisplayMessage(new InformationMessage($"Faction {factionIndex} not found, unable to update lord."));
                return;
            }

            faction.lordId = newLord.VirtualPlayer.ToPlayerId();

            GameNetwork.BeginBroadcastModuleEvent();
            GameNetwork.WriteMessage(new FactionUpdateLord(factionIndex, newLord));
            GameNetwork.EndBroadcastModuleEvent();

            InformationManager.DisplayMessage(new InformationMessage($"New lord for faction {faction.name}: {newLord.UserName}"));
        }
    }
}

