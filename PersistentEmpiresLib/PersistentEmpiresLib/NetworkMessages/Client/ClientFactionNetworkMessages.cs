using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using PersistentEmpiresLib.Factions;
using PersistentEmpiresLib.Helpers;

namespace PersistentEmpiresLib.NetworkMessages.Client
{
    #region Faction Management Requests

    // Fordert Fraktionsschlüssel an.
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
    public sealed class RequestFactionKeys : GameNetworkMessage
    {
        public enum FactionKeyType
        {
            Door = 0,
            Chest = 1,
            Armory = 2,
            Treasury = 3,
            Prison = 4
        }

        public FactionKeyType KeyType { get; private set; }

        public RequestFactionKeys() { }
        public RequestFactionKeys(FactionKeyType keyType)
        {
            KeyType = keyType;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.None;
        protected override string OnGetLogFormat() => $"Requesting Faction Key: {KeyType}";
        protected override bool OnRead()
        {
            bool result = true;
            KeyType = (FactionKeyType)GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 4, true), ref result);
            return result;
        }
        protected override void OnWrite()
        {
            GameNetworkMessage.WriteIntToPacket((int)KeyType, new CompressionInfo.Integer(0, 4, true));
        }
    }

    // Fordert einen Fraktionswechsel (Lordship Transfer) an.
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
    public sealed class RequestLordshipTransfer : GameNetworkMessage
    {
        public NetworkCommunicator TargetPlayer { get; private set; }

        public RequestLordshipTransfer() { }
        public RequestLordshipTransfer(NetworkCommunicator targetPlayer)
        {
            TargetPlayer = targetPlayer;
        }
        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.None;
        protected override string OnGetLogFormat() => "Lordship transfer request";
        protected override bool OnRead()
        {
            bool result = true;
            TargetPlayer = GameNetworkMessage.ReadNetworkPeerReferenceFromPacket(ref result);
            return result;
        }
        protected override void OnWrite()
        {
            GameNetworkMessage.WriteNetworkPeerReferenceToPacket(TargetPlayer);
        }
    }

    // Aktualisiert das Fraktionsbanner.
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
    public sealed class UpdateFactionBanner : GameNetworkMessage
    {
        public string BannerCode { get; private set; }

        public UpdateFactionBanner() { }
        public UpdateFactionBanner(string bannerCode)
        {
            BannerCode = bannerCode;
        }
        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.None;
        protected override string OnGetLogFormat() => $"Request update on faction banner: {BannerCode}";
        protected override bool OnRead()
        {
            bool result = true;
            BannerCode = PENetworkModule.ReadBannerCodeFromPacket(ref result);
            return result;
        }
        protected override void OnWrite()
        {
            PENetworkModule.WriteBannerCodeToPacket(BannerCode);
        }

        // Beispiel-Methode, die auch direkt auf dem Client genutzt werden könnte.
        public static void TryChangeFactionBanner(NetworkCommunicator sender, string newBannerCode)
        {
            PersistentEmpireRepresentative rep = sender.GetComponent<PersistentEmpireRepresentative>();
            if (rep == null || rep.GetFaction() == null) return;
            Faction faction = rep.GetFaction();

            if (faction.lordId != sender.VirtualPlayer.ToPlayerId() && !faction.marshalls.Contains(sender.VirtualPlayer.ToPlayerId()))
            {
                InformationManager.DisplayMessage(new InformationMessage("Only the Lord or a Marshall can change the faction banner."));
                return;
            }
            if (string.IsNullOrEmpty(newBannerCode) || newBannerCode.Length < 10)
            {
                InformationManager.DisplayMessage(new InformationMessage("Invalid banner format."));
                return;
            }
            if (faction.Gold < 1000)
            {
                InformationManager.DisplayMessage(new InformationMessage("Your faction does not have enough gold to change the banner."));
                return;
            }
            faction.Gold -= 1000;
            faction.banner = new Banner(newBannerCode);
            GameNetwork.BeginBroadcastModuleEvent();
            GameNetwork.WriteMessage(new UpdateFactionBanner(newBannerCode));
            GameNetwork.EndBroadcastModuleEvent();
            InformationManager.DisplayMessage(new InformationMessage($"Faction banner updated. Cost: 1000 gold."));
        }
    }

    // Aktualisiert den Fraktionsnamen.
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
    public sealed class UpdateFactionName : GameNetworkMessage
    {
        public string NewName { get; private set; }
        public UpdateFactionName() { }
        public UpdateFactionName(string newName)
        {
            NewName = newName;
        }
        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.None;
        protected override string OnGetLogFormat() => $"Change Faction Name Requested: {NewName}";
        protected override bool OnRead()
        {
            bool result = true;
            NewName = GameNetworkMessage.ReadStringFromPacket(ref result);
            return result;
        }
        protected override void OnWrite()
        {
            GameNetworkMessage.WriteStringToPacket(NewName);
        }
        public static void TryChangeFactionName(NetworkCommunicator sender, string newName)
        {
            PersistentEmpireRepresentative rep = sender.GetComponent<PersistentEmpireRepresentative>();
            if (rep == null || rep.GetFaction() == null) return;
            Faction faction = rep.GetFaction();
            if (faction.lordId != sender.VirtualPlayer.ToPlayerId() && !faction.marshalls.Contains(sender.VirtualPlayer.ToPlayerId()))
            {
                InformationManager.DisplayMessage(new InformationMessage("Only the Lord or a Marshall can change the faction name."));
                return;
            }
            if (FactionManager.GetAllFactions().Exists(f => f.name == newName))
            {
                InformationManager.DisplayMessage(new InformationMessage("This faction name is already taken."));
                return;
            }
            if (faction.Gold < 500)
            {
                InformationManager.DisplayMessage(new InformationMessage("Your faction does not have enough gold to change the name."));
                return;
            }
            faction.Gold -= 500;
            faction.name = newName;
            GameNetwork.BeginBroadcastModuleEvent();
            GameNetwork.WriteMessage(new UpdateFactionName(newName));
            GameNetwork.EndBroadcastModuleEvent();
            InformationManager.DisplayMessage(new InformationMessage($"Faction name changed to: {newName}. Cost: 500 gold."));
        }
    }

    #endregion

    #region Conquest, Diplomacy & Polling

    // Erklärt ein Eroberungsanliegen (Conquest) – (Verwende nur eine Variante, da doppelte Versionen vorhanden waren)
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
    public sealed class DeclareConquestRequest : GameNetworkMessage
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
            return faction.ControlledTerritories.Count < maxTerritories;
        }

        public bool ValidateVassalOption(Faction faction)
        {
            return faction.CanCreateVassal();
        }
    }

    // Erklärt eine Diplomatie-Aktion.
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
    public sealed class DeclareDiplomacyRequest : GameNetworkMessage
    {
        public enum DiplomacyType
        {
            DeclareWar,
            MakePeace,
            Alliance,
            TradeAgreement,
            Vassal
        }

        public int FactionIndex { get; set; }
        public DiplomacyType Action { get; set; }

        public DeclareDiplomacyRequest() { }
        public DeclareDiplomacyRequest(int factionIndex, DiplomacyType action)
        {
            FactionIndex = factionIndex;
            Action = action;
        }
        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.Administration;
        protected override string OnGetLogFormat() => $"Diplomacy Action: {Action} with Faction {FactionIndex}";
        protected override bool OnRead()
        {
            bool result = true;
            FactionIndex = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 200, true), ref result);
            Action = (DiplomacyType)GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 5, true), ref result);
            return result;
        }
        protected override void OnWrite()
        {
            GameNetworkMessage.WriteIntToPacket(FactionIndex, new CompressionInfo.Integer(0, 200, true));
            GameNetworkMessage.WriteIntToPacket((int)Action, new CompressionInfo.Integer(0, 5, true));
        }
    }

    // Anfrage zur Zuweisung eines Marschalls.
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
    public sealed class FactionAssignMarshall : GameNetworkMessage
    {
        public NetworkCommunicator TargetPlayer { get; private set; }
        public FactionAssignMarshall() { }
        public FactionAssignMarshall(NetworkCommunicator targetPlayer)
        {
            TargetPlayer = targetPlayer;
        }
        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.Administration;
        protected override string OnGetLogFormat() => $"Assigning Marshall to faction | Player: {TargetPlayer?.UserName ?? "Unknown"}";
        protected override bool OnRead()
        {
            bool result = true;
            TargetPlayer = GameNetworkMessage.ReadNetworkPeerReferenceFromPacket(ref result);
            return result;
        }
        protected override void OnWrite()
        {
            GameNetworkMessage.WriteNetworkPeerReferenceToPacket(TargetPlayer);
        }
        // Die Logik zur Zuweisung (TryAssignMarshall) ist serverseitig implementiert.
    }

    // Anfrage zur Einleitung einer Lord-Wahl.
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
    public sealed class FactionLordPollRequest : GameNetworkMessage
    {
        public NetworkCommunicator Player { get; private set; }
        public FactionLordPollRequest() { }
        public FactionLordPollRequest(NetworkCommunicator player)
        {
            Player = player;
        }
        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.General;
        protected override string OnGetLogFormat() => $"Faction Lord Poll Request | Requested by: {Player?.UserName ?? "Unknown"}";
        protected override bool OnRead()
        {
            bool result = true;
            Player = GameNetworkMessage.ReadNetworkPeerReferenceFromPacket(ref result);
            return result;
        }
        protected override void OnWrite()
        {
            GameNetworkMessage.WriteNetworkPeerReferenceToPacket(Player);
        }
        // Die Logik zum Starten der Wahl (TryStartLordPoll) wird serverseitig abgehandelt.
    }

    // Antwort auf eine Fraktionsabstimmung.
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
    public sealed class FactionPollResponse : GameNetworkMessage
    {
        public bool Accepted { get; private set; }
        public int FactionIndex { get; private set; }
        public NetworkCommunicator Voter { get; private set; }

        public FactionPollResponse() { }
        public FactionPollResponse(int factionIndex, NetworkCommunicator voter, bool accepted)
        {
            FactionIndex = factionIndex;
            Voter = voter;
            Accepted = accepted;
        }
        protected override bool OnRead()
        {
            bool result = true;
            FactionIndex = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(-1, 200, true), ref result);
            Voter = GameNetworkMessage.ReadNetworkPeerReferenceFromPacket(ref result);
            Accepted = GameNetworkMessage.ReadBoolFromPacket(ref result);
            return result;
        }
        protected override void OnWrite()
        {
            GameNetworkMessage.WriteIntToPacket(FactionIndex, new CompressionInfo.Integer(-1, 200, true));
            GameNetworkMessage.WriteNetworkPeerReferenceToPacket(Voter);
            GameNetworkMessage.WriteBoolToPacket(Accepted);
        }
        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.Administration;
        protected override string OnGetLogFormat() => $"Faction poll response received from {Voter?.UserName ?? "Unknown"} for faction {FactionIndex}: {(Accepted ? "Accepted" : "Not accepted")}";
    }

    #endregion
}
