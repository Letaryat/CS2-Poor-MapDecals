# CS2-Poor-MapDecals

This plugin allows for server owners to create spray type advertisements that are placed on wall.<br/>
[![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/H2H8TK0L9)

## [📺] Video presentation
SoonTM
<p align="center">
    <img src="img/1.jpg" width="500">
</p>

## [📌] Setup
- Download latest release,
- Drag files to /plugins/
- Restart your server,
- Config file should be created in configs/plugins/
- Edit to your liking,

## [📝] Configuration
| Option  | Description |
| ------------- | ------------- |
| Admin Flag (string) | Which flag will have access to all of the commands  |
| Vip Flag (string) | Which flag would not see advertisements that are not forced on vip users |
| Props Path (string[]) | Paths for all advertisements that your addon have |
| Enable commands (bool) | If you want commands to be enabled. (for example, after you placed all of the advertisements you might not need commands anymore) |
| Debug Mode (bool) | If plugin should log errors, etc |

### [📝] Config example:
```
{
  "Admin Flag": "@css/root",
  "Vip Flag": "@vip/noadv",
  "Props Path": [
	"materials/Example/exampleTexture.vmat", // ID 0
	"materials/Example/exampleTexture2.vmat" // ID 1 etc...
  ],
  "Enable commands": true,
  "Debug Mode": true,
  "ConfigVersion": 1
}
```

## [🛡️] Admin commands
Tried to make plugin idiot proof (since I did a lot of mistakes).
| Command  | Description |
| ------------- | ------------- |
| css_placedecals | Allow to place advertisements |
| css_setdecal **ID_OF_DECAL** **WIDTH** **HEIGHT** **FORCE_ON_VIP (TRUE/FALSE)** | Configure decal that you want to place |
| css_pingdecals | Allows to place decals using Ping function |
| css_removedecal **ID** | Remove already placed decal using ID |
| css_tpdecal **ID** | Teleports to already existing decal using ID |
| css_showdecals | Prints info to console about all decals that are placed on map |
| css_printdecals | Prints a list of all decals that can be placed to console |

## [❤️] Special thanks to:
- [CS2-SkyboxChanger by samyycX](https://github.com/samyycX/CS2-SkyboxChanger) - For function to find id of cached material.
- [Edgegamers JailBreak](https://github.com/edgegamers/Jailbreak/blob/main/mod/Jailbreak.Warden/Paint/WardenPaintBehavior.cs#L131) - For function to check if player is looking at his pretty feet.

### [🚨] Plugin might be poorly written and have some issues. I have no idea what I am doing, but when tested it worked fine.