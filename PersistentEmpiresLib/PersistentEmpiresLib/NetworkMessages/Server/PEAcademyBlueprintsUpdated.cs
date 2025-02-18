using System.Collections.Generic;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace PersistentEmpiresLib.NetworkMessages.Server
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class PEAcademyBlueprintsUpdated : GameNetworkMessage
    {
        public int PlayerID { get; private set; }
        public List<string> Blueprints { get; private set; }

        public PEAcademyBlueprintsUpdated() { }

        public PEAcademyBlueprintsUpdated(int playerId, List<string> blueprints)
        {
            this.PlayerID = playerId;
            this.Blueprints = blueprints ?? new List<string>();
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.MissionObjects;
        }

        protected override string OnGetLogFormat()
        {
            return $"📜 Blueprints aktualisiert für Spieler mit ID {PlayerID}: {string.Join(", ", Blueprints)}";
        }

        protected override bool OnRead()
        {
            bool result = true;
            this.PlayerID = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 10000, true), ref result);
            int count = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 100, true), ref result);
            this.Blueprints = new List<string>();

            for (int i = 0; i < count; i++)
            {
                this.Blueprints.Add(GameNetworkMessage.ReadStringFromPacket(ref result));
            }

            return result;
        }

        protected override void OnWrite()
        {
            GameNetworkMessage.WriteIntToPacket(this.PlayerID, new CompressionInfo.Integer(0, 10000, true));
            GameNetworkMessage.WriteIntToPacket(this.Blueprints.Count, new CompressionInfo.Integer(0, 100, true));

            foreach (var blueprint in this.Blueprints)
            {
                GameNetworkMessage.WriteStringToPacket(blueprint);
            }
        }
    }
}
