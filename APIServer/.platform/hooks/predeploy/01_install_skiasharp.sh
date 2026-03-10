##!/bin/bash

## Update package list
#sudo dnf update -y


## Install fontconfig (required dependency)
#sudo dnf install -y fontconfig

#sudo dnf install -y libicu libicu-devel
## Create necessary directories
#mkdir -p /var/task
#mkdir -p /tmp/libskiasharp

## Copy native libraries from NuGet package location
#NUGET_LIBRARY_PATH="/var/app/current/runtimes/linux-x64/native"
#if [ -d "$NUGET_LIBRARY_PATH" ]; then
#    sudo cp $NUGET_LIBRARY_PATH/libSkiaSharp.so /usr/lib64/
#    sudo cp $NUGET_LIBRARY_PATH/libSkiaSharp.so /var/task/
#fi

## Set up library paths
#echo "/var/task" >> /etc/ld.so.conf.d/libskiasharp.conf
#echo "/tmp" >> /etc/ld.so.conf.d/libskiasharp.conf
#echo "/lib64" >> /etc/ld.so.conf.d/libskiasharp.conf
#echo "/usr/lib64" >> /etc/ld.so.conf.d/libskiasharp.conf

## Update library cache
#ldconfig

## Set environment variable for SkiaSharp
#echo "export LD_LIBRARY_PATH=/var/app/current/runtimes/linux-x64/native:/var/task:/tmp:/lib64:/usr/lib64:$LD_LIBRARY_PATH" >> /etc/profile.d/libskiasharp.sh

## Verify library installation
#if [ -f /usr/lib64/libSkiaSharp.so ]; then
#    echo "libSkiaSharp.so found in /usr/lib86"
#    sudo chmod 755 /usr/lib64/libSkiaSharp.so
#    #sudo chmod 777 /usr/lib64/libSkiaSharp.so.88.1.0
#    ldd /usr/lib64/libSkiaSharp.so
#    ldconfig
#else
#    echo "libSkiaSharp.so not found in /usr/lib64"
#fi

#echo "Current user: $(whoami)"
#echo "Library permissions:"


