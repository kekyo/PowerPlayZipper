@echo off

rem PowerPlayZipper - An implementation of Lightning-Fast Zip file
rem compression/decompression library on .NET.
rem Copyright (c) 2021 Kouji Matsui (@kozy_kekyo, @kekyo2)
rem 
rem Licensed under the Apache License, Version 2.0 (the "License");
rem you may not use this file except in compliance with the License.
rem You may obtain a copy of the License at
rem 
rem http://www.apache.org/licenses/LICENSE-2.0
rem 
rem Unless required by applicable law or agreed to in writing, software
rem distributed under the License is distributed on an "AS IS" BASIS,
rem WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
rem See the License for the specific language governing permissions and
rem limitations under the License.

echo.
echo "==========================================================="
echo "Build PowerPlayZipper"
echo.

rem git clean -xfd

dotnet restore
dotnet build -c Release -p:Platform="Any CPU" PowerPlayZipper.sln
dotnet publish -c Release -p:TargetFramework=net5.0 ppzip\ppzip.csproj
dotnet pack -p:Configuration=Release -p:Platform=AnyCPU -o artifacts PowerPlayZipper\PowerPlayZipper.csproj
