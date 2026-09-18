function Get-NextVersion {
    git fetch --tags --force | Out-Null

    $lastTag = git tag --list "v*" --sort=-v:refname | Select-Object -First 1

    if (-not $lastTag) {
        return "01.00.00"
    }

    $clean = $lastTag.TrimStart("v")
    $parts = $clean.Split(".")

    if ($parts.Count -lt 3) {
        throw "Invalid tag format: $lastTag"
    }

    $major = [int]$parts[0]
    $minor = [int]$parts[1]
    $patch = [int]$parts[2]

    return "{0:D2}.{1:D2}.{2:D2}" -f $major, $minor, $patch
}
