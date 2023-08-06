#!/usr/bin/env bash

set -e

./build.sh
_DIR=$PWD
# cd ~/win/Documents/Trackmania
# cd ~/OpExtract/
rm -v ./*_Chunk_*.Map.Gbx || true
$_DIR/bin/Debug/net6.0/linux-x64/publish/map-chunker "$@" || true

# cp -a -v ./*.Map.Gbx ~/Trackmania/Maps/Testing/Z_MAP_CHUNKER/
