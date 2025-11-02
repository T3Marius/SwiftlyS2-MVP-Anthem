# MVP-Anthem

# Installation
## -Drag&Drop MVP_Anthem in swiftlys2/plugins

# Requirements
- **[** [**SwiftlyS2**](https://github.com/swiftly-solution/swiftlys2) **]**
- **[** [**AudioApi**](https://github.com/SwiftlyS2-Plugins/Audio) **]**
# Config
```jsonc
{
  "Main": {
    "SoundEventFiles": ["soundevents/mvp_anthem.vsndevts"],
    "ShakePlayerScreen": true,
    "GiveRandomMVPOnFirstConnect": true,
    "FreezePlayerInMenu": true,
    "EnableMenuSounds": true,
    "DefaultVolume": 0.2,
    "CenterHTMLTimer": 10.0,
    "CenterTimer": 10.0,
    "AlertTimer": 10.0,
    "Commands": {
      "MVPCommands": [
        "mvp",
        "music"
      ],
      "VolumeCommands": [ // not yet implemented, you can safely ignore this.
        "mvpvol",
        "vol"
      ]
    },
    "MVPSettings": {
      "PUBLIC MVP": { // category
        "mvp.1": {
          "MVPName": "Flawless",
          "MVPSound": "MVP_Flawless", // this is the sound that will be played.
          "EnablePreview": true,
          "PrintToAlert": false,
          "PrintToCenter": false,
          "PrintToHtml": true,
          "PrintToChat": false,
          "SteamID": ""
        },
        "mvp.2": {
          "MVPName": "Protection Charm",
          "MVPSound": "MVP_ProtectionCharm",
          "EnablePreview": true,
          "PrintToAlert": false,
          "PrintToCenter": false,
          "PrintToHtml": true,
          "PrintToChat": true,
          "SteamID": "76561199478674655" // private mvp
        }
      }
    }
  }
}
```

# Uploading Workshop Sounds Tutorial.
- [CLICK HERE](https://youtu.be/ELnCfj0xGQ8)

## You use for testing my WorkshopAddon: **[** [**Workshop**](https://steamcommunity.com/sharedfiles/filedetails/?id=3450055137) **]**
- Just add the addon id in multiaddongmanager.cfg and you can use the 2 mvp's.
