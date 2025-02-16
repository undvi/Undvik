using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using PersistentEmpiresLib.Factions;
using TaleWorlds.Core;

namespace PersistentEmpiresLib.NetworkMessages.Server
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class FactionPollProgress : GameNetworkMessage
    {
        public int FactionIndex { get; private set; }
        public int VotesAccepted { get; private set; }
        public int VotesRejected { get; private set; }

        public FactionPollProgress() { }

        public FactionPollProgress(int factionIndex, int votesAccepted, int votesRejected)
        {
            this.FactionIndex = factionIndex;
            this.VotesAccepted = votesAccepted;
            this.VotesRejected = votesRejected;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.Administration;
        }

        protected override string OnGetLogFormat()
        {
            return $"Voting progress update for faction {FactionIndex}: Accepted {VotesAccepted} / Rejected {VotesRejected}";
        }

        protected override bool OnRead()
        {
            bool result = true;
            this.FactionIndex = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(-1, 200), ref result);
            this.VotesAccepted = GameNetworkMessage.ReadIntFromPacket(CompressionBasic.PlayerCompressionInfo, ref result);
            this.VotesRejected = GameNetworkMessage.ReadIntFromPacket(CompressionBasic.PlayerCompressionInfo, ref result);

            return result;
        }

        protected override void OnWrite()
        {
            GameNetworkMessage.WriteIntToPacket(this.FactionIndex, new CompressionInfo.Integer(-1, 200));
            GameNetworkMessage.WriteIntToPacket(this.VotesAccepted, CompressionBasic.PlayerCompressionInfo);
            GameNetworkMessage.WriteIntToPacket(this.VotesRejected, CompressionBasic.PlayerCompressionInfo);
        }

        public static void BroadcastPollProgress(int factionIndex, int votesAccepted, int votesRejected)
        {
            Faction faction = FactionManager.GetFactionByIndex(factionIndex);
            if (faction == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("Faction not found, unable to broadcast poll progress."));
                return;
            }

            GameNetwork.BeginBroadcastModuleEvent();
            GameNetwork.WriteMessage(new FactionPollProgress(factionIndex, votesAccepted, votesRejected));
            GameNetwork.EndBroadcastModuleEvent();

            InformationManager.DisplayMessage(new InformationMessage(
                $"Poll progress for faction {faction.name}: {votesAccepted} votes accepted, {votesRejected} votes rejected."
            ));
        }
    }
}
