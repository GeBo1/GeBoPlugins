
$topdir = $PSScriptRoot
if ($topdir -eq "") {
    $topdir = "src"
}


function Zip-Plugin {
    param([string]$plugin_dir)
    $plugin = (Get-Item $plugin_dir).Basename
    $plugin_bin_dir = $plugin_dir + "\bin\Release"
    $dll_name = $plugin + ".dll"
    #$readme_src = $plugin_dir + "\..\README.md";
    #$readme_name = "README." + $plugin + ".md";


    
    $dlls = (Get-ChildItem -Path ($plugin_bin_dir) -Filter $dll_name -Recurse -Force)
    if (!$dlls) {
        # echo "$plugin_dir : no dlls"
        return
    }
    $dll = $dlls[0];

    $version = $dll.VersionInfo.FileVersion.ToString() 

    if (!$version) {
        # echo "$plugin_dir : no version"
        return
    }

    
    $workdir = $topdir + "\work"
    $destdir = $workdir + "\BepInEx\plugins\GeBo_Plugins"

    $zipfile = $topdir + "\dist\" + $plugin + " v" + $version + ".zip"

    if (Test-Path $zipfile) {
        # echo "$plugin_dir : $zipfile exists"
        return
    }

    if (Test-Path $workdir) {
        Remove-Item -Force -Path $workdir -Recurse
    }

    $dummy = New-Item -ItemType Directory -Force -Path $destdir 

    $plugin_files = Get-ChildItem -Path ($plugin_bin_dir) -Filter "*.dll" -Depth 1 -Recurse -File
    foreach ($pf in $plugin_files) {
        Copy-Item -Path $pf.FullName -Destination $destdir -Recurse -Force
    }

    #if (Test-Path $readme_src) {
    #    $readme = Get-Item -Path $readme_src
    #    Copy-Item -Path $readme.FullName -Destination ($destdir + "\" + $readme_name)
    #}
        

    $dummy = New-Item -ItemType Directory -Force -Path ($topdir + "\dist")

    pushd $workdir
    Compress-Archive -Path "BepInEx" -Force -CompressionLevel "Optimal" -DestinationPath $zipfile
    popd

    echo $zipfile
    
    Remove-Item -Force -Path $workdir -Recurse
}

$plugins = Get-ChildItem -Path ($topdir) -Depth 2 -Name
foreach ($plugin_dir in $plugins) {
    $plugin_bin_dir = $plugin_dir + "\bin\Release"

    if (Test-Path $plugin_bin_dir) {
       
        Zip-Plugin $plugin_dir $plugin
    }
}


