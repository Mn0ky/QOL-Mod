# QOL-Mod
<p align="center">
  <a href="https://github.com/Mn0ky/QOL-Mod/releases/latest">
    <img src="https://img.shields.io/github/downloads/Mn0ky/QOL-Mod/total?label=Github%20downloads&logo=github">
  </a>
</p>

A mod that offers quality-of-life improvements and additions to [Stick Fight: The Game](https://store.steampowered.com/app/674940/Stick_Fight_The_Game/).<br/>
This is accomplished through a GUI menu but alternative chat commands are listed below.<br/>
To open the menu, use the keybind: <kbd>LeftShift</kbd> + <kbd>F1</kbd><br/>

A previous message system allows you to use the <kbd>↑</kbd> & <kbd>↓</kbd> keys to easily return to your previous messages.<br/>
There is a maximum of **``20``** messages stored before they start being overwritten.<br/>

The mod is a plugin for BepInEx which is required to load it. Everything is patched at runtime.<br/>
Scroll to the bottom of this README for a video of a general overview of this mod.

## Installation

To use the mod, here are the steps required:<br/> 
  1)  Download [BepInEx](https://github.com/BepInEx/BepInEx/releases/tag/v5.4.19), make sure it's version **``5.4.19``** (**must be 32-bit, x86**).
  2)  Extract the newly downloaded zip into the ``StickFightTheGame`` folder.
  3)  Drag all contents from the folder into the ``StickFightTheGame`` folder (``winhttp.dll``, ``doorstop_config.ini``, the ``BepInEx`` folder etc.).
  4)  Launch the game and then exit (BepInEx will have generated new files and folders).
  5)  Download the latest version of the QOL mod from the [Releases](https://github.com/Mn0ky/QOL-Mod/releases/latest) section.
  6)  Put the mod zip into the newly generated folder located at ``BepInEx/plugins`` and **extract it to a folder named QOL-MOD** for BepInEx to load.
  7)  Start the game, join a lobby, and enjoy!

## Caveats

The following are some general things to take note of:
  - Both the ``/private`` & ``/public`` commands require you to be the host in order to function.
  - The ``/rich`` command only enables rich text for you, a.k.a client-side only.
  - The auto-translation feature uses the Google Translate API and has a rate-limit of **``100``** requests per hour.
  - A custom player color only shows for you, a.k.a client-side only.

## GUI Menu

The menu is the primary way to use and enable/disable features.<br/>
It can be opened with the keybind: <kbd>LeftShift</kbd> + <kbd>F1</kbd><br/>
An image below shows a visual overview:<br/>
![Image of QOL Menu](https://i.ibb.co/LhWr9hV/QOL-MENU-cropped.png)<br/>
Alternative chat commands are listed directly below.
## Chat Commands

Command | Description
--------- | -----------
**Usage:**		| ```/<command_name> [<additional parameter>]```
/adv		| Outputs whatever you set it to in the config.
/gg		| Enables automatic sending of "gg" upon death of mod user.
/help		| Opens up the Steam overlay and takes you to this page.
/hp	```[<target_color>]```	| Outputs the percent based health of the target color to chat. Leave as ``/hp`` to always get your own.
/id	```[<target_color>]```		| Copies the Steam ID of the target player to clipboard.
/invite		| Generates a "join game" link and copies it to clipboard.
/lobhealth		| Outputs the health set for the whole lobby.
/lobregen		| Outputs whether or not regen is enabled for the lobby.
/nukychat		| Lets you talk like Nuky. Splits up any message you send and outputs it word by word.
/ping ```[<target_color>]```		| Outpus the ping for the targeted player.
/private		| Privates the current lobby (**must be host**).
/public		| Opens the current lobby to the public (**must be host**).
/rich		| Enables rich text for chat (**visible to mod user only**).
/shrug ```[<message>]```		| Appends ¯\\\_(ツ)\_/¯ to the end of the typed message.
/stat ```[<target_color> <stat_type>]```		| Opens/closes the stats menu.
/translate		| Enables auto-translation for messages from others to English.
/uncensor		| Disables chat censorship.
/uwu		| *uwuifies* any message you send.
/ver		| Outputs the mod version string.
/winstreak		| Enables winstreak mode.

## Using The Config

A configuration file named ``monky.plugins.QOL.cfg`` can be found under ``BepInEx\config``.<br/>
Please note that you ___must run the mod at least once___ for it to be generated.<br/>
You can currently use it to set certain features to be enabled on startup.<br/>
Example: 
```cfg
## Enable rich text for chat on startup?
# Setting type: Boolean
# Default value: false
RichTextInChat = true
```
Changing ``RichTextInChat = false`` to ``RichTextInChat = true`` will enable it on startup without the need for doing ``/rich`` to enable it.<br/>

To change your player color to a custom value, please look in the config and replace the default value of ``FFFFFFFF`` to a [HEX color](https://g.co/kgs/qJMEDR).<br/>
An example is the color neon pink, which the HEX value is: ``FF10F0``<br/>
Please *do not* include a ``#`` character at the front of your HEX value.

Another important option to mention for the config is the ability to specify an API key for Google Translate.<br/>
In doing so, this will allow you to bypass the rate-limit that comes normally.<br/> 
**You are responsible for creating the key, and any potential charges accrued.**<br/>
Instructions & documentation for all of that can be found [here](https://cloud.google.com/translate).<br/>

Simply delete the config file to have a new one generated with default settings.<br/>
Updating the mod ***does not*** require you to delete the config file.

## QOL Mod Overview

Video coming soon!
