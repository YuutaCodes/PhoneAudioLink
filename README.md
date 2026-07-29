# PhoneAudioLink

Play audio from your phone through your PC over Bluetooth. Windows supports this
(A2DP sink) but only exposes it to apps, so this is a small tray app that turns it on.

## Requirements

- Windows 10 build 19041 or newer
- A Bluetooth adapter that supports A2DP sink
- Your phone already paired in Windows Settings

## Usage

1. Pair your phone in Windows Settings if you have not already.
2. Start PhoneAudioLink and pick your phone from the list.
3. Click Connect, then play something on the phone.

Closing the window hides it to the tray. Use Exit in the tray menu to quit.

If no audio is present, close the connection and connect again.

## Building

```
dotnet publish -c Release -r win-x64 --self-contained -p:WindowsPackageType=None -p:WindowsAppSDKSelfContained=true
```

Output goes to `bin/Release/net8.0-windows10.0.19041.0/win-x64/publish/`.

## Notes

Trimming is off. The XAML loader resolves types by reflection, so a trimmed build
crashes on startup.
