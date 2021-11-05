# QOL-Mod
A mod that offers quality-of-life improvements and additions to [Stick Fight: The Game](https://store.steampowered.com/app/674940/Stick_Fight_The_Game/).<br/>
This is accomplished through a GUI menu but alternative chat commands are listed below.<br/>
To open the menu, use the keybind: <kbd>LeftShift</kbd> + <kbd>F1</kbd><br/>

A previous message system allows you to use the <kbd>↑</kbd> & <kbd>↓</kbd> keys to easily return to your previous messages.<br/>
There is a maximum of **``20``** messages stored before they start being overwritten.<br/>

The mod is a plugin for BepInEx which is required to load it. Everything is patched at runtime.<br/>
Scroll to the bottom of this README for a video of a general overview of this mod.

## Installation

To use the mod, here are the steps required:<br/> 
  1)  Download [BepInEx](https://github.com/BepInEx/BepInEx/releases), grab the lastest release of version **``5.4``**.
  2)  Follow the [Installation Tutorial](https://docs.bepinex.dev/master/articles/user_guide/installation/unity_mono.html).
  3)  Put the mod into the now generated ``BepInEx/plugins`` folder for BepInEx to load.
  4)  Start the game, join a lobby, and enjoy!

## Caveats

The following are some general things to take note of:
  - Both the ``/private`` & ``/public`` commands require you to be the host in order to function.
  - The ``/rich`` command only enables rich text for you, and anyone else using the mod.
  - The auto-translation feature uses the Google Translate API and has a rate-limit of **``100``** requests per hour.
  - The ``ツ`` character outputted by the ``/shrug`` command shows up as invalid (�) ingame.

## Chat Commands

Command | Description
--------- | -----------
**Usage:**		| ```/<command_name> [<additional parameter>]```
/gg		| Enables automatic sending of "gg" upon death of mod user.
/shrug ```[<message>]```		| Appends ¯\\\_(ツ)\_/¯ to the end of the typed message.
/rich		| Enables rich text for chat (**visible to mod user only**).
/private		| Privates the current lobby (**must be host**).
/public		| Opens the current lobby to the public (**must be host**).
/uncensor		| Disables chat censorship.
/hp	```[<target_color>]```	| Outputs the percent based health of the target color to chat. Leave as ``/hp`` to always get your own.
/invite		| Generates a "join game" link and copies it to clipboard.
/translate		| Enables auto-translation for messages from others to English.

## QOL Mod Overview

Video coming soon!
