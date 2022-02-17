rm -rf dist
publish="publish --sc \
        -p:PublishSingleFile=true \
        -p:DebugType=embedded \
        -p:PublishReadyToRunComposite=true \
        -p:PublishTrimmed=true"
echo $publish

dotnet $publish -r osx-x64 -o dist/macos-x64
mv dist/macos-x64/chktls dist/chktls-macos-x64
rm -rf dist/macos-x64

dotnet $publish -r win-x64 -o dist/win-x64
mv dist/win-x64/chktls.exe dist/chktls-win-x64.exe
rm -rf dist/win-x64

dotnet $publish -r linux-x64 -o dist/linux-x64
mv dist/linux-x64/chktls dist/chktls-linux-x64
rm -rf dist/linux-x64
