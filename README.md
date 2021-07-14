# GeBoPlugins

A collection of plugins for various Illusion games.

## How to install
1. Install the latest build of [BepInEx](https://builds.bepis.io/projects/bepinex_be) and latest release of [BepisPlugins](https://github.com/IllusionMods/BepisPlugins/releases).
2. Check below for the latest release of a given plugin for your game.
3. Extract the BepInEx folder from the release into your game directory, overwrite when asked.
4. Run the game and look for any errors on the screen. Ensure you have the listed dependencies (note, most plugins here rely on **GeBoCommon**, and will come with a copy, but you can always grab the latest version separately).
5. Most plugins have some level of configuration available in the plugin configuration menu.

## Plugin descriptions

### GeBoCommon (GeBoAPI)

Contains shared code used by other plugins in this repo. Unless otherwise noted latest version should continue to work with older versions of plugins. 

**Note:** Requires [IllusionModdingAPI](https://github.com/IllusionMods/IllusionModdingAPI/)

- [AI_GeBoCommon]
- [HS2_GeBoCommon]
- [KK_GeBoCommon]

### [GameDialogHelper](src/GameDialogHelper/README.md)

Highlights correct dialog choice in KK main game. Can be configured to use advanced game logic or simply your relationship level with the character.

- [KK_GameDialogHelper]

### [GameDressForSuccess](src/GameDressForSuccess/README.md)

Fixes it so when player invites (or is invited) to swim/exercise with a girl he remembers to change his clothes rather than going in what he's already wearing. By default only applies if player clothes are already set to automatic, but can be configured to always be applied.

- [KK_GameDressForSuccess]

### [GameWhoIsThere](src/GameWhoIsThere/README.md)

Let's you see who will in "surprise" events before they start.

- [HS2_GameWhoIsThere]

### [StudioMultiselectChara](src/StudioMultiselectChara/README.md)

If you select a character in studio and press the hotkey, all other instances of that character in the scene will also be selected. Useful for replacing all instances of a given character.

- [AI_StudioMultiselectChara]
- [HS2_StudioMultiselectChara]
- [KK_StudioMultiselectChara]

[//]: # (### StudioSceneCharaInfo)

### [StudioSceneInitialCamera](src/StudioSceneInitialCamera/README.md)

Provides methods for returning to camera position when scene was initially loaded in studio.

- [AI_StudioSceneInitialCamera]
- [HS2_StudioSceneInitialCamera]
- [KK_StudioSceneInitialCamera]


### [StudioSceneNavigation](src/StudioSceneNavigation/README.md)

Provides hotkeys for loading the next/previous scene from the scenes folder. Supports [Illusion BrowserFolders](https://github.com/ManlyMarco/Illusion_BrowserFolders). Tracks last loaded image so it can pick up where it left off between sessions (or if you change folders).

- [AI_StudioSceneNavigation]
- [HS2_StudioSceneNavigation]
- [KK_StudioSceneNavigation]

### [TranslationCacheCleaner](src/TranslationCacheCleaner/README.md)

Removes all entries from your translation cache file that would be translated by existing translations.  Useful when translation files are updated to ensure you aren't still getting old translations from your cache.  

**Note:** Requires [XUnity.AutoTranslator](https://github.com/bbepis/XUnity.AutoTranslator)

- [AI_TranslationCacheCleaner]
- [HS2_TranslationCacheCleaner]
- [KK_TranslationCacheCleaner]

### [TranslationHelper](src/TranslationHelper/README.md)

Improves the experience of using XUnity.AutoTranslator in numerous ways (see [README](src/TranslationHelper/README.md) for details).

Check plugin options to configure. Defaults to only using the translation cache, but for the best experience you may wish to set to `Fully Enabled`.

- [AI_TranslationHelper]
- [HS2_TranslationHelper]
- [KK_TranslationHelper]


[//]: # (## Latest Links)
[AI_GeBoCommon]: https://github.com/GeBo1/GeBoPlugins/releases/download/r27/AI_GeBoCommon.v1.1.2.1.zip "v1.1.2.1"
[HS2_GeBoCommon]: https://github.com/GeBo1/GeBoPlugins/releases/download/r27/HS2_GeBoCommon.v1.1.2.1.zip "v1.1.2.1"
[KK_GeBoCommon]: https://github.com/GeBo1/GeBoPlugins/releases/download/r27/KK_GeBoCommon.v1.1.2.1.zip "v1.1.2.1"
[KK_GameDialogHelper]: https://github.com/GeBo1/GeBoPlugins/releases/download/r29/KK_GameDialogHelper.v1.0.1.2.zip "v1.0.1.2"
[KK_GameDressForSuccess]: https://github.com/GeBo1/GeBoPlugins/releases/download/r34/KK_GameDressForSuccess.v1.2.0.2.zip "v1.2.0.2"
[HS2_GameWhoIsThere]: https://github.com/GeBo1/GeBoPlugins/releases/download/r30/HS2_GameWhoIsThere.v1.0.1.3.zip "v1.0.1.3"
[AI_StudioMultiselectChara]: https://github.com/GeBo1/GeBoPlugins/releases/download/r33/AI_StudioMultiselectChara.v1.0.0.2.zip "v1.0.0.2"
[HS2_StudioMultiselectChara]: https://github.com/GeBo1/GeBoPlugins/releases/download/r33/HS2_StudioMultiselectChara.v1.0.0.2.zip "v1.0.0.2"
[KK_StudioMultiselectChara]: https://github.com/GeBo1/GeBoPlugins/releases/download/r33/KK_StudioMultiselectChara.v1.0.0.2.zip "v1.0.0.2"
[AI_StudioSceneInitialCamera]: https://github.com/GeBo1/GeBoPlugins/releases/download/r36/AI_StudioSceneInitialCamera.v0.6.0.3.zip "v0.6.0.3"
[HS2_StudioSceneInitialCamera]: https://github.com/GeBo1/GeBoPlugins/releases/download/r36/HS2_StudioSceneInitialCamera.v0.6.0.3.zip "v0.6.0.3"
[KK_StudioSceneInitialCamera]: https://github.com/GeBo1/GeBoPlugins/releases/download/r36/KK_StudioSceneInitialCamera.v0.6.0.3.zip "v0.6.0.3"
[AI_StudioSceneNavigation]: https://github.com/GeBo1/GeBoPlugins/releases/download/r31/AI_StudioSceneNavigation.v1.0.2.2.zip "v1.0.2.2"
[HS2_StudioSceneNavigation]: https://github.com/GeBo1/GeBoPlugins/releases/download/r31/HS2_StudioSceneNavigation.v1.0.2.2.zip "v1.0.2.2"
[KK_StudioSceneNavigation]: https://github.com/GeBo1/GeBoPlugins/releases/download/r31/KK_StudioSceneNavigation.v1.0.2.2.zip "v1.0.2.2"
[AI_TranslationCacheCleaner]: https://github.com/GeBo1/GeBoPlugins/releases/download/r32/AI_TranslationCacheCleaner.v0.6.0.2.zip "v0.6.0.2"
[HS2_TranslationCacheCleaner]: https://github.com/GeBo1/GeBoPlugins/releases/download/r32/HS2_TranslationCacheCleaner.v0.6.0.2.zip "v0.6.0.2"
[KK_TranslationCacheCleaner]: https://github.com/GeBo1/GeBoPlugins/releases/download/r32/KK_TranslationCacheCleaner.v0.6.0.2.zip "v0.6.0.2"
[AI_TranslationHelper]: https://github.com/GeBo1/GeBoPlugins/releases/download/r28/AI_TranslationHelper.v1.1.0.8.zip "v1.1.0.8"
[HS2_TranslationHelper]: https://github.com/GeBo1/GeBoPlugins/releases/download/r28/HS2_TranslationHelper.v1.1.0.8.zip "v1.1.0.8"
[KK_TranslationHelper]: https://github.com/GeBo1/GeBoPlugins/releases/download/r28/KK_TranslationHelper.v1.1.0.8.zip "v1.1.0.8"


