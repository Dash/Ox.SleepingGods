# Sleeping Gods Companion App

This is an UNOFFICIAL fan-made companion app for the 
[Sleeping Gods board game](https://www.redravengames.com/sleeping-gods/).  It is in no way endorsed or sponsored by
the creators or publishers of the Sleeping Gods game.

This code repository contains the source code for the companion app.  The app is free to use by navigating to
[sleeping-gods.ox.rs](https://sleeping-gods.ox.rs).  This repository is useful if you wish to host a copy of the app
yourself, or make changes to how the app works.

# Features
The app supports both the base game and the Tides of Ruin expansion.

* Mark locations for:
  * Encountered combat level
  * Encountered skill checks
  * Resources obtainable
  * Completness
  * Safe / Dangerous
  * Whether to avoid in future
  * Any free-form notes
* Record the source of quests and keywords
* Record where quests and keywords are used
* Search for keyword locations
* Ability to import and export your database to share with others

This is the sort of information you mark on your campaign map, but after a few games this can become difficult to
manage.  The app does not currently feature granular details (unless you add custom free-form notes), so there remains
the degree of mystery between campaigns.

None of the quests/keywords are visible by default, you have to enter the keyword in initially to display it, avoiding
spoilers.

# About
This is a Progressive Web App that runs entirely within your browser.  Entries made are stored in your browser's local
storage and not synchronised to a server; it is therefore important to ensure you export a backup from time to time to
avoid data loss.
Use your browser's "Install app" or "Add to home screen" buttons to make this app available to use offline (helpful
in poor signal areas).

## Backup & Sharing
As your browser can clear site data, you could lose your records - even if you install it as an app!  Unless you have
your privacy settings configured to do this as a matter of course (I do), or you're running low on space, it is
unlikely to be cleared; so I'd probably suggest backing up after each campaign.  Likewise, if you want to share your
records with another person, you can create a backup export it and send it as a file via email or whatever hip and
trendy way you like to share files (maybe burn it to CD).

You can do this from the maintenance screen, download your data as a JSON file and keep it somewhere safe!  You can
also import a previously exported file through this screen - be warned: this will wipe out any exisitng data you have.

## Sync service
Your database can be configured to sync with a HTTP service, for sharing.  If configured in the app, it will
automatically export and POST the export to the provided URL along with the database's unique id (enabling multiple
saves being shared).  On startup, the app will the try to retrieve any changes and import from the same URL.

Concurrency is done simply on last updated time. The assumption is, two people won't be playing the same game at the
same time.  But this does mean, only one person/device should be altering the db during a game session.

The project does not provide an official sync-server for use, but there is a C# and PHP sample implementation in this
repo which you can self-host on your home-NAS or some other secure location.

# Building and hosting
You need either Visual Studio (or equivalent IDE) or the .NET SDK.  Publish the Ox.SleepingGods project to a file
location of your choice.  This will build a self-contained WSAM website that can be hosted on any generic web server
(.net etc does not need to be installed).  Configure your web-server as you would normally to serve the file
wwwroot/index.html as the default file.

Review [WASM hosting documentation](https://learn.microsoft.com/en-us/aspnet/core/blazor/host-and-deploy/webassembly/?view=aspnetcore-10.0&tabs=windows)
for specifics for configuring your web server.

## Sync server
See [API project documentation](Ox.SleepingGods.API/README.md) for .NET build.  PHP version drop the index.php file
into a relevant web directory.

# Contributing
See [CONTRIBUTING.md](CONTRIBUTING.md).

# Copyright and licences
Copyright © 2026. Alastair Grant.
Licensed under the [MIT](LICENSE.txt) licence.

Sleeping Gods is a trademark of Red Raven Games.  All game assets are copyright of Red Raven Games and the game 
authors.