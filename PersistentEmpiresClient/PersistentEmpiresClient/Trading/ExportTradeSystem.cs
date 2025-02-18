using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace PersistentEmpiresMod.Trading
{
	public class ExportTradeSystem
	{
		private static List<TradeOrder> activeTradeOrders = new List<TradeOrder>();
		private static Random random = new Random();
		private static readonly string savePath = "export_trade_orders.json";

		static ExportTradeSystem()
		{
			LoadTradeOrders();
		}

		public static void GenerateTradeOrder(Faction faction)
		{
			if (faction == null) return;

			string[] possibleGoods = { "Grain", "Iron", "Wood", "Silk" };
			string selectedGood = possibleGoods[random.Next(possibleGoods.Length)];
			int requiredAmount = random.Next(50, 200);
			int goldReward = requiredAmount * 5;
			int influenceReward = requiredAmount / 10;

			TradeOrder newOrder = new TradeOrder(faction.StringId, selectedGood, requiredAmount, goldReward, influenceReward, DateTime.UtcNow);
			activeTradeOrders.Add(newOrder);
			SaveTradeOrders();

			InformationManager.DisplayMessage(new InformationMessage($"Neue Handelsmission: {faction.Name} soll {requiredAmount}x {selectedGood} exportieren!"));
		}

		public static void DeliverGoods(Faction faction, string good, int amount)
		{
			TradeOrder order = activeTradeOrders.Find(o => o.FactionId == faction.StringId && o.Good == good);
			if (order == null || amount < order.RequiredAmount)
			{
				InformationManager.DisplayMessage(new InformationMessage("Lieferung fehlgeschlagen! Falsche Waren oder nicht genug Menge."));
				return;
			}

			faction.Leader.Gold += order.GoldReward;
			faction.Influence += order.InfluenceReward;
			activeTradeOrders.Remove(order);
			SaveTradeOrders();

			InformationManager.DisplayMessage(new InformationMessage($"{faction.Name} hat den Auftrag erfüllt! +{order.GoldReward} Gold, +{order.InfluenceReward} Einfluss."));
		}

		private static void SaveTradeOrders()
		{
			File.WriteAllText(savePath, JsonSerializer.Serialize(activeTradeOrders));
		}

		private static void LoadTradeOrders()
		{
			if (File.Exists(savePath))
			{
				string json = File.ReadAllText(savePath);
				activeTradeOrders = JsonSerializer.Deserialize<List<TradeOrder>>(json) ?? new List<TradeOrder>();
			}
		}

		public static void PlayerEnteredExportArea(NetworkCommunicator player, string good, int amount)
		{
			PersistentEmpireRepresentative rep = player.GetComponent<PersistentEmpireRepresentative>();
			if (rep == null || rep.GetFaction() == null) return;

			DeliverGoods(rep.GetFaction(), good, amount);
		}
	}

	public class TradeOrder
	{
		public string FactionId { get; }
		public string Good { get; }
		public int RequiredAmount { get; }
		public int GoldReward { get; }
		public int InfluenceReward { get; }
		public DateTime Timestamp { get; }

		public TradeOrder(string factionId, string good, int requiredAmount, int goldReward, int influenceReward, DateTime timestamp)
		{
			FactionId = factionId;
			Good = good;
			RequiredAmount = requiredAmount;
			GoldReward = goldReward;
			InfluenceReward = influenceReward;
			Timestamp = timestamp;
		}
	}
}