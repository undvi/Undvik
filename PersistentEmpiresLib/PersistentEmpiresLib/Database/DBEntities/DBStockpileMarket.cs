using System;
using System.Collections.Generic;
using System.Text.Json;
using System.ComponentModel.DataAnnotations;

namespace PersistentEmpiresLib.Database.DBEntities
{
    public class DBStockpileMarket
    {
        [Key] // Falls Entity Framework genutzt wird
        public int Id { get; set; }

        [Required]
        [StringLength(255)]
        public string MissionObjectHash { get; set; }

        [Required]
        public string MarketItemsSerialized { get; private set; }

        public DBStockpileMarket(int id, string missionObjectHash, List<MarketItem> marketItems)
        {
            if (id < 0)
                throw new ArgumentException("Id cannot be negative.", nameof(id));

            MissionObjectHash = missionObjectHash ?? throw new ArgumentNullException(nameof(missionObjectHash));
            SetMarketItems(marketItems);
        }

        // Setzt die MarketItems und serialisiert sie als JSON
        public void SetMarketItems(List<MarketItem> marketItems)
        {
            if (marketItems == null)
                throw new ArgumentNullException(nameof(marketItems));

            MarketItemsSerialized = JsonSerializer.Serialize(marketItems);
        }

        // Deserialisiert MarketItemsSerialized zurück in eine Liste
        public List<MarketItem> GetMarketItems()
        {
            return string.IsNullOrEmpty(MarketItemsSerialized)
                ? new List<MarketItem>()
                : JsonSerializer.Deserialize<List<MarketItem>>(MarketItemsSerialized);
        }
    }
}
