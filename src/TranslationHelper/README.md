# TranslationHelper

Improves the experience of using XUnity.AutoTranslator in numerous ways, mostly around how card names are handled.  


## Supported Games

|                         | Game  | Studio  | Download     |
| ----------------------: | :---: | :-----: | ------------ |
| Koikatu/Koikatsu Party  | ✔️     | ✔️       | [KK_TranslationHelper]  |
| AI-Shoujo/AI-Syoujyo    | ✔️     | ✔️       | [AI_TranslationHelper]  |
| Honey Select 2          | ✔️     | ✔️       | [HS2_TranslationHelper] |

## Features

The specific features vary slightly from game to game, but generally does the following:

- Translates card names as they're loaded (for games with separate given/family names it translates them independently, for games with a single full-name field there's the option to split before translating).  
- Deals with the fact that some names need to be translated with context (the same name may be translated differently based on gender, or if it's being used as a given or family name).
- ***NEW*** Preset system that allows for quick translation of known characters and can handle cases where the same name may have a different preferred translation for a specific individual (presets for NPC and default cards from all supported games are built in, additional presets can be added externally).
- Uses specific translation scopes for names, so name translation files won't pollute the global translations and can handle the specific types of names.
- Uses multiple methods to cache results to avoid repeat attempts to translate the same names and minimize performance impact.

### Main Game

- Handles translation of card names when there is in-game browsing.
- Handles game specific UIs (Roster in KK, Girl/Room selection in HS2)
- Special handling for merchant character (AI)

### Maker

- Handles translation of the card names when browsing.
- Places translated name into the name editor to simplify updating.
- Can optionally save cards using translated names.

### Studio

- Handles translation of the card lists for adding new characters to scene.
- Updates character nodes in the object tree to use the correct translated name.


## Configuration

### Settings

#### Maker

##### Save Translated Names

When enabled translated names will be saved with cards in maker, otherwise original unmodified names will be restored. If you edit the name, it keeps your edit always.

#### Translate Card Name Modes

These settings control how card name translation is handled.  It can be adjusted individually for the main game, maker and studio.  These default to **Cache Only** but I recommend you change them to **Fully Enabled**.

Disabled
: Card name translation handling will be disabled.

Cache Only
: Card name translation will be enabled. Translation will first be attempted via [presets], and then by checking for cached translations with `XUnity.AutoTranslator`.

Fully Enabled
: Card name translation will be enabled. This mode does everything **CacheOnly** does, and if that fails it will use `XUnity.AutoTranslator` to perform an asynchronous translation.


#### Translate Card Name Options

##### Characters to Trim

Characters to trim from returned translation strings.  Normally not needed but may be helpful with specific translation endpoints.

##### Register Active Characters

Register active character names as replacements with translator.  This helps keep your translation cache from being filled with a lot of similar entries when character names are substituted into dialog, etc.

##### Show given name first

If enabled, reverses the order of names to be Given then Family instead of Family then Given (only available in games where card names are stored in separate fields).

##### Split Names Before Translate

If enabled, split on space and translate names separately (only available in games where card names are stored in a single field).

##### Use Suffix

Append suffix to names before translating to send hint they are names to translation endpoints.  If the translation returns with translated suffix, it will be removed.  Some automatic translation endpoints handle this better than others.



### Presets

TBD

#### Example
```xml
<?xml version='1.0' encoding='utf-8'?>
<NamePresets>
  <NamePreset>
    <Sex>Female</Sex>
    <GivenNames>
      <Name>つむぎ</Name>
      <Name>紬</Name>
    </GivenNames>
    <FamilyNames>
      <Name>ことぶき</Name>
      <Name>琴吹</Name>
    </FamilyNames>
    <NickNames>
      <Name>むぎ</Name>
      <Name>ムギ</Name>
    </NickNames>
    <Translations>
      <Translation>
        <GivenName>Tsumugi</GivenName>
        <FamilyName>Kotobuki</FamilyName>
        <NickName>Mugi</NickName>
      </Translation>
    </Translations>
  </NamePreset>
</NamePresets>
```


### Translation Scopes

TBD

## Technical Info


### Dependencies

- [GeBoCommon](https://github.com/GeBo1/GeBoPlugins)
- [ExtensibleSaveFormat](https://github.com/IllusionMods/BepisPlugins) v16.3.1+
- [Illusion Modding API](https://github.com/IllusionMods/IllusionModdingAPI) v1.20.3+
- [XUnity.AutoTranslator](https://github.com/bbepis/XUnity.AutoTranslator) v4.16.0+
- [BepInEx](https://github.com/BepInEx/BepInEx) v5.4.8+

[//]: # (## Latest Links)

[AI_TranslationHelper]: https://github.com/GeBo1/GeBoPlugins/releases/download/r28/AI_TranslationHelper.v1.1.0.8.zip "v1.1.0.8"
[HS2_TranslationHelper]: https://github.com/GeBo1/GeBoPlugins/releases/download/r28/HS2_TranslationHelper.v1.1.0.8.zip "v1.1.0.8"
[KK_TranslationHelper]: https://github.com/GeBo1/GeBoPlugins/releases/download/r28/KK_TranslationHelper.v1.1.0.8.zip "v1.1.0.8"
