using TaleWorlds.Library;

namespace PersistentEmpiresLib.UI
{
	public class PEInfluenceDisplayVM : ViewModel
	{
		private int _playerInfluence;
		private string _factionName;
		private int _factionInfluence;

		public int PlayerInfluence
		{
			get => _playerInfluence;
			set
			{
				if (_playerInfluence != value)
				{
					_playerInfluence = value;
					OnPropertyChanged(nameof(PlayerInfluence));
				}
			}
		}

		public string FactionName
		{
			get => _factionName;
			set
			{
				if (_factionName != value)
				{
					_factionName = value;
					OnPropertyChanged(nameof(FactionName));
				}
			}
		}

		public int FactionInfluence
		{
			get => _factionInfluence;
			set
			{
				if (_factionInfluence != value)
				{
					_factionInfluence = value;
					OnPropertyChanged(nameof(FactionInfluence));
				}
			}
		}

		public PEInfluenceDisplayVM()
		{
			PlayerInfluence = 0;
			FactionName = "Unabhängig";
			FactionInfluence = 0;
		}
	}
}
