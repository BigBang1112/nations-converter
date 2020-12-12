# Nations Converter

Powered by [GBX.NET](https://github.com/BigBang1112/gbx-net).

This is the official repository for the Nations Converter project. The conversion is handled by the NationsConverter library which is used for user interface projects.

Currently there are two user interface projects, a graphical user interface using WPF, and a command line interface.

A web interface is planned for the future.

## Metadata

Every converted map adds some metadata that can be used in gamemodes or map editor plugins.

**Included since version** describes the version of the [Nations Converter library](NationsConverter/NationsConverter.csproj) (unless bracket note)

| Type | Variable | Example value | Included since version | Description
| --- | --- | --- | --- | ---
| Boolean | MadeWithNationsConverter | True | 0.1.0 | Track was made with (or with assist) of this tool. Always true.
| Text | NC_Assembly | NationsConverterGUI, Version=1.0.2.0, Culture=neutral, PublicKeyToken=null | 0.1.0 | User interface project version the conversion was made.
| Boolean | NC_EarlyAccess | False | 0.1.0 | Conversion was made in early access. False since 1.0.0 (GUI)
| Text | NC_GBXNET_Assembly | GBX.NET, Version=0.5.4.0, Culture=neutral, PublicKeyToken=null | 1.0.0 (GUI) | GBX.NET version used for the GBX parse.
| Text | NC_OriginalAuthorLogin | bigbang1112 | 1.0.1 (GUI) | Login of the original author name. Bugged in 1.0.1, fixed since 1.0.2.
| Text | NC_OriginalAuthorNickname | $h[bigbang1112]$fff$o$n$t$iBigBang1112$h  $z$40F | 1.0.1 (GUI) | Formatted nickname of the original author name. Bugged in 1.0.1, fixed since 1.0.2.
| Text | NC_Lib_Assembly | NationsConverter, Version=1.0.2.0, Culture=neutral, PublicKeyToken=null | 1.0.2 | Version of the conversion processing library used on this map.

### Usage in ManiaScript

`declare metadata Text MadeWithNationsConverter for Map`
