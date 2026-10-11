#!/usr/bin/env bash
# Zips the Xcode project made by Tools > Keep the Ball Up > Build iOS (Xcode project) and uploads it
# to the "ios-xcode" GitHub release, where the "iOS Unsigned Build" workflow turns it into an .ipa.
# Needs the GitHub CLI (gh), logged in. Add --run to start the workflow straight after uploading.
set -euo pipefail
cd "$(dirname "$0")/.."

TAG="ios-xcode"
ZIP="Builds/KeepTheBallUp-iOS-Xcode.zip"

if [ ! -d "Builds/iOS/Unity-iPhone.xcodeproj" ]; then
    echo "No Xcode project found. In Unity, run Tools > Keep the Ball Up > Build iOS (Xcode project) first."
    exit 1
fi

echo "Zipping Builds/iOS..."
rm -f "$ZIP"
# -y keeps symlinks; the DoNotShip folder is debug info Unity says not to include
(cd Builds && zip -qry "$(basename "$ZIP")" iOS -x "iOS/*DoNotShip*")

if ! gh release view "$TAG" >/dev/null 2>&1; then
    gh release create "$TAG" --prerelease --title "iOS Xcode project (CI input)" \
        --notes "Input for the iOS Unsigned Build workflow. Replaced on every upload by tools/upload-ios-build.sh."
fi

echo "Uploading $(du -h "$ZIP" | cut -f1)..."
gh release upload "$TAG" "$ZIP" --clobber

if [ "${1:-}" = "--run" ]; then
    gh workflow run ios-unsigned.yml -f release_tag="$TAG"
    echo "Build started: see the Actions tab, or run 'gh run watch'."
else
    echo "Uploaded. Start the build from Actions > iOS Unsigned Build > Run workflow (or re-run this with --run)."
fi
