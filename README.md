# **IMPORTANT!!**

- Acts From The Past isn't yet finished! It still has things like placeholder audio & visual effects, missing logic in places, and more.
- If you play with Acts From The Past and then play on the same modded profile with the mod disabled afterwards, trying to enter the Run History screen will crash the game! Either make a new modded profile just for testing this mod OR back up your modded profile. If you make a new profile, I'd recommend running `unlock all` in the console.
- Launching the game for the first time with Acts From The Past loaded will cause a `aftp_config.json` file to be created in your modded profile's file path. **By default, the new (old?) acts are mixed in with the base game's. However, if you want to ONLY play with Exordium/The City/The Beyond, you can edit this .json and set the "LegacyActsOnly" field to "true".** You can do this while the game is still running.
- The files in this repository only cover the .dll contents, not the .pck contents (which contain things like scenes, images, etc). This may change if/when I decide to restructure the mod to be made in [Alchyr's ModTemplate](https://github.com/Alchyr/ModTemplate-StS2).
- I have to imagine this mod breaks if played on widescreen or resolutions other than 16:9.
- **Acts From The Past was only tested with fast mode enabled**, no idea what timing stuff is broken when playing with it disabled
- Acts From The Past is an English-only mod for now. If anyone is willing to translate, let me know and I can send the .json files!

# Acts From The Past

Acts From The Past is a work-in-progress port of the Acts from Slay the Spire 1, including Exordium, The City, and The Beyond.

![image 1](https://cdn.discordapp.com/attachments/1481936803576680558/1481936804167811092/image.png?ex=69bbb833&is=69ba66b3&hm=ca60f38c7c83050e6b4a6c43670c17c92d20dc147f4214f0104174a180ff4a19)

The mod *only* includes the encounters and act-specific events from Slay the Spire 1's acts, meaning Shrine events such as Wheel of Change or Lady in Blue have *not* been ported (though I may do that later if there is a demand for it)

![image 2](https://cdn.discordapp.com/attachments/1481936803576680558/1481936872065335296/image.png?ex=69bbb843&is=69ba66c3&hm=993a7a28c533bcd65a4f6829f815db4da893006dbff624f9dd42ba5cd555e89a)

Some enemies have been given visual & auditory touchups to help them feel a bit more fresh in the new game environment.

![image 3](https://cdn.discordapp.com/attachments/1481936803576680558/1481936947201970176/image.png?ex=69bbb855&is=69ba66d5&hm=80d94356639033cb1da17202490898a9e797210aac1f1f47c85d9a70c18420ca)

## Installation instructions
1. If there isn't already a `mods` folder located at `C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2` (or wherever your file path equivalent is), make one. I'd also highly recommend making a folder within the `mods` folder called `ActsFromThePast`, which is where you should put all the related files.
2. Put the latest release of this mod's .pck, .dll, and mod_manifest.json files in that folder.
3. Launch the game. Note that launching the game for the first time created new "modded" save folders, this is intentional and your unmodded saves have not been wiped.

