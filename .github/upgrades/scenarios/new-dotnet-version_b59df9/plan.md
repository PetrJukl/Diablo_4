# Plán migrace: WinForms → WinUI 3

## Obsah
- [1. Executive Summary](#1-executive-summary)
- [2. Migrační strategie](#2-migrační-strategie)
- [3. Detailní analýza závislostí](#3-detailní-analýza-závislostí)
- [4. Plány pro jednotlivé komponenty](#4-plány-pro-jednotlivé-komponenty)
- [5. Řízení rizik](#5-řízení-rizik)
- [6. Testování a validace](#6-testování-a-validace)
- [7. Hodnocení složitosti a náročnosti](#7-hodnocení-složitosti-a-náročnosti)
- [8. Source control strategie](#8-source-control-strategie)
- [9. Kritéria úspěchu](#9-kritéria-úspěchu)

---

## 1. Executive Summary

### Zjištěné metriky
- **Rozsah**: 1 projekt, 2 formuláře (Form1, WeekendMotivation)
- **Aktuální stav**: .NET Framework 4.8.1 + Windows Forms
- **Cílový stav**: .NET 8 + WinUI 3 desktop aplikace
- **Hlavní závislosti**: System.Windows.Forms, System.Deployment, NotifyIcon, ClickOnce, IP-vázaná pomocná logika
- **Kritická funkcionalita**: Tray scénář, process monitoring, auto-update z GitHubu, podmíněná web kontrola podle názvu PC

### Klasifikace složitosti: **Medium**
- Malý rozsah aplikace (1 projekt)
- Vysoká míra změn v UI vrstvě (kompletní přepis z WinForms do WinUI 3 XAML)
- Střední doménová logika k zachování
- 2 vysoká rizika: UI přepis, náhrada update mechanismu
- 1 střední riziko: NotifyIcon integrace

### Zvolená iterační strategie: **Phase-based migration**
Postupný přechod po fázích:
1. Příprava infrastruktury a projektu
2. Migrace doménové logiky
3. Migrace UI (hlavní okno)
4. Migrace UI (motivační dialog)
5. Integrace tray funkcionalitě
6. Implementace nového update mechanismu
7. Validace a dokončení

### Očekávaný počet iterací: 7 hlavních fází

---

## 2. Migrační strategie

### Vybraný přístup: **Incremental Migration (Postupná migrace)**

**Zdůvodnění:**
- Aplikace má jasné vrstvy: doménová logika, UI, platformní integrace
- Každá vrstva má různé migrační riziko
- Postupný přístup umožní validaci po každé fázi
- Minimalizace rizika kompletního selhání
- Lepší kontrola nad kvalitou každého kroku

### Migrační fáze

#### Fáze 0: Příprava (Low complexity)
- Instalace .NET 8 SDK
- Instalace WinUI 3 templates
- Příprava source control větve
- Backup současného stavu

#### Fáze 1: Nový projekt + doménová logika (Medium complexity)
- Vytvoření nového WinUI 3 projektu SDK-style
- Migrace `ProcessMonitor` třídy (zachování logiky, úprava pro WinUI threading)
- Migrace utility metod (file handling, machine context checks)
- Validace: business logika funguje nezávisle na UI

#### Fáze 2: Hlavní okno (High complexity)
- Přepis `Form1` do `MainWindow.xaml`
- XAML layout pro labels, background image
- Data binding pro dynamický text
- Window lifecycle (hide to tray místo close)
- Validace: hlavní okno se zobrazí, aktualizuje data

#### Fáze 3: Motivační okno (Medium complexity)
- Přepis `WeekendMotivation` do XAML
- ComboBox + tlačítka v moderním WinUI 3 stylu
- Logika spouštění her
- Validace: dialog funguje samostatně i při volání z hlavního okna

#### Fáze 4: NotifyIcon integrace (High complexity)
- Integrace knihovny pro tray support (např. H.NotifyIcon nebo vlastní Win32 wrapper)
- Tray ikona, tooltip, kontextové menu
- Obnovení okna z tray
- Validace: aplikace se chová jako původní tray app

#### Fáze 5: Update mechanismus (High complexity)
- Návrh manifestu verzí (JSON na GitHub/OneDrive)
- Logika kontroly aktualizace při startu
- Download a aplikace updatu
- Restart aplikace po updatu
- Validace: update flow funguje end-to-end

#### Fáze 6: Finální validace (Medium complexity)
- Smoke testing všech scénářů
- Kontrola memory leaks
- Verifikace všech původních funkcí
- Příprava pro deployment

### Paralelní vs. sekvenční provedení
**Sekvenční provedení doporučeno:**
- Každá fáze závisí na předchozí
- Malý tým (1 vývojář)
- Redukce koordinačních nákladů

### Ordering principles
1. **Infrastruktura před logikou**: Nejdřív projekt, pak kód
2. **Logika před UI**: Oddělení business logiky usnadní testování UI
3. **Core UI před integracemi**: Základní okna fungují dřív než tray/update
4. **High-risk poslední**: Kritické platformní integrace až po stabilizaci aplikace

---

## 3. Detailní analýza závislostí

### Přehled závislostí migrace

```
Fáze 0: Příprava
  ↓
Fáze 1: Nový WinUI 3 projekt + doménová logika
  ↓
Fáze 2: Hlavní okno (Form1 → MainWindow)
  ↓
Fáze 3: Motivační okno (WeekendMotivation → WeekendMotivationWindow)
  ↓
Fáze 4: NotifyIcon integrace
  ↓
Fáze 5: Update mechanismus
  ↓
Fáze 6: Validace
```

### Kritická cesta
1. **Projekt a infrastruktura** - nelze pokračovat bez SDK-style projektu pro WinUI 3
2. **Doménová logika** - musí být oddělená od UI před přepisem formulářů
3. **Hlavní okno** - základní UI musí fungovat před přidáním dalších oken
4. **NotifyIcon** - vyžaduje funkční hlavní okno
5. **Update mechanismus** - finální integrace po stabilizaci aplikace

### Žádné cirkulární závislosti
Migrace je lineární - každá fáze staví na předchozí bez zpětných vazeb.

### Skupinování pro migraci

**Fáze 1: Infrastruktura**
- Vytvoření nového WinUI 3 projektu
- Migrace business logiky (ProcessMonitor, file utils)

**Fáze 2-3: UI migrace**
- Form1 → MainWindow.xaml
- WeekendMotivation → WeekendMotivationWindow.xaml

**Fáze 4-5: Platformní integrace**
- Tray funkcionalita
- Auto-update mechanismus

**Fáze 6: Validace**
- End-to-end testování

---

## 4. Plány pro jednotlivé komponenty

### Fáze 0: Příprava prostředí

**Aktuální stav**: Starý .NET Framework 4.8.1 projekt  
**Cílový stav**: Připraveno pro migraci  
**Složitost**: Low

#### Kroky migrace

1. **Ověřit .NET 8 SDK instalaci**
   - Spustit `dotnet --list-sdks`
   - Pokud chybí, stáhnout z https://dotnet.microsoft.com/download/dotnet/8.0

2. **Instalovat WinUI 3 project templates**
   - Visual Studio 2022 s Windows App SDK workload
   - Nebo `dotnet new install Microsoft.WindowsAppSDK.Templates`

3. **Vytvořit novou source control větev**
   - `git checkout -b feature/winui3-migration`
   - Commit současného stavu jako výchozí bod

4. **Zdokumentovat současnou funkcionalitu**
   - Screenshots hlavního okna
   - Seznam všech funkcí
   - Test scenários pro validaci

#### Validační checklist

- [ ] .NET 8 SDK nainstalováno
- [ ] WinUI 3 templates dostupné
- [ ] Nová větev vytvořena
- [ ] Současný stav zacommitován
- [ ] Dokumentace funkcionality připravena

---

### Fáze 1: Nový WinUI 3 projekt + doménová logika

**Aktuální stav**: 
- Starý nesdk-style csproj
- Business logika smíšená s UI v Form1.cs
- ProcessMonitor třída používá WinForms Timer

**Cílový stav**:
- Nový WinUI 3 projekt SDK-style
- ProcessMonitor oddělený od UI
- Business logika testovatelná samostatně

**Složitost**: Medium

#### Kroky migrace

1. **Vytvořit nový WinUI 3 Desktop projekt**
   - Visual Studio: New Project → "Blank App, Packaged (WinUI 3 in Desktop)"
   - Název: `Diablo4.WinUI` (nebo ponechat `Diablo 4`)
   - Target: .NET 8
   - Package name: `Diablo4.WinUI` (pro MSIX packaging)

2. **Nastavit projekt struktu**
   - Vytvořit složky: `/Services`, `/Models`, `/ViewModels`, `/Views`, `/Helpers`
   - Zachovat `/Pictures` pro assety

3. **Migrovat ProcessMonitor třídu**
   - Zkopírovat `ProcessMonitor` do `/Services/ProcessMonitor.cs`
   - **Změnit `System.Timers.Timer` na `DispatcherTimer` (WinUI compatible)**
   - Odstranit závislost na `Form1.localIP`
   - Zavést podmínku pro web kontrolu podle názvu PC (`Environment.MachineName` nebo konfigurovatelný seznam názvů PC)
   - Testovat izolovaně mimo UI

4. **Migrovat utility metody**
   - `EnsureFileExists` → `/Helpers/FileHelper.cs`
   - `GetLocalIPAddress` odstranit bez náhrady
   - Přidat `MachineContextHelper` nebo obdobnou službu pro rozhodování podle názvu PC
   - `CloseAnotherInstance` → `/Helpers/ProcessHelper.cs`
   - Zachovat logiku, ale oddělit od UI

5. **Zachovat P/Invoke deklarace**
   - `FindWindow` → `/Helpers/Win32Helper.cs`
   - Případně použít `CsWin32` NuGet package pro type-safe P/Invoke

6. **Nastavit app lifecycle**
   - `App.xaml.cs`: Inicializace služeb, single instance check
   - Připravit `MainWindow` placeholder (prázdný)

#### Expected breaking changes

- **Threading model**: WinUI používá `DispatcherQueue` místo `SynchronizationContext`
- **Timer změna**: `System.Timers.Timer` → `DispatcherTimer` nebo `DispatcherQueueTimer`
- **File paths**: Zachovat stejné, ale ověřit sandbox restrictions (MSIX)

#### Validační checklist

- [ ] Nový WinUI 3 projekt builduje
- [ ] ProcessMonitor funguje izolovaně (unit test nebo console runner)
- [ ] File I/O funguje (čtení/zápis do %LocalAppData%)
- [ ] Rozhodování podle názvu PC funguje
- [ ] Single instance check funguje
- [ ] Žádné compilation errors

---

### Fáze 2: Hlavní okno (Form1 → MainWindow)

**Aktuální stav**: Form1 s WinForms designerem, 3 Labels, NotifyIcon, ContextMenuStrip  
**Cílový stav**: MainWindow.xaml s WinUI 3 controls  
**Složitost**: High

#### Kroky migrace

1. **Vytvořit MainWindow.xaml layout**
   - Background image (BackgroundImageLayout.Stretch → ImageBrush)
   - 3 TextBlocky pro:
     - `messageLabel` (čas od poslední hry)
     - `weekDuration` (tento týden)
     - `lastWeekDuration` (minulý týden)
   - Velikost okna: 1245x575 (zachovat stejné rozměry)
   - `FormBorderStyle.FixedSingle` → `ResizeMode="NoResize"`

2. **Implementovat data binding**
   - Vytvořit `MainViewModel` s properties:
     - `MessageText` (ObservableProperty)
     - `WeekDurationText`
     - `LastWeekDurationText`
   - Bindovat TextBlocky na ViewModel properties
   - Použít `INotifyPropertyChanged` nebo Community Toolkit MVVM

3. **Migrovat timer logiku**
   - Timer pro aktualizaci `messageLabel` (Interval 50ms) → `DispatcherTimer`
   - Timer pro aktualizaci týdenních statistik (Interval 800ms) → `DispatcherTimer`
   - Event handlery → metody ve ViewModel nebo code-behind

4. **Implementovat window lifecycle**
   - `FormClosing` → `Window.Closed` event
   - `e.Cancel = true` + `this.Visible = false` →
     - `args.Handled = true` + `MainWindow.Hide()` (tray scénář - dořešit ve Fázi 4)

5. **Zachovat startup chování**
   - `Form1_Load` logika → `MainWindow.Loaded` event nebo `OnNavigatedTo` override
   - Async inicializace zachovat (`async void` event handler je OK pro UI events)

6. **Ikona aplikace**
   - Zkopírovat `211668_controller_b_game_icon.ico`
   - Nastavit v Package.appxmanifest → Visual Assets

#### Expected breaking changes

- **Image resources**: WinForms Resources.resx → WinUI Assets nebo Embedded Resources
- **Font sizes**: WinForms Point → WinUI Pixel (může být potřeba adjustace)
- **Layout**: Absolute positioning → Grid nebo StackPanel
- **Event wiring**: Designer generated → XAML nebo code-behind manual

#### Code modifications

**Původní Form1.cs:**
```csharp
messageLabel.Text = $"Už jsi nepařila: {timeSinceLastWrite.Days} dní...";
messageLabel.Invalidate();
messageLabel.Update();
```

**Nový MainWindow (MVVM pattern):**
```csharp
// ViewModel
public string MessageText 
{ 
    get => _messageText; 
    set => SetProperty(ref _messageText, value); 
}

// Timer handler
MessageText = $"Už jsi nepařila: {timeSinceLastWrite.Days} dní...";
// Invalidate/Update není potřeba - automatic binding update
```

#### Validační checklist

- [ ] MainWindow se zobrazí s background image
- [ ] Všechny 3 TextBlocky zobrazují správný text
- [ ] Timer aktualizace fungují (50ms a 800ms)
- [ ] Okno má správnou velikost a nelze resize
- [ ] Async inicializace dokončí bez chyb
- [ ] Žádné memory leaky (profiling)
- [ ] Ikona aplikace se zobrazí správně

---

### Fáze 3: Motivační dialog (WeekendMotivation → WeekendMotivationWindow)

**Aktuální stav**: WinForms Form s ComboBox, 2 Buttons, 2 Labels  
**Cílový stav**: ContentDialog nebo Window v WinUI 3  
**Složitost**: Medium

#### Kroky migrace

1. **Rozhodnout typ dialogu**
   - **Varianta A**: `ContentDialog` (WinUI 3 recommended, modal overlay)
   - **Varianta B**: Samostatný `Window` (více flexibility)
   - **Doporučení**: `ContentDialog` pro jednoduchost

2. **Vytvořit WeekendMotivationDialog.xaml**
   - Background image (gaming support.jpg)
   - 2 TextBlocky:
     - `WeekLabel1`: "Těch 10 hodin přeci zvládneme."
     - `StartGameLabel`: "Chceš si zapařit?"
   - ComboBox s games: ["Diablo IV", "Diablo III64", "Dragon Age The Veilguard", "DragonAgeInquisition"]
   - 2 tlačítka: "Ano", "Ne"
   - Layout: Grid nebo StackPanel

3. **Implementovat logiku spouštění her**
   - Zkopírovat `GetExecutableName` metodu
   - Zkopírovat `FindExecutablePathAsync` (zachovat logiku)
   - Zkopírovat `IsProcessRunning`
   - **Poznámka**: Vyhledávání exe na C:\ může trvat dlouho - zvážit cachování nebo uživatelský config

4. **Event handling**
   - "Ano" button → `YesBtn_Click` logika
   - "Ne" button → zavřít dialog
   - ComboBox selection handling

5. **Volání z MainWindow**
   - Původní: `weekendMotivation.ShowDialog()` (WinForms blocking)
   - Nový: `await dialog.ShowAsync()` (WinUI async pattern)
   - Upravit `OpenWeekendMotivation` metodu pro async pattern

#### Expected breaking changes

- **ShowDialog()**: Blocking WinForms call → `ShowAsync()` async pattern
- **DialogResult**: WinForms enum → ContentDialogResult (Primary, Secondary, None)
- **Background image**: Může vyžadovat jiný approach než WinForms

#### Code modifications

**Původní:**
```csharp
this.weekendMotivation = new WeekendMotivation();
weekendMotivation.ShowDialog(); // Blocking
```

**Nový:**
```csharp
var dialog = new WeekendMotivationDialog();
dialog.XamlRoot = this.Content.XamlRoot; // Required for WinUI 3
var result = await dialog.ShowAsync(); // Non-blocking
```

#### Validační checklist

- [ ] Dialog se zobrazí správně
- [ ] ComboBox obsahuje všechny hry
- [ ] "Ano" button spustí vybranou hru
- [ ] "Ne" button zavře dialog
- [ ] Vyhledávání exe funguje (nebo uživatel vidí progress)
- [ ] Dialog je modal (blokuje hlavní okno)
- [ ] Background image se zobrazí

---

### Fáze 4: NotifyIcon integrace

**Aktuální stav**: System.Windows.Forms.NotifyIcon  
**Cílový stav**: Tray ikona přes H.NotifyIcon nebo Win32 wrapper  
**Složitost**: High

#### Kroky migrace

1. **Vybrat knihovnu pro NotifyIcon**
   - **Doporučeno**: `H.NotifyIcon.WinUI` (NuGet package)
   - Alternativy: `Wpf.Ui` (má WinUI backport) nebo custom Win32 wrapper
   - Install: `dotnet add package H.NotifyIcon.WinUI`

2. **Nastavit TaskbarIcon v App.xaml**
   - Přidat namespace: `xmlns:tb="using:H.NotifyIcon"`
   - Definovat `TaskbarIcon` jako application resource
   - Ikona: `9193516_swords_protection_shield_weapon_violence_icon.ico`
   - Tooltip: "Kontrola pařby"

3. **Vytvořit context menu**
   - Původně: `ContextMenuStrip` s 1 položkou "Exit"
   - Nový: `MenuFlyout` s MenuFlyoutItems
   - Položky:
     - "Obnovit" (Left click simulace)
     - "Exit" (Right click menu)

4. **Implementovat tray behavior**
   - Left click → Obnovit MainWindow (`MainWindow.Activate()`)
   - Right click → Zobrazit context menu
   - Window.Hide() když minimalizovat
   - Window.Activate() z tray

5. **Upravit window lifecycle**
   - `MainWindow.Closed` event:
     - Pokud z tray menu "Exit" → skutečně zavřít
     - Pokud uživatel zavře okno → Hide()
   - Přidat flag `_isExitRequested` pro rozlišení

6. **Synchronizovat s původním chováním**
   - Původní: `notifyIcon1.Visible = !this.Visible`
   - Nový: TaskbarIcon je vždy visible, jen MainWindow hide/show

#### Expected breaking changes

- **API differences**: WinForms NotifyIcon API vs H.NotifyIcon API
- **Context menu**: `ContextMenuStrip` → `MenuFlyout`
- **Click events**: Jiný event handling pattern

#### Code modifications

**Původní Form1.cs:**
```csharp
private void notifyIcon1_Click(object sender, EventArgs e)
{
    if (((MouseEventArgs)e).Button == MouseButtons.Left)
        Application.Restart();
    else if (((MouseEventArgs)e).Button == MouseButtons.Right)
        contextMenuStrip1.Show(cursorPosition);
}
```

**Nový MainWindow:**
```csharp
// H.NotifyIcon uses TrayIcon.LeftClick / RightClick events
TaskbarIcon.LeftClick += (s, e) => {
    MainWindow.DispatcherQueue.TryEnqueue(() => {
        this.Activate();
    });
};

// Context menu defined in XAML or code-behind
```

#### Validační checklist

- [ ] Tray ikona se zobrazí po startu aplikace
- [ ] Left click obnoví hlavní okno
- [ ] Right click zobrazí context menu
- [ ] "Exit" z menu skutečně ukončí aplikaci
- [ ] Zavření okna X buttonem jen schová okno
- [ ] Tooltip zobrazí "Kontrola pařby"
- [ ] Ikona se zobrazí správně (ne default Windows icon)

---

### Fáze 5: Update mechanismus

**Aktuální stav**: ClickOnce ApplicationDeployment  
**Cílový stav**: Custom update checker + manifest na GitHub  
**Složitost**: High

#### Kroky migrace

1. **Navrhnout update manifest strukturu**

   **update-manifest.json** (hosted na GitHub):
   ```json
   {
     "latestVersion": "1.1.0",
     "downloadUrl": "https://github.com/user/repo/releases/download/v1.1.0/Diablo4.msix",
     "releaseNotes": "Migrace na WinUI 3",
     "minimumVersion": "1.0.0",
     "releaseDate": "2026-03-26"
   }
   ```

2. **Vytvořit UpdateService**
   - `/Services/UpdateService.cs`
   - Metody:
     - `CheckForUpdatesAsync()` - stáhne manifest, porovná verzi
     - `DownloadUpdateAsync(string url)` - stáhne nový MSIX/installer
     - `ApplyUpdateAsync()` - spustí installer, zavře aplikaci

3. **Implementovat check při startu**
   - V `App.OnLaunched`:
     - Po startu vždy zkusit zavolat `UpdateService.CheckForUpdatesAsync()`
     - Při chybě sítě nebo nedostupném GitHubu pokračovat bez blokace startu aplikace
     - Pokud update dostupný, zobrazit ContentDialog s dotazem

4. **Vytvořit UpdateDialog**
   - "Je dostupná aktualizace X.Y.Z, přeješ si ji nainstalovat?"
   - Tlačítka: "Ano", "Ne"
   - Pokud Ano:
     - Stáhnout update (zobrazit progress)
     - Spustit installer
     - Zavřít aplikaci

5. **Hosting strategy**

   **GitHub Releases (požadovaný cílový mechanismus):**
   - Výhody: Veřejná historie, stabilní URL, versioning, release assets
   - Manifest URL: `https://raw.githubusercontent.com/user/repo/main/update-manifest.json`
   - MSIX URL: GitHub Release assets
   - Kontrola nové verze probíhá při každém startu aplikace

6. **Verzování**
   - Použít `PackageVersion` v `.csproj`
   - Assembly version pro runtime check
   - Git tags pro releases

7. **Bezpečnost**
   - **Důležité**: Ověřovat signature MSIX před instalací
   - HTTPS pro download (GitHub/OneDrive default)
   - Checksum validation (optional, ale doporučeno)

#### Expected breaking changes

- **Kompletní náhrada** ClickOnce logiky
- Žádná automatická integrace - vše custom
- MSIX packaging místo ClickOnce .application

#### Code modifications

**Původní Form1.cs:**
```csharp
if (ApplicationDeployment.IsNetworkDeployed)
{
    ApplicationDeployment ad = ApplicationDeployment.CurrentDeployment;
    ad.CheckForUpdateAsync();
}
```

**Nový App.xaml.cs:**
```csharp
protected override async void OnLaunched(LaunchActivatedEventArgs args)
{
    // ... initialize MainWindow ...

    var updateService = new UpdateService(
        manifestUrl: "https://raw.githubusercontent.com/user/repo/main/update-manifest.json"
    );

    var updateInfo = await updateService.CheckForUpdatesAsync();
    if (updateInfo.IsUpdateAvailable)
    {
        var dialog = new UpdateDialog(updateInfo);
        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            await updateService.DownloadAndInstallAsync(updateInfo);
        }
    }
}
```

#### Validační checklist

- [ ] Update manifest je správně formatted JSON
- [ ] Manifest je dostupný přes GitHub URL
- [ ] UpdateService správně parsuje manifest
- [ ] Verze se porovnává správně (current vs. latest)
- [ ] UpdateDialog se zobrazí když je update dostupný
- [ ] Download funguje (progress reporting)
- [ ] Installer se spustí po downloadu
- [ ] Aplikace se zavře po spuštění installeru
- [ ] Při nedostupném GitHubu aplikace pokračuje bez pádu
- [ ] Kontrola nové verze se spustí při každém startu

---

### Fáze 6: Validace

**Aktuální stav**: Aplikace ještě v migraci  
**Cílový stav**: Kompletně funkční WinUI 3 aplikace  
**Složitost**: Medium

#### Kroky validace

1. **Smoke testing všech scénářů**
   - Spustit aplikaci fresh install
   - Ověřit hlavní okno se zobrazí
   - Počkat na timer updates
   - Minimalizovat do tray
   - Obnovit z tray
   - Zavřít aplikaci z tray menu

2. **Testovat weekend motivace**
   - Nastavit systémový čas na pátek/sobotu/nedělu
   - Ověřit že se zobrazí motivační dialog
   - Vybrat hru z ComboBox
   - Kliknout "Ano" (pokud je hra nainstalována)
   - Ověřit že se spustí správný proces

3. **Testovat process monitoring**
   - Spustit monitorovaný proces (např. VS Code)
   - Ověřit že aplikace loguje do souboru
   - Zkontrolovat formát logu
   - Ověřit statistiky pro tento/minulý týden

4. **Testovat update mechanismus**
   - Nahrát test manifest s vyšší verzí
   - Spustit aplikaci
   - Ověřit že se zobrazí update dialog
   - Test download (mock nebo real)
   - Ověřit že se aplikace zavře po update

5. **Memory leak testing**
   - Spustit aplikaci a nechat běžet 30+ minut
   - Monitorovat Task Manager
   - Ověřit že memory usage je stabilní
   - Profiling s Visual Studio Diagnostic Tools

6. **Edge cases**
   - Spustit 2 instance aplikace (měla by zavřít starší)
   - Soubor `Diablo IV.txt` neexistuje (měl by vytvořit)
   - Soubor `Diablo IV.txt` je prázdný (měl by inicializovat)
   - Síť není dostupná (aplikace pokračuje bez update checku)
   - Update manifest není dostupný (graceful degradation)

7. **UI/UX kontrola**
   - Všechny fonty jsou čitelné
   - Background images se zobrazují správně
   - Ikona aplikace správná
   - Tray tooltip správný
   - Žádné flickering nebo visual bugs

#### Performance kritéria

- Startup time: < 3 sekundy
- Memory usage: < 100 MB idle
- CPU usage: < 5% idle (mimo timery)
- File operations: žádné lock conflicts

#### Validační checklist

- [ ] Všechny smoke testy prošly
- [ ] Weekend motivace funguje
- [ ] Process monitoring loguje správně
- [ ] Update mechanismus funguje end-to-end
- [ ] Žádné memory leaky
- [ ] Všechny edge cases pokryté
- [ ] UI/UX je polished
- [ ] Performance kritéria splněna
- [ ] Aplikace je stabilní po 1+ hodině běhu

---

## 5. Řízení rizik

### High-level hodnocení rizik

| Komponenta | Úroveň rizika | Popis | Mitigation |
|-----------|--------------|-------|-----------|
| UI přepis (Form1) | **Vysoké** | Kompletní přepis z WinForms do XAML, event handling, data binding | Postupná migrace, validace po každém kroku |
| Update mechanismus | **Vysoké** | Náhrada ClickOnce vlastní logikou | Důkladné testování, fallback strategie |
| NotifyIcon integrace | **Vysoké** | Není nativní WinUI 3 součást | Použití osvědčené knihovny (H.NotifyIcon) |
| UI Automation (Firefox) | **Střední** | Možné rozdíly v threading modelu | Testování v novém prostředí, úprava thread synchronizace |
| Přechod z IP kontroly na název PC | **Střední** | Změna podmínky pro spuštění specifické web kontroly | Centralizovat pravidlo do jedné služby a pokrýt testy názvů PC |
| ProcessMonitor logika | **Střední** | Změna timer mechanismu | Zachování logiky, jen změna threading |
| File I/O | **Nízké** | Beze změny | Žádná akce potřeba |
| Process checking | **Nízké** | Beze změny | Žádná akce potřeba |

### Security vulnerabilities
Žádné identifikované v assessmentu.

### Contingency plans

**Pokud UI migrace selže:**
- Alternativa 1: Použít Avalonia UI místo WinUI 3
- Alternativa 2: Zůstat u WinForms, jen upgrade na .NET 8

**Pokud NotifyIcon integrace nefunguje:**
- Alternativa 1: Použít jiné knihovny (WPF NotifyIcon backport)
- Alternativa 2: Vlastní Win32 wrapper přes P/Invoke

**Pokud update mechanismus je příliš složitý:**
- Alternativa: Manuální download z GitHub Releases
- Fallback: Notifications o nové verzi bez auto-updatu

**Pokud performance problémy:**
- Optimalizace polling intervalů
- Async/await pro dlouhotrvající operace
- Profiling a identifikace bottlenecks

---

## 6. Testování a validace

### Multi-level testing strategie

#### Per-Phase Testing
Po každé fázi migrace:

**Fáze 0: Příprava**
- [ ] .NET 8 SDK funkční
- [ ] WinUI 3 templates dostupné
- [ ] Git větev vytvořena

**Fáze 1: Projekt + logika**
- [ ] Nový projekt builduje bez errors
- [ ] ProcessMonitor funguje izolovaně
- [ ] File I/O funguje
- [ ] Machine context helper funguje
- [ ] Single instance check funguje

**Fáze 2: Hlavní okno**
- [ ] MainWindow se zobrazí
- [ ] Background image loaded
- [ ] Všechny TextBlocky zobrazují data
- [ ] Timer updates fungují (50ms, 800ms)
- [ ] Async init dokončí

**Fáze 3: Motivační okno**
- [ ] Dialog se zobrazí
- [ ] ComboBox population
- [ ] Button handlers fungují
- [ ] Exe vyhledávání funguje

**Fáze 4: NotifyIcon**
- [ ] Tray ikona visible
- [ ] Left click obnoví okno
- [ ] Right click menu funguje
- [ ] Exit z menu ukončí app

**Fáze 5: Update mechanismus**
- [ ] Manifest parsing funguje
- [ ] Version comparison správná
- [ ] Dialog zobrazení
- [ ] Download + install flow
- [ ] Kontrola GitHub release při startu funguje

**Fáze 6: Finální validace**
- [ ] Všechny integrační testy prošly
- [ ] Memory/performance OK
- [ ] Edge cases pokryté

#### Integration Testing
Po dokončení všech fází:

**End-to-end scénáře:**
1. Startup → zobrazí okno → minimalizuje do tray
2. Tray → obnoví okno → zavře aplikaci z menu
3. Weekend motivace → vyber hru → spustí
4. Process monitoring → spustí hru → loguje statistiky
5. Update check → najde update → stáhne → nainstaluje

**Regresní testy:**
- Všechny původní funkce stále fungují
- Žádná data loss
- File format kompatibilita

#### Manual Testing Checklist

**Základní funkcionalita:**
- [ ] Aplikace se spustí
- [ ] Zobrazí se správná statistika
- [ ] Timery aktualizují UI
- [ ] Minimalizace do tray
- [ ] Obnovení z tray
- [ ] Exit z tray menu

**Weekend motivace:**
- [ ] Zobrazí se v pátek/sobotu/neděli
- [ ] Když < 25 hodin tento týden
- [ ] ComboBox obsahuje hry
- [ ] Spustí vybranou hru

**Process monitoring:**
- [ ] Detekuje spuštěné hry
- [ ] Loguje do souboru
- [ ] Formát logu správný
- [ ] Statistiky správné

**Update mechanismus:**
- [ ] Check při startu (pokud online)
- [ ] Dialog když je update
- [ ] Download funguje
- [ ] Install a restart funguje
- [ ] Při nedostupném GitHubu aplikace běží dál bez chyby

#### Automated Testing (optional, ale doporučeno)

**Unit testy:**
- ProcessMonitor logika
- File helper metody
- Network helper metody
- Version comparison logika

**Integration testy:**
- UpdateService end-to-end
- File I/O operations
- Process detection

**UI testy (optional):**
- WinAppDriver pro automatizaci WinUI 3
- Appium pro MSIX package testing

---

## 7. Hodnocení složitosti a náročnosti

### Složitost podle fází

| Fáze | Složitost | Zdůvodnění | Hlavní výzvy |
|------|-----------|-----------|--------------|
| 0: Příprava | **Low** | Instalace SDK, příprava větve | Žádné technické výzvy |
| 1: Projekt + logika | **Medium** | Nový projekt setup, oddělení logiky | Threading model, project structure |
| 2: Hlavní okno | **High** | Kompletní UI přepis, XAML, binding | Learning curve WinUI 3, event handling |
| 3: Motivační okno | **Medium** | Menší okno, podobné jako Fáze 2 | XAML layout, button handling |
| 4: NotifyIcon | **High** | Externí integrace, Win32 interop | Library selection, tray menu |
| 5: Update mechanismus | **High** | Custom logika, network, file operations | Manifest design, error handling |
| 6: Validace | **Medium** | Testování všech scénářů | Coverage všech edge cases |

### Celkové hodnocení složitosti

**Projekt jako celek: Medium-High**

**Důvody:**
- Malý rozsah aplikace (1 projekt, 2 okna)
- Ale vysoká míra změn (kompletní UI přepis)
- Vyžaduje znalost WinUI 3 (nová technologie)
- Kritické platformní integrace (tray, update)

### Resource requirements

**Dovednosti:**
- C# a .NET
- WinUI 3 / XAML (střední až pokročilá)
- Windows desktop development
- Git / source control
- Znalost původní aplikace

**Nástroje:**
- Visual Studio 2022 s WinUI 3 workload
- .NET 8 SDK
- Git

**Paralelní kapacita:**
- Doporučeno: 1 vývojář (konzistentní approach)
- Možné: 2 vývojáře (jeden na UI, jeden na integrace)
- Nedoporučeno: více než 2 (koordinační overhead)

### Relativní náročnost jednotlivých částí

**Nejvíce náročné (40% celkové práce):**
- Fáze 2: Hlavní okno (20%)
- Fáze 5: Update mechanismus (20%)

**Středně náročné (40% celkové práce):**
- Fáze 1: Projekt setup + logika (15%)
- Fáze 4: NotifyIcon (15%)
- Fáze 3: Motivační okno (10%)

**Méně náročné (20% celkové práce):**
- Fáze 0: Příprava (5%)
- Fáze 6: Validace (15%)

---

## 8. Source control strategie

### Branching strategie

**Hlavní větev migrace:**
- Název: `feature/winui3-migration`
- Base: `main` (současný WinForms stav)
- Merge target: `main` (po dokončení migrace)

**Důvod pro feature branch:**
- Migrace je velká změna
- Původní WinForms app zůstává funkční na `main`
- Možnost paralelního vývoje bugfixů

### Commit strategie

**Frekvence commitů:** Po každé fázi + významných milestones

**Doporučená struktura commitů:**

```
feat(project): Initialize WinUI 3 project structure
feat(services): Migrate ProcessMonitor and utilities
feat(ui): Implement MainWindow with XAML layout
feat(ui): Add data binding and timers to MainWindow
feat(ui): Implement WeekendMotivationDialog
feat(tray): Integrate H.NotifyIcon for tray functionality
feat(update): Implement custom update mechanism
test(all): Add comprehensive validation tests
docs(readme): Update for WinUI 3 migration
```

**Commit message format:**
- `<type>(<scope>): <subject>`
- Types: `feat`, `fix`, `refactor`, `test`, `docs`, `chore`
- Scope: component or phase name
- Subject: krátký popis změny

**Příklady:**
- `feat(phase1): Create new WinUI 3 project with .NET 8`
- `refactor(services): Extract ProcessMonitor from UI layer`
- `fix(tray): Resolve tray icon not showing on startup`

### Review a merge proces

**Pull Request requirements:**
- Všechny testy prošly
- Code review (pokud team)
- Dokumentace aktualizována
- Changelog entry

**PR checklist:**
- [ ] Build prošel bez warnings
- [ ] Všechny fáze dokončeny
- [ ] Validační testy prošly
- [ ] Memory leak testing OK
- [ ] Performance kritéria splněna
- [ ] Breaking changes dokumentovány
- [ ] Migration guide vytvořen (pokud potřeba)

**Merge kritéria:**
- Všechny checklisty splněny
- Aplikace je plně funkční
- Žádné známé blocker bugs
- Dokumentace kompletní

### Post-merge strategie

**Po merge do main:**
1. Tag verze: `v1.1.0-winui3` nebo podobně
2. Create GitHub Release s MSIX package
3. Update manifest.json na novou verzi
4. Oznámit deployment

**Rollback plan:**
- Pokud po merge objeveny kritické problémy
- Revert merge commit
- Fix na feature branch
- Re-merge po opravě

### Git workflow summary

```
main (WinForms .NET Framework 4.8.1)
  └── feature/winui3-migration
        ├── commit: phase 0
        ├── commit: phase 1
        ├── commit: phase 2
        ├── commit: phase 3
        ├── commit: phase 4
        ├── commit: phase 5
        └── commit: phase 6
              └── PR → main (after validation)
```

---

## 9. Kritéria úspěchu

### Technická kritéria

#### Build a kompilace
- [ ] Projekt builduje bez errors
- [ ] Žádné warnings (nebo dokumentované acceptable warnings)
- [ ] MSIX package generuje úspěšně
- [ ] Aplikace se spustí po instalaci MSIX

#### Funkcionalita
- [ ] Všechny původní funkce zachovány:
  - [ ] Zobrazení statistik (nepařila X dní)
  - [ ] Týdenní statistiky (tento + minulý týden)
  - [ ] Process monitoring všech definovaných her
  - [ ] Weekend motivace dialog
  - [ ] Spouštění her z dialogu
  - [ ] Minimalizace do tray
  - [ ] Obnovení z tray
  - [ ] Zavření aplikace z tray menu
  - [ ] Single instance check
  - [ ] Update mechanismus přes GitHub při startu

#### Data integrita
- [ ] Formát `Diablo IV.txt` zachován (backward compatible)
- [ ] Existující data čitelná po migraci
- [ ] Log entries formát beze změny: `{week}||{datetime}||{seconds}`
- [ ] Žádná data loss

#### Performance
- [ ] Startup time ≤ 3 sekundy
- [ ] Memory usage ≤ 100 MB idle
- [ ] CPU usage ≤ 5% idle
- [ ] Žádné memory leaky po 1+ hodině běhu
- [ ] UI responsive (žádné freezing)

#### Platformní integrace
- [ ] Tray ikona funguje spolehlivě
- [ ] Context menu reaguje na right click
- [ ] Left click obnovuje okno
- [ ] Update check funguje proti GitHub release manifestu při startu
- [ ] Update download + install funguje

### Kvalitní kritéria

#### Code quality
- [ ] Code je strukturovaný (Services, Models, ViewModels, Views)
- [ ] Business logika oddělená od UI
- [ ] MVVM pattern použit kde vhodný
- [ ] Žádné hardcoded values (použít config nebo constants)
- [ ] Error handling implementován
- [ ] Logging implementován (pro debugging)

#### Test coverage
- [ ] Všechny smoke testy prošly
- [ ] Edge cases pokryté
- [ ] Regresní testy prošly
- [ ] Integration tests pro kritické scénáře

#### Dokumentace
- [ ] README aktualizován pro WinUI 3
- [ ] Migration notes vytvořeny
- [ ] Known issues dokumentovány (pokud nějaké)
- [ ] User guide aktualizován (pokud existoval)

### Procesní kritéria

#### Source control
- [ ] Všechny změny zacommitovány
- [ ] Commit messages jsou jasné
- [ ] Feature branch merged do main
- [ ] Version tagged (např. `v1.1.0`)

#### Deployment
- [ ] MSIX package vytvořen
- [ ] GitHub Release vytvořen (nebo OneDrive upload)
- [ ] Update manifest.json publikován
- [ ] Installation guide vytvořen

#### Validace
- [ ] Manual testing dokončen
- [ ] Acceptance testing (pokud má uživatel requirements)
- [ ] Sign-off od uživatele (nebo self-sign-off)

### Definice "Done"

Migrace je považována za **dokončenou** když:

1. ✅ **Všechna technická kritéria splněna**
2. ✅ **Všechna kvalitní kritéria splněna**
3. ✅ **Všechna procesní kritéria splněna**
4. ✅ **Žádné blocker bugs**
5. ✅ **Aplikace je production-ready**

### Acceptance test scénáře

**Scénář 1: Fresh install uživatele**
1. Stáhnout MSIX z GitHub/OneDrive
2. Nainstalovat aplikaci
3. Spustit poprvé
4. Ověřit že vytvoří soubor `Diablo IV.txt`
5. Spustit monitorovaný proces
6. Ověřit logging funguje

**Scénář 2: Weekend motivace flow**
1. Nastavit datum na pátek
2. Spustit aplikaci
3. Hrát < 25 hodin tento týden
4. Ověřit že se zobrazí motivační dialog
5. Vybrat hru a spustit
6. Ověřit že se hra spustila

**Scénář 3: Tray functionality**
1. Spustit aplikaci
2. Minimalizovat okno
3. Ověřit ikona v tray
4. Right click → ověřit menu
5. Left click → ověřit obnovení okna
6. Right click → Exit → ověřit ukončení

**Scénář 4: Update flow**
1. Publikovat novou verzi na GitHub
2. Aktualizovat manifest
3. Spustit starší verzi aplikace
4. Ověřit update dialog
5. Kliknout "Ano"
6. Ověřit download + install
