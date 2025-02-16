using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using PersistentEmpiresLib.Factions;
using TaleWorlds.Core;

namespace PersistentEmpiresLib.NetworkMessages.Server
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class PlayerJoinedFaction : GameNetworkMessage
    {
        public int FactionIndex { get; private set; }
        public int JoinedFrom { get; private set; }
        public NetworkCommunicator Player { get; private set; }

        public PlayerJoinedFaction() { }

        public PlayerJoinedFaction(int factionIndex, int joinedFrom, NetworkCommunicator player)
        {
            this.FactionIndex = factionIndex;
            this.JoinedFrom = joinedFrom;
            this.Player = player;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.General;
        }

        protected override string OnGetLogFormat()
        {
            return $"Player {Player.UserName} joined faction {FactionIndex} (from {JoinedFrom})";
        }

        protected override bool OnRead()
        {
            bool result = true;
            this.FactionIndex = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(-1, 200, true), ref result);
            this.JoinedFrom = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(-1, 200, true), ref result);
            this.Player = GameNetworkMessage.ReadNetworkPeerReferenceFromPacket(ref result);

            return result;
        }

        protected override void OnWrite()
        {
            GameNetworkMessage.WriteIntToPacket(this.FactionIndex, new CompressionInfo.Integer(-1, 200, true));
            GameNetworkMessage.WriteIntToPacket(this.JoinedFrom, new CompressionInfo.Integer(-1, 200, true));
            GameNetworkMessage.WriteNetworkPeerReferenceToPacket(this.Player);
        }

        public static void BroadcastPlayerJoinedFaction(int factionIndex, int joinedFrom, NetworkCommunicator player)
        {
            if (player == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("Error: Player reference is null in PlayerJoinedFaction."));
                return;
            }

            Faction faction = FactionManager.GetFactionByIndex(factionIndex);
            if (faction == null)
            {
                InformationManager.DisplayMessage(new InformationMessage($"Faction {factionIndex} not found, unable to add player {player.UserName}."));
                return;
            }

            if (!faction.members.Contains(player))
            {
                faction.members.Add(player);
            }

            GameNetwork.BeginBroadcastModuleEvent();
            GameNetwork.WriteMessage(new PlayerJoinedFaction(factionIndex, joinedFrom, player));
            GameNetwork.EndBroadcastModuleEvent();

            InformationManager.DisplayMessage(new InformationMessage($"{player.UserName} has joined the faction {faction.name}."));
        }
    }
}
