using System;
using System.ComponentModel.DataAnnotations;

namespace PersistentEmpiresLib.Database.DBEntities
{
    public class DBHorseMarket
    {
        [Key] // Primärschlüssel für die Datenbank
        public int Id { get; set; }

        [Required]
        [StringLength(255)]
        public string MissionObjectHash { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Stock cannot be negative.")]
        public int Stock { get; private set; }  // Stock darf nicht negativ sein

        // Konstruktor für Initialisierung
        public DBHorseMarket(int id, string missionObjectHash, int stock)
        {
            if (id < 0)
                throw new ArgumentException("Id cannot be negative.", nameof(id));
            if (string.IsNullOrWhiteSpace(missionObjectHash))
                throw new ArgumentException("MissionObjectHash cannot be empty.", nameof(missionObjectHash));
            if (stock < 0)
                throw new ArgumentException("Stock cannot be negative.", nameof(stock));

            Id = id;
            MissionObjectHash = missionObjectHash;
            Stock = stock;
        }

        // Methode zum Hinzufügen von Pferden zum Lagerbestand
        public void AddStock(int amount)
        {
            if (amount < 0)
                throw new ArgumentException("Amount must be positive.", nameof(amount));
            Stock += amount;
        }

        // Methode zum Entfernen von Pferden aus dem Lagerbestand
        public bool RemoveStock(int amount)
        {
            if (amount < 0)
                throw new ArgumentException("Amount must be positive.", nameof(amount));
            if (Stock < amount)
                return false; // Nicht genug Bestand

            Stock -= amount;
            return true;
        }
    }
}

