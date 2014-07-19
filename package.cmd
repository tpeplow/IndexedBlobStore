set /p ver=<ver.txt
.\.nuget\NuGet.exe pack .\IndexedBlobStore.nuspec -version %ver%