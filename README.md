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

- AI
- HS2
- KK v0.9.1 [Download](https://github.com/GeBo1/GeBoPlugins/releases/download/r2/KK_GeBoCommon.v0.9.1.zip)

### GameDialogHelper 

Highlights correct dialog choice in KK main game. You can configure level of relationship required (defaults to `Friend`).

- KK v0.9.1 [Download](https://github.com/GeBo1/GeBoPlugins/releases/download/r2/KK_GameDialogHelper.v0.9.1.zip) 

### GameDressForSuccess

Fixes it so when player invites (or is invited) to swim/exercise with a girl he remembers to change his clothes rather than going in what he's already wearing. By default only applies if player clothes are already set to automatic, but can be configured to always be applied.

- KK v1.0 [Download](https://github.com/GeBo1/GeBoPlugins/releases/download/r5/KK_GameDressForSuccess.v1.0.zip)


### StudioMultiselectChara

If you select a character in studio and press the hotkey, all other instances of that character in the scene will also be selected. Useful for replacing all instances of a given character.

- KK v0.9.0 [Download](https://github.com/GeBo1/GeBoPlugins/releases/download/r3/KK_StudioMultiselectChara.v0.9.0.zip)

[//]: # (### StudioSceneCharaInfo)

### StudioSceneNavigation

Provides hotkeys for loading the next/previous scene from the scenes folder. Supports [Illusion BrowserFolders](https://github.com/ManlyMarco/Illusion_BrowserFolders). Tracks last loaded image so it can pick up where it left off between sessions (or if you change folders).

- KK v0.8.5 [Download](https://github.com/GeBo1/GeBoPlugins/releases/download/r4/KK_StudioSceneNavigation.v0.8.5.zip)

### TranslationCacheCleaner

Removes all entries from your translation cache file that would be translated by existing translations.  Useful when translation files are updated to ensure you aren't still getting old translations from your cache.  

**Note:** Requires [XUnity.AutoTranslator](https://github.com/bbepis/XUnity.AutoTranslator)

- KK v0.5.1 [Download](https://github.com/GeBo1/GeBoPlugins/releases/download/r2/KK_TranslationCacheCleaner.v0.5.1.zip)

### TranslationHelper

Improves the experience of using XUnity.AutoTranslator in numerous ways.

- Translates card names as they're loaded (for games with separate given/family names it translates them independently, for games with a single full-name field there's the option to split before translating).
- Registers card names with XUnity.AutoTranslator as replacements.  This keeps your translation cache clean/useful for multiple characters.  Replacements are removed when cards are unloaded.
- Uses specific translation scopes for names, so you can have a file(s) with just name translations (detailed docs to come).
- Maker option to toggle between saving translated names or original names for any unmodified names on the card (If you edit the name, it keeps your edit always)
- Game specific features to update lists/trees/card previews in maker/studio/roster once card translation finishes.
- **KK only** Option to reverse fullname to return 'GivenName FamilyName' (like Party)

Check plugin options to configure. Defaults to only using the translation cache, but for the best experience you may wish to set to `Fully Enabled`.

- AI
- HS2
- KK



