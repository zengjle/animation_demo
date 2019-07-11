#!/bin/bash
target_path=BuildProject/android/app/src/main/assets
copy_path=AssetBundles/android/
rm -rf $target_path/assets
rm $target_path/Android
mkdir $target_path/assets
cp -r $copy_path/assets $target_path
find $target_path/assets -name '*.manifest' |xargs rm
cp $copy_path/Android $target_path