using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using PersistentEmpiresLib.Factions;
using System.Collections.Generic;

namespace PersistentEmpiresLib.NetworkMessages.Server
{
    public enum FactionKeyType
    {
        DoorKey = 0,
        ChestKey = 1,
        VaultKey = 2,
        WatchtowerKey = 3,
        Other = 4
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

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.None;
        }

        protected override string OnGetLogFormat()
        {
            return $"SyncFactionKey | Faction: {FactionIndex}, Player: {PlayerId}, KeyType: {KeyType}, Rank: {PlayerRank}, IsVassal: {IsVassal}";
        }

        protected override bool OnRead()
        {
            bool result = true;
            this.FactionIndex = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 200, true), ref result);
            this.PlayerId = GameNetworkMessage.ReadStringFromPacket(ref result);
            this.KeyType = (FactionKeyType)GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 10, true), ref result);
            this.PlayerRank = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(1, 5, true), ref result);
            this.IsVassal = GameNetworkMessage.ReadBoolFromPacket(ref result);

            if (string.IsNullOrEmpty(this.PlayerId))
            {
                this.PlayerId = "Unknown_Player";
            }

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
}
