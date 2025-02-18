1. Forschung & Blueprint-Akademie

    Forschungssystem (Blueprint-Freischaltung)
        Ziel: Spieler schalten Blueprints nicht mehr über Timer, sondern durch das Herstellen einer bestimmten Anzahl von Waffen, Rüstungen oder anderen Items frei.
        Aufgaben:
            Anpassung der UI und Logik in PEAcademyVM.cs und verwandten Klassen, sodass beim Überprüfen der Voraussetzungen (z. B. Anzahl gecrafteter Items) ein Blueprint sofort freigeschaltet werden kann.
            Entfernen oder Auslagern von Timer-/Korreoutine-Logik, die ursprünglich zeitbasierte Forschung steuerte.
            Serverseitige Implementierung:
                Erfassen und Speichern der hergestellten Items pro Spieler bzw. Fraktion (z. B. in einer Datenbank oder in JSON).
                Prüfung der Crafting-Zähler im Crafting-Request-Handler (z. B. in RequestExecuteCraft.cs).
            Feedback an den Client: Senden einer Nachricht (z. B. PEAcademyBlueprintUnlocked.cs), wenn die Bedingungen erfüllt sind.

    Mögliche Erweiterungen:
        Einführung eines kleinen „Tech-Tree“, in dem Abhängigkeiten zwischen Blueprints (z. B. erst IronSword → dann SteelSword) visuell und programmatisch abgebildet werden.
        Optionale Kombination von Elementen: Neben der reinen Anzahl könnten auch Ressourcen-Kosten oder Prestige-Punkte als Voraussetzung dienen.

2. Crafting-Station & Rezepte

    Crafting-UI und Logik
        Bestehende Dateien: PECraftingStationScreen.cs, PECraftingStationVM.cs, PECraftingRecipeVM.cs
        Aufgaben:
            Anpassung der Anzeige: Nur Rezepte, deren zugehöriger Blueprint (Forschung) freigeschaltet wurde, werden aktiv und auswählbar dargestellt – nicht freigeschaltete Rezepte erscheinen ausgegraut oder sind versteckt.
            Verknüpfung: Sicherstellen, dass jede Rezept-ID eindeutig mit einer Blueprint-ID verknüpft ist.

    Serverseitige Validierung
        Beim Erhalt eines Crafting-Requests (z. B. RequestExecuteCraft.cs) muss überprüft werden, ob der entsprechende Blueprint freigeschaltet wurde.
        Nach erfolgreichem Crafting wird der entsprechende Zähler (z. B. „SwordsCrafted“) erhöht, um den Fortschritt im Forschungssystem zu aktualisieren.

3. Fraktions- & Kriegssystem

    Fraktionssystem
        Aufgaben:
            Implementierung einer Mitglieder- und Rangstruktur:
                z. B. Rang 1: max. 20 Mitglieder, Rang 2: max. 30, Rang 3: max. 50 (plus Gebiet), usw.
            Integration von Adelsrängen und Prestige-Mechanik: Spieler können innerhalb der Fraktion durch Prestige aufsteigen, was Einfluss auf interne Entscheidungen und Kriegsführung hat.
            Fraktionsübernahme nur durch Erbfolge oder Umsturz ermöglichen, um mehr Stabilität und Struktur zu gewährleisten.

    Kriegssystem
        Aufgaben:
            Erweiterung um neue Kriegsarten:
                Handelskrieg (Blockieren von Handelsrouten)
                Überfall (kleinere Kriegsaktionen mit Plünderungen)
                Eroberung (Verbessertes System für Dörfer & Burgen)
            Einschränkungen: Kriegserklärungen nur für Adelsränge bzw. hohe Fraktionsleiter erlauben.
            Implementierung von NPC-Unterstützung:
                NPC-Truppen (Wachtruppen, Stadtwachen, Söldner) als Verstärkung und automatische Verteidigung bei Belagerungen.
            AI-Unterstützung:
                Automatischer Ressourcenabbau und Garnisonsbesetzung über einfache AI-Soldaten.

    Datenpersistenz und Automatisierung
        Alle Fraktionsdaten (Mitglieder, Ränge, Gebietszuweisungen) und Kriegsvorgänge sollten serverseitig persistent gespeichert werden.
        Eigene Manager-Klassen (z. B. FactionRankManager, WarSystem) zur Trennung der Logik und besseren Skalierbarkeit.

