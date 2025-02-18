using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using PersistentEmpiresLib.Factions;
using PersistentEmpiresLib.Helpers;
using TaleWorlds.Core;

namespace PersistentEmpiresLib.NetworkMessages.Client
{
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
            this.KeyType = keyType;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.None;
        }

        protected override string OnGetLogFormat()
        {
            return $"Requesting Faction Key: {KeyType}";
        }

        protected override bool OnRead()
        {
            bool result = true;
            this.KeyType = (FactionKeyType)GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 4, true), ref result);
            return result;
        }

        protected override void OnWrite()
        {
            GameNetworkMessage.WriteIntToPacket((int)this.KeyType, new CompressionInfo.Integer(0, 4, true));
        }

        public static void HandleFactionKeyRequest(NetworkCommunicator sender, FactionKeyType requestedKeyType)
        {
            PersistentEmpireRepresentative rep = sender.GetComponent<PersistentEmpireRepresentative>();
            if (rep == null || rep.GetFaction() == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("You are not in a faction."));
                return;
            }

            Faction faction = rep.GetFaction();

            // Nur Lord oder Marschall kann Schlüssel vergeben
            if (faction.lordId != sender.VirtualPlayer.ToPlayerId() && !faction.marshalls.Contains(sender.VirtualPlayer.ToPlayerId()))
            {
                InformationManager.DisplayMessage(new InformationMessage("Only the Lord or a Marshall can issue faction keys."));
                return;
            }

            // Sicherheitsprüfung für KeyType
            if (!Enum.IsDefined(typeof(FactionKeyType), requestedKeyType))
            {
                InformationManager.DisplayMessage(new InformationMessage("Invalid key type requested."));
                return;
            }

            // Überprüfe, ob Spieler bereits den Schlüssel hat
            if (rep.HasFactionKey(requestedKeyType))
            {
                InformationManager.DisplayMessage(new InformationMessage($"You already have a {requestedKeyType} key."));
                return;
            }

            // Schlüssel vergeben
            rep.AddFactionKey(requestedKeyType);
            GameNetwork.BeginBroadcastModuleEvent();
            GameNetwork.WriteMessage(new SyncFactionKey(faction.FactionIndex, sender.UserName, requestedKeyType));
            GameNetwork.EndBroadcastModuleEvent();

            InformationManager.DisplayMessage(new InformationMessage($"{sender.UserName} has received a {requestedKeyType} key."));
        }
    }
}
