#!/bin/bash
# Copies the built mod into the RimWorld Mods folder.
# Runs automatically after `dotnet build` (disable with -p:Deploy=false); can also be run by hand.
# Target: $VT_DEPLOY_DIR, or the default Steam install on macOS.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
TARGET="${VT_DEPLOY_DIR:-$HOME/Library/Application Support/Steam/steamapps/common/RimWorld/Mods/ViolentTakeoff}"

# rsync --delete below mirrors into TARGET; never point it at anything but a ViolentTakeoff folder.
if [[ "$(basename "$TARGET")" != "ViolentTakeoff" ]]; then
	echo "deploy: refusing to deploy into '$TARGET' (folder must be named ViolentTakeoff)" >&2
	exit 1
fi
if [[ ! -d "$(dirname "$TARGET")" ]]; then
	echo "deploy: Mods folder not found: $(dirname "$TARGET"); skipping (set VT_DEPLOY_DIR)" >&2
	exit 0
fi

mkdir -p "$TARGET"
cd "$SCRIPT_DIR"
rsync -a --delete \
	--include='/About/***' \
	--include='/Defs/***' \
	--include='/Languages/***' \
	--include='/Textures/***' \
	--include='/LoadFolders.xml' \
	--include='/1.6/' --include='/1.6/Assemblies/' --include='/1.6/Assemblies/ViolentTakeoff.dll' \
	--include='/1.7/' --include='/1.7/Assemblies/' --include='/1.7/Assemblies/ViolentTakeoff.dll' \
	--exclude='*' \
	./ "$TARGET/"

echo "deploy: mod copied to $TARGET"
