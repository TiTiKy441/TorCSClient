# TorCSClient
Windows torifier. Provides UI to work with tor and to use it with your system. Contains all necessary functionality to torify. Installs [WinpkFilter](https://github.com/wiresock/ndisapi) to your system to ensure that only tor traffic leaves the system in "Everything" mode. Additionally provides basic anti-circumvention functionality and application selection (similar to orbot) (uses [ProxiFyre](https://github.com/wiresock/proxifyre)). 

# Disclaimer
This application should not be treated as anonymity tool. 

## Platforms
Tested and developed on win10 64

## Platform
[.NET 7.0](https://dotnet.microsoft.com/download/dotnet/7.0)

## Used projects
Tor

[WinpkFilter, aka ndisapi](https://github.com/wiresock/ndisapi)

[ProxiFyre](https://github.com/wiresock/proxifyre)

[TorRelayScannerCS](https://github.com/TiTiKy441/TorRelayScannerCS)

## Manual

When the application starts, you get an icon in the notification bar

![notify icon](https://github.com/user-attachments/assets/e5c41375-b54c-400c-8b12-208c0d79ea02)

Exit - stops and exitst the application

Enable - button which switches on and off torification after connection

Connect - start connecting to tor

Disconnect - disconnect from tor

NoProcess - current status of tor

Tor control - opens/closes tor control panel

Settings - opens/closes torifier settings

## Tor control panel

![Tor control panel](https://github.com/user-attachments/assets/1a275f2a-df15-4d77-b8c1-c2f7c89850ce)

Panel for controlling tor proxy parameters.

Provides functionality for adding bridges (also supports adding bridges from a url or a local file, just write the file path or the URL in the bridges textbox and it will fetch them automatically). 

Filter - every N seconds application checks whenever the amount of bridges is sufficient (>= Running bridges count) and reloads tor only with those bridges.

## TorRelayScannerCS

When selecting bridge type, there is a Tor relay scanner option. By pressing connect with this bridge type, TorRelayScannerCS is launched. It scans for reachable tor relays to use them as bridges until the desired amount of bridges was found (Running bridges count)

## Settings

![image](https://github.com/user-attachments/assets/429fc1d5-b645-4cab-a5b7-1188a107a284)

Provides functionality for selecting what to torify.

### Everything

In this mode application forces the system to use Tor's DNS server and to use Tor as system proxy. Additionally it installs a firewall (a bunch or ndis static filters) into the system to block all outcoming and incoming traffic which doesnt go to or from one of the Tor's addresses in use.

### Selected apps 

In this mode application starts a [ProxiFyre](https://github.com/wiresock/proxifyre) process which proxifies all of the desired applications through tor. By uncheking the box next to the application icon, you can exclude a specified application from the proxification without removing them from the list.

## Additional info and setup 

If you got any issues or questions, I would gladly answer them in issues.

Additional configuration could be accessed from the file called ``configuration`` in the app's directory. It's pretty straightforward.
