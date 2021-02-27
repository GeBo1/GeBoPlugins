# GameDressForSuccess

When the player is invited (or invites a girl) to swim/exercise he can automatically change clothes rather than going in what he's already wearing. By default only applies if player clothes are already set to automatic, but can be configured to always be applied.

## Supported Games

|                         | Game  | Studio  | Download     |
| ----------------------: | :---: | :-----: | ------------ |
| Koikatu/Koikatsu Party  | ✔️     | ❌      | [KK Latest]  |

## Configuration

### Settings

#### Enabled

If not set to true entire plugin is disabled.

#### Mode

- **AutomaticOnly** - Default option. Only change players clothes if the players clothing type is already set to automatic in the game
- **Always** - Ignore the game setting and always change clothes when it makes sense.

### Reset to Automatic

- **Never** - Default game behavior
- **DayChange** - Reset to automatic each day
- **PeriodChange** - Default option. Reset player clothes to automatic at each period change.

## Technical Info

### Dependencies

- [Illusion Modding API](https://github.com/IllusionMods/IllusionModdingAPI) v1.15.0+
- [BepInEx](https://github.com/BepInEx/BepInEx) v5.4.4+

[//]: # (## Latest Links)

[KK Latest]: https://github.com/GeBo1/GeBoPlugins/releases/download/r18/KK_GameDressForSuccess.v1.2.0.zip "v1.2.0"