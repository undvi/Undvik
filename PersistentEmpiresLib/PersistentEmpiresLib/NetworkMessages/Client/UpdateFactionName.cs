using System;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using PersistentEmpiresLib.Factions;
using TaleWorlds.Core;

namespace PersistentEmpiresLib.NetworkMessages.Client
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
    public sealed class UpdateFactionName : GameNetworkMessage
    {
        private const int NameChangeCost = 500; // Goldkosten für Namensänderung

        public UpdateFactionName() { }

        public UpdateFactionName(string newName)
        {
            this.NewName = newName;
        }

        public string NewName { get; private set; }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.None;
        }

        protected override string OnGetLogFormat()
        {
            return $"Change Faction Name Requested: {NewName}";
        }

        protected override bool OnRead()
        {
            bool result = true;
            this.NewName = GameNetworkMessage.ReadStringFromPacket(ref result);
            return result;
        }

        protected override void OnWrite()
        {
            GameNetworkMessage.WriteStringToPacket(this.NewName);
        }

        public static void TryChangeFactionName(NetworkCommunicator sender, string newName)
        {
            PersistentEmpireRepresentative rep = sender.GetComponent<PersistentEmpireRepresentative>();
            if (rep == null || rep.GetFaction() == null) return;

            Faction faction = rep.GetFaction();

            // Nur Lord oder Marschall darf den Namen ändern
            if (faction.lordId != sender.VirtualPlayer.ToPlayerId() && !faction.marshalls.Contains(sender.VirtualPlayer.ToPlayerId()))
            {
                InformationManager.DisplayMessage(new InformationMessage("Only the Lord or a Marshall can change the faction name."));
                return;
            }

            // Überprüfen, ob Name bereits existiert
            if (FactionManager.GetAllFactions().Exists(f => f.name == newName))
            {
                InformationManager.DisplayMessage(new InformationMessage("This faction name is already taken."));
                return;
            }

            // Goldkosten prüfen
            if (faction.Gold < NameChangeCost)
            {
                InformationManager.DisplayMessage(new InformationMessage("Your faction does not have enough gold to change the name."));
                return;
            }

            // Gold abziehen und Namen ändern
            faction.Gold -= NameChangeCost;
            faction.name = newName;

            // Netzwerk-Broadcast für alle Mitglieder
            GameNetwork.BeginBroadcastModuleEvent();
            GameNetwork.WriteMessage(new UpdateFactionName(newName));
            GameNetwork.EndBroadcastModuleEvent();

            InformationManager.DisplayMessage(new InformationMessage($"Faction name changed to: {newName}. Cost: {NameChangeCost} gold."));
        }
    }
}
