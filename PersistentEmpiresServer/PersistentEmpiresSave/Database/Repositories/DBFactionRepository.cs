using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using PersistentEmpiresLib.Factions;
using PersistentEmpiresLib.Helpers;
using System.Collections.Generic;
using System;

namespace PersistentEmpiresLib.NetworkMessages.Server
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
    public sealed class DeclareDiplomacyRequest : GameNetworkMessage
    {
        public enum DiplomacyType
        {
            DeclareWar,
            MakePeace,
            Alliance,
            TradeAgreement,
            Vassal,
            BreakAlliance,
            CancelTradeAgreement,
            EndVassalage
        }

        public int FactionIndex { get; set; }
        public DiplomacyType Action { get; set; }

        public DeclareDiplomacyRequest() { }
        public DeclareDiplomacyRequest(int factionIndex, DiplomacyType action)
        {
            this.FactionIndex = factionIndex;
            this.Action = action;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.Administration;
        }

        protected override string OnGetLogFormat()
        {
            return $"Diplomacy Action: {Action} with Faction {FactionIndex}";
        }

        protected override bool OnRead()
        {
            bool result = true;
            this.FactionIndex = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 200, true), ref result);
            this.Action = (DiplomacyType)GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 8, true), ref result);
            return result;
        }

        protected override void OnWrite()
        {
            GameNetworkMessage.WriteIntToPacket(this.FactionIndex, new CompressionInfo.Integer(0, 200, true));
            GameNetworkMessage.WriteIntToPacket((int)this.Action, new CompressionInfo.Integer(0, 8, true));
        }
    }

    public sealed class DiplomacyResponse : GameNetworkMessage
    {
        public int FactionIndex { get; set; }
        public DeclareDiplomacyRequest.DiplomacyType Action { get; set; }
        public bool Accepted { get; set; }

        public DiplomacyResponse() { }
        public DiplomacyResponse(int factionIndex, DeclareDiplomacyRequest.DiplomacyType action, bool accepted)
        {
            this.FactionIndex = factionIndex;
            this.Action = action;
            this.Accepted = accepted;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.Administration;
        }

        protected override string OnGetLogFormat()
        {
            return $"Diplomacy Response: {Action} with Faction {FactionIndex}, Accepted: {Accepted}";
        }

        protected override bool OnRead()
        {
            bool result = true;
            this.FactionIndex = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 200, true), ref result);
            this.Action = (DeclareDiplomacyRequest.DiplomacyType)GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 8, true), ref result);
            this.Accepted = GameNetworkMessage.ReadBoolFromPacket(ref result);
            return result;
        }

        protected override void OnWrite()
        {
            GameNetworkMessage.WriteIntToPacket(this.FactionIndex, new CompressionInfo.Integer(0, 200, true));
            GameNetworkMessage.WriteIntToPacket((int)this.Action, new CompressionInfo.Integer(0, 8, true));
            GameNetworkMessage.WriteBoolToPacket(this.Accepted);
        }
    }

    public static class DiplomacyHandler
    {
        private static Dictionary<int, long> lastDiplomacyActions = new Dictionary<int, long>();
        private const int DiplomacyCooldown = 3600; // 1 hour cooldown in seconds

        public static void HandleDiplomacyRequest(NetworkCommunicator sender, DeclareDiplomacyRequest request)
        {
            PersistentEmpireRepresentative rep = sender.GetComponent<PersistentEmpireRepresentative>();
            if (rep == null || rep.GetFaction() == null) return;

            Faction senderFaction = rep.GetFaction();
            Faction targetFaction = FactionManager.GetFactionByIndex(request.FactionIndex);
            if (targetFaction == null) return;

            if (senderFaction.lordId != sender.VirtualPlayer.ToPlayerId() && !senderFaction.marshalls.Contains(sender.VirtualPlayer.ToPlayerId()))
            {
                return;
            }

            long currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (lastDiplomacyActions.TryGetValue(senderFaction.FactionIndex, out long lastActionTime) && (currentTime - lastActionTime) < DiplomacyCooldown)
            {
                return;
            }
            lastDiplomacyActions[senderFaction.FactionIndex] = currentTime;

            switch (request.Action)
            {
                case DeclareDiplomacyRequest.DiplomacyType.DeclareWar:
                    senderFaction.DeclareWar(targetFaction);
                    break;
                case DeclareDiplomacyRequest.DiplomacyType.MakePeace:
                    senderFaction.MakePeace(targetFaction);
                    break;
                case DeclareDiplomacyRequest.DiplomacyType.Alliance:
                    senderFaction.ProposeAlliance(targetFaction);
                    break;
                case DeclareDiplomacyRequest.DiplomacyType.TradeAgreement:
                    senderFaction.ProposeTradeAgreement(targetFaction);
                    break;
                case DeclareDiplomacyRequest.DiplomacyType.Vassal:
                    senderFaction.ProposeVassalage(targetFaction);
                    break;
                case DeclareDiplomacyRequest.DiplomacyType.BreakAlliance:
                    senderFaction.BreakAlliance(targetFaction);
                    break;
                case DeclareDiplomacyRequest.DiplomacyType.CancelTradeAgreement:
                    senderFaction.CancelTradeAgreement(targetFaction);
                    break;
                case DeclareDiplomacyRequest.DiplomacyType.EndVassalage:
                    senderFaction.EndVassalage(targetFaction);
                    break;
            }
        }

        public static void HandleDiplomacyResponse(NetworkCommunicator sender, DiplomacyResponse response)
        {
            Faction senderFaction = FactionManager.GetFactionByIndex(sender.GetComponent<PersistentEmpireRepresentative>().GetFactionIndex());
            Faction targetFaction = FactionManager.GetFactionByIndex(response.FactionIndex);
            if (senderFaction == null || targetFaction == null) return;

            if (response.Accepted)
            {
                switch (response.Action)
                {
                    case DeclareDiplomacyRequest.DiplomacyType.Alliance:
                        senderFaction.FormAlliance(targetFaction);
                        break;
                    case DeclareDiplomacyRequest.DiplomacyType.TradeAgreement:
                        senderFaction.EstablishTradeAgreement(targetFaction);
                        break;
                    case DeclareDiplomacyRequest.DiplomacyType.Vassal:
                        senderFaction.MakeVassal(targetFaction);
                        break;
                }
            }
        }
    }
}
