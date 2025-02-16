using PersistentEmpires.Views.ViewsVM.FactionManagement;
using PersistentEmpiresLib;
using PersistentEmpiresLib.Factions;
using PersistentEmpiresLib.NetworkMessages.Client;
using TaleWorlds.MountAndBlade;
using PersistentEmpiresLib.Helpers;
using System;

namespace PersistentEmpires.Views.Views.FactionManagement
{
    public class PEFactionAssignMarshall : PEMenuItem
    {
        public PEFactionAssignMarshall() : base("PEFactionMembers") { }

        public override void OnMissionScreenInitialize()
        {
            base.OnMissionScreenInitialize();
            this._factionManagementComponent.OnAssignMarshallClick += this.OnOpen;

            this._dataSource = new PEFactionMembersVM("Assign Marshall", "Assign a new Marshall", () =>
            {
                CloseManagementMenu();
                _factionManagementComponent.OnFactionManagementClickHandler();
            },
            (PEFactionMemberItemVM selectedMember) =>
            {
                if (selectedMember == null)
                {
                    PersistentEmpiresLib.Helpers.PELogger.Log("No member selected for assignment!", PersistentEmpiresLib.Helpers.LogLevel.Warning);
                    return;
                }

                // Hole die Fraktions-ID
                int factionIndex = selectedMember.Peer.GetComponent<PersistentEmpireRepresentative>()?.GetFactionIndex() ?? -1;
                if (factionIndex == -1)
                {
                    PersistentEmpiresLib.Helpers.PELogger.Log("Invalid faction index!", PersistentEmpiresLib.Helpers.LogLevel.Error);
                    return;
                }

                // Prüfe, ob der Spieler bereits Marschall ist
                if (selectedMember.IsGranted)
                {
                    PersistentEmpiresLib.Helpers.PELogger.Log("Player is already a Marshall!", PersistentEmpiresLib.Helpers.LogLevel.Warning);
                    return;
                }

                // Netzwerk-Event senden
                try
                {
                    GameNetwork.BeginModuleEventAsClient();
                    GameNetwork.WriteMessage(new FactionAssignMarshall(selectedMember.Peer));
                    GameNetwork.EndModuleEventAsClient();

                    PersistentEmpiresLib.Helpers.PELogger.Log($"Marshall assigned to {selectedMember.Peer.VirtualPlayer.UserName}", PersistentEmpiresLib.Helpers.LogLevel.Info);

                    selectedMember.IsGranted = true; // UI-Update erst nach Bestätigung vom Server
                }
                catch (Exception ex)
                {
                    PersistentEmpiresLib.Helpers.PELogger.Log($"Error sending Marshall assignment: {ex.Message}", PersistentEmpiresLib.Helpers.LogLevel.Error);
                }
            },
            () =>
            {
                CloseManagementMenu();
            });
        }

        protected override void OnOpen()
        {
            PersistentEmpireRepresentative persistentEmpireRepresentative = GameNetwork.MyPeer.GetComponent<PersistentEmpireRepresentative>();
            if (persistentEmpireRepresentative == null) return;

            Faction faction = persistentEmpireRepresentative.GetFaction();
            if (faction == null)
            {
                PersistentEmpiresLib.Helpers.PELogger.Log("Faction not found!", PersistentEmpiresLib.Helpers.LogLevel.Error);
                return;
            }

            PEFactionMembersVM dataSource = (PEFactionMembersVM)this._dataSource;
            dataSource.RefreshItems(faction, true);

            foreach (var member in dataSource.Members)
            {
                if (faction.marshalls.Contains(member.Peer.VirtualPlayer.ToPlayerId()))
                {
                    member.IsGranted = true;
                }
            }

            base.OnOpen();
        }
    }
}
