namespace PersistentEmpiresLib.Database.DBEntities
{
    public class DBFactions
    {
        public int Id { get; set; }
        public int FactionIndex { get; set; }
        public string Name { get; set; }
        public string BannerKey { get; set; }
        public string LordId { get; set; }
        public long PollUnlockedAt { get; set; }
        public string Marshalls { get; set; } // Gespeichert als JSON oder CSV

        // 🔹 Rang & Mitgliederverwaltung
        public int Rank { get; set; } // Rang der Fraktion (1-5)
        public int Gold { get; set; } // Fraktionsvermögen
        public int MaxMembers { get; set; } // Maximale Mitgliederanzahl basierend auf Rang
        public int Influence { get; set; } // Einfluss der Fraktion für Diplomatie

        // 🔹 Diplomatie & Vasallensystem
        public string Allies { get; set; } // JSON-String mit verbündeten Fraktionen
        public string Vassals { get; set; } // JSON-String mit Vasallenfraktionen
        public string WarDeclarations { get; set; } // Fraktionen im Krieg, gespeichert als JSON

        // 🔹 Wirtschaft & Steuern
        public int TaxRate { get; set; } // Steuersatz für Fraktionsmitglieder
        public int ExportBonus { get; set; } // Bonus für Exporte basierend auf Fraktionsgebiet
        public string ControlledTerritories { get; set; } // JSON mit eroberten Gebieten
        public string ResourceBonuses { get; set; } // JSON mit Boni (z. B. mehr Holz, Erz)

        // 🔹 Schutzmaßnahmen für ungültige Werte
        public DBFactions()
        {
            Rank = Math.Clamp(Rank, 1, 5);
            Gold = Math.Max(Gold, 0);
            MaxMembers = Math.Max(MaxMembers, 10);
            Influence = Math.Max(Influence, 0);
            TaxRate = Math.Clamp(TaxRate, 0, 50); // Steuern maximal 50%
        }
    }
}
