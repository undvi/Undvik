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

        public PEAcademyVM()
        {
            PlayerInfluence = 0;
            AvailableBlueprints = new MBBindingList<BlueprintItemVM>();

            GameNetwork.MessageHandlerManager.RegisterHandler<PEInfluenceUpdated>(OnInfluenceUpdated);
            GameNetwork.MessageHandlerManager.RegisterHandler<PEAcademyBlueprintsUpdated>(OnBlueprintsUpdated);
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

        /// <summary>
        /// Spieler betritt die Akademie, falls er genug Einfluss hat.
        /// </summary>
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

        /// <summary>
        /// Aktualisiert den Einfluss des Spielers basierend auf Server-Updates.
        /// </summary>
        private void OnInfluenceUpdated(PEInfluenceUpdated message)
        {
            if (message.Player == GameNetwork.MyPeer)
            {
                PlayerInfluence = message.Influence;
            }
        }

        /// <summary>
        /// Aktualisiert die freigeschalteten Blueprints.
        /// </summary>
        private void OnBlueprintsUpdated(PEAcademyBlueprintsUpdated message)
        {
            if (message.Player != GameNetwork.MyPeer) return;

            AvailableBlueprints.Clear();
            foreach (var blueprint in message.UnlockedBlueprints)
            {
                AvailableBlueprints.Add(new BlueprintItemVM(blueprint));
            }
        }

        /// <summary>
        /// Sendet eine Anfrage zum Freischalten eines Blueprints.
        /// </summary>
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
        }

        public void Cleanup()
        {
            GameNetwork.MessageHandlerManager.UnregisterHandler<PEInfluenceUpdated>(OnInfluenceUpdated);
            GameNetwork.MessageHandlerManager.UnregisterHandler<PEAcademyBlueprintsUpdated>(OnBlueprintsUpdated);
        }
    }

    /// <summary>
    /// ViewModel für einzelne Blueprints.
    /// </summary>
    public class BlueprintItemVM : ViewModel
    {
        private string _name;
        private int _id;
        private bool _isUnlocked;

        public BlueprintItemVM(string name, int id, bool isUnlocked)
        {
            _name = name;
            _id = id;
            _isUnlocked = isUnlocked;
        }

        public BlueprintItemVM(PEAcademyBlueprint blueprint)
        {
            _name = blueprint.Name;
            _id = blueprint.Id;
            _isUnlocked = blueprint.IsUnlocked;
        }

        [DataSourceProperty]
        public string BlueprintName
        {
            get => _name;
            set
            {
                if (value != _name)
                {
                    _name = value;
                    OnPropertyChanged(nameof(BlueprintName));
                }
            }
        }

        [DataSourceProperty]
        public int BlueprintID
        {
            get => _id;
            set
            {
                if (value != _id)
                {
                    _id = value;
                    OnPropertyChanged(nameof(BlueprintID));
                }
            }
        }

        [DataSourceProperty]
        public bool IsUnlocked
        {
            get => _isUnlocked;
            set
            {
                if (value != _isUnlocked)
                {
                    _isUnlocked = value;
                    OnPropertyChanged(nameof(IsUnlocked));
                }
            }
        }
    }
}
