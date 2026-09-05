# Claw Command Station

<p align="center"><img src="https://raw.githubusercontent.com/Claw-Command14/space/master/Resources/Textures/Logo/flooflogo.png" width="512px" /></p>

---

Space Station 14 is a remake of SS13 that runs on [Robust Toolbox](https://github.com/space-wizards/RobustToolbox), our homegrown engine written in C#, ameowzing I know.

This is the some random fork of Space Station 14, use spoon to eat it.

To prevent people forking RobustToolbox, a "content" pack is loaded by the client and server. This content pack contains everything needed to play the game on one specific server.

If you want to host or create content for SS14, this is the repo you need (at least for LAN and 'sketchy' hosts, you need shit ton of configs to make actual server, also happiness not included). It contains both RobustToolbox and the content pack for development of new content packs.

## Links

[Wiki](https://wiki.spacestation14.com/wiki/) (remember it's Wizden wiki)

[Online Cookbook](https://heurl.in/ss14/recipes?fork=floof) (kindly provided by the wonderful Arimah <3)


## Contributing

Feel free to improve, add, balance and stuff.

## Building

Refer to [the Space Wizards' guide](https://docs.spacestation14.com/en/general-development/setup/setting-up-a-development-environment.html) on setting up a development environment and for general information.
I will provide scripts for all of that in one point in the future.

### Build dependencies

> - Git
> - .NET SDK 9.0.100

### Windows

> 1. Clone this repository
> 2. Run `python RUN_THIS.py` in a terminal to download the engine
> 3. Run `dotnet build (for Release build, add '-c Release')` to build and test code
> 4. Run `dotnet run --project Content.Server --no-build` to launch the server
> 5. Connect to localhost in the client and play

### Linux

> 1. Basically the same as Winslop but you must wear thigh highs :3

### MacOS

> I dunno, probably simillar to all above

## License

You will find it code of conduict
