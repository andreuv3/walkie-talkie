#/bin/bash
dotnet build --nologo -v q -c Release --property WarningLevel=0 /clp:ErrorsOnly -o dist/ >> /dev/null
./dist/WalkieTalkie