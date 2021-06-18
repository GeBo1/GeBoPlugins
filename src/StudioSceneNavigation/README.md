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
- [ExtensibleSaveFormat](https://github.com/IllusionMods/BepisPlugins) v16.3.1+
- [Illusion Modding API](https://github.com/IllusionMods/IllusionModdingAPI) v1.20.3+
- [BepInEx](https://github.com/BepInEx/BepInEx) v5.4.8+

[//]: # (## Latest Links)

[AI Latest]: https://github.com/GeBo1/GeBoPlugins/releases/download/r31/AI_StudioSceneNavigation.v1.0.2.2.zip "v1.0.2.2"
[HS2 Latest]: https://github.com/GeBo1/GeBoPlugins/releases/download/r31/HS2_StudioSceneNavigation.v1.0.2.2.zip "v1.0.2.2"
[KK Latest]: https://github.com/GeBo1/GeBoPlugins/releases/download/r31/KK_StudioSceneNavigation.v1.0.2.2.zip "v1.0.2.2"