using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using PersistentEmpiresLib.Factions;

namespace PersistentEmpiresLib.NetworkMessages.Client
{
    public class DeclareConquestRequest : GameNetworkMessage
    {
        public enum ConquestActionType
        {
            CaptureTerritory,
            AssignAsVassal,
            PillageTerritory
        }

        public int FactionId { get; private set; }
        public int TargetCastleId { get; private set; }
        public ConquestActionType Action { get; private set; }

        public DeclareConquestRequest(int factionId, int targetCastleId, ConquestActionType action)
        {
            FactionId = factionId;
            TargetCastleId = targetCastleId;
            Action = action;
        }

        protected override bool OnRead()
        {
            bool bufferReadValid = true;
            FactionId = GameNetworkMessage.ReadIntFromPacket(ref bufferReadValid);
            TargetCastleId = GameNetworkMessage.ReadIntFromPacket(ref bufferReadValid);
            Action = (ConquestActionType)GameNetworkMessage.ReadIntFromPacket(ref bufferReadValid);
            return bufferReadValid;
        }

        protected override void OnWrite()
        {
            GameNetworkMessage.WriteIntToPacket(FactionId);
            GameNetworkMessage.WriteIntToPacket(TargetCastleId);
            GameNetworkMessage.WriteIntToPacket((int)Action);
        }

        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.FactionManagement;
        protected override string OnGetLogFormat() => $"DeclareConquestRequest: Faction {FactionId}, Target {TargetCastleId}, Action {Action}";

        public bool ValidateConquest(Faction faction)
        {
            int maxTerritories = faction.GetMaxTerritoriesByRank();
            if (faction.ControlledTerritories.Count >= maxTerritories)
            {
                return false; // Fraktion hat die maximale Anzahl an Gebieten erreicht
            }
            return true;
        }

        public bool ValidateVassalOption(Faction faction)
        {
            return faction.CanCreateVassal(); // Überprüft, ob die Fraktion Vasallenstaaten verwalten kann
        }
    }
}
