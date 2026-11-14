# Starts the Appium server the device tests connect to, on the default port 4723.
# adb_shell lets tests read dumpsys output (alarms, notification channels) through mobile: shell.
# chromedriver_autodownload fetches the Chromedriver that matches the emulator's WebView, which the WEBVIEW context needs.

$ErrorActionPreference = 'Stop'

. "$PSScriptRoot\appium-env.ps1"

appium server --address 127.0.0.1 --port 4723 --allow-insecure 'uiautomator2:adb_shell,uiautomator2:chromedriver_autodownload'
