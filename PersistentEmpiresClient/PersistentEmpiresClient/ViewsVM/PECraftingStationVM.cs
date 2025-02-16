using PersistentEmpiresLib.SceneScripts;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using System.Linq;

namespace PersistentEmpiresLib.NetworkMessages.Server
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class CraftingCompleted : GameNetworkMessage
    {
        public NetworkCommunicator Player { get; private set; }
        public string CraftedItem { get; private set; }
        public PEFactionSmithing CraftingStation { get; private set; }

        public CraftingCompleted() { }

        public CraftingCompleted(NetworkCommunicator player, string craftedItem, PEFactionSmithing craftingStation)
        {
            this.Player = player ?? throw new System.ArgumentNullException(nameof(player), "❌ Fehler: Spieler ist null in CraftingCompleted!");
            this.CraftedItem = craftedItem ?? throw new System.ArgumentNullException(nameof(craftedItem), "❌ Fehler: Kein Item angegeben!");
            this.CraftingStation = craftingStation ?? throw new System.ArgumentNullException(nameof(craftingStation), "❌ Fehler: CraftingStation ist null!");
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.MissionObjects;
        }

        protected override string OnGetLogFormat()
        {
            return this.Player != null
                ? $"✅ Crafting abgeschlossen: {CraftedItem} für {Player.UserName}"
                : "⚠️ Fehler: Spieler ist NULL beim Crafting-Abschluss!";
        }

        protected override bool OnRead()
        {
            bool result = true;
            this.Player = GameNetworkMessage.ReadNetworkPeerReferenceFromPacket(ref result);
            this.CraftedItem = GameNetworkMessage.ReadStringFromPacket(ref result);
            this.CraftingStation = Mission.Current?.GetMissionBehavior<PEFactionSmithing>();

            if (!result || this.Player == null || string.IsNullOrEmpty(this.CraftedItem))
            {
                InformationManager.DisplayMessage(new InformationMessage("⚠️ Fehler beim Lesen der Crafting-Daten!"));
                return false;
            }

            return true;
        }

        protected override void OnWrite()
        {
            if (this.Player == null || string.IsNullOrEmpty(this.CraftedItem))
            {
                InformationManager.DisplayMessage(new InformationMessage("⚠️ Fehler: Ungültige Daten für Crafting-Abschluss!"));
                return;
            }

            GameNetworkMessage.WriteNetworkPeerReferenceToPacket(this.Player);
            GameNetworkMessage.WriteStringToPacket(this.CraftedItem);
        }

        public void ApplyCraftingCompletion()
        {
            if (CraftingStation == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("⚠️ Fehler: Keine gültige Schmiede gefunden!"));
                return;
            }

            if (!CraftingStation.CanCraftItem(CraftedItem))
            {
                InformationManager.DisplayMessage(new InformationMessage($"❌ Fehler: {CraftedItem} ist nicht in den Blueprints dieser Schmiede!"));
                return;
            }

            // Übergibt das gecraftete Item an den Spieler
            if (PlayerInventory.AddItemToPlayer(Player, CraftedItem))
            {
                InformationManager.DisplayMessage(new InformationMessage($"🎁 {CraftedItem} wurde erfolgreich an {Player.UserName} übergeben!"));

                // Sende das Event an den Client
                GameNetwork.BeginBroadcastModuleEvent();
                GameNetwork.WriteMessage(new CraftingCompleted(Player, CraftedItem, CraftingStation));
                GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None, null);
            }
            else
            {
                InformationManager.DisplayMessage(new InformationMessage($"⚠️ Fehler: {CraftedItem} konnte nicht ins Inventar gelegt werden!"));
            }
        }
    }
}
