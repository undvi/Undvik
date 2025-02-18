using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using PersistentEmpiresLib.Factions;
using System;
using System.Linq;
using TaleWorlds.Library;

namespace PersistentEmpiresLib.NetworkMessages.Server
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class FactionLordPollClosed : GameNetworkMessage
    {
        private const int LordChangeCost = 50000; // Kosten für den Lordwechsel
        private const int LordChangeCooldown = 86400; // 24h Cooldown

        public FactionLordPollClosed() { }

        public FactionLordPollClosed(NetworkCommunicator targetPlayer, bool accepted, int factionIndex)
        {
            this.TargetPlayer = targetPlayer;
            this.Accepted = accepted;
            this.FactionIndex = factionIndex;
        }

        public NetworkCommunicator TargetPlayer { get; private set; }
        public bool Accepted { get; private set; }
        public int FactionIndex { get; private set; }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.Administration;
        }

        protected override string OnGetLogFormat()
        {
            return $"🔹 Lord-Wahl abgeschlossen! Neuer Lord von Fraktion {FactionIndex}: {TargetPlayer.UserName}";
        }

        protected override bool OnRead()
        {
            bool result = true;
            this.TargetPlayer = GameNetworkMessage.ReadNetworkPeerReferenceFromPacket(ref result);
            this.Accepted = GameNetworkMessage.ReadBoolFromPacket(ref result);
            this.FactionIndex = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(-1, 200), ref result);
            return result;
        }

        protected override void OnWrite()
        {
            GameNetworkMessage.WriteNetworkPeerReferenceToPacket(this.TargetPlayer);
            GameNetworkMessage.WriteBoolToPacket(this.Accepted);
            GameNetworkMessage.WriteIntToPacket(this.FactionIndex, new CompressionInfo.Integer(-1, 200));
        }

        public static bool CanChangeLord(Faction faction, NetworkCommunicator requester)
        {
            if (faction.Rank < 2)
            {
                InformationManager.DisplayMessage(new InformationMessage("❌ Eure Fraktion muss mindestens Rang 2 sein, um den Lord zu wechseln."));
                return false;
            }

            if (!faction.members.Contains(requester))
            {
                InformationManager.DisplayMessage(new InformationMessage("❌ Du musst Mitglied der Fraktion sein, um den Lord zu wechseln."));
                return false;
            }

            if (faction.Gold < LordChangeCost)
            {
                InformationManager.DisplayMessage(new InformationMessage("❌ Eure Fraktion hat nicht genug Gold (50.000 Gold benötigt)."));
                return false;
            }

            if (faction.pollUnlockedAt > DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            {
                InformationManager.DisplayMessage(new InformationMessage("⚠️ Eure Fraktion muss 24 Stunden warten, bevor sie erneut einen Lord wechseln kann."));
                return false;
            }

            return true;
        }

        public static void TryChangeLord(Faction faction, NetworkCommunicator newLord, NetworkCommunicator requester)
        {
            if (!CanChangeLord(faction, requester))
            {
                return;
            }

            if (!faction.members.Contains(newLord))
            {
                InformationManager.DisplayMessage(new InformationMessage("❌ Der gewählte Spieler ist nicht in eurer Fraktion."));
                return;
            }

            // ✅ Gold abziehen
            faction.Gold -= LordChangeCost;

            // ✅ Lord wechseln
            faction.lordId = newLord.VirtualPlayer.ToPlayerId();
            faction.pollUnlockedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + LordChangeCooldown; // 24 Stunden Cooldown setzen

            GameNetwork.BeginBroadcastModuleEvent();
            GameNetwork.WriteMessage(new FactionLordPollClosed(newLord, true, faction.FactionIndex));
            GameNetwork.EndBroadcastModuleEvent();

            // ✅ Nachricht an die Fraktion senden
            faction.AddToFactionLog($"⚔️ {requester.UserName} hat {newLord.UserName} als neuen Lord ernannt.");
            InformationManager.DisplayMessage(new InformationMessage($"🎉 {newLord.UserName} ist jetzt der neue Lord von {faction.name}!"));
        }
    }
}
