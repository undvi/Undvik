using System;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using PersistentEmpiresLib.Factions;
using PersistentEmpiresLib.NetworkMessages.Client;
using PersistentEmpiresLib.NetworkMessages.Server.Factions; // <-- Damit wir auf SyncFaction zugreifen können

namespace PersistentEmpiresLib.NetworkMessages.Server
{
    /// <summary>
    /// Serverseitige Nachricht: Führt eine Fraktions-Aktion aus,
    /// z. B. AddMember, RemoveMember, UpgradeRank, ChangeLeader, AssignMarshall.
    /// Roadmap: Du kannst hier noch "PrestigeCheck" oder "Kriegssperre" integrieren.
    /// </summary>
    public class HandleFactionAction : GameNetworkMessage
    {
        protected override bool OnRead()
        {
            bool bufferReadValid = true;

            // factionId = Faction-ID
            int factionId = GameNetworkMessage.ReadIntFromPacket(ref bufferReadValid);

            // Art der Aktion (AddMember, RemoveMember, etc.)
            DeclareFactionActionRequest.FactionActionType action =
                (DeclareFactionActionRequest.FactionActionType)
                GameNetworkMessage.ReadIntFromPacket(ref bufferReadValid);

            // PlayerId als String (z. B. "Steam_12345")
            string targetPlayerId = GameNetworkMessage.ReadStringFromPacket(ref bufferReadValid);

            if (bufferReadValid)
            {
                ExecuteFactionAction(factionId, action, targetPlayerId);
            }
            return bufferReadValid;
        }

        protected override void OnWrite()
        {
            // Server-seitig, kein "OnWrite" nötig.
        }

        /// <summary>
        /// Hauptlogik: je nach Aktion die passende Methode in der <see cref="Faction"/> aufrufen.
        /// Dann schickt ein SyncFaction, damit alle Clients die neuen Fraktionsdaten bekommen.
        /// </summary>
        private void ExecuteFactionAction(int factionId,
            DeclareFactionActionRequest.FactionActionType action,
            string targetPlayerId)
        {
            Faction faction = FactionManager.GetFactionById(factionId);
            if (faction == null)
            {
                // Optional: Clientmessage, "Fraktion nicht gefunden"
                return;
            }

            switch (action)
            {
                case DeclareFactionActionRequest.FactionActionType.AddMember:
                    {
                        if (string.IsNullOrEmpty(targetPlayerId))
                        {
                            // Ggf. Fehlermeldung
                            return;
                        }
                        if (!faction.CanAddMember())
                        {
                            // Ggf. Fehlermeldung an den Spieler
                            return;
                        }

                        faction.AddMember(targetPlayerId);

                        // Alle Clients updaten
                        BroadcastSyncFaction(faction);
                        break;
                    }

                case DeclareFactionActionRequest.FactionActionType.RemoveMember:
                    {
                        faction.RemoveMember(targetPlayerId);

                        // Alle Clients updaten
                        BroadcastSyncFaction(faction);
                        break;
                    }

                case DeclareFactionActionRequest.FactionActionType.UpgradeRank:
                    {
                        // Roadmap: Prestige-Check, Gold-Kosten,
                        // Minimaler Fraktionsrang oder Abkühlzeit
                        if (!faction.CanUpgradeFaction())
                        {
                            // Optional: Fehlermeldung an den Spieler
                            return;
                        }
                        faction.UpgradeFactionRank();

                        // Evtl. Prestigebonus oder mehr "MaxMembers" etc.
                        // Dann an Clients syncen
                        BroadcastSyncFaction(faction);
                        break;
                    }

                case DeclareFactionActionRequest.FactionActionType.ChangeLeader:
                    {
                        // Roadmap: Erbfolge, Adelsränge, etc.
                        faction.SelectNewLeader(targetPlayerId);
                        BroadcastSyncFaction(faction);
                        break;
                    }

                case DeclareFactionActionRequest.FactionActionType.AssignMarshall:
                    {
                        faction.AssignMarshall(targetPlayerId);
                        BroadcastSyncFaction(faction);
                        break;
                    }
            }
        }

        /// <summary>
        /// Hilfsmethode: Sendet die aktualisierte Faction an alle Clients.
        /// Nutzt unsere SyncFaction(int factionIndex, Faction faction)-Message
        /// aus FactionNetworkMessages.cs
        /// </summary>
        private void BroadcastSyncFaction(Faction faction)
        {
            GameNetwork.BeginBroadcastModuleEvent();
            GameNetwork.WriteMessage(new SyncFaction(faction.FactionIndex, faction));
            GameNetwork.EndBroadcastModuleEvent();
        }
    }
}
