# Environment for the Appium scripts, set for the current process only.
# The Android SDK and the JDK are the ones the Visual Studio MAUI workload installed; neither variable is set system-wide.

# A shell opened before Node was installed does not have it on PATH yet.
$env:Path = [Environment]::GetEnvironmentVariable('Path', 'Machine') + ';' + [Environment]::GetEnvironmentVariable('Path', 'User')

$env:ANDROID_HOME = 'C:\Program Files (x86)\Android\android-sdk'
$env:ANDROID_SDK_ROOT = $env:ANDROID_HOME
$env:JAVA_HOME = 'C:\Program Files (x86)\Android\openjdk\jdk-17.0.14'
