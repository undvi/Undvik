using PersistentEmpiresLib.SceneScripts;
using PersistentEmpiresLib.Helpers;
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

            // Schmiedetyp-Check (Waffen oder Rüstung)
            bool isWeaponSmith = CraftingStation.IsWeaponSmith;
            var recipes = CraftingStation.GetCraftingReceipts(isWeaponSmith);

            // Blueprint-Check
            if (!CraftingStation.BlueprintSlots.Contains(CraftedItem))
            {
                InformationManager.DisplayMessage(new InformationMessage($"❌ Fehler: {CraftedItem} ist nicht in den Blueprints dieser Schmiede!"));
                return;
            }

            // Überprüfe, ob das Item in den Crafting-Rezepten existiert
            var validRecipe = recipes.SelectMany(x => x.Value)
                                     .FirstOrDefault(r => r.ResultItem == CraftedItem);

            if (validRecipe == null)
            {
                InformationManager.DisplayMessage(new InformationMessage($"⚠️ Fehler: {CraftedItem} ist kein gültiges Crafting-Item in dieser Schmiede!"));
                return;
            }

            // **Tier 5 Integration** – Einschränkungen für Waffenschmiede & Rüstungsschmiede
            int itemTier = GetItemTier(CraftedItem);
            if (isWeaponSmith)
            {
                if (itemTier > 3 && !CraftedItem.Contains("pe_sword") && !CraftedItem.Contains("pe_axe"))
                {
                    InformationManager.DisplayMessage(new InformationMessage($"❌ Fehler: {CraftedItem} kann in dieser Waffenschmiede nicht hergestellt werden! (Max Tier 3 für Rüstungen)"));
                    return;
                }
            }
            else
            {
                if (itemTier > 3 && !CraftedItem.Contains("pe_armor") && !CraftedItem.Contains("pe_helmet"))
                {
                    InformationManager.DisplayMessage(new InformationMessage($"❌ Fehler: {CraftedItem} kann in dieser Rüstungsschmiede nicht hergestellt werden! (Max Tier 3 für Waffen)"));
                    return;
                }
            }

            // Materialien prüfen
            var playerInventory = Player.GetComponent<PlayerInventory>();
            if (!playerInventory.HasRequiredMaterials(CraftedItem, isWeaponSmith))
            {
                InformationManager.DisplayMessage(new InformationMessage($"❌ Fehler: {Player.UserName} hat nicht genügend Materialien für {CraftedItem}!"));
                return;
            }

            // Materialien verbrauchen
            playerInventory.ConsumeMaterials(CraftedItem, isWeaponSmith);

            // Übergibt das gecraftete Item an den Spieler
            if (playerInventory.AddItemToPlayer(Player, CraftedItem))
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

        /// <summary>
        /// Bestimmt das Tier-Level eines Items anhand des Namens.
        /// </summary>
        private int GetItemTier(string itemName)
        {
            if (itemName.Contains("legendary") || itemName.Contains("royal"))
                return 5;
            if (itemName.Contains("greatsword") || itemName.Contains("knight"))
                return 4;
            if (itemName.Contains("waraxe") || itemName.Contains("plate"))
                return 3;
            if (itemName.Contains("sword") || itemName.Contains("chainmail"))
                return 2;
            return 1;
        }
    }
}