4. NPC-/AI-System

    Datenstruktur und Speicherung
        Beispielhafte JSON-Struktur für NPCs:

        {
          "npc_id": "NPC_123",
          "npc_type": "Farmer",
          "faction_id": "kingdom_x",
          "current_task": "gather_wood",
          "location": {
            "scene": "town_smithy",
            "position": {"x": 123, "y": 45}
          },
          "inventory": {
            "wood": 10,
            "grain": 0
          },
          "schedule": {
            "work_start": "06:00",
            "work_end": "18:00"
          },
          "state": "Idle"
        }

        Speicherung der NPC-Daten serverseitig (z. B. in einer Datenbank oder als JSON-Datei).

    KI-Logik
        Aufgaben:
            Erstellung von NPCAIBehavior.cs für grundlegende Zustandsmaschinen oder Behavior Trees, die Aufgaben wie „gather wood“, „guard city“ oder „travel“ steuern.
            Entwicklung von AIJobs.cs für spezifische Job-Logik, wie das Anwerben von Söldnern oder das Erfüllen von Aufgaben innerhalb einer Fraktion.
            Implementierung von AIDatabase.cs für das Laden und Speichern der NPC-Daten.

    Leistungsoptimierung
        Ticking-Strategien für viele NPCs:
            Simuliere entfernte NPCs weniger häufig (z. B. durch Hochrechnen), um Performance zu schonen.

5. Handelssystem

    Einflussmechanik im Handel
        ✅ Bereits implementierte Einflussmechanik als Basis.

    Exporthandel für Fraktionen
        Aufgaben:
            Generierung zufälliger Exportaufträge für Fraktionen.
            Implementierung der Logik, bei der Fraktionen die geforderten Güter zum Exporthafen liefern müssen, um zusätzliche Belohnungen zu erhalten.
            Integration der Exporthandel-Mechanik in das bestehende Fraktionssystem, sodass Erfüllung von Exportaufträgen Einfluss und möglicherweise Prestige bringt.

6. Bausystem

    Basis-UI und Bauplätze
        ✅ Existierendes Bau-Menü.
        ✅ Verfügbare fraktionsspezifische und neutrale Bauplätze, die erworben werden können.

    Baumaterialien & Bau-Stufen
        Stufe 1: Hardwood, Stone, Bretter, Lehm
        Stufe 2: Zusätzlich Eisenbarren, Einfluss, Gold
        Stufe 3: Erhöhte Anforderungen an alle Ressourcen

    Gebäudearten & Funktionen
        Lagerhäuser zur Ressourcenspeicherung
        Waffenschmiede (für Waffen-Crafting)
        Rüstungsschmiede (für Rüstungs-Crafting)
        Märkte (für Spieler-Handel)
        Hafen (Exporthandel)
        Felder & Farmen (Nahrungsmittelproduktion)

    Bau-Gameplay
        Ressourcen müssen von Spielern abgeliefert werden.
        Nutzung eines Hammers, um den Baufortschritt sichtbar zu machen und Gebäude fertigzustellen.
        Gebäude können auch abgerissen werden.

    Persistenz & Verwaltung
        ✅ Alle Gebäude werden in der Datenbank gespeichert und bleiben bei Server-Restarts erhalten.
        🔲 Implementierung einer Überprüfung des tatsächlichen Ressourcenverbrauchs beim Bau.
        🔲 Optimierung der Verwaltung der Bauplätze, um Spielern einen besseren Überblick zu geben.

    Belohnungssystem
        🔲 Fertiggestellte Bauprojekte sollen mit Gold und zusätzlichem Einfluss belohnt werden – unter Nutzung des bestehenden Einfluss-Systems.

7. Schmiedesystem

    Integration in Bau- und Crafting-Prozesse
        Aufgaben:
            Weiterentwicklung der bestehenden Logik in der Waffenschmiede und Rüstungsschmiede.
            Optimierung des Crafting-Prozesses, um nahtlos zwischen Bau, Crafting und Forschung (Blueprint-Freischaltung) zu verbinden.
            Optional: Implementierung eines Systems, bei dem das Craften bestimmter Items zusätzliche Blueprints freischaltet, falls dies im Zusammenspiel mit der Akademie gewünscht ist.

Zusammenfassung und Ausblick

Diese Roadmap integriert alle relevanten Bereiche:

    Forschung & Blueprint-Akademie (item-basiert, keine Timer mehr)
    Crafting-Station (mit Rezeptvalidierung und serverseitiger Fortschrittsverfolgung)
    Fraktions- und Kriegssystem (mit Rang-, Prestige- und Kriegsvorgängen inklusive NPC-Unterstützung)
    NPC-/AI-System (für dynamische, fraktionsbezogene Aufgaben)
    Handelssystem (mit Exporthandel und Einflussmechanik)
    Bausystem (mit mehrstufigen Baumaterialien, persistenter Speicherung und Belohnungen)
    Schmiedesystem (als integraler Bestandteil von Crafting und Bau)

Jedes Modul sollte in sich gut gekapselt sein, sodass Änderungen in einem Bereich (z. B. Forschung durch Crafting) nicht ungewollt in anderen Bereichen zu Problemen führen. Durch klare Trennung der Logik in separate Manager und Services wird die Erweiterbarkeit und Wartbarkeit des Codes langfristig gesichert.

