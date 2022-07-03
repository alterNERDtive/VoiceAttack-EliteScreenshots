# EliteScreenshots VoiceAttack plugin

Elite Dangerous saves screenshots taken natively (using F10 or alt+F10) as
bitmaps in your “Pictures” folder. There are many tools to automatically
convert, move and rename those. This one is a
[VoiceAttack](https://voiceattack.com) plugin.

**Note**: In order to use ingame information like your CMDR name or your
current system in the output file name **you will have to use
[EDDI](https://github.com/EDCD/EDDI)** and [install it as a VoiceAttack
plugin](https://github.com/EDCD/EDDI/wiki/VoiceAttack-Integration#using-eddi-with-voiceattack).

## Install

1. Go to the [latest release](https://github.com/alterNERDtive/VoiceAttack-EliteScreenshots/releases/latest).
2. Download the attached `VoiceAttack-EliteScreenshots.zip` file.
3. Extract the contents to the `Apps` directory in your VoiceAttack installation folder.

## Upgrade

See [Install](#Install).

## Configuration

### Output Directory

By default, the plugin saves new screenshots to your Desktop.

You can change that by setting the `EliteScreenshots.outputDirectory#` text
variable in VoiceAttack.

### File Name Template

The default file name for screenshots is `%datetime%-%cmdr%-%system%-%body%`
(plus the `.png` extension).

You can set your own file name template by setting the `EliteScreenshots.format#`
text variable in VoiceAttack.

The following tokens will automatically replaced by the corresponding values.

* `%body%`: the stellar object in which sphere of influence you currently are
* `%cmdr%`: your current CMDR name
* `%date%`: the current date (YYYY-MM-DD)
* `%datetime%`: the current data and time (YYYY-MM-DD hh-mm-ss)
* `%shipname%`: your current ship
* `%system%`: the system you are currently in
* `%time%`: the current time (hh-mm-ss)
* `%vehicle%`: you current vehicle (one of “Ship”, “SRV”, “Fighter”)

## Converting Old Screenhots

On startup the plugin will inform you if it finds older screenshots. You can run
the `convertold` plugin context to batch convert them .

## Need Help / Want to Contribute?

If you run into any errors, please try running the profile in question on its 
own / get a fresh version. If that doesn’t fix the error, look at the 
[devel](https://github.com/alterNERDtive/VoiceAttack-EliteScreenshts/tree/devel) branch 
and see if it’s fixed there already.

If you have no idea what I was saying in that last parargraph and / or the 
things mentioned there don’t fix your problem, please [file an 
issue](https://github.com/alterNERDtive/VoiceAttack-EliteScreenshots/issues). Thanks! :)

You can also [say “Hi” on Discord](https://discord.gg/kXtXm54) if that is your 
thing.

[![GitHub Sponsors](https://img.shields.io/github/sponsors/alterNERDtive?style=for-the-badge)](https://github.com/sponsors/alterNERDtive)
[![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/S6S1DLYBS)
