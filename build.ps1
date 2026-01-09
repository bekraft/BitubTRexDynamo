$date=Get-Date; 
$build=[int][Math]::Floor((([int]$date.Hour) * 3600 + ([int]$date.Minute) * 60 + ([int]$date.Second))/2); 
dotnet msbuild BitubTRexDynamo.sln /p:configuration="Dev" /p:platform="x64" /p:Build="$build" /t:"clean;build"