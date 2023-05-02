# Bitub TRex Dynamo Plugin

![Build status](https://dev.azure.com/bitub/BitubTRexXbim/_apis/build/status/bekraft.BitubTRexDynamo?branchName=master&label=MASTER)
![Build status](https://dev.azure.com/bitub/BitubTRexXbim/_apis/build/status/bekraft.BitubTRexDynamo?branchName=dev&label=DEV)

TRex is collection of utility libraries of model transformation and deployment tasks in the AEC domain. 
Internally it's using i.e. [Xbim libraries](https://github.com/xBimTeam) to read, transform and write IFC model files.

Currently supports [Dynamo 2.12+](https://github.com/DynamoDS/Dynamo) and tested against 2.17.

## Installation of binary releases

TRex is collection of utility libraries of model transformation and deployment tasks in the AEC domain. 
Install a recent version of [Dynamo](https://dynamobuilds.com) and extract the TRex release to the 

```~/AppData/Roaming/Dynamo/Dynamo Core``` or ```~/AppData/Roaming/Dynamo/Dynamo Revit``` packages sub folder. Keep the entire
package rooted by a single parent folder (i.e. TRex).

## Building from development/master branch

The current implementation is under heavy development. It's not guranteed that the latest dev head will seemlessly
work and compile. If you like to build your own relase, check out the dev branch run restore packages in Visual Studio and
run a clean build.

In general, the master branch is the best option to run your own build.

### Dependencies

   - Xbim 5.1+ via nuget.org
   - Assimp 5.2+ (release binary embedded with headers, otherwise get from https://github.com/assimp/assimp/tree/master)

### Build configuration and impacts

Each build will generate a package under ```./DeployedPackages``` (see *DeployPath*).

   - *Debug* will run a debug build and copy plugin over to the predefined Dynamo solution directory.
   - *Dev* will run a debug build (without expecting an installation folder)
   - *Release* will run a release build with documentation extraction added (for Dynamo help)

Currently only x64 is supported for Assimp and Xbim. 
If you like to generate x86 native libraries feel free to modify the [build configuration](Directory.Build.props).

## Licenses in use

Bitub TRex Dynamo is licensed by Apache-2.0.

[Assimp Asset Importer](https://github.com/assimp/assimp) is licensed by Assimp License (3-clause BSD)

[Xbim Framework](https://github.com/xbimteam) is licensed by CDDL-1.0 license.

