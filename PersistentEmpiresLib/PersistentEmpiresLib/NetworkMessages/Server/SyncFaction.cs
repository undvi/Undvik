using PersistentEmpiresLib.Factions;
using PersistentEmpiresLib.Helpers;
using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace PersistentEmpiresLib.NetworkMessages.Server
{
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
}
