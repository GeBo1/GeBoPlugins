# StudioSceneNavigation

Provides hotkeys for loading the next/previous scene from the scenes folder. Supports [Illusion BrowserFolders](https://github.com/ManlyMarco/Illusion_BrowserFolders). Tracks last loaded image so it can pick up where it left off between sessions (or if you change folders).

## Supported Games

|                         | Game  | Studio  | Download     |
| ----------------------: | :---: | :-----: | ------------ |
| Koikatu/Koikatsu Party  | ❌    | ✔️       | [KK Latest]  |
| AI-Shoujo/AI-Syoujyo    | ❌    | ✔️       | [AI Latest]  |
| Honey Select 2          | ❌    | ✔️       | [HS2 Latest] |


## Configuration

### Settings

#### Notification Sounds

When enabled, notification sounds will play when scene loading is complete, or navigation fails.

#### Track Last Loaded Scene

When enabled, the last loaded scene will be tracked externally and can be reloaded upon return.

#### Restore Loader Page

When opening the scene browser, scroll to the last loaded scene.

#### Keyboard Shortcuts

##### Navigate Next

Navigate to the next (newer) scene.

##### Navigate Previous

Navigate to the previous (older) scene.

##### Reload Current

Reload the currently loaded scene.

## Technical Info

### Dependencies

- [GeBoCommon](https://github.com/GeBo1/GeBoPlugins)
- [ExtensibleSaveFormat](https://github.com/IllusionMods/BepisPlugins) v16.2.1+
- [Illusion Modding API](https://github.com/IllusionMods/IllusionModdingAPI) v1.15.0+
- [BepInEx](https://github.com/BepInEx/BepInEx) v5.4.4+

[//]: # (## Latest Links)

[AI Latest]: https://github.com/GeBo1/GeBoPlugins/releases/download/r6/AI_StudioSceneNavigation.v0.8.6.zip "v0.8.6"
[HS2 Latest]: https://github.com/GeBo1/GeBoPlugins/releases/download/r6/HS2_StudioSceneNavigation.v0.8.6.zip "v0.8.6"
[KK Latest]: https://github.com/GeBo1/GeBoPlugins/releases/download/r6/KK_StudioSceneNavigation.v0.8.6.zip "v0.8.6"