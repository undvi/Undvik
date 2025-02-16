Roadmap zur Verbesserung der MOD in RP richtung 

Hier ist eine strukturierte Roadmap für dein Projekt. Diese kannst du auch für dein Team nutzen, damit jeder weiß, welche Features und Anpassungen geplant sind.


---

Persistent Empires Mod - Erweiterungs-Roadmap

Phase 1: Analyse & Planung

:white_check_mark: Projektstruktur analysieren

Identifikation der relevanten Dateien für:

Handelssystem

Schmiedesystem

Klassensystem

Fraktions- & Rängesystem

Kriegssystem

AI-System

Bausystem
:white_check_mark: Bestehenden Code dokumentieren
:white_check_mark: Backup des aktuellen Codes erstellen




---

Phase 2: Neue Features & Überarbeitung bestehender Systeme

1. Handelssystem überarbeiten

:black_square_button: Spielerbasierte Märkte statt statischer Shops
:black_square_button: Marktstände für Spieler einführen
:black_square_button: Dynamische Preise & Handelssteuer für Städte & Burgen

2. Schmiedesystem erweitern

:white_check_mark:Schmiede erfordert Bau & Blueprints
:white_check_mark:Blueprints müssen in einer Akademie erforscht werden
:white_check_mark:Neue Waffen & Rüstungen freischalten durch Forschung

3. Klassensystem überarbeiten & begrenzen

:black_square_button: Neue Klassenhierarchie mit festen Limits
:black_square_button: Exklusive Führungsklassen (Lord, Ritter, Offizier)
:white_check_mark:Beschränkung auf eine begrenzte Anzahl an Eliteeinheiten
:black_square_button: Balancing der Geschwindigkeit & Kampfstärken

4. Fraktions- & Rängesystem

:white_check_mark: Adelsränge einführen mit Einfluss auf:

Anzahl an besitzbaren Ländereien

Rekrutierbare Truppenarten
:white_check_mark: Ränge können durch Prestige & Events verdient werden
:white_check_mark:Ränge sind an den Fraktionsanführer gebunden & nicht übertragbar (außer durch Erbfolge)

5. Kriegssystem erweitern
:white_check_mark:Neue Kriegsarten (z. B. Handelskrieg, Überfall, Eroberung)
:white_check_mark: Kriegserklärung nur mit bestimmtem Adelsrang möglich
:black_square_button: Eroberungssystem für Dörfer & Burgen erweitern

6. AI-System für lebendige NPCs

:black_square_button: Dynamische NPCs, die arbeiten, kämpfen & mit Spielern interagieren
:black_square_button: Bauern sammeln Ressourcen, Schmiede arbeiten in Werkstätten
:black_square_button: Stadtwachen können Spielern den Weg zeigen
:black_square_button: Kriegsmodus: NPCs rüsten sich & kämpfen mit
:black_square_button: Optional: KI-Dialoge mit ChatGPT (falls technisch umsetzbar)

7. Bausystem überarbeiten

:black_square_button: Neues Bau-Menü mit Auswahl für Gebäude
:black_square_button: Platzierung nur an bestimmten Baupunkten möglich
:black_square_button: Spieler können eigene Gebäude für Handel & Produktion bauen

8. Multiplayer-Integration von neuen Items (optional)

:black_square_button: Versuch, zusätzliche Rüstungen & Waffen in Multiplayer einzufügen
:black_square_button: Server- und clientseitige Item-Registrierung testen


---

Phase 3: Testen & Optimieren

:black_square_button: Alpha-Test mit internen Spielern
:black_square_button: Balancing-Anpassungen für Klassen, Wirtschaft & Krieg
:black_square_button: Bugfixing & Performance-Optimierung


---

Phase 4: Veröffentlichung & Weiterentwicklung

:black_square_button: Beta-Release für die Community
:black_square_button: Feedback sammeln & Features anpassen
:black_square_button: Regelmäßige Updates & neue Inhalte


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
