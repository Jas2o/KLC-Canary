# KLC-Canary 
Canary is a forked version of KLC-Finch which is an alternative frontend to Kaseya Live Connect (which is required to be installed to use it with VSA) written in C#.

It was functional up to VSA 9.5.20 however will not receive any further VSA testing/development.

## Difference from KLC-Finch
A colleague had asked if Finch could combine remote control sessions into a single window like mRemoteNG or MTPuTTy. A code restructure was required to implement the "Charm" (term for a group of finches) interface, but it never received enough development to be considered stable enough for KLC-Finch.

I did find the Charm interface to be really neat in niche situations like onsite IT support where you are constantly moving around a building with your laptop but must also remote control a bunch of devices you're setting up that are sitting in the IT office.

Canary can be used with KLC-Lanner to simulate what Finch was like to use, without needing VSA access or Live Connect to be installed.

## Usage
Typically KLC-Canary is launched by KLC-Proxy rather than directly - Proxy has settings to "Use Canary" and "Use Charm" to adjust its redirect behaviour.

However when used with KLC-Lanner, Canary would be launched manually.

![Screenshot of KLC-Canary](/Resources/KLC-Canary-Charm.png?raw=true)

## Required other repos to build (all the same as KLC-Finch)
- LibKaseya
- LibKaseyaAuth
- LibKaseyaLiveConnect
- VP8.NET (modified)

## Required packages to build (all the same as KLC-Finch)
- CredentialManagement.Standard
- Fleck
- Newtonsoft.Json
- nucs.JsonSettings
- Ookii.Dialogs.Wpf
- OpenTK, GLControl and GLWpfControl (GLControl while not WPF was better for hardware and workflows I was using in the past)
- RestSharp
- VtNetCore (this is used for the CMD/PowerShell/Mac Terminal interfaces)
