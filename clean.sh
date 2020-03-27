#!/bin/bash

RW_INSTALL_PATH="$HOME/.local/share/Steam/steamapps/common/RimWorld"

for dir in ./PRF_*/
do
    echo -n "Cleaning $RW_INSTALL_PATH/Mods/$dir... "
    rm -rf "$RW_INSTALL_PATH/Mods/$dir"
    echo "Done"
done
