# LMeter

[![ko-fi](https://img.shields.io/badge/donate-ko--fi-blue)](https://ko-fi.com/lichie)

LMeter is a Dalamud plugin for displaying your ACT combat log data. The purpose of this plugin is to provide a highly customizable dps/hps meter without having to rely on clunky web-based overlays.

## Features

* Customize meter information to your liking:

![](https://github.com/joshua-software-dev/LMeter/blob/master/repo/meter_demo_1.png)
![](https://github.com/joshua-software-dev/LMeter/blob/master/repo/meter_demo_2.png)

* Hide meter from view based on many in game criteria:

![](https://github.com/joshua-software-dev/LMeter/blob/master/repo/auto_hide.png)

* Optionally track encounters more closely using "In Combat" status

![](https://github.com/joshua-software-dev/LMeter/blob/master/repo/end_encounter.png)

Track encounters more closely by automatically sending `/end` commands to ACT only when the "In Combat" status ends, rather than by time-out. This is optional.

* IINACT IPC Support

![](https://github.com/joshua-software-dev/LMeter/blob/master/repo/act_connection.png)

Support for obtaining data from ACT using the WebSocket protocol, or using Dalamud's IPC (Inter-Plugin Communication) feature to directly communicate with [IINACT](https://github.com/marzent/IINACT), bypassing the WebSocket altogether. This makes setting up a parser in a restricted environment (ex. Linux, Steam Deck) much simpler. Connecting to IINACT using the WebSocket is also supported.

## Experimental Features

* Cactbot Integration

Display Cactbot timeline events and alerts using the same integrated dalamud rendering rather than a web browser overlay. More information on the limitations and how to enable and configure this feature [here](https://github.com/joshua-software-dev/LMeter/blob/master/Cactbot.md).


## How to Install

LMeter is not available in the standard Dalamud plugin repository and must be installed from my third party repository.

Here is the URL for my plugin repository: `https://raw.githubusercontent.com/joshua-software-dev/LMeter/master/repo.json`
