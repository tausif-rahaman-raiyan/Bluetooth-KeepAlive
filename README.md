# Bluetooth KeepAlive

[![Build KeepAlive for Windows](https://github.com/tausif-rahaman-raiyan/Bluetooth-KeepAlive/actions/workflows/build.yml/badge.svg)](https://github.com/tausif-rahaman-raiyan/Bluetooth-KeepAlive/actions/workflows/build.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![Platform](https://img.shields.io/badge/Platform-Windows%2010%20%2F%2011%20(x64)-0078d4.svg)]()
[![Framework](https://img.shields.io/badge/.NET-8.0-512bd4.svg)]()

**Bluetooth KeepAlive** is a lightweight, 100% native, and completely offline Windows system-tray utility (C#, .NET 8, WinForms, NAudio) designed to keep Bluetooth speakers (HOCO, JBL, Sony, Anker, Realtek, Jerry, JL chips) and wireless audio devices connected during long periods of silence by maintaining an active, continuous digital silent audio stream through the selected Windows playback device.

---

## Table of Contents
1. [Overview & How It Works](#overview--how-it-works)
2. [100% Native & Offline Architecture](#100-native--offline-architecture)
3. [Features](#features)
4. [Anti-Sleep Dither Mode](#anti-sleep-dither-mode)
5. [Download & Deliverables](#download--deliverables)
6. [Installation & Setup](#installation--setup)
7. [How to Use](#how-to-use)
8. [Windows Startup Configuration](#windows-startup-configuration)
9. [Known Limitations & Disclaimers](#known-limitations--disclaimers)
10. [Local Build from Source](#local-build-from-source)
11. [License](#license)

---

## Overview & How It Works

Many Bluetooth speakers incorporate aggressive power-saving firmware. When Windows stops playing audio for several seconds or minutes, the Bluetooth audio sink goes idle. The speaker or adapter may turn off its internal amplifier, suspend the Bluetooth link, or enter a deep sleep timeout, requiring reconnecting or power cycling.

**Bluetooth KeepAlive** solves this by supplying a continuous stream of digital silence (48,000 Hz, 16-bit stereo) directly to the audio output endpoint via NAudio:
- Zero audible noise, music, or volume changes.
- Uses low-latency audio callbacks with static buffer clearing, consuming near **0% CPU** and minimal memory.
- Keeps the Windows audio subsystem and Bluetooth A2DP link actively engaged.

---

## 100% Native & Offline Architecture

- Built strictly with C#, .NET 8, and WinForms using custom GDI+ anti-aliased controls.
- **100% Offline**: Requires zero Internet connectivity and uses no browser runtimes (no WebView2, Electron, or CEF).
- Single self-contained Windows x64 binary + Inno Setup installer.

---

## Features

- **Modern Dark Design**: Built with GDI+ rounded cards, pill badges, and smooth waveform visualizer matching high-precision dark design tokens.
- **Native WinRT DeviceWatcher**: Event-driven Bluetooth connection monitoring using Windows WinRT APIs for zero UI freeze and instant auto-reconnect.
- **Global Hotkey Toggle**: Toggle KeepAlive audio stream from anywhere in Windows using `Ctrl + Alt + K`.
- **Bilingual UI Support**: Switch seamlessly between **English 🇬🇧** and **Bangla 🇧🇩** in Settings.
- **Low Battery Alert**: System tray balloon warnings when speaker battery drops below 20%.
- **Active Hours Schedule**: Restrict KeepAlive active hours (e.g. 8:00 PM to 12:00 AM).
- **Diagnostics Log Viewer**: Live "Why did my speaker disconnect?" log viewer with one-click report export.
- **Per-Monitor High-DPI Support**: Crisp, sharp text and layout across 100%, 125%, 150%, and 200% display scaling.
- **System Tray Native**: Runs in the Windows taskbar notification area; double-click or right-click the tray icon to control stream playback.
- **Auto Re-Pair Watchdog**: Continuously monitors paired Bluetooth devices and triggers Windows PnP re-enabling if connection drops.
- **WhatsApp & Call Protection**: Automatically releases the audio handle during active voice or video calls so Bluetooth headsets can switch to Hands-Free mode.
- **Anti-Sleep Dither**: Optional sub-audible 1-LSB dither noise for speakers with strict hardware amplitude thresholds.

---

## Anti-Sleep Dither Mode

Some Bluetooth speakers feature hardware DSPs that measure audio sample amplitude directly in silicon. If absolute zero PCM (`0x00`) is received for 10-15 minutes, the hardware DSP assumes silence and powers off.

**Bluetooth KeepAlive** includes an optional **Anti-sleep dither** setting (off by default):
- Injects an inaudible 1-LSB signal (`+1` / `-1` out of 32,768, -90.3 dB) roughly once every 480 samples (~100 Hz at 48 kHz).
- Keeps amplitude-sensing hardware DSPs awake without producing audible sound.
- *Note:* Enabling anti-sleep dither keeps the Bluetooth power amplifier continuously engaged, which may result in a small extra battery consumption on portable speakers compared to pure zero silence.

---

## Download & Deliverables

1. **`Keep-Alive-Setup-1.0.5.exe`**: Clean Inno Setup installer with optional desktop shortcut and sign-in startup.
2. **`Keep-Alive-Portable-1.0.5.zip`**: Standalone portable release containing self-contained single-file binary.

---

## Installation & Setup

1. Download **`Keep-Alive-Setup-1.0.5.exe`**.
2. Run the installer (runs with standard user privileges; no administrator rights required).
3. Follow the wizard prompts to optionally add a desktop shortcut or enable launch on Windows sign-in.
4. Click **Finish** to launch Bluetooth KeepAlive.

---

## How to Use

1. Launch **Bluetooth KeepAlive**.
2. The app icon appears in your notification area (taskbar tray, next to the clock).
3. **Start Keep-Alive**: Right-click the tray icon and select **Start Keep-Alive** (or click the button in the main window).
4. **Stop Keep-Alive**: Right-click the tray icon and select **Stop Keep-Alive** when not needed.
5. **Open Dashboard**: Double-click the tray icon to view live telemetry and battery levels.

---

## Windows Startup Configuration

To have Bluetooth KeepAlive launch automatically when you sign in to Windows:
1. Open the Dashboard and click **Settings**.
2. Check **Start Bluetooth KeepAlive automatically when signing in to Windows**.

---

## Known Limitations & Disclaimers

- **Hardware Sleep Timers**: Hardware that shuts down based on physical timers rather than Bluetooth link state can be kept awake using the **Anti-sleep dither** option in Smart Controls.
- **Physical Range & Power**: Software cannot prevent physical disconnects caused by battery exhaustion, out-of-range distance, or Windows System Sleep / Modern Standby.

---

## Local Build from Source

Requirements:
- Windows 10/11 x64
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Inno Setup 6 (for compiling setup installer)

```powershell
# 1. Clone repository
git clone https://github.com/tausif-rahaman-raiyan/Bluetooth-KeepAlive.git
cd Bluetooth-KeepAlive

# 2. Run automated unit tests
dotnet test tests/HocoKeepAlive.Tests/HocoKeepAlive.Tests.csproj

# 3. Publish self-contained executable
dotnet publish HocoKeepAlive.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish

# 4. Build Inno Setup installer
& "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" installer.iss
```

---

## License

This project is licensed under the [MIT License](LICENSE).
