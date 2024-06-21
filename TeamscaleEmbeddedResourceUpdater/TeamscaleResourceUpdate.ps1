param(
    [string]$path,
    [string]$revision,
    [string]$timestamp,
    [string]$project
)

# Validate input parameters
if ((-not $path) -or ((-not $revision) -and (-not $timestamp)) -or ($revision -and $timestamp)) {
    Write-Host "Usage: TeamscaleResourceUpdate.ps1 -path <Path> (-revision <Revision> | -timestamp <Timestamp>) [-project <Project>]"
    Exit 1
}

# Function to update .resx file
function Update-ResxFile {
    param(
        [string]$resxPath,
        [string]$key,
        [string]$value,
        [string]$project
    )

    # Read existing .resx content
    [xml]$resxXml = Get-Content $resxPath

    $rootNode = $resxXml.SelectSingleNode("/root")
    # Find or create the specified key-value pair
    $dataNode = $resxXml.SelectSingleNode("/root/data[@name='$key']")
    if ($dataNode -eq $null) {
        $dataNode = $resxXml.CreateElement("data")
        $dataNode.SetAttribute("name", $key)
        $rootNode.AppendChild($dataNode)
    }

    # Delete existing "revision" or "timestamp" node if opposite is provided
    $oppositeKey = if ($key -eq 'Revision') { 'Timestamp' } else { 'Revision' }
    $oppositeNode = $resxXml.SelectSingleNode("/root/data[@name='$oppositeKey']")
    if ($oppositeNode -ne $null) {
        $rootNode.RemoveChild($oppositeNode)
    }

    # Update the value
    $dataNode.InnerXml = "<value>$value</value>"

    # Add or update the 'Project' property
    if ($project) {

        $projectNode =  $resxXml.SelectSingleNode("/root/data[@name='Project']")
        if ($projectNode -eq $null) {
            $projectNode = $resxXml.CreateElement("data")
            $projectNode.SetAttribute("name", 'Project')
            $rootNode.AppendChild($projectNode)
        }
        $projectNode.InnerXml = "<value>$project</value>"
    }

    # Write updated content back to .resx file
    $resxXml.Save($resxPath)
}

# Determine which parameter was provided and update the Teamscale.resx file accordingly
if ($revision) {
   $key = "Revision"
   $value = $revision
} elseif ($timestamp) {
   $key = "Timestamp"
   $value = $timestamp
}

# Update the .resx file
Update-ResxFile -resxPath $path -key $key -value $value -project $project
if (-not $project){
    Write-Host "Updated Teamscale Resource with $($key): $($value)"
} else {
    Write-Host "Updated Teamscale Resource with $($key): $($value) and $($project)"
}