Diese Roadmap bietet dir einen umfassenden Überblick und konkrete To-Dos, um dein Projekt weiterzuentwickeln und zu optimieren.


# Persistent Empires Open Sourced

Persistent empires is a Mount & Blade II: Bannerlord mod that introduces some new mechanics to multiplayer gaming that allow players to do roleplay, team fight, clan fight, farming, mining etc...

## Requirements

A Windows Server ( )

You need a database, MariaDB (Preferably 10.4.27-MariaDB) installation (https://mariadb.com/kb/en/installing-mariadb-msi-packages-on-windows/)
or you may use MYSQL8 (8.0.36 should work fine) (https://dev.mysql.com/downloads/installer/)

You need Mount & Blade II: Bannerlord Dedicated Server ( can be installed from steamcmd https://steamcdn-a.akamaihd.net/client/installer/steamcmd.zip )

`steamcmd.exe +force_install_dir "C:\Desktop\YourServerFolderLocation" +login your_steam_username "your_steam_password" +app_update 1863440 validate`

## Installation

- After installing the dedicated server, you will see a folder called `Modules` inside of your dedicated server location.

- Extract PersistentEmpires Modules file and folders `YourServerLocation/Modules/PersistentEmpires`

- Extract `PersistentEmpires/bin/Win64_ShippingServer` to `YourServerLocation/bin/Win64_ShippingServer`

- Install a MySQL explorer (Navicat, DBeaver, phpmyadmin or mysqlworkbench)

<p align="center">
  <img src="https://github.com/Heavybob/PersistentEmpires-OpenSourced/assets/4519067/e83817b5-a4e7-44a3-81c0-bb099206452a" alt="DBeaver Setup">
</p>
<p align="center"><em>DBeaver Setup</em></p>

- Authorize to your MariaDB setup and create a database, lets name it pe_production

<p align="center">
  <img src="https://github.com/Heavybob/PersistentEmpires-OpenSourced/assets/4519067/a7c801f7-92a7-430b-a77d-7ee90d3dcff5" alt="image">
</p>
<p align="center"><em>Created a database called pe_production, ignore other databases</em></p>

- Now you need to set up your database connection for your PE server. To do that go to file
`YourServerFolder/Modules/PersistentEmpires/ModuleData/Configs/SaveConfig.xml`

- If you edit the file you see the file content like this:

SaveConfig.xml
```xml
<DatabaseConfig>
	<ConnectionString>
		Server=localhost;User ID=root;Password=password;Database=pe_production
	</ConnectionString>
</DatabaseConfig>
```

- Set the User ID, Password and Database field for your database.

- You need to create this file at this location.  `YourServerLocation/Modules/Native/persistent_empires.txt`

persistent_empires.txt
```txt
ServerName Persistent Empires
GameType PersistentEmpires
Map pe_test3
CultureTeam1 khuzait
CultureTeam2 vlandia
AllowPollsToKickPlayers False
AllowPollsToBanPlayers False
AllowPollsToChangeMaps False
MapTimeLimit 60000
RespawnPeriodTeam1 5
RespawnPeriodTeam1 5
MinNumberOfPlayersForMatchStart 0
MaxNumberOfPlayers 500
DisableInactivityKick True
add_map_to_automated_battle_pool pe_test3
end_game_after_mission_is_over
start_game_and_mission
```
- Create your starter bat file and you are ready to go.

```.bat
start DedicatedCustomServer.Starter.exe /dedicatedcustomserverconfigfile persistent_empires.txt /port 7211 /DisableErrorReporting /no_watchdog /tickrate 75 /multiplayer /dedicatedcustomserverauthtoken INSERTCUSTOMSERVERAUTHTOKENHERE _MODULES_*Native*Multiplayer*PersistentEmpires*_MODULES_
```

- Don't forget to set your `/dedicatedcustomserverauthtoken` that you must obtain from bannerlord.
- Refer to https://moddocs.bannerlord.com/multiplayer/hosting_server/

- Ensure you've also set the correct map you intend to use in your persistent_empires.txt

- Maps must only be placed into the `Multiplayer` module. `YourServerLocation/Modules/Multiplayer/SceneObj` and must be targeted with `add_map_to_automated_battle_pool` in your persistent_empires.txt in order to appear as downloadable via the ingame download panel. 

## Server Configuration

Persistent Empires allow users to configure some options server side.

You can find the configuration file under `YourServerLocation/Modules/PersistentEmpires/ModuleData/Configs/GeneralConfig.xml`

This file could be empty, you can use this configuration file below if you wish however undefined values will just utilize the default values.

GeneralConfig.xml
```xml
<GeneralConfig>
  <VoiceChatEnabled>true</VoiceChatEnabled>
  <StartingGold>1000</StartingGold>

  <!-- Auto Restart Settings -->
  <AutorestartActive>true</AutorestartActive>
  <AutorestartIntervalHours>24</AutorestartIntervalHours>

  <!-- Bank Settings -->
  <BankAmountLimit>1000000</BankAmountLimit>

  <!-- Combat Log System -->
  <CombatlogDuration>5</CombatlogDuration>

  <!-- Doctor Settings -->
  <RequiredMedicineSkillForHealing>50</RequiredMedicineSkillForHealing>
  <MedicineHealingAmount>15</MedicineHealingAmount>
  <MedicineItemId>pe_doctorscalpel</MedicineItemId>

  <!-- Hunger Settings -->
  <HungerInterval>72</HungerInterval>
  <HungerReduceAmount>1</HungerReduceAmount>
  <HungerRefillHealthLowerBoundary>25</HungerRefillHealthLowerBoundary>
  <HungerHealingAmount>10</HungerHealingAmount>
  <HungerHealingReduceAmount>5</HungerHealingReduceAmount>
  <HungerStartHealingUnderHealthPct>75</HungerStartHealingUnderHealthPct>

  <!-- Lord Poll Settings -->
  <LordPollRequiredGold>1000</LordPollRequiredGold>
  <LordPollTimeOut>60</LordPollTimeOut>

  <!-- Politics -->
  <WarDeclareTimeOut>30</WarDeclareTimeOut>
  <PeaceDeclareTimeOut>30</PeaceDeclareTimeOut>
  <MaxBannerLength>100</MaxBannerLength>

  <!-- Thief -->
  <LockpickItem>pe_lockpick</LockpickItem>
  <PickpocketingItem>pe_stealing_dagger</PickpocketingItem>
  <RequiredPickpocketing>10</RequiredPickpocketing>
  <PoisonItemId>pe_poison_dagger</PoisonItemId>
  <AntidoteItemId>pe_antidote</AntidoteItemId>
  <PickpocketingPercentageThousands>10</PickpocketingPercentageThousands>
  <DeathMoneyDropPercentage>25</DeathMoneyDropPercentage>

  <!-- Misc -->
  <AnimationsEnabled>true</AnimationsEnabled>
  <AgentLabelEnabled>true</AgentLabelEnabled> <!-- Banners on top of head -->
  <DontOverrideMangonelHit>false</DontOverrideMangonelHit> <!-- Decide to override mangonel damage to the players -->
  <NameChangeGold>5000</NameChangeGold> <!-- Name changing gold -->
  <NameChangeCooldownInSeconds>3600</NameChangeCooldownInSeconds> <!-- Name changing cooldown -->
  <RepairTimeoutAfterHit>60</RepairTimeoutAfterHit> <!-- Cooldown for repairs after damage -->
  <DecapitationChance>25</DecapitationChance>
</GeneralConfig>
```

## API

Persistent Empires features an API 

With it, you can submit GET/POST requests in order to remotely perform functions on the server.
This can be extremely powerful if your intention is to create moderation tools such as through an external panel or a discord bot. 

You can find the configuration file under `YourServerLocation/Modules/PersistentEmpires/ModuleData/Configs/ApiConfig.xml`

You will need to generate a secretkey and use said secretkey to generate a JWT token. DANGER - DO NOT USE THE EXAMPLE KEY, generate your own.

ApiConfig.xml
```xml
<ApiConfig>
	<Port>3169</Port>
	<SecretKey>2b6f1a8c3e74d0f5e92a7bdcf5013c1e9f5aeb6a74066eb45d03f11a5b7486d13721b53223e9f4d87c66b32b6e3b5d4ff8c951b1b05619d1e2c2616c1f8d39ba</SecretKey>
</ApiConfig>
```

Here are the following things you can do using the api.

```
POST /compensateplayer - Gives a player gold.
{
    "PlayerId": "player_id_here",
    "Gold": 100
}

POST /kickplayer - Kicks player from the server.
{
    "PlayerId": "player_id_here"
}

POST /fadeplayer - Kills a player and deletes their armor.
{
    "PlayerId": "player_id_here"
}

POST /unbanplayer - Unbans a player.
{
    "PlayerId": "player_id_here",
    "UnbanReason": "Reason for unbanning"
}

POST /banplayer - Bans a player.
{
    "PlayerId": "player_id_here",
    "BanEndsAt": "2024-05-01T00:00:00",
    "BanReason": "Reason for banning"
}

POST /announce - Posts an announcement into the server for all players to see.
{
    "Message": "Your announcement message here"
}

GET /servercap - Returns player count.

GET /restart - Issues Restart.

GET /shutdown - Shuts down the server.
```
