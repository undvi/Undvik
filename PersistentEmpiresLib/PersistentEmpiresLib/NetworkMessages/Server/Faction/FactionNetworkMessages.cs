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
    // Dieses File fasst alle fraktionsbezogenen Netzwerk-Nachrichten in einer Datei zusammen.

    #region UpdateFactionFromServer
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

        public UpdateFactionFromServer() { }

        public UpdateFactionFromServer(Faction updatedFaction, int factionIndex)
        {
            this.FactionIndex = factionIndex;
            this.Name = string.IsNullOrEmpty(updatedFaction.name) ? "Unknown Faction" : updatedFaction.name;
            this.BannerCode = string.IsNullOrEmpty(updatedFaction.banner.Serialize()) ? "DefaultBanner" : updatedFaction.banner.Serialize();
            this.TaxRate = updatedFaction.TaxRate;
            this.ExportOrders = updatedFaction.GetExportOrdersAsString();
            this.TerritoryBonus = updatedFaction.TerritoryBonus;
            this.Vassals = updatedFaction.GetVassalFactionIndexes();
            this.FactionRank = updatedFaction.Rank;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.General;
        }

        protected override string OnGetLogFormat()
        {
            return $"Faction Update | Index: {FactionIndex}, Name: {Name}, BannerCode: {BannerCode}, TaxRate: {TaxRate}, TerritoryBonus: {TerritoryBonus}, Rank: {FactionRank}";
        }

        protected override bool OnRead()
        {
            bool result = true;
            this.FactionIndex = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(-1, 200, true), ref result);
            this.Name = GameNetworkMessage.ReadStringFromPacket(ref result);
            this.BannerCode = PENetworkModule.ReadBannerCodeFromPacket(ref result);
            this.TaxRate = GameNetworkMessage.ReadFloatFromPacket(ref result);
            this.ExportOrders = GameNetworkMessage.ReadStringFromPacket(ref result);
            this.TerritoryBonus = GameNetworkMessage.ReadStringFromPacket(ref result);
            this.Vassals = GameNetworkMessage.ReadListFromPacket(() => GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(-1, 200, true), ref result), ref result);
            this.FactionRank = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(1, 5, true), ref result);

            if (string.IsNullOrEmpty(this.Name))
                this.Name = "Unknown Faction";

            if (string.IsNullOrEmpty(this.BannerCode))
                this.BannerCode = "DefaultBanner";

            return result;
        }

        protected override void OnWrite()
        {
            GameNetworkMessage.WriteIntToPacket(this.FactionIndex, new CompressionInfo.Integer(-1, 200, true));
            GameNetworkMessage.WriteStringToPacket(this.Name);
            PENetworkModule.WriteBannerCodeToPacket(this.BannerCode);
            GameNetworkMessage.WriteFloatToPacket(this.TaxRate);
            GameNetworkMessage.WriteStringToPacket(this.ExportOrders);
            GameNetworkMessage.WriteStringToPacket(this.TerritoryBonus);
            GameNetworkMessage.WriteListToPacket(this.Vassals, (value) => GameNetworkMessage.WriteIntToPacket(value, new CompressionInfo.Integer(-1, 200, true)));
            GameNetworkMessage.WriteIntToPacket(this.FactionRank, new CompressionInfo.Integer(1, 5, true));
        }
    }
    #endregion

    #region AddFaction
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

                // Fraktionsboni aktivieren (z. B. Wirtschaft, Diplomatie)
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

        private static void ApplyFactionBonuses(Faction faction)
        {
            if (faction == null)
            {
                return;
            }
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
            InformationManager.DisplayMessage(new InformationMessage($"🌍 Fraktionsboni angewendet: {faction.name} (Holz: {faction.WoodProductionBonus}, Bergbau: {faction.MiningBonus}, Handel: {faction.TradeBonus})"));
        }

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
    #endregion

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
        {
            return MultiplayerMessageFilter.Administration;
        }

        protected override string OnGetLogFormat()
        {
            return $"🔹 Marshall {MarshallId} wurde zur Fraktion {FactionIndex} hinzugefügt.";
        }

        protected override bool OnRead()
        {
            bool result = true;
            this.FactionIndex = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(-1, 200, true), ref result);
            this.MarshallId = GameNetworkMessage.ReadStringFromPacket(ref result);
            return result;
        }

        protected override void OnWrite()
        {
            GameNetworkMessage.WriteIntToPacket(this.FactionIndex, new CompressionInfo.Integer(-1, 200, true));
            GameNetworkMessage.WriteStringToPacket(this.MarshallId);
        }

        public static bool CanAddMarshall(Faction faction, NetworkCommunicator requester)
        {
            if (faction.Rank < 2)
            {
                InformationManager.DisplayMessage(new InformationMessage("❌ Deine Fraktion muss mindestens Rang 2 sein, um Marschälle zu ernennen."));
                return false;
            }
            if (faction.lordId != requester.VirtualPlayer.ToPlayerId())
            {
                InformationManager.DisplayMessage(new InformationMessage("❌ Nur der Fraktionsführer kann Marschälle ernennen."));
                return false;
            }
            int maxMarshalls = faction.MaxMembers / 10;
            if (faction.marshalls.Count >= maxMarshalls)
            {
                InformationManager.DisplayMessage(new InformationMessage($"⚠️ Maximale Anzahl an Marschällen erreicht! (Max: {maxMarshalls})"));
                return false;
            }
            return true;
        }

        public static void TryAddMarshall(Faction faction, string marshallId, NetworkCommunicator requester)
        {
            if (!CanAddMarshall(faction, requester))
            {
                return;
            }
            if (faction.marshalls.Contains(marshallId))
            {
                InformationManager.DisplayMessage(new InformationMessage("⚠️ Dieser Spieler ist bereits ein Marschall."));
                return;
            }
            faction.marshalls.Add(marshallId);
            GameNetwork.BeginBroadcastModuleEvent();
            GameNetwork.WriteMessage(new AddMarshallIdToFaction(marshallId, faction.FactionIndex));
            GameNetwork.EndBroadcastModuleEvent();
            faction.AddToFactionLog($"🔹 {requester.UserName} hat {marshallId} zum Marschall ernannt.");
            InformationManager.DisplayMessage(new InformationMessage($"🎖️ {marshallId} wurde zum Marschall von {faction.name} ernannt!"));
        }
    }
    #endregion

    #region FactionLordPollClosed
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class FactionLordPollClosed : GameNetworkMessage
    {
        private const int LordChangeCost = 50000;
        private const int LordChangeCooldown = 86400;

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
            faction.Gold -= LordChangeCost;
            faction.lordId = newLord.VirtualPlayer.ToPlayerId();
            faction.pollUnlockedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + LordChangeCooldown;
            GameNetwork.BeginBroadcastModuleEvent();
            GameNetwork.WriteMessage(new FactionLordPollClosed(newLord, true, faction.FactionIndex));
            GameNetwork.EndBroadcastModuleEvent();
            faction.AddToFactionLog($"⚔️ {requester.UserName} hat {newLord.UserName} als neuen Lord ernannt.");
            InformationManager.DisplayMessage(new InformationMessage($"🎉 {newLord.UserName} ist jetzt der neue Lord von {faction.name}!"));
        }
    }
    #endregion

    #region FactionLordPollOpened
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class FactionLordPollOpened : GameNetworkMessage
    {
        private const int RequiredRank = 2;
        private const int PollCost = 50000;
        private const int PollCooldown = 86400;

        public FactionLordPollOpened() { }

        public FactionLordPollOpened(NetworkCommunicator pollCreator, NetworkCommunicator lordCandidate)
        {
            this.PollCreator = pollCreator;
            this.LordCandidate = lordCandidate;
        }

        public NetworkCommunicator PollCreator { get; private set; }
        public NetworkCommunicator LordCandidate { get; private set; }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.General;
        }

        protected override string OnGetLogFormat()
        {
            return $"🗳️ Lord-Wahl gestartet! Fraktion: {PollCreator.GetComponent<PersistentEmpireRepresentative>().GetFaction()?.name}";
        }

        protected override bool OnRead()
        {
            bool result = true;
            this.PollCreator = GameNetworkMessage.ReadNetworkPeerReferenceFromPacket(ref result);
            this.LordCandidate = GameNetworkMessage.ReadNetworkPeerReferenceFromPacket(ref result);
            return result;
        }

        protected override void OnWrite()
        {
            GameNetworkMessage.WriteNetworkPeerReferenceToPacket(this.PollCreator);
            GameNetworkMessage.WriteNetworkPeerReferenceToPacket(this.LordCandidate);
        }

        public static bool TryOpenPoll(NetworkCommunicator pollCreator, NetworkCommunicator lordCandidate)
        {
            PersistentEmpireRepresentative creatorRep = pollCreator.GetComponent<PersistentEmpireRepresentative>();
            PersistentEmpireRepresentative candidateRep = lordCandidate.GetComponent<PersistentEmpireRepresentative>();

            if (creatorRep == null || candidateRep == null || creatorRep.GetFaction() == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("⚠️ Fehler: Ungültige Fraktionsdaten."));
                return false;
            }

            Faction faction = creatorRep.GetFaction();

            if (faction.Rank < RequiredRank)
            {
                InformationManager.DisplayMessage(new InformationMessage("❌ Eure Fraktion muss mindestens Rang 2 haben, um eine Lord-Wahl zu starten."));
                return false;
            }

            if (faction.Gold < PollCost)
            {
                InformationManager.DisplayMessage(new InformationMessage("❌ Eure Fraktion hat nicht genug Gold für eine Lord-Wahl (50.000 Gold benötigt)."));
                return false;
            }

            if (creatorRep.GetFactionIndex() != candidateRep.GetFactionIndex())
            {
                InformationManager.DisplayMessage(new InformationMessage("❌ Der gewählte Lord-Kandidat ist nicht in eurer Fraktion."));
                return false;
            }

            if (faction.pollUnlockedAt > DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            {
                InformationManager.DisplayMessage(new InformationMessage("⚠️ Eure Fraktion kann erst nach 24 Stunden eine neue Wahl starten."));
                return false;
            }

            faction.Gold -= PollCost;
            faction.pollUnlockedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + PollCooldown;
            GameNetwork.BeginBroadcastModuleEvent();
            GameNetwork.WriteMessage(new FactionLordPollOpened(pollCreator, lordCandidate));
            GameNetwork.EndBroadcastModuleEvent();
            faction.AddToFactionLog($"🗳️ {pollCreator.UserName} hat eine Lord-Wahl für {lordCandidate.UserName} gestartet.");
            InformationManager.DisplayMessage(new InformationMessage($"🗳️ Eine Lord-Wahl wurde in der Fraktion {faction.name} gestartet. 50.000 Gold wurden abgezogen."));
            return true;
        }
    }
    #endregion

    #region FactionPollCancelled
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class FactionPollCancelled : GameNetworkMessage
    {
        public int FactionIndex { get; private set; }
        public string CancelledBy { get; private set; }

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
            GameNetwork.BeginBroadcastModuleEvent();
            GameNetwork.WriteMessage(new FactionPollCancelled(factionIndex, cancellerName));
            GameNetwork.EndBroadcastModuleEvent();
            faction.AddToFactionLog($"📜 Die Wahl wurde von {cancellerName} abgebrochen.");
            InformationManager.DisplayMessage(new InformationMessage($"🛑 Die laufende Abstimmung für die Fraktion {faction.name} wurde von {cancellerName} abgebrochen."));
            return true;
        }
    }
    #endregion

    #region FactionPollProgress
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class FactionPollProgress : GameNetworkMessage
    {
        public int FactionIndex { get; private set; }
        public int VotesAccepted { get; private set; }
        public int VotesRejected { get; private set; }
        public int RequiredVotes { get; private set; }

        private static Dictionary<int, HashSet<string>> voterRegistry = new Dictionary<int, HashSet<string>>();

        public FactionPollProgress() { }

        public FactionPollProgress(int factionIndex, int votesAccepted, int votesRejected, int requiredVotes)
        {
            this.FactionIndex = factionIndex;
            this.VotesAccepted = votesAccepted;
            this.VotesRejected = votesRejected;
            this.RequiredVotes = requiredVotes;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.Administration;
        }

        protected override string OnGetLogFormat()
        {
            return $"📊 Abstimmungsfortschritt für Fraktion {FactionIndex}: {VotesAccepted} Ja / {VotesRejected} Nein | Benötigt: {RequiredVotes}";
        }

        protected override bool OnRead()
        {
            bool result = true;
            this.FactionIndex = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(-1, 200), ref result);
            this.VotesAccepted = GameNetworkMessage.ReadIntFromPacket(CompressionBasic.PlayerCompressionInfo, ref result);
            this.VotesRejected = GameNetworkMessage.ReadIntFromPacket(CompressionBasic.PlayerCompressionInfo, ref result);
            this.RequiredVotes = GameNetworkMessage.ReadIntFromPacket(CompressionBasic.PlayerCompressionInfo, ref result);
            return result;
        }

        protected override void OnWrite()
        {
            GameNetworkMessage.WriteIntToPacket(this.FactionIndex, new CompressionInfo.Integer(-1, 200, true));
            GameNetworkMessage.WriteIntToPacket(this.VotesAccepted, CompressionBasic.PlayerCompressionInfo);
            GameNetworkMessage.WriteIntToPacket(this.VotesRejected, CompressionBasic.PlayerCompressionInfo);
            GameNetworkMessage.WriteIntToPacket(this.RequiredVotes, CompressionBasic.PlayerCompressionInfo);
        }

        public static void HandleVote(NetworkCommunicator voter, int factionIndex, bool accepted)
        {
            Faction faction = FactionManager.GetFactionByIndex(factionIndex);
            if (faction == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("⚠️ Fraktion nicht gefunden. Abstimmung nicht möglich."));
                return;
            }
            if (!voterRegistry.ContainsKey(factionIndex))
                voterRegistry[factionIndex] = new HashSet<string>();

            if (voterRegistry[factionIndex].Contains(voter.VirtualPlayer.ToPlayerId()))
            {
                InformationManager.DisplayMessage(new InformationMessage("❌ Du hast bereits abgestimmt!"));
                return;
            }
            voterRegistry[factionIndex].Add(voter.VirtualPlayer.ToPlayerId());
            if (accepted) faction.VotesAccepted++;
            else faction.VotesRejected++;
            int totalVotes = faction.VotesAccepted + faction.VotesRejected;
            int requiredVotes = faction.MaxMembers / 2 + 1;
            if (faction.VotesAccepted >= requiredVotes)
            {
                FinalizePoll(factionIndex, true);
                return;
            }
            if (faction.VotesRejected >= requiredVotes)
            {
                FinalizePoll(factionIndex, false);
                return;
            }
            BroadcastPollProgress(factionIndex, faction.VotesAccepted, faction.VotesRejected, requiredVotes);
        }

        public static void BroadcastPollProgress(int factionIndex, int votesAccepted, int votesRejected, int requiredVotes)
        {
            Faction faction = FactionManager.GetFactionByIndex(factionIndex);
            if (faction == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("⚠️ Fraktion nicht gefunden, Abstimmungsfortschritt nicht sendbar."));
                return;
            }
            GameNetwork.BeginBroadcastModuleEvent();
            GameNetwork.WriteMessage(new FactionPollProgress(factionIndex, votesAccepted, votesRejected, requiredVotes));
            GameNetwork.EndBroadcastModuleEvent();
            InformationManager.DisplayMessage(new InformationMessage($"📢 Abstimmung in {faction.name}: {votesAccepted} Ja / {votesRejected} Nein | Benötigt: {requiredVotes}"));
        }

        private static void FinalizePoll(int factionIndex, bool accepted)
        {
            Faction faction = FactionManager.GetFactionByIndex(factionIndex);
            if (faction == null) return;
            string resultMessage = accepted ? "✅ Wahl erfolgreich!" : "❌ Wahl abgelehnt!";
            InformationManager.DisplayMessage(new InformationMessage($"📢 Fraktion {faction.name}: {resultMessage}"));
            faction.VotesAccepted = 0;
            faction.VotesRejected = 0;
            voterRegistry[factionIndex].Clear();
            faction.AddToFactionLog($"📜 Abstimmung abgeschlossen: {resultMessage}");
            if (accepted && faction.PendingLordCandidate != null)
            {
                faction.lordId = faction.PendingLordCandidate.VirtualPlayer.ToPlayerId();
                faction.PendingLordCandidate = null;
                InformationManager.DisplayMessage(new InformationMessage($"👑 Neuer Lord der Fraktion {faction.name}: {faction.lordId}"));
            }
        }
    }
    #endregion

    #region FactionUpdateLord
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class FactionUpdateLord : GameNetworkMessage
    {
        public int FactionIndex { get; private set; }
        public NetworkCommunicator Player { get; private set; }

        private const int InfluenceCost = 500;

        public FactionUpdateLord() { }

        public FactionUpdateLord(int factionIndex, NetworkCommunicator player)
        {
            this.FactionIndex = factionIndex;
            this.Player = player;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.Administration;
        }

        protected override string OnGetLogFormat()
        {
            return $"⚔️ Fraktion {FactionIndex} hat einen neuen Lord: {Player.UserName}";
        }

        protected override bool OnRead()
        {
            bool result = true;
            this.FactionIndex = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(-1, 200, true), ref result);
            this.Player = GameNetworkMessage.ReadNetworkPeerReferenceFromPacket(ref result);
            return result;
        }

        protected override void OnWrite()
        {
            GameNetworkMessage.WriteIntToPacket(this.FactionIndex, new CompressionInfo.Integer(-1, 200, true));
            GameNetworkMessage.WriteNetworkPeerReferenceToPacket(this.Player);
        }

        public static void BroadcastNewLord(NetworkCommunicator requester, int factionIndex, NetworkCommunicator newLord)
        {
            Faction faction = FactionManager.GetFactionByIndex(factionIndex);
            if (faction == null)
            {
                InformationManager.DisplayMessage(new InformationMessage($"⚠️ Fraktion {factionIndex} nicht gefunden. Aktion abgebrochen."));
                return;
            }
            PersistentEmpireRepresentative requesterRep = requester.GetComponent<PersistentEmpireRepresentative>();
            PersistentEmpireRepresentative newLordRep = newLord.GetComponent<PersistentEmpireRepresentative>();
            if (requesterRep == null || newLordRep == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("⚠️ Fehler: Spielerreferenz ist null."));
                return;
            }
            if (faction.lordId != requester.VirtualPlayer.ToPlayerId() && !faction.marshalls.Contains(requester.VirtualPlayer.ToPlayerId()))
            {
                InformationManager.DisplayMessage(new InformationMessage($"❌ {requester.UserName} hat keine Berechtigung, einen neuen Lord zu ernennen."));
                return;
            }
            if (!requesterRep.ReduceIfHaveEnoughInfluence(InfluenceCost))
            {
                InformationManager.DisplayMessage(new InformationMessage($"❌ Nicht genug Einfluss! {InfluenceCost} Einfluss erforderlich."));
                return;
            }
            faction.lordId = newLord.VirtualPlayer.ToPlayerId();
            GameNetwork.BeginBroadcastModuleEvent();
            GameNetwork.WriteMessage(new FactionUpdateLord(factionIndex, newLord));
            GameNetwork.EndBroadcastModuleEvent();
            InformationManager.DisplayMessage(new InformationMessage($"👑 {newLord.UserName} wurde zum neuen Lord der Fraktion {faction.name} ernannt."));
            foreach (var member in faction.members)
            {
                InformationManager.DisplayMessage(new InformationMessage($"📢 {newLord.UserName} ist nun der neue Lord eurer Fraktion!"));
            }
            faction.AddToFactionLog($"📜 {newLord.UserName} wurde von {requester.UserName} zum neuen Lord ernannt.");
        }
    }
    #endregion

    #region FactionUpdateMarshall
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class FactionUpdateMarshall : GameNetworkMessage
    {
        public int FactionIndex { get; private set; }
        public NetworkCommunicator TargetPlayer { get; private set; }
        public bool IsMarshall { get; private set; }

        private const int InfluenceCost = 100;

        public FactionUpdateMarshall() { }

        public FactionUpdateMarshall(int factionIndex, NetworkCommunicator targetPlayer, bool isMarshall)
        {
            this.FactionIndex = factionIndex;
            this.TargetPlayer = targetPlayer;
            this.IsMarshall = isMarshall;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.Administration;
        }

        protected override string OnGetLogFormat()
        {
            return $"⚔️ Fraktion {FactionIndex}: {TargetPlayer.UserName} ist nun ein Marschall: {IsMarshall}";
        }

        protected override bool OnRead()
        {
            bool result = true;
            this.FactionIndex = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(-1, 200, true), ref result);
            this.TargetPlayer = GameNetworkMessage.ReadNetworkPeerReferenceFromPacket(ref result);
            this.IsMarshall = GameNetworkMessage.ReadBoolFromPacket(ref result);
            return result;
        }

        protected override void OnWrite()
        {
            GameNetworkMessage.WriteIntToPacket(this.FactionIndex, new CompressionInfo.Integer(-1, 200, true));
            GameNetworkMessage.WriteNetworkPeerReferenceToPacket(this.TargetPlayer);
            GameNetworkMessage.WriteBoolToPacket(this.IsMarshall);
        }

        public static void BroadcastMarshallUpdate(NetworkCommunicator requester, int factionIndex, NetworkCommunicator targetPlayer, bool isMarshall)
        {
            Faction faction = FactionManager.GetFactionByIndex(factionIndex);
            if (faction == null)
            {
                InformationManager.DisplayMessage(new InformationMessage($"⚠️ Fraktion {factionIndex} nicht gefunden. Aktion abgebrochen."));
                return;
            }
            PersistentEmpireRepresentative requesterRep = requester.GetComponent<PersistentEmpireRepresentative>();
            PersistentEmpireRepresentative targetRep = targetPlayer.GetComponent<PersistentEmpireRepresentative>();
            if (requesterRep == null || targetRep == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("⚠️ Fehler: Spielerreferenz ist null."));
                return;
            }
            if (faction.lordId != requester.VirtualPlayer.ToPlayerId() && !faction.marshalls.Contains(requester.VirtualPlayer.ToPlayerId()))
            {
                InformationManager.DisplayMessage(new InformationMessage($"❌ {requester.UserName} hat keine Berechtigung, Marschälle zu ernennen oder zu entfernen."));
                return;
            }
            if (isMarshall && !requesterRep.ReduceIfHaveEnoughInfluence(InfluenceCost))
            {
                InformationManager.DisplayMessage(new InformationMessage($"❌ Nicht genug Einfluss! {InfluenceCost} Einfluss erforderlich."));
                return;
            }
            if (isMarshall)
            {
                if (!faction.marshalls.Contains(targetPlayer.VirtualPlayer.ToPlayerId()))
                {
                    faction.marshalls.Add(targetPlayer.VirtualPlayer.ToPlayerId());
                }
            }
            else
            {
                faction.marshalls.Remove(targetPlayer.VirtualPlayer.ToPlayerId());
            }
            GameNetwork.BeginBroadcastModuleEvent();
            GameNetwork.WriteMessage(new FactionUpdateMarshall(factionIndex, targetPlayer, isMarshall));
            GameNetwork.EndBroadcastModuleEvent();
            string statusMessage = isMarshall ? "zum Marschall befördert" : "vom Marschall-Rang entfernt";
            InformationManager.DisplayMessage(new InformationMessage($"📢 {targetPlayer.UserName} wurde {statusMessage} in der Fraktion {faction.name}."));
            foreach (var member in faction.members)
            {
                InformationManager.DisplayMessage(new InformationMessage($"📢 {targetPlayer.UserName} ist jetzt {statusMessage}."));
            }
            faction.AddToFactionLog($"📜 {targetPlayer.UserName} wurde {statusMessage} von {requester.UserName}.");
        }
    }
    #endregion

    #region SyncFaction
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
        {
            return MultiplayerMessageFilter.Mission;
        }

        protected override string OnGetLogFormat()
        {
            return $"Syncing Faction {FactionIndex}: {Faction?.name ?? "Unknown"}";
        }

        protected override bool OnRead()
        {
            bool result = true;
            this.FactionIndex = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(-1, 200, true), ref result);
            string factionName = GameNetworkMessage.ReadStringFromPacket(ref result);
            Team factionTeam = Mission.MissionNetworkHelper.GetTeamFromTeamIndex(GameNetworkMessage.ReadTeamIndexFromPacket(ref result));
            string bannerKey = PENetworkModule.ReadBannerCodeFromPacket(ref result);
            string lordId = GameNetworkMessage.ReadStringFromPacket(ref result);
            int factionRank = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(1, 5, true), ref result);
            int maxMembers = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(10, 100, true), ref result);
            bool isVassal = GameNetworkMessage.ReadBoolFromPacket(ref result);
            int overlordFactionIndex = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(-1, 200, true), ref result);
            float taxRate = GameNetworkMessage.ReadFloatFromPacket(ref result);

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

            int warDeclarationCount = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 200, true), ref result);
            for (int i = 0; i < warDeclarationCount; i++)
            {
                this.Faction.warDeclaredTo.Add(GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 200, true), ref result));
            }
            int allianceCount = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 200, true), ref result);
            for (int i = 0; i < allianceCount; i++)
            {
                this.Faction.alliedFactions.Add(GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 200, true), ref result));
            }
            int tradeAgreementCount = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 200, true), ref result);
            for (int i = 0; i < tradeAgreementCount; i++)
            {
                this.Faction.tradeAgreements.Add(GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 200, true), ref result));
            }
            return result;
        }

        protected override void OnWrite()
        {
            GameNetworkMessage.WriteIntToPacket(this.FactionIndex, new CompressionInfo.Integer(-1, 200, true));
            GameNetworkMessage.WriteStringToPacket(this.Faction.name);
            GameNetworkMessage.WriteTeamIndexToPacket(this.Faction.team.TeamIndex);
            PENetworkModule.WriteBannerCodeToPacket(this.Faction.banner.Serialize());
            GameNetworkMessage.WriteStringToPacket(this.Faction.lordId);
            GameNetworkMessage.WriteIntToPacket(this.Faction.Rank, new CompressionInfo.Integer(1, 5, true));
            GameNetworkMessage.WriteIntToPacket(this.Faction.MaxMembers, new CompressionInfo.Integer(10, 100, true));
            GameNetworkMessage.WriteBoolToPacket(this.Faction.IsVassal);
            GameNetworkMessage.WriteIntToPacket(this.Faction.OverlordFactionIndex, new CompressionInfo.Integer(-1, 200, true));
            GameNetworkMessage.WriteFloatToPacket(this.Faction.TaxRate);

            int warDeclarationCount = this.Faction.warDeclaredTo.Count;
            GameNetworkMessage.WriteIntToPacket(warDeclarationCount, new CompressionInfo.Integer(0, 200, true));
            for (int i = 0; i < warDeclarationCount; i++)
            {
                GameNetworkMessage.WriteIntToPacket(this.Faction.warDeclaredTo[i], new CompressionInfo.Integer(0, 200, true));
            }
            int allianceCount = this.Faction.alliedFactions.Count;
            GameNetworkMessage.WriteIntToPacket(allianceCount, new CompressionInfo.Integer(0, 200, true));
            for (int i = 0; i < allianceCount; i++)
            {
                GameNetworkMessage.WriteIntToPacket(this.Faction.alliedFactions[i], new CompressionInfo.Integer(0, 200, true));
            }
            int tradeAgreementCount = this.Faction.tradeAgreements.Count;
            GameNetworkMessage.WriteIntToPacket(tradeAgreementCount, new CompressionInfo.Integer(0, 200, true));
            for (int i = 0; i < tradeAgreementCount; i++)
            {
                GameNetworkMessage.WriteIntToPacket(this.Faction.tradeAgreements[i], new CompressionInfo.Integer(0, 200, true));
            }
        }
    }
    #endregion

    #region Allgemeine Fraktions-Nachrichten
    // Bereits zuvor integrierte Nachrichten (AddFaction, UpdateFactionFromServer, etc.)
    // (siehe vorheriges Beispiel – hier stehen alle bestehenden Klassen in eigenen Regions)
    #endregion

    #region Friedens-, Beitritts- und Synchronisations-Nachrichten

    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class PeaceDecleration : GameNetworkMessage
    {
        public int PeaceDeclarerIndex { get; set; }
        public int PeaceDeclaredTo { get; set; }
        public PeaceDecleration() { }
        public PeaceDecleration(int peaceDeclarer, int peaceDeclaredTo)
        {
            this.PeaceDeclarerIndex = peaceDeclarer;
            this.PeaceDeclaredTo = peaceDeclaredTo;
        }
        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.Administration;
        protected override string OnGetLogFormat() => "PeaceDecleration";
        protected override bool OnRead()
        {
            bool result = true;
            this.PeaceDeclarerIndex = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 200, true), ref result);
            this.PeaceDeclaredTo = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 200, true), ref result);
            return result;
        }
        protected override void OnWrite()
        {
            GameNetworkMessage.WriteIntToPacket(this.PeaceDeclarerIndex, new CompressionInfo.Integer(0, 200, true));
            GameNetworkMessage.WriteIntToPacket(this.PeaceDeclaredTo, new CompressionInfo.Integer(0, 200, true));
        }
    }

    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class PlayerJoinedFaction : GameNetworkMessage
    {
        public int FactionIndex { get; private set; }
        public int JoinedFrom { get; private set; }
        public NetworkCommunicator Player { get; private set; }

        private const int InfluenceCostForSwitch = 50;
        private const int GoldBonusForJoining = 100;

        public PlayerJoinedFaction() { }
        public PlayerJoinedFaction(int factionIndex, int joinedFrom, NetworkCommunicator player)
        {
            this.FactionIndex = factionIndex;
            this.JoinedFrom = joinedFrom;
            this.Player = player;
        }
        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.General;
        protected override string OnGetLogFormat() => $"📢 Spieler {Player.UserName} ist Fraktion {FactionIndex} beigetreten (vorher in {JoinedFrom})";
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
    }

    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class SyncCastleBanner : GameNetworkMessage
    {
        public PE_CastleBanner CastleBanner;
        public int FactionIndex;
        public SyncCastleBanner() { }
        public SyncCastleBanner(PE_CastleBanner castleBanner, int factionIndex)
        {
            this.CastleBanner = castleBanner;
            this.FactionIndex = factionIndex;
        }
        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.Mission;
        protected override string OnGetLogFormat() => "Sync castle banner";
        protected override bool OnRead()
        {
            bool result = true;
            this.CastleBanner = (PE_CastleBanner)Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(GameNetworkMessage.ReadMissionObjectIdFromPacket(ref result));
            this.FactionIndex = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 200, true), ref result);
            return result;
        }
        protected override void OnWrite()
        {
            GameNetworkMessage.WriteMissionObjectIdToPacket(this.CastleBanner.Id);
            GameNetworkMessage.WriteIntToPacket(this.FactionIndex, new CompressionInfo.Integer(0, 200, true));
        }
    }

    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class SyncFactionKey : GameNetworkMessage
    {
        public int FactionIndex { get; private set; }
        public string PlayerId { get; private set; }
        public FactionKeyType KeyType { get; private set; }
        public int PlayerRank { get; private set; }
        public bool IsVassal { get; private set; }

        public SyncFactionKey() { }
        public SyncFactionKey(int factionIndex, string playerId, FactionKeyType keyType, int playerRank, bool isVassal)
        {
            this.FactionIndex = factionIndex;
            this.PlayerId = string.IsNullOrEmpty(playerId) ? "Unknown_Player" : playerId;
            this.KeyType = keyType;
            this.PlayerRank = playerRank;
            this.IsVassal = isVassal;
        }
        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.None;
        protected override string OnGetLogFormat() => $"SyncFactionKey | Faction: {FactionIndex}, Player: {PlayerId}, KeyType: {KeyType}, Rank: {PlayerRank}, IsVassal: {IsVassal}";
        protected override bool OnRead()
        {
            bool result = true;
            this.FactionIndex = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 200, true), ref result);
            this.PlayerId = GameNetworkMessage.ReadStringFromPacket(ref result);
            this.KeyType = (FactionKeyType)GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 10, true), ref result);
            this.PlayerRank = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(1, 5, true), ref result);
            this.IsVassal = GameNetworkMessage.ReadBoolFromPacket(ref result);
            if (string.IsNullOrEmpty(this.PlayerId))
                this.PlayerId = "Unknown_Player";
            return result;
        }
        protected override void OnWrite()
        {
            GameNetworkMessage.WriteIntToPacket(this.FactionIndex, new CompressionInfo.Integer(0, 200, true));
            GameNetworkMessage.WriteStringToPacket(this.PlayerId);
            GameNetworkMessage.WriteIntToPacket((int)this.KeyType, new CompressionInfo.Integer(0, 10, true));
            GameNetworkMessage.WriteIntToPacket(this.PlayerRank, new CompressionInfo.Integer(1, 5, true));
            GameNetworkMessage.WriteBoolToPacket(this.IsVassal);
        }
    }

    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class SyncMember : GameNetworkMessage
    {
        public NetworkCommunicator Peer;
        public int FactionIndex;
        public bool IsMarshall;
        public SyncMember() { }
        public SyncMember(NetworkCommunicator peer, int factionIndex, bool isMarshall)
        {
            this.Peer = peer;
            this.FactionIndex = factionIndex;
            this.IsMarshall = isMarshall;
        }
        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.None;
        protected override string OnGetLogFormat() => "Sync members";
        protected override bool OnRead()
        {
            bool result = true;
            this.Peer = GameNetworkMessage.ReadNetworkPeerReferenceFromPacket(ref result);
            this.FactionIndex = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(-1, 200, true), ref result);
            this.IsMarshall = GameNetworkMessage.ReadBoolFromPacket(ref result);
            return result;
        }
        protected override void OnWrite()
        {
            GameNetworkMessage.WriteNetworkPeerReferenceToPacket(this.Peer);
            GameNetworkMessage.WriteIntToPacket(this.FactionIndex, new CompressionInfo.Integer(-1, 200, true));
            GameNetworkMessage.WriteBoolToPacket(this.IsMarshall);
        }
    }

    #endregion

    #region Krieg- und Deklarations-Nachrichten

    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class WarDecleration : GameNetworkMessage
    {
        public int WarDeclarerIndex { get; set; }
        public int WarDeclaredTo { get; set; }
        public WarDecleration() { }
        public WarDecleration(int warDeclarerIndex, int warDeclaredTo)
        {
            this.WarDeclarerIndex = warDeclarerIndex;
            this.WarDeclaredTo = warDeclaredTo;
        }
        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.Administration;
        protected override string OnGetLogFormat() => "WarDecleration";
        protected override bool OnRead()
        {
            bool result = true;
            this.WarDeclarerIndex = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 200, true), ref result);
            this.WarDeclaredTo = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 200, true), ref result);
            return result;
        }
        protected override void OnWrite()
        {
            GameNetworkMessage.WriteIntToPacket(this.WarDeclarerIndex, new CompressionInfo.Integer(0, 200, true));
            GameNetworkMessage.WriteIntToPacket(this.WarDeclaredTo, new CompressionInfo.Integer(0, 200, true));
        }
    }

    #endregion
}
