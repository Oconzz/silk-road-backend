##!/bin/bash


## Function to check if Chrome is installed
#check_chrome_installed() {
#    if rpm -q google-chrome-stable &> /dev/null; then
#        return 0  # Chrome is installed
#    else
#        return 1  # Chrome is not installed
#    fi
#}

## Main script
#if check_chrome_installed; then
#    echo "Google Chrome is already installed:"
#    google-chrome --version
#else
#    echo "Chrome not found. Installing Google Chrome..."
## Update package list
#    cd /tmp
#    wget https://dl.google.com/linux/direct/google-chrome-stable_current_x86_64.rpm
#    sudo dnf install -y ./google-chrome-stable_current_x86_64.rpm
#    sudo ln -s /usr/bin/google-chrome-stable /usr/bin/chromium

#    # Verify the installation
#    if check_chrome_installed; then
#        echo "Chrome installation successful:"
#        google-chrome --version
#    else
#        echo "Chrome installation failed. Please check the error messages above."
#        exit 1
#    fi
#fi


