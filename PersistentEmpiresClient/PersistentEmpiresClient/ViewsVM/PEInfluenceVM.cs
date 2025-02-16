using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using PersistentEmpiresLib.NetworkMessages.Client;
using PersistentEmpiresLib.Systems;

namespace PersistentEmpiresClient.ViewsVM
{
	public class PEInfluenceVM : ViewModel
	{
		private int _playerInfluence;
		private int _factionInfluence;
		private string _factionName;

		public PEInfluenceVM()
		{
			PlayerInfluence = 0;
			FactionInfluence = 0;
			FactionName = "Unabhängig";

			GameNetwork.MessageHandlerManager.RegisterHandler<PEInfluenceUpdated>(OnInfluenceUpdated);
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
		public int FactionInfluence
		{
			get => _factionInfluence;
			set
			{
				if (value != _factionInfluence)
				{
					_factionInfluence = value;
					OnPropertyChanged(nameof(FactionInfluence));
				}
			}
		}

		[DataSourceProperty]
		public string FactionName
		{
			get => _factionName;
			set
			{
				if (value != _factionName)
				{
					_factionName = value;
					OnPropertyChanged(nameof(FactionName));
				}
			}
		}

		private void OnInfluenceUpdated(PEInfluenceUpdated message)
		{
			if (message.Player == GameNetwork.MyPeer)
			{
				PlayerInfluence = message.Influence;
				FactionInfluence = message.FactionInfluence;
				FactionName = string.IsNullOrEmpty(message.FactionName) ? "Unabhängig" : message.FactionName;
			}
		}

		public override void RefreshValues()
		{
			base.RefreshValues();
			OnPropertyChanged(nameof(PlayerInfluence));
			OnPropertyChanged(nameof(FactionInfluence));
			OnPropertyChanged(nameof(FactionName));
		}

		public void Cleanup()
		{
			GameNetwork.MessageHandlerManager.UnregisterHandler<PEInfluenceUpdated>(OnInfluenceUpdated);
		}
	}
}
