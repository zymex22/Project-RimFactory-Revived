#!/bin/bash

RW_INSTALL_PATH="$HOME/.local/share/Steam/steamapps/common/RimWorld"

for dir in ./PRF_*/
do
    echo -n "Copying $dir to $RW_INSTALL_PATH/Mods/$dir... "
    # Make mod directory if it does not exist
    mkdir -p "$RW_INSTALL_PATH/Mods/$dir"
    # Copy contents to directory
    cp -r $dir/* "$RW_INSTALL_PATH/Mods/$dir"
    echo "Done"
done
