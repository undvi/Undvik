using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace PersistentEmpiresLib.UI
{
    public class PEInfluenceDisplay : MissionView
    {
        private GauntletLayer _layer;
        private PEInfluenceDisplayVM _viewModel;

        public override void OnMissionScreenInitialize()
        {
            base.OnMissionScreenInitialize();
            _viewModel = new PEInfluenceDisplayVM();
            _layer = new GauntletLayer(1);
            _layer.LoadMovie("PEInfluenceDisplay", _viewModel);
            MissionScreen.AddLayer(_layer);
        }

        public void UpdateInfluence(int playerInfluence, string factionName, int factionInfluence)
        {
            _viewModel.PlayerInfluence = playerInfluence;
            _viewModel.FactionName = factionName;
            _viewModel.FactionInfluence = factionInfluence;
        }

        public override void OnMissionScreenFinalize()
        {
            base.OnMissionScreenFinalize();
            MissionScreen.RemoveLayer(_layer);
            _layer = null;
            _viewModel = null;
        }
    }
}
