# GeBoPlugins

A collection of plugins for various Illusion games.

## How to install
1. Install the latest build of [BepInEx](https://builds.bepis.io/projects/bepinex_be) and latest release of [BepisPlugins](https://github.com/IllusionMods/BepisPlugins/releases).
2. Check below for the latest release of a given plugin for your game.
3. Extract the BepInEx folder from the release into your game directory, overwrite when asked.
4. Run the game and look for any errors on the screen. Ensure you have the listed dependencies (note, most plugins here rely on **GeBoCommon**, and will come with a copy, but you can always grab the latest version separately).
5. Most plugins have some level of configuration available in the plugin configuration menu.

## Plugin descriptions

### GeBoCommon

Contains shared code used by other plugins in this repo. Unless otherwise noted latest version should continue to work with older versions of plugins. 

**Note:** Requires [IllusionModdingAPI](https://github.com/IllusionMods/IllusionModdingAPI/)

### GameDialogHelper 

Highlights correct dialog choice in KK main game. You can configure level of relationship required (defaults to `Friend`).

### StudioMultiselectChara

If you select a character in studio and press the hotkey, all other instances of that character in the scene will also be selected. Useful for replacing all instances of a given character.

[//]: # (### StudioSceneCharaInfo)

### StudioSceneNavigation

Provides hotkeys for loading the next/previous scene from the scenes folder. Supports [Illusion BrowserFolders](https://github.com/ManlyMarco/Illusion_BrowserFolders). Tracks last loaded image so it can pick up where it left off between sessions (or if you change folders).

### TranslationCacheCleaner

Removes all entries from your translation cache file that would be translated by existing translations.  Useful when translation files are updated to ensure you aren't still getting old translations from your cache.  

**Note:** Requires [XUnity.AutoTranslator](https://github.com/bbepis/XUnity.AutoTranslator)

[//]: # (### TranslationHelper)

