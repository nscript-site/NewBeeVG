$OutDir = "../../dist/publish-aot-nbvg"

dotnet publish -r win-x64 -c Release -o $OutDir

# remove any leftover .pdb files (recursively)
Get-ChildItem -Path $OutDir -Filter *.pdb -Recurse -ErrorAction SilentlyContinue |
    Remove-Item -Force -ErrorAction SilentlyContinue

# remove any leftover .lib files (recursively)
Get-ChildItem -Path $OutDir -Filter *.lib -Recurse -ErrorAction SilentlyContinue |
    Remove-Item -Force -ErrorAction SilentlyContinue
