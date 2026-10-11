# Building for iPhone without a Mac (free Apple ID)

1. **Your PC:** Unity exports the Xcode project.
2. **GitHub (free Mac runner):** compiles it into an **unsigned** `.ipa`.
3. **Your PC:** a sideloading tool signs the `.ipa` with a free Apple ID and installs it on your iPhone.

No Mac, no paid Apple Developer account and no Unity licence on GitHub are needed.

**Limits of a free Apple ID:** the app stops opening after **7 days** (sideload it again to refresh;
your save data is kept), and only **3** sideloaded apps can be installed at once.

---

## 1. Export and upload the Xcode project

1. In Unity: **Tools → Keep the Ball Up → Build iOS (Xcode project)**. It creates `Builds/iOS`.
2. In a terminal in the project folder:

   ```bash
   ./tools/upload-ios-build.sh --run
   ```

   This zips `Builds/iOS` (about 330 MB), uploads it to the `ios-xcode` release on GitHub
   (replacing the previous upload) and starts the build. Needs the GitHub CLI (`gh`) logged in.

   Without `--run`, start it yourself: **Actions → iOS Unsigned Build → Run workflow**.

## 2. Download the .ipa

The build takes roughly 15–30 minutes. Open the finished run in the **Actions** tab and download
**KeepTheBallUp-unsigned-ipa** at the bottom, then unzip it to get `KeepTheBallUp-unsigned.ipa`.
(Or from a terminal: `gh run download --name KeepTheBallUp-unsigned-ipa`.)

The repo is public, so GitHub's Mac runners are free. Note that the uploaded Xcode project is also public
on the release page, like the rest of the repo.

## 3. Sign and install on your iPhone

You can use any Apple ID. A spare one is fine; it doesn't have to be the one on your phone.

### On this Linux PC: Sideloader
1. Download Sideloader for Linux: https://github.com/Dadoum/Sideloader (latest build from its Actions or Releases page).
2. Make sure `usbmuxd` is installed and running (it lets Linux talk to the iPhone over USB).
3. Plug in the iPhone, unlock it and tap **Trust** when asked.
4. Open Sideloader, sign in with the Apple ID, choose `KeepTheBallUp-unsigned.ipa` and install.

If Sideloader gives you trouble, **PlumeImpactor** is an alternative that also runs on Linux.
On a Windows PC or Mac, **Sideloadly** or **AltStore** do the same job.

### On the iPhone (first time only)
1. **Settings → Privacy & Security → Developer Mode** → turn on (the phone restarts).
2. **Settings → General → VPN & Device Management** → tap your Apple ID → **Trust**.
3. Open Keep It Up.

Every 7 days, sideload the same `.ipa` again to keep it working.

---

## Troubleshooting

- **"No Xcode project found":** run the Unity menu item in step 1 first.
- **Xcode step fails with an Xcode version error:** the newest Xcode on GitHub's Mac may be too new
  for this Unity version. In `.github/workflows/ios-unsigned.yml`, change `xcode-version: latest-stable`
  to a version Unity supports, e.g. `xcode-version: "16.4"`.
- **The sideloading tool says the app ID is taken:** another app uses the bundle ID
  `com.faisalmustafa.keeptheballup` under that Apple ID; most tools change it automatically, or use another Apple ID.
