using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using PersistentEmpiresLib.NetworkMessages.Client;
using PersistentEmpiresLib.NetworkMessages.Server;
using System.Collections.Generic;

namespace PersistentEmpiresClient.ViewsVM
{
    public class PEAcademyVM : ViewModel
    {
        private int _playerInfluence;
        private MBBindingList<BlueprintItemVM> _availableBlueprints;
        private MBBindingList<BlueprintResearchVM> _activeResearch;

        public PEAcademyVM()
        {
            PlayerInfluence = 0;
            AvailableBlueprints = new MBBindingList<BlueprintItemVM>();
            ActiveResearch = new MBBindingList<BlueprintResearchVM>();

            GameNetwork.MessageHandlerManager.RegisterHandler<PEInfluenceUpdated>(OnInfluenceUpdated);
            GameNetwork.MessageHandlerManager.RegisterHandler<PEAcademyBlueprintsUpdated>(OnBlueprintsUpdated);
            LoadActiveResearch();
        }

        [DataSourceProperty]
        public int PlayerInfluence
        {
            get => _playerInfluence;
            set
            {
                if (value != _playerInfluence)
                {
                    _playerInfluence = value;
                    OnPropertyChanged(nameof(PlayerInfluence));
                }
            }
        }

        [DataSourceProperty]
        public MBBindingList<BlueprintItemVM> AvailableBlueprints
        {
            get => _availableBlueprints;
            set
            {
                if (value != _availableBlueprints)
                {
                    _availableBlueprints = value;
                    OnPropertyChanged(nameof(AvailableBlueprints));
                }
            }
        }

        [DataSourceProperty]
        public MBBindingList<BlueprintResearchVM> ActiveResearch
        {
            get => _activeResearch;
            set
            {
                if (value != _activeResearch)
                {
                    _activeResearch = value;
                    OnPropertyChanged(nameof(ActiveResearch));
                }
            }
        }

        public void EnterAcademy()
        {
            if (PlayerInfluence < 50)
            {
                InformationManager.DisplayMessage(new InformationMessage("⚠️ Nicht genug Einfluss, um die Akademie zu betreten!"));
                return;
            }

            GameNetwork.BeginModuleEventAsClient();
            GameNetwork.WriteMessage(new PEAcademyEnter());
            GameNetwork.EndModuleEventAsClient();
        }

        private void OnInfluenceUpdated(PEInfluenceUpdated message)
        {
            if (message.Player == GameNetwork.MyPeer)
            {
                PlayerInfluence = message.Influence;
            }
        }

        private void OnBlueprintsUpdated(PEAcademyBlueprintsUpdated message)
        {
            if (message.Player != GameNetwork.MyPeer) return;

            AvailableBlueprints.Clear();
            foreach (var blueprint in message.UnlockedBlueprints)
            {
                AvailableBlueprints.Add(new BlueprintItemVM(blueprint));
            }
            LoadActiveResearch();
        }

        private void LoadActiveResearch()
        {
            var researchList = BlueprintResearchSystem.GetActiveResearch();
            ActiveResearch.Clear();
            foreach (var research in researchList)
            {
                ActiveResearch.Add(new BlueprintResearchVM(research));
            }
        }

        public void UnlockBlueprint(int blueprintId)
        {
            GameNetwork.BeginModuleEventAsClient();
            GameNetwork.WriteMessage(new PEAcademyUnlockBlueprint(blueprintId));
            GameNetwork.EndModuleEventAsClient();
        }

        public override void RefreshValues()
        {
            base.RefreshValues();
            OnPropertyChanged(nameof(PlayerInfluence));
            OnPropertyChanged(nameof(AvailableBlueprints));
            OnPropertyChanged(nameof(ActiveResearch));
        }

        public void Cleanup()
        {
            GameNetwork.MessageHandlerManager.UnregisterHandler<PEInfluenceUpdated>(OnInfluenceUpdated);
            GameNetwork.MessageHandlerManager.UnregisterHandler<PEAcademyBlueprintsUpdated>(OnBlueprintsUpdated);
        }
    }
}