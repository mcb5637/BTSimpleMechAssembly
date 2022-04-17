del ".\BTSimpleMechAssembly.zip"
mkdir .\tmp\BTSimpleMechAssembly
copy .\BTSimpleMechAssembly\bin\Release\BTSimpleMechAssembly.dll .\tmp\BTSimpleMechAssembly
copy .\BTSimpleMechAssembly\mod.json .\tmp\BTSimpleMechAssembly
copy .\BTSimpleMechAssembly\modBTX.json .\tmp\BTSimpleMechAssembly
cd .\tmp
"C:\Program Files\7-Zip\7z.exe" a "..\BTSimpleMechAssembly.zip" "BTSimpleMechAssembly\" "..\LICENSE" "..\README.md"
cd ..\
rmdir /s /q .\tmp
pause