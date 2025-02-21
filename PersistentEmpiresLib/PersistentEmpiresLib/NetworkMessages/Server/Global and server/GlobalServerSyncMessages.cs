using System;
using System.Collections.Generic;
using System.IO;
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

    #region Global Game State Synchronization
    /// <summary>
    /// Neue Nachricht, die globale Zustände an einen neuen Client sendet:
    /// - Alle Fraktionen (wie bisher)
    /// - Castle-Banner (wie bisher)
    /// - Zusätzlich: GlobalResearchData (Forschung) und ActiveWars (Kriegssystem)
    /// </summary>
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class SyncNewUser : GameNetworkMessage
    {
        public Dictionary<int, Faction> Factions { get; set; }
        public List<PE_CastleBanner> CastleBanners { get; set; }

        // NEU: Global Research Data (z. B. Projekt-ID -> Fortschritt in Prozent)
        public Dictionary<string, float> GlobalResearchData { get; set; }

        // NEU: Liste aktiver Kriege
        public List<WarInfo> ActiveWars { get; set; }

        public SyncNewUser() { }

        public SyncNewUser(
            Dictionary<int, Faction> factions,
            List<PE_CastleBanner> castleBanners,
            Dictionary<string, float> globalResearchData,
            List<WarInfo> activeWars)
        {
            Factions = factions;
            CastleBanners = castleBanners;
            GlobalResearchData = globalResearchData;
            ActiveWars = activeWars;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.Mission;
        protected override string OnGetLogFormat() => "Sending full global game state to client";

        protected override bool OnRead()
        {
            Factions = new Dictionary<int, Faction>();
            CastleBanners = new List<PE_CastleBanner>();
            GlobalResearchData = new Dictionary<string, float>();
            ActiveWars = new List<WarInfo>();

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

                // Optional: Lese zusätzliche Daten wie Prestige, etc.
                Factions[factionIndex] = faction;
            }

            // NEU: GlobalResearchData
            int researchCount = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 100, true), ref result);
            for (int i = 0; i < researchCount; i++)
            {
                string researchId = GameNetworkMessage.ReadStringFromPacket(ref result);
                float progress = GameNetworkMessage.ReadFloatFromPacket(new CompressionInfo.Float(0f, 100f, 0.1f), ref result);
                GlobalResearchData[researchId] = progress;
            }

            // NEU: ActiveWars
            int warCount = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 100, true), ref result);
            for (int i = 0; i < warCount; i++)
            {
                WarInfo war = new WarInfo();
                war.WarDeclarerIndex = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 200, true), ref result);
                war.WarDeclaredTo = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 200, true), ref result);
                war.WarType = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 10, true), ref result);
                ActiveWars.Add(war);
            }

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
            }

            // NEU: GlobalResearchData schreiben
            GameNetworkMessage.WriteIntToPacket(GlobalResearchData.Count, new CompressionInfo.Integer(0, 100, true));
            foreach (var kvp in GlobalResearchData)
            {
                GameNetworkMessage.WriteStringToPacket(kvp.Key);
                GameNetworkMessage.WriteFloatToPacket(kvp.Value, new CompressionInfo.Float(0f, 100f, 0.1f));
            }

            // NEU: ActiveWars schreiben
            GameNetworkMessage.WriteIntToPacket(ActiveWars.Count, new CompressionInfo.Integer(0, 100, true));
            foreach (var war in ActiveWars)
            {
                GameNetworkMessage.WriteIntToPacket(war.WarDeclarerIndex, new CompressionInfo.Integer(0, 200, true));
                GameNetworkMessage.WriteIntToPacket(war.WarDeclaredTo, new CompressionInfo.Integer(0, 200, true));
                GameNetworkMessage.WriteIntToPacket(war.WarType, new CompressionInfo.Integer(0, 10, true));
            }
        }
    }

    /// <summary>
    /// Neue Klasse zur Darstellung eines aktiven Krieges.
    /// WarType: 0 = Handelskrieg, 1 = Überfall, 2 = Eroberung.
    /// </summary>
    public class WarInfo
    {
        public int WarDeclarerIndex { get; set; }
        public int WarDeclaredTo { get; set; }
        public int WarType { get; set; }
    }
    #endregion
}
