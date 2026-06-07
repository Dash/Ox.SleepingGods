# Ox.SleepingGods.API - .NET Sync service

This project provides a C# implementation of the sync API, optional for the Ox.SleepingGods application.  It has beenS
designed to be run natively on Linux with Systemd's a Socket Activated Service.  As it's unlikely to be used a lot,
the focus is on something lightweight that shuts down when not in use to save on resources.

## Building AOT
For optimum performance and no external dependencies on .NET etc, this project can be compiled "Ahead-of-Time" as a
traditional Linux application.  With the .NET 10 SDK installed, from this directory, run:

```bash
dotnet publish -p:PublishProfile=AOT
```

This will build the minimal required to run the service, with full http stack built in.  

## Running
Simply run the build result (./Ox.SleepingGods.API) for a http daemon to startup, by default on port 5000.

The .NET HTTP server is "kestrel" and has lots of configuration options available, which aren't documented here.
Though if you wish to rebind to another port, you can specify what to bind through: --urls http://[::1]:80 (for
instance).

## Configuration
The appsettings.json file enables configuration options to be persisted.  These top level JSON elements are part
of the app customisation:

| Name           | Description                                                                 |
| -------------- | --------------------------------------------------------------------------- |
| StoreDirectory | Writable path to where the app should store database sync files.            |
| MaxUploadBytes | Maximum permissible number of bytes that can be uploaded per database file. |
| IdleSeconds    | Seconds with no new requests before the program will exit.                  |

## Systemd service
The included example sleepinggodsapi.service and accompanying .socket file can be placed in your /etc/systemd/system
directory (ensuring ownership is set to root), and edited to fit your install.

### Hardening
The service file comes preconfigured for service hardening to restrict what the process can do.  This is a good 
practice piece to ensure that if somebody replaced the service with something nefarious, there would still be a degree
of protection against what could be done.

As part of this, DynamicUser is used, which will transparently create a temporary user for the service to run under,
this saves having to setup user accounts etc on the host.  If you need to store your files in a shared location you
may need to disable this and configure for a static user.

### Socket Activation
By only starting the sleepinggodsapi.socket socket, the service will not start until a TCP request is received on the
configured port.  This will transparently start the service on demand and hand off the connection.  Use the 
IdleSeconds configuration option for the service to have it shut back down again after a period of inactivity.

With a AOT build, a cold startup in this situation should be returning a result in under 100ms on a local network.

## Security
There is no security built in.  If you expose this service to the Internet, anybody with knowledge of it could upload
and download files.  It was intended to run on a home-NAS etc without public Internet access.

If you are using the official hosted version of the app, you will only be allowed to use HTTPS secured endpoints to
sync with.  You therefore may wish to run a reverse proxy (NGinx or Apache etc) to provide SSL and forward requests
to the listening systemd socket.