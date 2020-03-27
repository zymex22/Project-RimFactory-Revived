#!/bin/bash

echo "Cleaning output directory..."
rm -rf Output

mkdir Output

for dir in ./PRF_*/
do
    echo "Making Output/$(basename $dir).zip..."
    zip -r "Output/$(basename $dir).zip" $dir 
    echo "Done"
done
