using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using PersistentEmpiresLib.Factions;
using PersistentEmpiresLib.Helpers;
using System.Collections.Generic;

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
            Vassal
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
            this.Action = (DiplomacyType)GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 5, true), ref result);
            return result;
        }

        protected override void OnWrite()
        {
            GameNetworkMessage.WriteIntToPacket(this.FactionIndex, new CompressionInfo.Integer(0, 200, true));
            GameNetworkMessage.WriteIntToPacket((int)this.Action, new CompressionInfo.Integer(0, 5, true));
        }
    }

    public static class DiplomacyHandler
    {
        public static void HandleDiplomacyRequest(NetworkCommunicator sender, DeclareDiplomacyRequest request)
        {
            PersistentEmpireRepresentative rep = sender.GetComponent<PersistentEmpireRepresentative>();
            if (rep == null || rep.GetFaction() == null) return;

            Faction senderFaction = rep.GetFaction();
            Faction targetFaction = FactionManager.GetFactionByIndex(request.FactionIndex);
            if (targetFaction == null) return;

            switch (request.Action)
            {
                case DeclareDiplomacyRequest.DiplomacyType.DeclareWar:
                    senderFaction.DeclareWar(targetFaction);
                    break;
                case DeclareDiplomacyRequest.DiplomacyType.MakePeace:
                    senderFaction.MakePeace(targetFaction);
                    break;
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
