using PersistentEmpiresLib.Helpers;
using PersistentEmpiresLib.Factions;
using System;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using TaleWorlds.Core;

namespace PersistentEmpiresLib.NetworkMessages.Client
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
    public sealed class UpdateFactionBanner : GameNetworkMessage
    {
        private const int BannerChangeCost = 1000; // Goldkosten für die Banneränderung

        public string BannerCode { get; private set; }

        public UpdateFactionBanner() { }

        public UpdateFactionBanner(string bannerCode)
        {
            this.BannerCode = bannerCode;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.None;
        }

        protected override string OnGetLogFormat()
        {
            return $"Request update on faction banner: {BannerCode}";
        }

        protected override bool OnRead()
        {
            bool result = true;
            this.BannerCode = PENetworkModule.ReadBannerCodeFromPacket(ref result);
            return result;
        }

        protected override void OnWrite()
        {
            PENetworkModule.WriteBannerCodeToPacket(this.BannerCode);
        }

        public static void TryChangeFactionBanner(NetworkCommunicator sender, string newBannerCode)
        {
            PersistentEmpireRepresentative rep = sender.GetComponent<PersistentEmpireRepresentative>();
            if (rep == null || rep.GetFaction() == null) return;

            Faction faction = rep.GetFaction();

            // Nur Lord oder Marschall darf das Banner ändern
            if (faction.lordId != sender.VirtualPlayer.ToPlayerId() && !faction.marshalls.Contains(sender.VirtualPlayer.ToPlayerId()))
            {
                InformationManager.DisplayMessage(new InformationMessage("Only the Lord or a Marshall can change the faction banner."));
                return;
            }

            // Überprüfen, ob das Banner-Format gültig ist
            if (string.IsNullOrEmpty(newBannerCode) || newBannerCode.Length < 10)
            {
                InformationManager.DisplayMessage(new InformationMessage("Invalid banner format."));
                return;
            }

            // Goldkosten prüfen
            if (faction.Gold < BannerChangeCost)
            {
                InformationManager.DisplayMessage(new InformationMessage("Your faction does not have enough gold to change the banner."));
                return;
            }

            // Gold abziehen und Banner ändern
            faction.Gold -= BannerChangeCost;
            faction.banner = new Banner(newBannerCode);

            // Netzwerk-Broadcast für alle Mitglieder
            GameNetwork.BeginBroadcastModuleEvent();
            GameNetwork.WriteMessage(new UpdateFactionBanner(newBannerCode));
            GameNetwork.EndBroadcastModuleEvent();

            InformationManager.DisplayMessage(new InformationMessage($"Faction banner updated. Cost: {BannerChangeCost} gold."));
        }
    }
}
