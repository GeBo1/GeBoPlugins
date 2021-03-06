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

- AI v1.1.0 [Download](https://github.com/GeBo1/GeBoPlugins/releases/download/r15/AI_GeBoCommon.v1.1.0.zip)
- HS2 v1.1.0 [Download](https://github.com/GeBo1/GeBoPlugins/releases/download/r15/HS2_GeBoCommon.v1.1.0.zip)
- KK v1.1.0 [Download](https://github.com/GeBo1/GeBoPlugins/releases/download/r15/KK_GeBoCommon.v1.1.0.zip)

### [GameDialogHelper](src/GameDialogHelper/README.md)

Highlights correct dialog choice in KK main game. Can be configured to use advanced game logic or simply your relationship level with the character.

- KK v1.0.0 [Download](https://github.com/GeBo1/GeBoPlugins/releases/download/r17/KK_GameDialogHelper.v1.0.0.zip) 

### [GameDressForSuccess](src/GameDressForSuccess/README.md)

Fixes it so when player invites (or is invited) to swim/exercise with a girl he remembers to change his clothes rather than going in what he's already wearing. By default only applies if player clothes are already set to automatic, but can be configured to always be applied.

- KK v1.2.0 [Download](https://github.com/GeBo1/GeBoPlugins/releases/download/r18/KK_GameDressForSuccess.v1.2.0.zip)

### [GameWhoIsThere](src/GameWhoIsThere/README.md)

Let's you see who will in "surprise" events before they start.

- HS2 v1.0.1 [Download](https://github.com/GeBo1/GeBoPlugins/releases/download/r22/HS2_GameWhoIsThere.v1.0.1.zip)

### [StudioMultiselectChara](src/StudioMultiselectChara/README.md)

If you select a character in studio and press the hotkey, all other instances of that character in the scene will also be selected. Useful for replacing all instances of a given character.

- AI v1.0.0 [Download](https://github.com/GeBo1/GeBoPlugins/releases/download/r20/AI_StudioMultiselectChara.v1.0.0.zip)
- HS2 v1.0.0 [Download](https://github.com/GeBo1/GeBoPlugins/releases/download/r20/HS2_StudioMultiselectChara.v1.0.0.zip)
- KK v1.0.0 [Download](https://github.com/GeBo1/GeBoPlugins/releases/download/r20/KK_StudioMultiselectChara.v1.0.0.zip)

[//]: # (### StudioSceneCharaInfo)

### [StudioSceneNavigation](src/StudioSceneNavigation/README.md)

Provides hotkeys for loading the next/previous scene from the scenes folder. Supports [Illusion BrowserFolders](https://github.com/ManlyMarco/Illusion_BrowserFolders). Tracks last loaded image so it can pick up where it left off between sessions (or if you change folders).

- AI v1.0.0 [Download](https://github.com/GeBo1/GeBoPlugins/releases/download/r21/AI_StudioSceneNavigation.v1.0.0.zip)
- HS2 v1.0.0 [Download](https://github.com/GeBo1/GeBoPlugins/releases/download/r21/HS2_StudioSceneNavigation.v1.0.0.zip)
- KK v1.0.0 [Download](https://github.com/GeBo1/GeBoPlugins/releases/download/r21/KK_StudioSceneNavigation.v1.0.0.zip)

### [TranslationCacheCleaner](src/TranslationCacheCleaner/README.md)

Removes all entries from your translation cache file that would be translated by existing translations.  Useful when translation files are updated to ensure you aren't still getting old translations from your cache.  

**Note:** Requires [XUnity.AutoTranslator](https://github.com/bbepis/XUnity.AutoTranslator)

- AI v0.6.0 [Download](https://github.com/GeBo1/GeBoPlugins/releases/download/r19/AI_TranslationCacheCleaner.v0.6.0.zip)
- HS2 v0.6.0 [Download](https://github.com/GeBo1/GeBoPlugins/releases/download/r19/HS2_TranslationCacheCleaner.v0.6.0.zip)
- KK v0.6.0 [Download](https://github.com/GeBo1/GeBoPlugins/releases/download/r19/KK_TranslationCacheCleaner.v0.6.0.zip)

### [TranslationHelper](src/TranslationHelper/README.md)

Improves the experience of using XUnity.AutoTranslator in numerous ways (see [README](src/TranslationHelper/README.md) for details).

Check plugin options to configure. Defaults to only using the translation cache, but for the best experience you may wish to set to `Fully Enabled`.

- AI v1.1.0 [Download](https://github.com/GeBo1/GeBoPlugins/releases/download/r16/AI_TranslationHelper.v1.1.0.zip)
- HS2 v1.1.0 [Download](https://github.com/GeBo1/GeBoPlugins/releases/download/r16/HS2_TranslationHelper.v1.1.0.zip)
- KK v1.1.0 [Download](https://github.com/GeBo1/GeBoPlugins/releases/download/r16/KK_TranslationHelper.v1.1.0.zip)



