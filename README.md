# Nations Converter

[![GitHub release (latest by date including pre-releases)](https://img.shields.io/github/v/release/BigBang1112/nations-converter?include_prereleases&style=for-the-badge)](https://github.com/BigBang1112/nations-converter/releases)
[![GitHub all releases](https://img.shields.io/github/downloads/BigBang1112/nations-converter/total?style=for-the-badge)](https://github.com/BigBang1112/nations-converter/releases)

Powered by [GBX.NET](https://github.com/BigBang1112/gbx-net).

This is the official repository for the Nations Converter project. The conversion is handled by the NationsConverter library which is used for user interface projects.

Currently there are two user interface projects, a graphical user interface using WPF, and a command line interface.

A web interface is planned for the future.

## Metadata

Every converted map adds some metadata that can be used in gamemodes or map editor plugins.

**Included since version** describes the version of the [Nations Converter library](NationsConverter/NationsConverter.csproj#L6) (unless bracket note)

- `Boolean MadeWithNationsConverter`
  - **Example value:** True
  - **Included since version:** 0.1.0
  - **Description:** Track was made with (or with assist) of this tool. Always true.
- `Text NC_Assembly`
  - **Example value:** NationsConverterGUI, Version=1.0.2.0, Culture=neutral, PublicKeyToken=null
  - **Included since version:** 0.1.0
  - **Description:** User interface project version the conversion was made.
- `Boolean NC_EarlyAccess`
  - **Example value:** False
  - **Included since version:** 0.1.0
  - **Description:** Conversion was made in early access. False since 1.0.0 (GUI)
- `Text NC_GBXNET_Assembly`
  - **Example value:** GBX.NET, Version=0.5.4.0, Culture=neutral, PublicKeyToken=null
  - **Included since version:** 1.0.0 (GUI)
  - **Description:** GBX.NET version used for the GBX parse.
- `Text NC_OriginalAuthorLogin`
  - **Example value:** bigbang1112
  - **Included since version:** 1.0.1 (GUI)
  - **Description:** Login of the original author name. Bugged in 1.0.1, fixed since 1.0.2.
- `Text NC_OriginalAuthorNickname`
  - **Example value:** $h[bigbang1112]$fff$o$n$t$iBigBang1112$h  $z$40F
  - **Included since version:** 1.0.1 (GUI)
  - **Description:** Formatted nickname of the original author name. Empty text on maps made in TMUF and lower versions.
- `Text NC_Lib_Assembly`
  - **Example value:** NationsConverter, Version=1.0.2.0, Culture=neutral, PublicKeyToken=null
  - **Included since version:** 1.0.2
  - **Description:** Version of the conversion processing library used on this map.
- `Text NC_OriginalMapUid`
  - **Example value:** JtPkBe105hUkDkQOoieBVFZTDK9
  - **Included since version:** 1.2.0
  - **Description:** Map UID of the map converted before the new UID was set. Useful for original map identification.

### Usage in ManiaScript

`declare metadata Boolean MadeWithNationsConverter for Map;`

## License

The source code files fall under [GNU General Public License v3.0](LICENSE). Markdown, custom items and blocks are usable with [The Unlicense](https://unlicense.org/) with an appreciation of author mention.