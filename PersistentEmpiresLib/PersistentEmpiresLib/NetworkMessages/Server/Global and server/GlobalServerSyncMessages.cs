using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using TaleWorlds.ObjectSystem;
using PersistentEmpiresLib.Factions;
using PersistentEmpiresLib.Helpers;
using PersistentEmpiresLib.SceneScripts;

namespace PersistentEmpiresLib.NetworkMessages.Server.GlobalSync
{
    #region Server Handshake
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class ServerHandshake : GameNetworkMessage
    {
        public string ServerSignature { get; set; }

        public ServerHandshake(string serverSignature)
        {
            ServerSignature = serverSignature;
        }

        public ServerHandshake() { }

        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.None;

        protected override string OnGetLogFormat() => "ServerHandshake";

        protected override bool OnRead()
        {
            bool result = true;
            ServerSignature = GameNetworkMessage.ReadStringFromPacket(ref result);
            return result;
        }

        protected override void OnWrite()
        {
            GameNetworkMessage.WriteStringToPacket(ServerSignature);
        }
    }
    #endregion

    #region Global Environment Updates
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class SetDayTime : GameNetworkMessage
    {
        public float TimeOfDay { get; private set; }

        public SetDayTime() { }
        public SetDayTime(float time)
        {
            TimeOfDay = time;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.Mission;
        protected override string OnGetLogFormat() => "SetDayTime";

        protected override bool OnRead()
        {
            bool result = true;
            TimeOfDay = GameNetworkMessage.ReadFloatFromPacket(new CompressionInfo.Float(0f, 24f, 22), ref result);
            return result;
        }

        protected override void OnWrite()
        {
            GameNetworkMessage.WriteFloatToPacket(TimeOfDay, new CompressionInfo.Float(0f, 24f, 22));
        }
    }

    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class SetHunger : GameNetworkMessage
    {
        public int Hunger { get; private set; }

        public SetHunger() { }
        public SetHunger(int hunger)
        {
            Hunger = hunger;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.AgentsDetailed;
        protected override string OnGetLogFormat() => "Hunger set";

        protected override bool OnRead()
        {
            bool result = true;
            Hunger = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 100, true), ref result);
            return result;
        }

        protected override void OnWrite()
        {
            GameNetworkMessage.WriteIntToPacket(Hunger, new CompressionInfo.Integer(0, 100, true));
        }
    }
    #endregion

    #region New User Synchronization
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class SyncNewUser : GameNetworkMessage
    {
        public Dictionary<int, Faction> Factions { get; set; }
        public List<PE_CastleBanner> CastleBanners { get; set; }

        public SyncNewUser() { }
        public SyncNewUser(Dictionary<int, Faction> factions, List<PE_CastleBanner> castleBanners)
        {
            Factions = factions;
            CastleBanners = castleBanners;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.Mission;
        protected override string OnGetLogFormat() => "Sending all data to client";

        protected override bool OnRead()
        {
            Factions = new Dictionary<int, Faction>();
            CastleBanners = new List<PE_CastleBanner>();
            bool result = true;

            int factionLength = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 201, true), ref result);
            for (int i = 0; i < factionLength; i++)
            {
                int factionIndex = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(-1, 200, true), ref result);
                BasicCultureObject bco = (BasicCultureObject)GameNetworkMessage.ReadObjectReferenceFromPacket(MBObjectManager.Instance, CompressionBasic.GUIDCompressionInfo, ref result);
                string name = GameNetworkMessage.ReadStringFromPacket(ref result);
                Team team = Mission.MissionNetworkHelper.GetTeamFromTeamIndex(GameNetworkMessage.ReadTeamIndexFromPacket(ref result));
                string bannerKey = PENetworkModule.ReadBannerCodeFromPacket(ref result);
                string lordId = GameNetworkMessage.ReadStringFromPacket(ref result);
                int memberLength = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 500, true), ref result);

                var faction = new Faction(bco, new Banner(bannerKey), name)
                {
                    lordId = lordId,
                    team = team
                };

                for (int j = 0; j < memberLength; j++)
                {
                    var member = GameNetworkMessage.ReadNetworkPeerReferenceFromPacket(ref result);
                    if (!faction.members.Contains(member))
                        faction.members.Add(member);
                }

                int warDeclerationLength = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 200, true), ref result);
                for (int j = 0; j < warDeclerationLength; j++)
                {
                    faction.warDeclaredTo.Add(GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 200, true), ref result));
                }

                Factions[factionIndex] = faction;
            }

            // Castle-Banner-Abschnitt ist derzeit auskommentiert
            return result;
        }

        protected override void OnWrite()
        {
            GameNetworkMessage.WriteIntToPacket(Factions.Count, new CompressionInfo.Integer(0, 201, true));
            foreach (int i in Factions.Keys)
            {
                Faction faction = Factions[i];
                GameNetworkMessage.WriteIntToPacket(i, new CompressionInfo.Integer(-1, 200, true));
                GameNetworkMessage.WriteObjectReferenceToPacket(faction.basicCultureObject, CompressionBasic.GUIDCompressionInfo);
                GameNetworkMessage.WriteStringToPacket(faction.name);
                GameNetworkMessage.WriteTeamIndexToPacket(faction.team.TeamIndex);
                PENetworkModule.WriteBannerCodeToPacket(faction.banner.Serialize());
                GameNetworkMessage.WriteStringToPacket(faction.lordId);
                int memberLength = faction.members.Count;
                GameNetworkMessage.WriteIntToPacket(memberLength, new CompressionInfo.Integer(0, 500, true));
                for (int j = 0; j < memberLength; j++)
                {
                    GameNetworkMessage.WriteNetworkPeerReferenceToPacket(faction.members[j]);
                }
                int warDeclerationLength = faction.warDeclaredTo.Count;
                GameNetworkMessage.WriteIntToPacket(warDeclerationLength, new CompressionInfo.Integer(0, 200, true));
                for (int j = 0; j < warDeclerationLength; j++)
                {
                    GameNetworkMessage.WriteIntToPacket(faction.warDeclaredTo[j], new CompressionInfo.Integer(0, 200, true));
                }
            }
        }
    }
    #endregion
}
