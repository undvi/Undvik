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
        public Faction faction { get; set; }
        public int factionIndex { get; set; }

        public AddFaction(Faction faction, int factionIndex)
        {
            this.faction = faction;
            this.factionIndex = factionIndex;
        }

        public AddFaction()
        {
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.General;
        }

        protected override string OnGetLogFormat()
        {
            return "Faction added.";
        }

        protected override bool OnRead()
        {
            bool result = true;
            this.factionIndex = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(-1, 200, true), ref result);
            BasicCultureObject bco = (BasicCultureObject)GameNetworkMessage.ReadObjectReferenceFromPacket(MBObjectManager.Instance, CompressionBasic.GUIDCompressionInfo, ref result);
            string name = GameNetworkMessage.ReadStringFromPacket(ref result);
            Team team = Mission.MissionNetworkHelper.GetTeamFromTeamIndex(GameNetworkMessage.ReadTeamIndexFromPacket(ref result));
            string BannerKey = PENetworkModule.ReadBannerCodeFromPacket(ref result);
            int rank = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(1, 5, true), ref result);
            int gold = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, int.MaxValue, true), ref result);
            int maxMembers = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(10, 100, true), ref result);

            int memberLength = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 500, true), ref result);
            this.faction = new Faction(bco, new Banner(BannerKey), name)
            {
                Rank = rank,
                Gold = gold,
                MaxMembers = maxMembers
            };
            this.faction.team = team;

            for (int i = 0; i < memberLength; i++)
            {
                var member = GameNetworkMessage.ReadNetworkPeerReferenceFromPacket(ref result);
                if (!faction.members.Contains(member))
                    faction.members.Add(member);
            }

            return result;
        }

        protected override void OnWrite()
        {
            GameNetworkMessage.WriteIntToPacket(factionIndex, new CompressionInfo.Integer(-1, 200, true));
            GameNetworkMessage.WriteObjectReferenceToPacket(faction.basicCultureObject, CompressionBasic.GUIDCompressionInfo);
            GameNetworkMessage.WriteStringToPacket(faction.name);
            GameNetworkMessage.WriteTeamIndexToPacket(faction.team.TeamIndex);
            PENetworkModule.WriteBannerCodeToPacket(faction.banner.Serialize());
            GameNetworkMessage.WriteIntToPacket(faction.Rank, new CompressionInfo.Integer(1, 5, true));
            GameNetworkMessage.WriteIntToPacket(faction.Gold, new CompressionInfo.Integer(0, int.MaxValue, true));
            GameNetworkMessage.WriteIntToPacket(faction.MaxMembers, new CompressionInfo.Integer(10, 100, true));

            int memberLength = faction.members.Count;
            GameNetworkMessage.WriteIntToPacket(memberLength, new CompressionInfo.Integer(0, 500, true));
            for (int i = 0; i < memberLength; i++)
            {
                GameNetworkMessage.WriteNetworkPeerReferenceToPacket(faction.members[i]);
            }
        }
    }
}

