#!/bin/bash
project=BuildProject/android/app
keystore=$(ls $project/*.keystore)
version=$(ls $ANDROID_HOME/build-tools|grep '^\d'|sort -nr|head -1)
aliasname=$1
password=$2
apkname=$(ls $project/build/outputs/apk/release/*unsigned.apk)
signedname=$(echo $apkname | sed 's/-unsigned//g')
jarsigner -digestalg SHA1 -sigalg MD5withRSA -keystore $keystore -storepass $password -keypass $password -signedjar $signedname-unzipalign $apkname $aliasname
rm $apkname
$ANDROID_HOME/build-tools/$version/zipalign -v 4 $signedname-unzipalign $signedname
rm $signedname-unzipalign