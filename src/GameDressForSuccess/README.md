# GameDressForSuccess

When the player is invited (or invites a girl) to swim/exercise he can automatically change clothes rather than going in what he's already wearing. By default only applies if player clothes are already set to automatic, but can be configured to always be applied.

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

### Supported Games

|                         | Game               | Studio             |
| ----------------------: | ------------------ | ------------------ |
| Koikatu/Koikatsu Party  | :heavy_check_mark: | :x:                |


### Dependencies

- [Illusion Modding API](https://github.com/IllusionMods/IllusionModdingAPI) v1.12.3+
- [BepInEx](https://github.com/BepInEx/BepInEx) v5.1+
