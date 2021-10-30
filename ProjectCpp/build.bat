@echo off
setlocal
call "C:\Program Files (x86)\Microsoft Visual Studio\2022\Preview\VC\Auxiliary\Build\vcvarsall.bat" x64

set RootDirectory=%~dp0%
pushd %RootDirectory%

if NOT EXIST build mkdir build
pushd build


set IncludeDirectories= ^
  /I %RootDirectory%dependencies\or-tools\include\

set LinkDirectories= ^
  /LIBPATH:%RootDirectory%dependencies\or-tools\lib\

set PreprocessorFlags= ^
  /D WIN32_LEAN_AND_MEAN ^
  /D _CRT_SECURE_NO_WARNINGS

set CompilerFlags=/std:c++17 /MD /EHsc /Od /nologo /Zo /Z7 /FC /MP%NUMBER_OF_PROCESSORS%

set SourceFiles= ^
  %RootDirectory%src\main.cpp

cl %SourceFiles% %CompilerFlags% %PreprocessorFlags% %IncludeDirectories% ^
/link %LinkDirectories% ortools.lib /INCREMENTAL:no /OPT:ref /OUT:Project.exe

popd
popd