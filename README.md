# Encyclopedia

A BepInEx mod for MycoPunk that provides a comprehensive browser for viewing all upgrades in the game, organized by category.

## Description

This client-side mod adds a complete encyclopedia system to MycoPunk, allowing players to browse through all available upgrades (both character skills and gear modifications) in an organized, searchable interface. The encyclopedia presents upgrades as collectible cards with detailed tooltips showing stats, descriptions, and rarity information.

The mod categorizes upgrades into character classes (like Engineer, Scout, etc.) and gear types (weapons, armor, etc.), making it easy to explore what upgrades are available. Uncollected upgrades appear dimmed to show progression status. The encyclopedia window is fully draggable and resizable for optimal viewing experience.

## Getting Started

### Dependencies

* MycoPunk (base game)
* [BepInEx](https://github.com/BepInEx/BepInEx) - Version 5.4.2403 or compatible
* .NET Framework 4.8

### Building/Compiling

1. Clone this repository
2. Open the solution file in Visual Studio, Rider, or your preferred C# IDE
3. Build the project in Release mode

Alternatively, use dotnet CLI:
```bash
dotnet build --configuration Release
```

### Installing

**Option 1: Via Thunderstore (Recommended)**
1. Download and install using the Thunderstore Mod Manager
2. Search for "Encyclopedia" under MycoPunk community
3. Install and enable the mod

**Option 2: Manual Installation**
1. Ensure BepInEx is installed for MycoPunk
2. Copy `Encyclopedia.dll` from the build folder
3. Place it in `<MycoPunk Game Directory>/BepInEx/plugins/`
4. Launch the game

### Executing program

Once installed, the encyclopedia becomes available whenever the main menu is open:

**Accessing the Encyclopedia:**
1. Open the main menu (press ESC during gameplay)
2. Look for a "📚 Encyclopedia" button in the top-right area
3. Click the button to open the encyclopedia window

**Navigating the Encyclopedia:**
1. **Left Panel:** Browse categories by clicking character classes or gear types
2. **Right Panel:** View upgrades as cards with rarity-based sorting
3. **Hover Cards:** Get detailed tooltips with stats, descriptions, and stat ranges
4. **Window Controls:** Drag the title bar to move, resize by dragging edges/corners
5. **Scrollbars:** Navigate through long lists of upgrades

The encyclopedia organizes upgrades by:
- **Character Classes** (Engineer, Scout, etc.) - skill tree upgrades
- **Gear Types** (Guns, Grenades, Armor, etc.) - equipment modifications

## Help

* **Window not showing?** Make sure you're in the main menu, not the pause menu during missions
* **No encyclopedia button?** Verify the mod is loaded in BepInEx console
* **Cards not displaying?** The mod only shows upgrade categories that actually have collectible upgrades
* **Hover tooltips flickering?** This can happen with mouse movement - stable hovering shows tooltips reliably
* **Window too small/large?** Resize by dragging the bottom-right corner or edges
* **Missing upgrades?** Only upgrades with visible effects are shown - some internal upgrades may be hidden
* **Performance impact?** The encyclopedia only processes when the window is open - no impact during gameplay
* **Categories empty?** Some gear types may not have upgrades in certain game versions

## Authors

* Sparroh
* funlennysub (original mod template)
* [@DomPizzie](https://twitter.com/dompizzie) (README template)

## License

* This project is licensed under the MIT License - see the LICENSE.md file for details
