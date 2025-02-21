using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using PersistentEmpiresLib.Data;
using PersistentEmpiresLib.Helpers;
using System.Collections.Generic;
using System.Linq;

namespace PersistentEmpiresLib.NetworkMessages.Server
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class RequestExecuteCraft : GameNetworkMessage
    {
        public int PlayerID { get; private set; }
        public string ItemID { get; private set; }

        public RequestExecuteCraft() { }

        public RequestExecuteCraft(int playerId, string itemId)
        {
            this.PlayerID = playerId;
            this.ItemID = itemId;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.MissionObjects;
        }

        protected override string OnGetLogFormat()
        {
            return $"🛠️ Crafting-Anfrage: Spieler {PlayerID} möchte {ItemID} herstellen.";
        }

        protected override bool OnRead()
        {
            bool result = true;
            this.PlayerID = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 10000, true), ref result);
            this.ItemID = GameNetworkMessage.ReadStringFromPacket(ref result);
            return result;
        }

        protected override void OnWrite()
        {
            GameNetworkMessage.WriteIntToPacket(this.PlayerID, new CompressionInfo.Integer(0, 10000, true));
            GameNetworkMessage.WriteStringToPacket(this.ItemID);
        }

        public bool ValidateCraftingRequest()
        {
            var playerBlueprints = BlueprintResearchSystem.GetPlayerBlueprints(PlayerID);
            if (!playerBlueprints.Contains(ItemID))
            {
                InformationManager.DisplayMessage(new InformationMessage($"❌ Fehler: Spieler {PlayerID} hat {ItemID} nicht erforscht!"));
                GameNetwork.BeginModuleEventAsServer(PlayerID);
                GameNetwork.WriteMessage(new CraftingRejected(PlayerID, ItemID));
                GameNetwork.EndModuleEventAsServer();
                return false;
            }
            PlayerProgressManager.Instance.UpdateCraftingProgress(PlayerID, ItemID);
            return true;
        }
    }

    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class CraftingRejected : GameNetworkMessage
    {
        public int PlayerID { get; private set; }
        public string ItemID { get; private set; }

        public CraftingRejected() { }

        public CraftingRejected(int playerId, string itemId)
        {
            this.PlayerID = playerId;
            this.ItemID = itemId;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.MissionObjects;
        }

        protected override string OnGetLogFormat()
        {
            return $"❌ Crafting-Anfrage abgelehnt: Spieler {PlayerID} kann {ItemID} nicht craften!";
        }

        protected override bool OnRead()
        {
            bool result = true;
            this.PlayerID = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 10000, true), ref result);
            this.ItemID = GameNetworkMessage.ReadStringFromPacket(ref result);
            return result;
        }

        protected override void OnWrite()
        {
            GameNetworkMessage.WriteIntToPacket(this.PlayerID, new CompressionInfo.Integer(0, 10000, true));
            GameNetworkMessage.WriteStringToPacket(this.ItemID);
        }

        private string GetBlueprintForItem(string itemId)
        {
            return "blueprint_" + itemId;
        }
    }
}

// File: PlayerProgressManager.cs
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.MountAndBlade;

namespace PersistentEmpiresLib.Managers
{
    public class PlayerProgressManager
    {
        private Dictionary<int, List<string>> _craftedItems = new Dictionary<int, List<string>>();

        public static PlayerProgressManager Instance { get; } = new PlayerProgressManager();

        public void UpdateCraftingProgress(int playerId, string itemId)
        {
            if (!_craftedItems.ContainsKey(playerId))
            {
                _craftedItems[playerId] = new List<string>();
            }

            _craftedItems[playerId].Add(itemId);
            CheckBlueprintUnlock(playerId);
        }

        private void CheckBlueprintUnlock(int playerId)
        {
            if (_craftedItems[playerId].Count(i => i == "Sword") >= 5)
            {
                UnlockBlueprint(playerId, "Advanced Sword");
            }
        }

        private void UnlockBlueprint(int playerId, string blueprint)
        {
            GameNetwork.BeginModuleEventAsServer(playerId);
            GameNetwork.WriteMessage(new PEAcademyBlueprintUnlocked(blueprint));
            GameNetwork.EndModuleEventAsServer();
        }
    }
}
