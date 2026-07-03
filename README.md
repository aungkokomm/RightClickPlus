# RightClick+

A lightweight, portable Windows utility that adds four genuinely useful actions to your right-click menu, and lets you switch each one on or off from a simple control panel. No bloat, no background service, no admin rights.

<img width="1774" height="887" alt="RightClick+ control panel" src="https://github.com/user-attachments/assets/90ad6635-a132-4e0c-8420-aeaa8b76b3de" />

## Why?

Windows' right-click menu is usually two things at once: cluttered with entries you never use, and missing the one small tool you actually need. RightClick+ takes the opposite approach: a handful of focused, everyday file actions you can turn on individually, and that stay completely out of your way otherwise.

- **Portable, no admin.** Every menu entry is written under your own user account (`HKCU`). Nothing touches the system, no UAC prompt.
- **Clean by design.** Turning a feature off (or uninstalling the app) removes its menu entry completely.
- **No lag.** Clicks run a tiny helper executable, so there's no app to launch on every right-click.

---

## The four features

### 📁 File to Folder
**The problem.** You end up with a pile of loose files (downloads, exports, scans, renders) and you want each one tucked into its own folder. By hand that means: create a folder, rename it to match the file, drag the file in, and repeat twenty times.

**The fix.** Select any files, right-click, **File to Folder**. Each file is moved into a new folder named after it. It works on a multi-selection and handles the awkward cases: it merges into a folder that already exists, and steps around a file that already owns the name (`report.exe` becomes `report/` even if `report.exe` is nearby). Extensionless files like `LICENSE` nest cleanly too.

### 📝 File Note
**The problem.** A file makes perfect sense the day you save it. A week later you are asking: where did I download this from? Why did I keep it? Which version is it, and was there anything special about it? With a name like `setup.exe`, good luck remembering.

**The fix.** Right-click a file, **File Note**, and jot down whatever future-you will want to know: the download page, the version, install notes, or just a reminder to yourself.

> Downloaded from the vendor's site · Version 4.8.2 · Portable build · Requires .NET 8 · Windows 11 only

The note is saved right next to the file (`.txt` or `.md`, your choice), so the information travels with the file wherever it goes. RightClick+ opens it in a small editor built specifically for File Notes, deliberately lightweight so it never competes with OneNote, Obsidian, or Notepad. It exists to answer one question: *"Why did I keep this file?"*

<img width="1015" height="599" alt="Built-in File Note editor" src="https://github.com/user-attachments/assets/c73929da-9905-4209-82a8-501f62051a2c" />

### 📋 Copy File Names
**The problem.** You need a file's name or full path to paste into a script, a terminal, an email, or a config, sometimes with the extension, sometimes without, sometimes quoted because it has spaces. Windows makes you rename-to-copy or retype it.

**The fix.** Right-click, **Copy File Names**, then pick exactly the form you need:

- **Copy file name** → `report.pdf`
- **Copy name without extension** → `report`
- **Copy full path** → `C:\Work\report.pdf`
- **Copy full path (quoted)** → `"C:\Work\report.pdf"`

Select several files and they are copied together, one per line or comma-separated, whichever you set.

### 🎨 Folder Color
**The problem.** Every folder is the same yellow. The three folders that actually matter are lost among the fifty that don't.

**The fix.** Right-click a folder, **Folder Color**, and choose from **11 colors**. The folder icon is recolored so the ones that matter stand out at a glance, with **Reset color** to put it back. It is per-folder and self-contained (a hidden `desktop.ini` plus an icon inside the folder), so the color travels with the folder, even to another PC.

---

## The control panel

RightClick+'s window is just a switchboard:

- Turn each of the four features **On / Off**. Each toggle adds or removes its right-click entry.
- Switch Windows between the **Classic** and **Modern** right-click menu (Windows 11).
- Set your preferences: note format (`.txt` / `.md`), copy separator, and light/dark theme.

The menu only ever shows the actions; all configuration lives here, so your right-click stays clean.

---

## Philosophy

RightClick+ follows one simple principle:

> **If an action saves time every day, it belongs in the right-click menu.**

Every feature exists because it removes unnecessary steps from a common file-management task.

---

## Install

1. Download the latest `RightClickPlus-Setup-x.y.z.exe` from the [Releases](../../releases) page.
2. Run it. It is a **per-user** install, so there is no admin/UAC prompt.
3. Open RightClick+ and switch on the features you want.

> **Windows 11 tip:** RightClick+'s entries appear on the top-level right-click menu when the **Classic** menu is enabled (there is a one-click switch inside the app). On the modern menu, they live under **Show more options**.

Portable and self-contained, no separate .NET runtime install needed.

## Uninstall

Uninstall from Windows **Settings → Apps**, or simply toggle features off; either removes RightClick+'s menu entries cleanly. Folders you have colored keep their color; choose **Reset color** on a folder first if you would like it plain again.

---

## Build from source

**Requirements:** Windows 10/11 and the .NET 10 SDK.

```powershell
# build, publish (portable, self-contained), and package the installer
.\build.ps1

# quick compile check only
.\build.ps1 -SkipInstaller
```

**Solution layout**

| Project | Role |
|---|---|
| `RightClick.App` | WinUI 3 control panel: UI, MVVM, dependency injection |
| `RightClick.Cli` | Tiny helper exe the right-click entries invoke (the action runner) |
| `RightClick.Core` | Models and service interfaces (no UI, no Windows-specific I/O) |
| `RightClick.Infrastructure` | Registry and filesystem implementations |

The installer is built with [Inno Setup](https://jrsoftware.org/isinfo.php) from `installer/RightClickPlus.iss`.

---

## License

Released under the [MIT License](LICENSE).
