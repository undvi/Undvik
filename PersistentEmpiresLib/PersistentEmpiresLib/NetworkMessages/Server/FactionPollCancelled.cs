using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using PersistentEmpiresLib.Factions;
using PersistentEmpiresLib.Helpers;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace PersistentEmpiresLib.NetworkMessages.Server
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class FactionPollCancelled : GameNetworkMessage
    {
        public int FactionIndex { get; private set; }
        public string CancelledBy { get; private set; } // ✅ Wer hat die Wahl abgebrochen?

        public FactionPollCancelled() { }

        public FactionPollCancelled(int factionIndex, string cancelledBy)
        {
            this.FactionIndex = factionIndex;
            this.CancelledBy = cancelledBy;
        }

        protected override bool OnRead()
        {
            bool result = true;
            this.FactionIndex = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(-1, 200), ref result);
            this.CancelledBy = GameNetworkMessage.ReadStringFromPacket(ref result);
            return result;
        }

        protected override void OnWrite()
        {
            GameNetworkMessage.WriteIntToPacket(this.FactionIndex, new CompressionInfo.Integer(-1, 200));
            GameNetworkMessage.WriteStringToPacket(this.CancelledBy);
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.Administration;
        }

        protected override string OnGetLogFormat()
        {
            return $"🛑 Abstimmung für Fraktion {FactionIndex} wurde von {CancelledBy} abgebrochen.";
        }

        public static bool TryCancelPoll(int factionIndex, NetworkCommunicator canceller)
        {
            Faction faction = FactionManager.GetFactionByIndex(factionIndex);
            if (faction == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("⚠️ Fraktion nicht gefunden. Abstimmung konnte nicht abgebrochen werden."));
                return false;
            }

            PersistentEmpireRepresentative rep = canceller.GetComponent<PersistentEmpireRepresentative>();
            if (rep == null || rep.GetFactionIndex() != factionIndex)
            {
                InformationManager.DisplayMessage(new InformationMessage("❌ Du bist nicht in dieser Fraktion."));
                return false;
            }

            // ✅ Nur Lord oder Marshall dürfen Wahl abbrechen
            if (faction.lordId != canceller.VirtualPlayer.ToPlayerId() && !faction.marshalls.Contains(canceller.VirtualPlayer.ToPlayerId()))
            {
                InformationManager.DisplayMessage(new InformationMessage("⚠️ Nur der Lord oder ein Marshall kann eine Abstimmung abbrechen."));
                return false;
            }

            if (!FactionPollComponent.CancelOngoingPoll(factionIndex))
            {
                InformationManager.DisplayMessage(new InformationMessage("⚠️ Es gibt derzeit keine laufende Abstimmung für diese Fraktion."));
                return false;
            }

            string cancellerName = canceller.UserName;

            // ✅ Nachricht an alle Spieler senden
            GameNetwork.BeginBroadcastModuleEvent();
            GameNetwork.WriteMessage(new FactionPollCancelled(factionIndex, cancellerName));
            GameNetwork.EndBroadcastModuleEvent();

            // ✅ Log und UI-Updates
            faction.AddToFactionLog($"📜 Die Wahl wurde von {cancellerName} abgebrochen.");
            InformationManager.DisplayMessage(new InformationMessage($"🛑 Die laufende Abstimmung für die Fraktion {faction.name} wurde von {cancellerName} abgebrochen."));

            return true;
        }
    }
}
