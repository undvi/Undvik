using System;
using System.Collections.Generic;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using PersistentEmpiresLib.Factions;
using PersistentEmpiresLib.Helpers;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;

namespace PersistentEmpiresLib.NetworkMessages.Server.Factions
{
    /// <summary>
    /// Aktualisiert eine Fraktion serverseitig (Name, Banner, Tax, Rang, etc.).
    /// Roadmap: Kann man erweitern um Prestige, Adelsränge etc.
    /// </summary>
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class UpdateFactionFromServer : GameNetworkMessage
    {
        public string Name { get; private set; }
        public string BannerCode { get; private set; }
        public int FactionIndex { get; private set; }
        public float TaxRate { get; private set; }
        public string ExportOrders { get; private set; }
        public string TerritoryBonus { get; private set; }
        public List<int> Vassals { get; private set; }
        public int FactionRank { get; private set; }

        // (Roadmap-Erweiterung) Prestige, z. B. ein Wert, der das "Adelslevel" beeinflusst
        public int Prestige { get; private set; }

        public UpdateFactionFromServer() { }

        public UpdateFactionFromServer(Faction updatedFaction, int factionIndex)
        {
            this.FactionIndex = factionIndex;
            this.Name = string.IsNullOrEmpty(updatedFaction.name) ? "Unknown Faction" : updatedFaction.name;

            this.BannerCode = string.IsNullOrEmpty(updatedFaction.banner.Serialize())
                ? "DefaultBanner"
                : updatedFaction.banner.Serialize();

            this.TaxRate = updatedFaction.TaxRate;
            this.ExportOrders = updatedFaction.GetExportOrdersAsString();
            this.TerritoryBonus = updatedFaction.TerritoryBonus;
            this.Vassals = updatedFaction.GetVassalFactionIndexes();
            this.FactionRank = updatedFaction.Rank;

            // Beispiel: aus Faction heraus Prestige entnehmen.
            // (Falls du so ein Feld bereits in "Faction" hast)
            this.Prestige = updatedFaction.Prestige;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
            => MultiplayerMessageFilter.General;

        protected override string OnGetLogFormat()
        {
            return $"Faction Update | Index: {FactionIndex}, Name: {Name}, BannerCode: {BannerCode}, " +
                   $"TaxRate: {TaxRate}, TerritoryBonus: {TerritoryBonus}, Rank: {FactionRank}, Prestige: {Prestige}";
        }

        protected override bool OnRead()
        {
            bool result = true;
            this.FactionIndex = GameNetworkMessage.ReadIntFromPacket(
                new CompressionInfo.Integer(-1, 200, true), ref result);
            this.Name = GameNetworkMessage.ReadStringFromPacket(ref result);
            this.BannerCode = PENetworkModule.ReadBannerCodeFromPacket(ref result);
            this.TaxRate = GameNetworkMessage.ReadFloatFromPacket(ref result);
            this.ExportOrders = GameNetworkMessage.ReadStringFromPacket(ref result);
            this.TerritoryBonus = GameNetworkMessage.ReadStringFromPacket(ref result);
            this.Vassals = GameNetworkMessage.ReadListFromPacket(
                () => GameNetworkMessage.ReadIntFromPacket(
                    new CompressionInfo.Integer(-1, 200, true), ref result), ref result);

            this.FactionRank = GameNetworkMessage.ReadIntFromPacket(
                new CompressionInfo.Integer(1, 5, true), ref result);

            // Optionaler Prestige-Wert
            this.Prestige = GameNetworkMessage.ReadIntFromPacket(
                new CompressionInfo.Integer(0, int.MaxValue, true), ref result);

            if (string.IsNullOrEmpty(this.Name))
                this.Name = "Unknown Faction";
            if (string.IsNullOrEmpty(this.BannerCode))
                this.BannerCode = "DefaultBanner";

            return result;
        }

        protected override void OnWrite()
        {
            GameNetworkMessage.WriteIntToPacket(this.FactionIndex,
                new CompressionInfo.Integer(-1, 200, true));
            GameNetworkMessage.WriteStringToPacket(this.Name);
            PENetworkModule.WriteBannerCodeToPacket(this.BannerCode);
            GameNetworkMessage.WriteFloatToPacket(this.TaxRate);
            GameNetworkMessage.WriteStringToPacket(this.ExportOrders);
            GameNetworkMessage.WriteStringToPacket(this.TerritoryBonus);
            GameNetworkMessage.WriteListToPacket(this.Vassals,
                (value) => GameNetworkMessage.WriteIntToPacket(value,
                new CompressionInfo.Integer(-1, 200, true)));
            GameNetworkMessage.WriteIntToPacket(this.FactionRank,
                new CompressionInfo.Integer(1, 5, true));

            // Neuer Prestige-Wert
            GameNetworkMessage.WriteIntToPacket(this.Prestige,
                new CompressionInfo.Integer(0, int.MaxValue, true));
        }
    }

    // ---------------------------------------------------------
    // Beispiel: Neue Fraktion hinzufügen
    // ---------------------------------------------------------
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
            => MultiplayerMessageFilter.General;

        protected override string OnGetLogFormat()
            => $"📢 Neue Fraktion hinzugefügt: {Faction?.name ?? "Unbekannt"} (Index: {FactionIndex})";

        protected override bool OnRead()
        {
            bool result = true;
            try
            {
                this.FactionIndex = GameNetworkMessage.ReadIntFromPacket(
                    new CompressionInfo.Integer(-1, 200, true), ref result);
                BasicCultureObject culture = (BasicCultureObject)GameNetworkMessage
                    .ReadObjectReferenceFromPacket(MBObjectManager.Instance, CompressionBasic.GUIDCompressionInfo, ref result);
                string name = GameNetworkMessage.ReadStringFromPacket(ref result);
                Team team = Mission.MissionNetworkHelper
                    .GetTeamFromTeamIndex(GameNetworkMessage.ReadTeamIndexFromPacket(ref result));
                string bannerKey = PENetworkModule.ReadBannerCodeFromPacket(ref result);
                int rank = GameNetworkMessage.ReadIntFromPacket(
                    new CompressionInfo.Integer(1, 5, true), ref result);
                int gold = GameNetworkMessage.ReadIntFromPacket(
                    new CompressionInfo.Integer(0, int.MaxValue, true), ref result);
                int maxMembers = GameNetworkMessage.ReadIntFromPacket(
                    new CompressionInfo.Integer(10, 100, true), ref result);
                int memberLength = GameNetworkMessage.ReadIntFromPacket(
                    new CompressionInfo.Integer(0, 500, true), ref result);

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

                // Fraktionsboni (Holz, Bergbau, Handel, etc.)
                ApplyFactionBonuses(this.Faction);
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"⚠️ Fehler beim Laden der Fraktion: {ex.Message}"));
                return false;
            }
            return result;
        }

        protected override void OnWrite()
        {
            try
            {
                GameNetworkMessage.WriteIntToPacket(FactionIndex,
                    new CompressionInfo.Integer(-1, 200, true));
                GameNetworkMessage.WriteObjectReferenceToPacket(
                    Faction.basicCultureObject, CompressionBasic.GUIDCompressionInfo);
                GameNetworkMessage.WriteStringToPacket(Faction.name);
                GameNetworkMessage.WriteTeamIndexToPacket(Faction.team.TeamIndex);
                PENetworkModule.WriteBannerCodeToPacket(Faction.banner.Serialize());
                GameNetworkMessage.WriteIntToPacket(Faction.Rank,
                    new CompressionInfo.Integer(1, 5, true));
                GameNetworkMessage.WriteIntToPacket(Faction.Gold,
                    new CompressionInfo.Integer(0, int.MaxValue, true));
                GameNetworkMessage.WriteIntToPacket(Faction.MaxMembers,
                    new CompressionInfo.Integer(10, 100, true));

                int memberLength = Faction.members.Count;
                GameNetworkMessage.WriteIntToPacket(memberLength,
                    new CompressionInfo.Integer(0, 500, true));
                foreach (var member in Faction.members)
                {
                    GameNetworkMessage.WriteNetworkPeerReferenceToPacket(member);
                }
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"⚠️ Fehler beim Schreiben der Fraktion: {ex.Message}"));
            }
        }

        private static void ApplyFactionBonuses(Faction faction)
        {
            if (faction == null) return;

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

            InformationManager.DisplayMessage(new InformationMessage(
                $"🌍 Fraktionsboni: {faction.name} (Holz: {faction.WoodProductionBonus}, " +
                $"Bergbau: {faction.MiningBonus}, Handel: {faction.TradeBonus})"));
        }

        public static void BroadcastNewFaction(Faction faction, int factionIndex)
        {
            if (faction == null)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    "⚠️ Fehler: Fraktion ist null."));
                return;
            }
            GameNetwork.BeginBroadcastModuleEvent();
            GameNetwork.WriteMessage(new AddFaction(faction, factionIndex));
            GameNetwork.EndBroadcastModuleEvent();
            InformationManager.DisplayMessage(new InformationMessage(
                $"🎖️ Neue Fraktion gegründet: {faction.name}"));
        }
    }

    // ---------------------------------------------------------
    // Beispiel: Marshall, Lord-Wechsel, Polls, Kriege usw.
    // ---------------------------------------------------------

    #region AddMarshallIdToFaction
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class AddMarshallIdToFaction : GameNetworkMessage
    {
        public string MarshallId { get; private set; }
        public int FactionIndex { get; private set; }

        public AddMarshallIdToFaction() { }

        public AddMarshallIdToFaction(string marshallId, int factionIndex)
        {
            this.MarshallId = marshallId;
            this.FactionIndex = factionIndex;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
            => MultiplayerMessageFilter.Administration;

        protected override string OnGetLogFormat()
            => $"🔹 Marshall {MarshallId} wurde zur Fraktion {FactionIndex} hinzugefügt.";

        protected override bool OnRead()
        {
            bool result = true;
            this.FactionIndex = GameNetworkMessage.ReadIntFromPacket(
                new CompressionInfo.Integer(-1, 200, true), ref result);
            this.MarshallId = GameNetworkMessage.ReadStringFromPacket(ref result);
            return result;
        }

        protected override void OnWrite()
        {
            GameNetworkMessage.WriteIntToPacket(this.FactionIndex,
                new CompressionInfo.Integer(-1, 200, true));
            GameNetworkMessage.WriteStringToPacket(this.MarshallId);
        }

        // etc. (siehe Original-Logik: CanAddMarshall, TryAddMarshall) ...
    }
    #endregion

    // ... Hier bleiben deine vorhandenen Klassen 
    // (FactionLordPollClosed, FactionLordPollOpened, FactionPollCancelled, etc.)
    // weitgehend unverändert, da sie schon recht gut zur Roadmap passen.
    // Lediglich optional in den Kommentaren zusätzliche Hinweise
    // zu Prestige- oder Fraktionsrang-Checks.

    #region WarDecleration
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class WarDecleration : GameNetworkMessage
    {
        public int WarDeclarerIndex { get; set; }
        public int WarDeclaredTo { get; set; }

        // NEU: Typ des Krieges, analog zur Roadmap:
        //  - 0 = Handelskrieg
        //  - 1 = Überfall (Raiding)
        //  - 2 = Eroberung
        public int WarType { get; set; }

        public WarDecleration() { }

        public WarDecleration(int warDeclarerIndex, int warDeclaredTo, int warType)
        {
            this.WarDeclarerIndex = warDeclarerIndex;
            this.WarDeclaredTo = warDeclaredTo;
            this.WarType = warType;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
            => MultiplayerMessageFilter.Administration;

        protected override string OnGetLogFormat()
            => $"WarDecleration: Faction {WarDeclarerIndex} erklärt {WarDeclaredTo} den Krieg (Typ: {WarType})";

        protected override bool OnRead()
        {
            bool result = true;
            this.WarDeclarerIndex = GameNetworkMessage.ReadIntFromPacket(
                new CompressionInfo.Integer(0, 200, true), ref result);
            this.WarDeclaredTo = GameNetworkMessage.ReadIntFromPacket(
                new CompressionInfo.Integer(0, 200, true), ref result);
            this.WarType = GameNetworkMessage.ReadIntFromPacket(
                new CompressionInfo.Integer(0, 10, true), ref result);

            return result;
        }
        protected override void OnWrite()
        {
            GameNetworkMessage.WriteIntToPacket(this.WarDeclarerIndex,
                new CompressionInfo.Integer(0, 200, true));
            GameNetworkMessage.WriteIntToPacket(this.WarDeclaredTo,
                new CompressionInfo.Integer(0, 200, true));
            GameNetworkMessage.WriteIntToPacket(this.WarType,
                new CompressionInfo.Integer(0, 10, true));
        }
    }
    #endregion

    // ... Restliche Nachrichten wie PeaceDecleration, 
    // PlayerJoinedFaction, SyncCastleBanner usw. bleiben weitgehend 
    // in ihrer Originalform erhalten. Du kannst jeweils optional 
    // Felder wie Prestige, FactionRank oder WarBlockade anfügen, 
    // falls du's brauchst.

    // Wenn du noch poll- oder fraktionsbezogene Nachrichten 
    // verbessern willst, findest du sie in diesem File. 
    // (Beispiel: FactionPollProgress, FactionPollCancelled etc.)

    // --------------------------------------------------
    // SyncFaction: synchronisiert Fraktion an den Client
    // --------------------------------------------------
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class SyncFaction : GameNetworkMessage
    {
        public int FactionIndex { get; private set; }
        public Faction Faction { get; private set; }

        public SyncFaction() { }

        public SyncFaction(int factionIndex, Faction faction)
        {
            this.FactionIndex = factionIndex;
            this.Faction = faction;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
            => MultiplayerMessageFilter.Mission;

        protected override string OnGetLogFormat()
            => $"Syncing Faction {FactionIndex}: {Faction?.name ?? "Unknown"}";

        protected override bool OnRead()
        {
            bool result = true;
            this.FactionIndex = GameNetworkMessage.ReadIntFromPacket(
                new CompressionInfo.Integer(-1, 200, true), ref result);

            string factionName = GameNetworkMessage.ReadStringFromPacket(ref result);
            Team factionTeam = Mission.MissionNetworkHelper
                .GetTeamFromTeamIndex(GameNetworkMessage.ReadTeamIndexFromPacket(ref result));
            string bannerKey = PENetworkModule.ReadBannerCodeFromPacket(ref result);
            string lordId = GameNetworkMessage.ReadStringFromPacket(ref result);
            int factionRank = GameNetworkMessage.ReadIntFromPacket(
                new CompressionInfo.Integer(1, 5, true), ref result);
            int maxMembers = GameNetworkMessage.ReadIntFromPacket(
                new CompressionInfo.Integer(10, 100, true), ref result);
            bool isVassal = GameNetworkMessage.ReadBoolFromPacket(ref result);
            int overlordFactionIndex = GameNetworkMessage.ReadIntFromPacket(
                new CompressionInfo.Integer(-1, 200, true), ref result);
            float taxRate = GameNetworkMessage.ReadFloatFromPacket(ref result);

            // Du könntest optional Prestige oder alliance/war arrays auch hier
            // anfügen, analog zu WarDecleration. 
            // ...
            this.Faction = new Faction(new Banner(bannerKey), factionName)
            {
                lordId = lordId,
                team = factionTeam,
                Rank = factionRank,
                MaxMembers = maxMembers,
                IsVassal = isVassal,
                OverlordFactionIndex = overlordFactionIndex,
                TaxRate = taxRate
            };

            int warDeclarationCount = GameNetworkMessage.ReadIntFromPacket(
                new CompressionInfo.Integer(0, 200, true), ref result);
            for (int i = 0; i < warDeclarationCount; i++)
            {
                this.Faction.warDeclaredTo.Add(GameNetworkMessage.ReadIntFromPacket(
                    new CompressionInfo.Integer(0, 200, true), ref result));
            }
            int allianceCount = GameNetworkMessage.ReadIntFromPacket(
                new CompressionInfo.Integer(0, 200, true), ref result);
            for (int i = 0; i < allianceCount; i++)
            {
                this.Faction.alliedFactions.Add(GameNetworkMessage.ReadIntFromPacket(
                    new CompressionInfo.Integer(0, 200, true), ref result));
            }
            int tradeAgreementCount = GameNetworkMessage.ReadIntFromPacket(
                new CompressionInfo.Integer(0, 200, true), ref result);
            for (int i = 0; i < tradeAgreementCount; i++)
            {
                this.Faction.tradeAgreements.Add(GameNetworkMessage.ReadIntFromPacket(
                    new CompressionInfo.Integer(0, 200, true), ref result));
            }
            return result;
        }

        protected override void OnWrite()
        {
            GameNetworkMessage.WriteIntToPacket(this.FactionIndex,
                new CompressionInfo.Integer(-1, 200, true));
            GameNetworkMessage.WriteStringToPacket(this.Faction.name);
            GameNetworkMessage.WriteTeamIndexToPacket(this.Faction.team.TeamIndex);
            PENetworkModule.WriteBannerCodeToPacket(this.Faction.banner.Serialize());
            GameNetworkMessage.WriteStringToPacket(this.Faction.lordId);
            GameNetworkMessage.WriteIntToPacket(this.Faction.Rank,
                new CompressionInfo.Integer(1, 5, true));
            GameNetworkMessage.WriteIntToPacket(this.Faction.MaxMembers,
                new CompressionInfo.Integer(10, 100, true));
            GameNetworkMessage.WriteBoolToPacket(this.Faction.IsVassal);
            GameNetworkMessage.WriteIntToPacket(this.Faction.OverlordFactionIndex,
                new CompressionInfo.Integer(-1, 200, true));
            GameNetworkMessage.WriteFloatToPacket(this.Faction.TaxRate);

            int warDeclarationCount = this.Faction.warDeclaredTo.Count;
            GameNetworkMessage.WriteIntToPacket(warDeclarationCount,
                new CompressionInfo.Integer(0, 200, true));
            for (int i = 0; i < warDeclarationCount; i++)
            {
                GameNetworkMessage.WriteIntToPacket(this.Faction.warDeclaredTo[i],
                    new CompressionInfo.Integer(0, 200, true));
            }

            int allianceCount = this.Faction.alliedFactions.Count;
            GameNetworkMessage.WriteIntToPacket(allianceCount,
                new CompressionInfo.Integer(0, 200, true));
            for (int i = 0; i < allianceCount; i++)
            {
                GameNetworkMessage.WriteIntToPacket(this.Faction.alliedFactions[i],
                    new CompressionInfo.Integer(0, 200, true));
            }

            int tradeAgreementCount = this.Faction.tradeAgreements.Count;
            GameNetworkMessage.WriteIntToPacket(tradeAgreementCount,
                new CompressionInfo.Integer(0, 200, true));
            for (int i = 0; i < tradeAgreementCount; i++)
            {
                GameNetworkMessage.WriteIntToPacket(this.Faction.tradeAgreements[i],
                    new CompressionInfo.Integer(0, 200, true));
            }
        }
    }
}
