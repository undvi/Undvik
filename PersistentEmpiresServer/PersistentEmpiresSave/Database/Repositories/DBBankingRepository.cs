using Dapper;
using PersistentEmpiresLib.Database.DBEntities;
using PersistentEmpiresLib.PersistentEmpiresMission.MissionBehaviors;
using PersistentEmpiresSave.Database.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using PersistentEmpiresLib.Helpers;
using TaleWorlds.MountAndBlade;

namespace PersistentEmpiresSave.Database.Repositories
{
    public class DBBankingRepository
    {
        public static int Tax_Rate { get; set; }

        public class DBBank
        {
            public int Id { get; set; }
            public string PlayerId { get; set; }
            public int Amount { get; set; }
        }

        // Initialisierung der Bank-Events
        public static void Initialize()
        {
            BankingComponent.OnBankQuery += QueryBankBalance;
            BankingComponent.OnBankDeposit += DepositToBank;
            BankingComponent.OnBankWithdraw += WithdrawFromBank;

            // Steuer wird aus der Konfig geladen (Standard: 10%)
            Tax_Rate = (100 - PersistentEmpiresLib.ConfigManager.GetIntConfig("BankTaxRate", 10));
        }

        // ✅ Abfrage des Bankkontos
        public static int QueryBankBalance(NetworkCommunicator player)
        {
            var collection = DBConnection.Connection.Query<DBPlayer>(
                "SELECT BankAmount FROM Players WHERE PlayerId = @PlayerId",
                new { PlayerId = player.VirtualPlayer.ToPlayerId() });

            return collection.FirstOrDefault()?.BankAmount ?? 0; // Falls kein Ergebnis → 0
        }

        // ✅ Einzahlung in die Bank (mit Steuerabzug)
        public static void DepositToBank(NetworkCommunicator player, int amount)
        {
            // Steuerabzug berechnen
            int amountAfterTax = (amount * Tax_Rate) / 100;

            DBConnection.Connection.Execute(
                "UPDATE Players SET BankAmount = BankAmount + @Amount WHERE PlayerId = @PlayerId",
                new { PlayerId = player.VirtualPlayer.ToPlayerId(), Amount = amountAfterTax });

            // ✅ Transaktions-Logging
            LogTransaction(player, "Deposit", amount, amountAfterTax);
        }

        // ✅ Abhebung mit Fehlerprüfung
        public static int WithdrawFromBank(NetworkCommunicator player, int amount)
        {
            int currentBalance = QueryBankBalance(player);

            if (amount > currentBalance)
            {
                // ❌ Fehlermeldung: Spieler kann nicht mehr abheben, als er hat
                player.SendChatMessage("❌ Fehler: Du kannst nicht mehr Geld abheben, als du besitzt!");
                return 0;
            }

            // Geld abheben
            DBConnection.Connection.Execute(
                "UPDATE Players SET BankAmount = BankAmount - @Amount WHERE PlayerId = @PlayerId",
                new { PlayerId = player.VirtualPlayer.ToPlayerId(), Amount = amount });

            // ✅ Transaktions-Logging
            LogTransaction(player, "Withdraw", amount, amount);

            return amount;
        }

        // ✅ Logging-Funktion für Transaktionen
        private static void LogTransaction(NetworkCommunicator player, string transactionType, int amount, int finalAmount)
        {
            string logEntry = $"[{DateTime.Now:HH:mm:ss}] {player.VirtualPlayer.UserName} | {transactionType} | Betrag: {amount} | Nach Steuer: {finalAmount}";

            DBConnection.Connection.Execute(
                "INSERT INTO BankLogs (PlayerId, TransactionType, Amount, FinalAmount, Timestamp) VALUES (@PlayerId, @TransactionType, @Amount, @FinalAmount, @Timestamp)",
                new
                {
                    PlayerId = player.VirtualPlayer.ToPlayerId(),
                    TransactionType = transactionType,
                    Amount = amount,
                    FinalAmount = finalAmount,
                    Timestamp = DateTime.UtcNow
                });

            // Debug-Log für den Server
            Console.WriteLine(logEntry);
        }
    }
}
