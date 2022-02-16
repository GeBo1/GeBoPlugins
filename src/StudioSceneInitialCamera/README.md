# StudioSceneInitalCamera

Provides methods for returning to camera position when scene was initially loaded in studio.

## Supported Games

|                         | Game  | Studio  | Download     |
| ----------------------: | :---: | :-----: | ------------ |
| Koikatu/Koikatsu Party  | ❌    | ✔️       | [KK_StudioSceneInitalCamera]  |
| AI-Shoujo/AI-Syoujyo    | ❌    | ✔️       | [AI_StudioSceneInitalCamera]  |
| Honey Select 2          | ❌    | ✔️       | [HS2_StudioSceneInitalCamera] |

## Features

- Allows you to select/activate the initial camera displayed when scene loaded.
- Will store the initial camera an unused camera slot, if there are at least two available
- Adds a camera object to the scene that can be selected to activate the initial camera when there is not a free slot available (disabled by default).
- Keeps a history so when you save a scene again you can navigate to the previous initial camera state (in case you forgot to reactive it before saving).
- Prevents [Autosave](https://github.com/IllusionMods/KK_Plugins#autosave) from updating the inital camera on save, so your autosave files will preserve the initial camera.

## Configuration

### Settings

#### Config

##### Activate Initial Camera

Key that changes to the initial camera (behaves like 1-9,0) (defaults to `` ` ``)

##### Restore Previous Initial Camera

Attempt to restore previous saved camera state (defaults to `` Ctrl+` ``).

##### Restore Next Initial Camera

Attempt to restore more recent saved camera state (defaults to `` Alt+` ``).

#### Save Camera

##### Save Initial Camera

Will attempt to save the initial camera to an unused scene camera button after scene is loaded

##### Preserve Camera During Autosave

Will attempt to preserve the initial camera when Autosave plugin saves scene.

##### Create Studio Camera Object

If [Save Initial Camera] is enabled, but the plugin is unable to find an unused camera slot, a special studio camera object will be a added to the scene. When activated it will jump to the initial camera instead (disabled by default).

## Technical Info

### Dependencies

- [GeBoCommon](https://github.com/GeBo1/GeBoPlugins)
- [Illusion Modding API](https://github.com/IllusionMods/IllusionModdingAPI) v1.20.3+
- [BepInEx](https://github.com/BepInEx/BepInEx) v5.4.8+

[//]: # (## Latest Links)

[AI_StudioSceneInitalCamera]: https://github.com/GeBo1/GeBoPlugins/releases/download/r35/AI_StudioSceneInitialCamera.v0.6.0.2.zip "v0.6.0.2"
[HS2_StudioSceneInitalCamera]: https://github.com/GeBo1/GeBoPlugins/releases/download/r35/HS2_StudioSceneInitialCamera.v0.6.0.2.zip "v0.6.0.2"
[KK_StudioSceneInitalCamera]: https://github.com/GeBo1/GeBoPlugins/releases/download/r35/KK_StudioSceneInitialCamera.v0.6.0.2.zip "v0.6.0.2"