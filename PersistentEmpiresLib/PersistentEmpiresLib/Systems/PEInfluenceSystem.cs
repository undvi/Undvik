using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace PersistentEmpiresLib.Systems
{
	public class PEInfluenceSystem : MissionObject
	{
		private Dictionary<NetworkCommunicator, int> PlayerInfluence = new Dictionary<NetworkCommunicator, int>();
		private Dictionary<string, int> FactionInfluence = new Dictionary<string, int>();

		private const int DefaultStartInfluence = 100; // Standard-Einfluss für neue Spieler
		private const int MinInfluence = 0; // Kein Einfluss unter 0

		private readonly string dbPath = "InfluenceDatabase.sqlite"; // Datenbank-Pfad

		/// <summary>
		/// Initialisiert das Einfluss-System und lädt gespeicherte Werte.
		/// </summary>
		public void InitializeSystem()
		{
			if (!File.Exists(dbPath))
				CreateDatabase();

			LoadInfluenceFromDatabase();
		}

		/// <summary>
		/// Erstellt eine SQLite-Datenbank für Einfluss-Werte.
		/// </summary>
		private void CreateDatabase()
		{
			using (var connection = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
			{
				connection.Open();
				using (var command = new SQLiteCommand(connection))
				{
					command.CommandText = "CREATE TABLE IF NOT EXISTS PlayerInfluence (PlayerID TEXT PRIMARY KEY, Influence INT);";
					command.ExecuteNonQuery();

					command.CommandText = "CREATE TABLE IF NOT EXISTS FactionInfluence (FactionName TEXT PRIMARY KEY, Influence INT);";
					command.ExecuteNonQuery();
				}
			}
		}

		/// <summary>
		/// Lädt Einfluss-Werte aus der Datenbank.
		/// </summary>
		private void LoadInfluenceFromDatabase()
		{
			using (var connection = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
			{
				connection.Open();
				using (var command = new SQLiteCommand("SELECT PlayerID, Influence FROM PlayerInfluence", connection))
				using (var reader = command.ExecuteReader())
				{
					while (reader.Read())
					{
						string playerID = reader.GetString(0);
						int influence = reader.GetInt32(1);
						var player = GameNetwork.NetworkPeers.Find(p => p.UserName == playerID);
						if (player != null)
							PlayerInfluence[player] = influence;
					}
				}

				using (var command = new SQLiteCommand("SELECT FactionName, Influence FROM FactionInfluence", connection))
				using (var reader = command.ExecuteReader())
				{
					while (reader.Read())
					{
						string factionName = reader.GetString(0);
						int influence = reader.GetInt32(1);
						FactionInfluence[factionName] = influence;
					}
				}
			}
		}

		/// <summary>
		/// Speichert den Einfluss eines Spielers in die Datenbank.
		/// </summary>
		private void SaveInfluenceToDatabase(NetworkCommunicator player)
		{
			using (var connection = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
			{
				connection.Open();
				using (var command = new SQLiteCommand("INSERT OR REPLACE INTO PlayerInfluence (PlayerID, Influence) VALUES (@PlayerID, @Influence)", connection))
				{
					command.Parameters.AddWithValue("@PlayerID", player.UserName);
					command.Parameters.AddWithValue("@Influence", GetInfluence(player));
					command.ExecuteNonQuery();
				}
			}
		}

		/// <summary>
		/// Speichert den Einfluss einer Fraktion in die Datenbank.
		/// </summary>
		private void SaveFactionInfluenceToDatabase(string factionName)
		{
			using (var connection = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
			{
				connection.Open();
				using (var command = new SQLiteCommand("INSERT OR REPLACE INTO FactionInfluence (FactionName, Influence) VALUES (@FactionName, @Influence)", connection))
				{
					command.Parameters.AddWithValue("@FactionName", factionName);
					command.Parameters.AddWithValue("@Influence", GetFactionInfluence(factionName));
					command.ExecuteNonQuery();
				}
			}
		}

		/// <summary>
		/// Gibt den aktuellen Einfluss eines Spielers zurück.
		/// </summary>
		public int GetInfluence(NetworkCommunicator player)
		{
			return PlayerInfluence.TryGetValue(player, out int influence) ? influence : 0;
		}

		/// <summary>
		/// Gibt den aktuellen Einfluss einer Fraktion zurück.
		/// </summary>
		public int GetFactionInfluence(string factionName)
		{
			return FactionInfluence.TryGetValue(factionName, out int influence) ? influence : 0;
		}

		/// <summary>
		/// Fügt Einfluss für eine Fraktion hinzu.
		/// </summary>
		public void AddFactionInfluence(string factionName, int amount)
		{
			if (!FactionInfluence.ContainsKey(factionName))
				FactionInfluence[factionName] = 0;

			FactionInfluence[factionName] += amount;
			SaveFactionInfluenceToDatabase(factionName);
		}

		/// <summary>
		/// Gibt Einfluss für Gebäude oder Events.
		/// </summary>
		public void GrantInfluenceForEvent(NetworkCommunicator player, int amount, string reason)
		{
			AddInfluence(player, amount);
			InformationManager.DisplayMessage(new InformationMessage($"🌟 {player.UserName} hat {amount} Einfluss für {reason} erhalten!"));
		}

		/// <summary>
		/// Fügt einem Spieler Einfluss hinzu.
		/// </summary>
		public void AddInfluence(NetworkCommunicator player, int amount)
		{
			if (player == null || amount <= 0) return;

			if (!PlayerInfluence.ContainsKey(player))
				PlayerInfluence[player] = DefaultStartInfluence;

			PlayerInfluence[player] += amount;
			SaveInfluenceToDatabase(player);
			SyncInfluence(player);
			InformationManager.DisplayMessage(new InformationMessage($"🔹 {player.UserName} hat {amount} Einfluss erhalten! Neuer Einfluss: {PlayerInfluence[player]}"));
		}

		/// <summary>
		/// Entfernt Einfluss von einem Spieler (nicht unter 0).
		/// </summary>
		public void RemoveInfluence(NetworkCommunicator player, int amount)
		{
			if (player == null || amount <= 0) return;

			if (!HasInfluence(player, amount))
			{
				InformationManager.DisplayMessage(new InformationMessage($"⚠️ {player.UserName} hat nicht genug Einfluss ({amount} benötigt)!"));
				return;
			}

			PlayerInfluence[player] = Math.Max(MinInfluence, PlayerInfluence[player] - amount);
			SaveInfluenceToDatabase(player);
			SyncInfluence(player);
			InformationManager.DisplayMessage(new InformationMessage($"🔻 {player.UserName} hat {amount} Einfluss verloren! Neuer Einfluss: {PlayerInfluence[player]}"));
		}

		/// <summary>
		/// Prüft, ob der Spieler genug Einfluss für eine Aktion hat.
		/// </summary>
		public bool HasInfluence(NetworkCommunicator player, int amount)
		{
			return player != null && GetInfluence(player) >= amount;
		}

		/// <summary>
		/// Synchronisiert den Einfluss eines Spielers mit dem Netzwerk.
		/// </summary>
		private void SyncInfluence(NetworkCommunicator player)
		{
			if (player == null) return;

			GameNetwork.BeginBroadcastModuleEvent();
			GameNetwork.WriteMessage(new PEInfluenceUpdated(player, GetInfluence(player)));
			GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None, null);
		}
	}
}
