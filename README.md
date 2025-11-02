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
    "ShakePlayerScreen": true,
    "GiveRandomMVPOnFirstConnect": true,
    "FreezePlayerInMenu": true,
    "EnableMenuSounds": true,
    "DefaultVolume": 0.2,
    "CenterHTMLTimer": 10.0,
    "CenterTimer": 10.0,
    "AlertTimer": 10.0,
    "MenuColor": [255, 0, 0] // red
    "Commands": {
      "MVPCommands": [
        "mvp",
        "music"
      ]
    },
    "MVPSettings": {
      "PUBLIC MVP": { // category
        "mvp.1": {
          "MVPName": "Flawless",
          "MVPSound": "mvp_sounds/flawless.mp3", // this is located at data/MVP_Anthem/mvp_sounds/
          "EnablePreview": true,
          "PrintToAlert": false,
          "PrintToCenter": false,
          "PrintToHtml": true,
          "PrintToChat": false,
          "SteamID": ""
        },
        "mvp.2": {
          "MVPName": "Protection Charm",
          "MVPSound": "mvp_sounds/protectioncharm.mp3",
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
