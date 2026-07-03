# RightClick+

A lightweight, portable Windows utility that adds four genuinely useful actions to your right‑click menu — and lets you switch each one on or off from a simple control panel. No bloat, no background service, no admin rights.
<img width="1774" height="887" alt="RightClickPlus" src="https://github.com/user-attachments/assets/90ad6635-a132-4e0c-8420-aeaa8b76b3de" />

<!-- Screenshot: the RightClick+ control panel — save to docs/screenshots/app.png and uncomment:
![RightClick+ control panel](docs/screenshots/app.png)
-->

## Why?

Windows' right‑click menu is usually two things at once: cluttered with entries you never use, and missing the one small tool you actually need. RightClick+ takes the opposite approach — a handful of focused, everyday file actions you can turn on individually, and that stay completely out of your way otherwise.

- **Portable, no admin** — every menu entry is written under your own user account (`HKCU`). Nothing touches the system, no UAC prompt.
- **Clean by design** — turning a feature off (or uninstalling the app) removes its menu entry completely.
- **No lag** — clicks run a tiny helper executable, so there's no app to launch on every right‑click.

---

## The four features

### 📁 Move to Folder
**The problem.** You end up with a pile of loose files — downloads, exports, scans, renders — and you want each one tucked into its own folder. By hand that's: create a folder, rename it to match the file, drag the file in… and repeat twenty times.

**The fix.** Select any files → right‑click → **Move to folder**. Each file is moved into a new folder named after it. Works on a multi‑selection, and handles the awkward cases: it merges into a folder that already exists, and steps around a file that already owns the name (`report.exe` → `report/` even if `report.exe` is nearby). Extensionless files like `LICENSE` nest cleanly too.

<!-- Screenshot: docs/screenshots/move-to-folder.png -->

### 📝 File Note

Files often make perfect sense today.

A few days later, you find yourself asking:

Where did I download this from?
Why did I keep this?
Which version was this?
Was there anything special about it?

For example, after downloading:

setup.exe

you probably remember exactly what it is.

A week later...

Not so much.

With File Note, simply right-click the file and save whatever future-you will want to remember.

Maybe it's the product description.

Maybe it's the download page.

Maybe it's installation instructions.

Maybe it's your own reminder.

Downloaded from vendor website

Version 4.8.2

Portable build

Requires .NET 8

Works with Windows 11 only

Because the note lives next to the file, the information stays with it wherever you move it.

To avoid interfering with your preferred note-taking application, RightClick+ includes its own lightweight built-in note editor dedicated to File Notes.

It's designed for quick reminders—not replacing OneNote, Obsidian, or Notepad, but solving the simple question:

"Why did I keep this file?"
<img width="1015" height="599" alt="image" src="https://github.com/user-attachments/assets/c73929da-9905-4209-82a8-501f62051a2c" />

<!-- Screenshot: docs/screenshots/file-note.png -->

### 📋 Copy File Name(s)
**The problem.** You need a file's name or full path to paste into a script, a terminal, an email, or a config — sometimes with the extension, sometimes without, sometimes quoted because it has spaces. Windows makes you rename‑to‑copy or retype it.

**The fix.** Right‑click → **Copy file name(s)** → pick exactly the form you need:

- **Copy file name** — `report.pdf`
- **Copy name without extension** — `report`
- **Copy full path** — `C:\Work\report.pdf`
- **Copy full path (quoted)** — `"C:\Work\report.pdf"`

Select several files and they're copied together — one per line, or comma‑separated, whichever you set.

<!-- Screenshot: docs/screenshots/copy-file-names.png -->

### 🎨 Folder Color
**The problem.** Every folder is the same yellow. The three folders that actually matter are lost among the fifty that don't.

**The fix.** Right‑click a folder → **Folder color** → choose from **19 colors**. The folder icon is recolored so the ones that matter stand out at a glance, with **Reset color** to put it back. It's per‑folder and self‑contained (a hidden `desktop.ini` + icon inside the folder), so the color travels with the folder — even to another PC.

<!-- Screenshot: docs/screenshots/folder-color.png -->

---

## The control panel

RightClick+'s window is just a switchboard:

- Turn each of the four features **On / Off** — each toggle adds or removes its right‑click entry.
- Switch Windows between the **Classic** and **Modern** right‑click menu (Windows 11).
- Set your preferences: note format (`.txt` / `.md`), copy separator, and light/dark theme.

The menu only ever shows the actions; all configuration lives here, so your right‑click stays clean.

---
## Philosophy

RightClick+ follows one simple principle:

***If an action saves time every day, it belongs in the right-click menu.***

Every feature exists because it removes unnecessary steps from common file management tasks.
## Install

1. Download the latest `RightClickPlus-Setup-x.y.z.exe` from the [Releases](../../releases) page.
2. Run it — it's a **per‑user** install, so there's no admin/UAC prompt.
3. Open RightClick+ and switch on the features you want.

> **Windows 11 tip:** RightClick+'s entries appear on the top‑level right‑click menu when the **Classic** menu is enabled (there's a one‑click switch inside the app). On the modern menu, they live under **Show more options**.

Portable and self‑contained — no separate .NET runtime install needed.

## Uninstall

Uninstall from Windows **Settings → Apps**, or simply toggle features off — either removes RightClick+'s menu entries cleanly. Folders you've colored keep their color; choose **Reset color** on a folder first if you'd like it plain again.

---

## Build from source

**Requirements:** Windows 10/11 and the .NET 10 SDK.

```powershell
# build, publish (portable, self‑contained), and package the installer
.\build.ps1

# quick compile check only
.\build.ps1 -SkipInstaller
```

**Solution layout**

| Project | Role |
|---|---|
| `RightClick.App` | WinUI 3 control‑panel — UI, MVVM, dependency injection |
| `RightClick.Cli` | Tiny helper exe the right‑click entries invoke (the action runner) |
| `RightClick.Core` | Models and service interfaces (no UI, no Windows‑specific I/O) |
| `RightClick.Infrastructure` | Registry + filesystem implementations |

The installer is built with [Inno Setup](https://jrsoftware.org/isinfo.php) from `installer/RightClickPlus.iss`.

---

## License

Released under the [MIT License](LICENSE).
