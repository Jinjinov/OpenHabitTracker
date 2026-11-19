# The builds under test run on a copy of the working tree, never in the checkout itself.
# In a shared obj folder an incremental build kept the manifest of an earlier store build, so the APK carried
# net.openhabittracker whatever ApplicationId said, and a later store build could pick up this build's id the same way.
# The copy keeps its own bin and obj between runs, so repeated builds stay incremental.
# Each checkout gets its own copy: robocopy and rsync keep the files' timestamps, so one checkout's older files mirrored over another's
# look older than the build output and the incremental build skips them.

# Everything the device tests keep between runs lives under this folder, which the fixtures find the same way.
$DeviceTestsFolder = Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'OpenHabitTracker.DeviceTests'

# Mirrors the checkout, given as a full path, into its own work folder and returns the folder.
function Sync-WorkFolder([string]$SourceRoot) {
    $sourceHash = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($SourceRoot.ToLowerInvariant()))).Substring(0, 12)
    $workFolder = Join-Path $DeviceTestsFolder $sourceHash

    if ($IsWindows) {
        robocopy $SourceRoot $workFolder /MIR /XD bin obj .git .vs .idea AppPackages publish /XF *.keystore *.pfx *.user /NFL /NDL /NJH /NJS /NP | Out-Null
        if ($LASTEXITCODE -ge 8) { throw "robocopy could not mirror $SourceRoot to $workFolder" }
    }
    else {
        # Excluded names are neither copied nor deleted, so the copy's own bin and obj survive the mirror.
        New-Item -ItemType Directory -Force $workFolder | Out-Null
        rsync -a --delete --exclude bin --exclude obj --exclude .git --exclude .vs --exclude .idea --exclude AppPackages --exclude publish --exclude '*.keystore' --exclude '*.pfx' --exclude '*.user' "$SourceRoot/" "$workFolder/"
        if ($LASTEXITCODE -ne 0) { throw "rsync could not mirror $SourceRoot to $workFolder" }
    }

    return $workFolder
}
