# GameDialogHelper

Highlights correct dialog choice in KK main game. Can be configured to use advanced game logic or simply your relationship level with the character.

## Supported Games

|                         | Game  | Studio  | Download     |
| ----------------------: | :---: | :-----: | ------------ |
| Koikatu/Koikatsu Party  | ✔️     | ❌      | [KK Latest]  |

## Configuration

### Settings

#### Plugin Mode

Controls how plugin operates.

- **Disabled** - Highlighting is disabled.
- **Relationship Based** - Highlighting will be controlled by the relationship level with the character, controlled by [Minimum Relationship].
- **Advanced Game Logic** - Every time a question is asked there's a chance it will be highlighted based on the players statistics, relationship level, and memory of past conversations.  Every time a question is answered there's an intelligence check to see how well the player will remember it for next time.  If used with [Highlight Mode] set to `Change Color` the level of highlighting is based on how certain the player is.


#### Highlight Mode

How to signify if an answer is right or wrong.

- **Change Color** - Changes the text color to signify correct/incorrect choices.
- **Append Text** - Appends string to correct/incorrect choices.

#### Highlight Character (correct)

String to append to highlighting correct answers (when using Append Text).

#### Highlight Character (incorrect)

String to append when highlighting incorrect answers (when using Append Text).

#### Relationship Mode

These settings are only used if [Plugin Mode] is `Relationship Based`.

##### Minimum Relationship

Highlight correct choice if relationship with character is the selected level 
or higher.

- **Anyone** - Always highlight the correct answer.
- **Acquaintance** - Highlight the correct answers for acquaintances (or higher).
- **Friend** - Highlight the correct answers for friends (or higher).
- **Lover** - Highlight the correct answers only if you're dating.
- **Disabled** - Disable showing correct answers.

### Dependencies

- [GeBoCommon](https://github.com/GeBo1/GeBoPlugins)
- [ExtensibleSaveFormat](https://github.com/IllusionMods/BepisPlugins) v16.2.1+
- [Illusion Modding API](https://github.com/IllusionMods/IllusionModdingAPI) v1.15.0+
- [BepInEx](https://github.com/BepInEx/BepInEx) v5.4.4+

[//]: # (## Latest Links)

[KK Latest]: https://github.com/GeBo1/GeBoPlugins/releases/download/r17/KK_GameDialogHelper.v1.0.0.zip "v1.0.0"