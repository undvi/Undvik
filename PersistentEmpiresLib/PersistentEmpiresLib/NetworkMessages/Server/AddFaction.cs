using PersistentEmpiresLib.Factions;
using PersistentEmpiresLib.Helpers;
using System;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using TaleWorlds.ObjectSystem;

namespace PersistentEmpiresLib.NetworkMessages.Server
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class AddFaction : GameNetworkMessage
    {
        public Faction Faction { get; private set; }
        public int FactionIndex { get; private set; }

        public AddFaction(Faction faction, int factionIndex)
        {
            this.Faction = faction;
            this.FactionIndex = factionIndex;
        }

        public AddFaction() { }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.General;
        }

        protected override string OnGetLogFormat()
        {
            return $"📢 Neue Fraktion hinzugefügt: {Faction?.name ?? "Unbekannt"} (Index: {FactionIndex})";
        }

        protected override bool OnRead()
        {
            bool result = true;

            try
            {
                this.FactionIndex = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(-1, 200, true), ref result);
                BasicCultureObject culture = (BasicCultureObject)GameNetworkMessage.ReadObjectReferenceFromPacket(MBObjectManager.Instance, CompressionBasic.GUIDCompressionInfo, ref result);
                string name = GameNetworkMessage.ReadStringFromPacket(ref result);
                Team team = Mission.MissionNetworkHelper.GetTeamFromTeamIndex(GameNetworkMessage.ReadTeamIndexFromPacket(ref result));
                string bannerKey = PENetworkModule.ReadBannerCodeFromPacket(ref result);
                int rank = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(1, 5, true), ref result);
                int gold = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, int.MaxValue, true), ref result);
                int maxMembers = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(10, 100, true), ref result);

                int memberLength = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 500, true), ref result);

                this.Faction = new Faction(culture, new Banner(bannerKey), name)
                {
                    Rank = rank,
                    Gold = gold,
                    MaxMembers = maxMembers
                };
                this.Faction.team = team;

                for (int i = 0; i < memberLength; i++)
                {
                    var member = GameNetworkMessage.ReadNetworkPeerReferenceFromPacket(ref result);
                    if (!Faction.members.Contains(member))
                    {
                        Faction.members.Add(member);
                    }
                }

                // 🔹 Fraktionsboni aktivieren (z. B. Wirtschaft, Diplomatie)
                ApplyFactionBonuses(this.Faction);
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage($"⚠️ Fehler beim Laden der Fraktion: {ex.Message}"));
                return false;
            }

            return result;
        }

        protected override void OnWrite()
        {
            try
            {
                GameNetworkMessage.WriteIntToPacket(FactionIndex, new CompressionInfo.Integer(-1, 200, true));
                GameNetworkMessage.WriteObjectReferenceToPacket(Faction.basicCultureObject, CompressionBasic.GUIDCompressionInfo);
                GameNetworkMessage.WriteStringToPacket(Faction.name);
                GameNetworkMessage.WriteTeamIndexToPacket(Faction.team.TeamIndex);
                PENetworkModule.WriteBannerCodeToPacket(Faction.banner.Serialize());
                GameNetworkMessage.WriteIntToPacket(Faction.Rank, new CompressionInfo.Integer(1, 5, true));
                GameNetworkMessage.WriteIntToPacket(Faction.Gold, new CompressionInfo.Integer(0, int.MaxValue, true));
                GameNetworkMessage.WriteIntToPacket(Faction.MaxMembers, new CompressionInfo.Integer(10, 100, true));

                int memberLength = Faction.members.Count;
                GameNetworkMessage.WriteIntToPacket(memberLength, new CompressionInfo.Integer(0, 500, true));

                foreach (var member in Faction.members)
                {
                    GameNetworkMessage.WriteNetworkPeerReferenceToPacket(member);
                }
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage($"⚠️ Fehler beim Schreiben der Fraktion: {ex.Message}"));
            }
        }

        /// <summary>
        /// Wendet Boni und spezielle Einstellungen für eine Fraktion an.
        /// </summary>
        /// <param name="faction"></param>
        private static void ApplyFactionBonuses(Faction faction)
        {
            if (faction == null)
            {
                return;
            }

            // Fraktionsboni basierend auf Gebiet
            switch (faction.name)
            {
                case "Waldläufer":
                    faction.WoodProductionBonus = 20;
                    break;
                case "Bergvolk":
                    faction.MiningBonus = 15;
                    break;
                case "Handelsgilde":
                    faction.TradeBonus = 10;
                    break;
                default:
                    faction.WoodProductionBonus = 0;
                    faction.MiningBonus = 0;
                    faction.TradeBonus = 0;
                    break;
            }

            // Nachricht für den Serverlog
            InformationManager.DisplayMessage(new InformationMessage($"🌍 Fraktionsboni angewendet: {faction.name} (Holz: {faction.WoodProductionBonus}, Bergbau: {faction.MiningBonus}, Handel: {faction.TradeBonus})"));
        }

        /// <summary>
        /// Erstellt eine neue Fraktion und sendet sie an alle Spieler.
        /// </summary>
        public static void BroadcastNewFaction(Faction faction, int factionIndex)
        {
            if (faction == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("⚠️ Fehler: Fraktion ist null."));
                return;
            }

            GameNetwork.BeginBroadcastModuleEvent();
            GameNetwork.WriteMessage(new AddFaction(faction, factionIndex));
            GameNetwork.EndBroadcastModuleEvent();

            InformationManager.DisplayMessage(new InformationMessage($"🎖️ Neue Fraktion gegründet: {faction.name}"));
        }
    }
}
