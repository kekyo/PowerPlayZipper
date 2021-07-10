#!/bin/sh

# PowerPlayZipper - An implementation of Lightning-Fast Zip file
# compression/decompression library on .NET.
# Copyright (c) 2021 Kouji Matsui (@kozy_kekyo, @kekyo2)
# 
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at
# 
# http://www.apache.org/licenses/LICENSE-2.0
# 
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.

echo ""
echo "==========================================================="
echo "Build PowerPlayZipper"
echo ""

# git clean -xfd

dotnet restore
dotnet build -c Release -p:Platform="Any CPU" PowerPlayZipper.sln
dotnet publish -c Release -p:TargetFramework=net5.0 ppzip/ppzip.csproj
dotnet pack -p:Configuration=Release -p:Platform=AnyCPU -o artifacts PowerPlayZipper/PowerPlayZipper.csproj
