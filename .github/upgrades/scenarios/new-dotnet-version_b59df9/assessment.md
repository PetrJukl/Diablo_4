# Assessment: migrace aplikace `Diablo 4` z `WinForms` do `WinUI 3`

## Executive summary

Projekt je dnes malá desktopová aplikace ve `Windows Forms` na `.NET Framework 4.8.1` s jedním hlavním formulářem a jedním pomocným formulářem. Migrace do `WinUI 3` je proveditelná, ale nepůjde o přímý upgrade. Bude to přepis UI vrstvy a části životního cyklu aplikace.

Největší překážky nejsou v doménové logice, ale v platformních závislostech:
- přímé použití `System.Windows.Forms`
- `NotifyIcon` a `ContextMenuStrip`
- `ClickOnce` / `ApplicationDeployment`
- start aplikace přes `Application.Run(new Form1())`
- designer-generated formuláře místo `XAML`

Naopak přenositelná je většina ne-UI logiky:
- práce se soubory v `%LocalAppData%`
- měření časů a výpočet týdnů
- kontrola procesů přes `System.Diagnostics.Process`
- síťová dostupnost přes `Ping`
- část P/Invoke a UI Automation logiky

Doporučený cílový stav pro stabilní modernizaci je:
- `WinUI 3` desktop aplikace
- cílení na moderní `.NET` (`.NET 8` jako konzervativní stabilní volba)
- náhrada ClickOnce aktualizací za novou distribuční strategii
- zachování tray scénáře (`NotifyIcon`) přes kompatibilní knihovnu nebo pomocnou Win32 vrstvu

## Scope potvrzený uživatelem

Uživatel potvrdil:
- migraci celé aplikace do `WinUI 3`
- zachování tray scénáře a související logiky
- zachování stávající logiky s tím, že změny budou dělané postupně
- budoucí publikaci s kontrolou nových verzí přes `OneDrive` nebo `GitHub`

## Zjištěný stav aplikace

### Projekt a runtime
- Projekt je starý nesdk-styl `csproj`: `Diablo 4/Diablo 4.csproj`
- Cílový framework je `.NET Framework 4.8.1`: `Diablo 4/Diablo 4.csproj:11`
- Aplikace je `WinExe`: `Diablo 4/Diablo 4.csproj:8`
- Používá reference na `System.Windows.Forms`, `System.Deployment`, `System.Drawing`, `UIAutomation*`: `Diablo 4/Diablo 4.csproj:76-90`

### UI struktura
- Hlavní okno je `Form1` s labely, tray ikonou a kontextovým menu: `Diablo 4/Form1.Designer.cs:33-143`
- Druhé okno `WeekendMotivation` je samostatný formulář s comboboxem a tlačítky: `Diablo 4/WeekendMotivation.Designer.cs:32-126`
- Start aplikace je čistě winformsový: `Diablo 4/Program.cs:18-22`

### Distribuce a aktualizace
- Projekt je nastavený pro ClickOnce publikaci: `PublishUrl`, `InstallFrom`, `ApplicationVersion`, `GenerateManifests`, `SignManifests`: `Diablo 4/Diablo 4.csproj:16-31`, `54-67`
- Aplikace za běhu používá `ApplicationDeployment` pro kontrolu a instalaci aktualizace: `Diablo 4/Form1.cs:167-220`
- Současná logika aktualizace je vázaná na dostupnost konkrétní IP adresy `192.168.0.35`: `Diablo 4/Form1.cs:167-175`, `224-235`, `260+`

### Aplikační logika
- Lokální stav se ukládá do `%LocalAppData%\Diablo Log\Diablo IV.txt`: `Diablo 4/Form1.cs:238-257`
- Probíhá polling procesů a zápis statistik do textového souboru: `Diablo 4/Form1.cs`, třída `ProcessMonitor`
- Je použita UI Automation nad Firefox oknem přes `FindWindow` + `AutomationElement`: `Diablo 4/Form1.cs`, metoda `CheckAllOpenTabsForUdemy`
- Aplikace se při zavření neshazuje, ale schovává do tray: `Diablo 4/Form1.cs:260+`

## Hlavní kompatibilitní zjištění pro `WinUI 3`

### 1. `WinForms` UI nepůjde převést 1:1
`Form`, `Label`, `ComboBox`, `ContextMenuStrip`, event wiring z designeru a vlastnosti formuláře (`FormBorderStyle`, `StartPosition`, `VisibleChanged`, `FormClosing`) je nutné přepsat do `XAML` a `WinUI 3` životního cyklu.

Dopad:
- vysoký pro UI vrstvu
- nízký až střední pro business logiku

### 2. `NotifyIcon` není nativní součást `WinUI 3`
Požadavek na zachování tray scénáře je realizovatelný, ale ne nativně stejným API jako ve `WinForms`.

