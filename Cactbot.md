# Integrated Cactbot Support

The testing builds of LMeter support experimental Cactbot integration.

https://github.com/joshua-software-dev/LMeter/assets/55586380/3a8070f4-afd6-4ba7-ae0d-6c469fe0e246

A comparison showing the default Cactbot web overlay (top) rendering timeline events compared with the LMeter integration (bottom)

https://github.com/joshua-software-dev/LMeter/assets/55586380/cbbb52aa-daec-40b7-bbd0-70efd7a40ceb

A comparison showing the default Cactbot web overlay (top) alerts compared with the LMeter integration (bottom)

https://github.com/joshua-software-dev/LMeter/assets/55586380/fbeab6eb-ac30-43e0-8f58-0e6b7da406fd

LMeter can optionally show alerts in chat

![](https://github.com/joshua-software-dev/LMeter/blob/master/repo/cactbot_preview_positioning.png)
A preview for the positioning of LMeter's integrated Cactbot Timeline Events / Alerts (both are separately user configurable)

## Why add Cactbot integration?

When you run a window over an existing one, your Operating System has to draw both windows, then draw them together in a combined frame before sending this new frame to your GPU, and ultimately to your display. This increases the latency before the frame is displayed, and more importantly for many monitors, preventing a game from being to sole source of frames breaks FreeSync/GSync, and its ability to dynamically change your monitor's refresh rate based on in game FPS. If this results in even momentary hitches due to lag or load times or any other interruption longer than the refresh rate of 1 frame on your monitor, it can create uneven frame pacing. Worse yet, many lower cost FreeSync monitors lose calibration during these events, causing potentially extremely disorienting color and brightness flickering lasting multiple seconds.

Dalamud and its plugins, by contrast, hooks into the game's DirectX renderer, adding their own rendering into the game's render pipeline at a point shortly after it is finished with the game's normal rendering. This avoids the issues with Window over Window like Cactbot web browser overlays and web browser dps meters overlays suffer from, by being nearly the same as the game's own menus, technically only being "1 window" in a manner of speaking.

## How does it work

Cactbot is fundamentally just a website. It connects using WebSockets to ACT to fetch combat data and then displays timeline events and alerts based on that data, using techniques identical to how a dynamic website would render. The Cactbot ACT plugin then uses an integrated web browser with a transparent background to render over your game, much the same way as placing any other window over your game while it is running.

A straightforward way to display Cactbot it then follows should be to integrate a web browser into Dalamud's renderer. However, attempts to do so have had mixed results. Their are two primary plugins with this functionality, [Browsingway](https://github.com/Styr1x/Browsingway) and [NextUI](https://gitlab.com/kaminariss/nextui-plugin). Neither has managed to perfectly integrate this feature, as both experience issues with unloading and reloading the plugin (essential for updates, forcing a restart of the game to update these plugins), crashes and instability under wine, and for some, significant lag depending on your hardware and Operating System.

LMeter's attempted solution then, is slightly different. LMeter runs a custom web browser as a background process, instead of integrating it into the plugin, and polls the web browser for the present state of the layout of the website (its HTML) every so often. This method would be much too slow to display the actual browser rendered frames without a massive performance cost, but fetching the small amount of text that makes up the Cactbot website is comparatively trivial. So then, the background web browser runs Cactbot, and LMeter simply fetches this text content and then reprocesses and renders it using Dalamud. This means a custom UI is required, and any updates to Cactbot's UI will **not** be trivially reflected in LMeter, as this feature manually recreated a facsimile of Cactbot's UI. However, as Cactbot is updated with support for future fights, no updating of the plugin should be required, as LMeter simply loads the latest version of the Cactbot website and thus pulls in any updates they push out right away.

## How do I enable and use this feature?

![](https://github.com/joshua-software-dev/LMeter/blob/master/repo/dalamud_settings_part1.png)

The first thing to do is to ensure you've added the LMeter `repo.json` url to your Custom Plugin Repositories in your Dalamud Settings.

![](https://github.com/joshua-software-dev/LMeter/blob/master/repo/dalamud_settings_part2.png)

As of the time of writing, the Cactbot integration is only in the testing builds. You can enable receiving testing builds in Dalamud Settings.

![](https://github.com/joshua-software-dev/LMeter/blob/master/repo/cactbot_browser_settings.png)

After doing both these steps and installing LMeter, open LMeter's settings menu, and go to the Cactbot tab. The browser is not automatically started because starting a subprocess on your computer without your permission is rude. The first time you wish to start it, you must click the `Start Web Browser` button, and then LMeter will automatically download the required web browser application, [TotallyNotCef](https://github.com/joshua-software-dev/TotallyNotCef). Every time after the first, the browser can be launched for you if you enable `Automatically Start Background Web Browser`, or you can also choose to do so manually each time, at your discretion. Whenever the `TotallyNotCef` is started, LMeter will first check for updates to `TotallyNotCef`, and if the default install location of `TotallyNotCef` is used, LMeter will update it for you.

![](https://github.com/joshua-software-dev/LMeter/blob/master/repo/cactbot_connection_settings.png)

After starting the browser, you need to enable connecting to it. This can be done with the `Enable Connection to Browser` toggle in the `Connection Settings` submenu of the Cactbot tab. From here, it is likely the feature will: "Just Work (tm)", however, if you are running other services on your computer, you may need to change the `HTTP Server Port` option. This setting changes what port `TotallyNotCef` hosts the Cactbot text content from. It is not likely you need to change this unless something else you are running is using port 8080.

## Troubleshooting

* Cactbot changed what URL they host their program from / I don't want to use the IINACT proxy to Cactbot / The IINACT proxy to Cactbot is broken!

If for any reason these things break or bother you, you can set a different URL using the `Cactbot URL` setting in the `Connection Settings` submenu. You will need to restart the web browser for a URL change to take effect when it is already running.

* Alerts / Timeline Events aren't showing up fast enough / there is a delay

How fast the background browser is polled for these updates can be changed by the user in the `Connection Settings` submenu. The default is `10` milliseconds in combat and `1000` outside of it. There should be no notable performance hit changing this to `1` ms for both, if any delay bothers you.

* The background web browser isn't starting / no Cactbot information ever renders

It is possible for the background web browser to fail to start depending on your hardware / software configuration. This problem occurs notably often for some wine distributions, such as proton or when running under flatpak. If for whatever reason `TotallyNotCef` fails to start reliably under wine for you, there is also a linux native version of `TotallyNotCef` available for download [here](https://github.com/joshua-software-dev/TotallyNotCef/releases). When you are running the linux native version ahead of time / in the background, you should only need to toggle `Enable Connection to Browser` on in the `Connection Settings` submenu, and disable `Automatically Start Background Web Browser` in the `Web Browser Settings` submenu for the feature to work. Whenever the plugin is unloaded cleanly (ex. When shutting down the game, updating, etc.), it will send a command to the background web browser to stop, so you will need to relaunch it when this happens.

For the `..._selfcontained.zip` releases, a stripped down version of the .NET runtime is included with the application, allowing it to run without installing any dependencies. However, this does make the total size on disk of the install larger, and if you have .NET 7+ installed at a system level, you likely only need the regular `...linux.zip` releases. If you don't know what that means, or are running a locked down linux release like SteamOS on the Steam Deck, the `..._selfcontained.zip` release is the one you should download.

It is recommended you download and extract the zip into its own folder, and that wherever you install it to, TotallyNotCef will have write permissions to write new files. Where you install isn't very important, as long as its writable and you can access / remember the path you choose. `TotallyNotCef` is a command line program, and you'll need to start it from a terminal (or with a script) with some arguments for it to work. These values are:

`./path/to/TotallyNotCef <url> <httpPort> <enableAudio=1/0>`

so a more complete example looks like

`./path/to/TotallyNotCef 'https://quisquous.github.io/cactbot/ui/raidboss/raidboss.html?OVERLAY_WS=ws://127.0.0.1:10501/ws' 8080 1`

or to run it in the background:

`nohup ./path/to/TotallyNotCef 'https://quisquous.github.io/cactbot/ui/raidboss/raidboss.html?OVERLAY_WS=ws://127.0.0.1:10501/ws' 8080 1 & disown`

so you should be able to do something like the following to get a GUI startable script:

```
$ cd path/to/TotallyNotCef
$ touch start_tncef.sh
$ chmod +x start_tncef.sh
```

then open the file in a text editor, and change its contents to:

```
#!/bin/bash

nohup ./TotallyNotCef 'https://quisquous.github.io/cactbot/ui/raidboss/raidboss.html?OVERLAY_WS=ws://127.0.0.1:10501/ws' 8080 1 & disown
```

This can likely be integrated into the launch arguments if you're running the game from Game Mode on the Steam Deck, but I do not own the hardware to test this, and it will likely be harder if you're running the flatpak release to do so than simply running it ahead of time and then launching the game. Its likely a script of this fashion would work for those not using flatpak:

```
#!/bin/bash

./path/to/TotallyNotCef 'https://quisquous.github.io/cactbot/ui/raidboss/raidboss.html?OVERLAY_WS=ws://127.0.0.1:10501/ws' 8080 1 &
exec "$@"
```
