# **IMPORTANT!!**

- Acts From The Past isn't yet finished! It still has things like placeholder audio & visual effects, missing logic in places, and more.
- Currently, you must play on the public beta branch to use this mod.
- If you play with Acts From The Past and then play on the same modded profile with the mod disabled afterwards, trying to enter the Run History screen will crash the game! Either make a new modded profile just for testing this mod OR back up your modded profile. If you make a new profile, I'd recommend running `unlock all` in the console.
- Acts From The Past is currently incompatible with several mods, including mods that add new Ancients (they probably won't show up). Please keep this in mind as you play.
- Launching the game for the first time with Acts From The Past loaded will cause a `aftp_config.json` file to be created in your modded profile's file path. **By default, the new (old?) acts are mixed in with the base game's. However, if you want to ONLY play with Exordium/The City/The Beyond, you can edit this .json and set the "LegacyActsOnly" field to "true".** You can do this while the game is still running.
- The files in this repository only cover the .dll contents, not the .pck contents (which contain things like scenes, images, etc). This may change if/when I decide to restructure the mod to be made in [Alchyr's ModTemplate](https://github.com/Alchyr/ModTemplate-StS2).
- I have to imagine this mod breaks if played on widescreen or resolutions other than 16:9.
- **This mod was not tested with multiplayer.**
- **Acts From The Past was only tested with fast mode enabled**, no idea what timing stuff is broken when playing with it disabled
- Acts From The Past is an English-only mod for now. If anyone is willing to translate, let me know and I can send the .json files!

# Acts From The Past

Acts From The Past is a work-in-progress port of the Acts from Slay the Spire 1, including Exordium, The City, and The Beyond.

![image 1](https://media.discordapp.net/attachments/1481936803576680558/1481936804167811092/image.png?ex=69b520b3&is=69b3cf33&hm=a55a05e6cfcb6642ca232b5376c7906cbabc7d6669c4a00ec5215629e259fd92&=&format=webp&quality=lossless)

The mod *only* includes the encounters and act-specific events from Slay the Spire 1's acts, meaning Shrine events such as Wheel of Change or Lady in Blue have *not* been ported (though I may do that later if there is a demand for it)

![image 2](https://media.discordapp.net/attachments/1481936803576680558/1481936872065335296/image.png?ex=69b520c3&is=69b3cf43&hm=02907ae7fca3e273ab5db2a2bee37220cf99ba50208160d88f17c9e9de8bfb12&=&format=webp&quality=lossless)

Some enemies have been given visual & auditory touchups to help them feel a bit more fresh in the new game environment.

![image 3](https://media.discordapp.net/attachments/1481936803576680558/1481936947201970176/image.png?ex=69b520d5&is=69b3cf55&hm=23edfc6eda9cde341ed2a9773c6f1440e02915205354540aecaba0ed7b6feaee&=&format=webp&quality=lossless)

## Installation instructions
1. If there isn't already a `mods` folder located at `C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2` (or wherever your file path equivalent is), make one. I'd also highly recommend making a folder within the `mods` folder called `ActsFromThePast`, which is where you should put all the related files.
2. Put the latest release of this mod's .pck, .dll, and mod_manifest.json files in that folder.
3. Launch the game. Note that launching the game for the first time created new "modded" save folders, this is intentional and your unmodded saves have not been wiped.