Dopad:
- bude potřeba použít pomocnou knihovnu nebo Win32 integraci
- tray menu nebude `ContextMenuStrip`, ale jiný mechanismus

Stav hodnocení:
- zachování funkcionality: ano
- přímý převod kódu: ne

### 3. `ClickOnce` / `ApplicationDeployment` je migrační blokátor pro přímý přenos
`WinUI 3` nepřebere stávající aktualizační logiku založenou na `System.Deployment.Application.ApplicationDeployment`.

Dopad:
- kompletní výměna update mechanismu
- část dnešní publikační konfigurace v `csproj` ztratí význam

Stav hodnocení:
- ekvivalent existuje jen změnou distribuční strategie, ne přepisem 1:1

### 4. `System.Windows.Automation` a P/Invoke jsou potenciálně přenositelné
Desktop `WinUI 3` aplikace může dál používat Win32 interoperabilitu. Logika s `FindWindow`, `AutomationElement` a `Process` tedy pravděpodobně zůstane použitelná, ale je potřeba ji ověřit v novém hostingu aplikace a správném thread modelu.

Dopad:
- střední riziko
- není to primární blokátor migrace

### 5. Skrytí okna místo ukončení je v `WinUI 3` řešitelné
Scénář „zavření = schovat do tray“ je realizovatelný, ale bude se řešit jinak než přes `FormClosingEventArgs.Cancel`.

Dopad:
- funkčně zachovatelné
- implementačně jiné API

## Zjištění k publikaci a update kanálu

Uživatel chce publikovat na `OneDrive` nebo `GitHub` a na klientovi pravidelně kontrolovat nové verze.

### Varianta A: hostovaný instalační/update kanál
Vhodná pro scénář, kdy aplikace sama nebo instalační mechanismus kontroluje nový balíček z pevného URL.

Hodnocení:
- `OneDrive`: možné jen pokud bude k dispozici stabilní a přímo stažitelný odkaz; prakticky bývá křehčí
- `GitHub`: vhodnější pro veřejné/verzované buildy, zvlášť přes release assety nebo vlastní manifest

### Varianta B: vlastní update logika v aplikaci
Aplikace by kontrolovala manifest/verzi na vzdáleném úložišti, stáhla nový instalátor/balíček a nabídla restart.

Hodnocení:
- dobře se hodí pro `GitHub`
- lze přizpůsobit i `OneDrive`
- nejblíže odpovídá dnešnímu chování s dotazem na aktualizaci

### Varianta C: pokračovat v clickonce-like přístupu
Pro cílový `WinUI 3` stav není stávající `ApplicationDeployment` cesta vhodná.

Hodnocení:
- nedoporučeno jako migrační cíl

## Předběžné doporučení k cílové architektuře

### Doporučená modernizační větev
1. Zachovat desktop charakter aplikace pro Windows.
2. Přesunout doménovou a monitorovací logiku mimo UI vrstvy.
3. Přepsat `Form1` a `WeekendMotivation` do `WinUI 3` oken / dialogů.
4. Tray scénář řešit separátně přes specializovanou integraci.
5. Update mechanismus navrhnout znovu, ideálně s jasným manifestem verze mimo aplikaci.

### Doporučený update backend
Z hlediska provozu je praktičtější `GitHub` než `OneDrive`, pokud:
- nevadí publikace buildů mimo lokální síť
- je požadovaná stabilní historie verzí
- je žádoucí automatická kontrola dostupné verze

`OneDrive` dává smysl spíš pro soukromé, malé nasazení, ale je slabší jako dlouhodobě stabilní update feed.

## Rizika a nejasnosti

### Vysoká rizika
- přepis celé UI vrstvy z `WinForms` do `WinUI 3`
- náhrada `ClickOnce` aktualizací
- zachování tray funkcí bez regresí

### Střední rizika
- chování `UI Automation` vůči Firefoxu v nové aplikaci
- rozdíly v startup/lifecycle modelu
- budoucí podpis a distribuce instalace

### Nízká rizika
- práce se soubory
- výpočty časů
- práce s procesy a `Ping`

## Celkové hodnocení proveditelnosti

Migrace je **proveditelná**, ale je to **refaktor/migrace aplikace**, ne běžný framework upgrade. Pro malou aplikaci je to rozumné, protože rozsah UI je omezený. Největší práci budou tvořit:
- nové UI v `WinUI 3`
- tray integrace
- nový update mechanismus

## Assessment verdict

- Přechod na `WinUI 3`: **Ano, doporučeno jako samostatná migrace**
- Přímý převod ze `WinForms`: **Ne, nutný přepis UI vrstvy**
- Zachování `NotifyIcon` scénáře: **Ano, s náhradní integrací**
- Zachování stávající doménové logiky: **Ve velké míře ano**
- Publikace přes `OneDrive`: **Možná, ale méně robustní**
- Publikace přes `GitHub`: **Vhodnější pro dlouhodobý update scénář**
