# ImpliciX.Linker

Build and package an application for the Connect platform.

## Creating a device app from source code
```
Description:
  Create the executable project for a given application from source projects

Usage:
  ImpliciX.Linker build-from-source [options]

Options:
  -r, --runtime <runtime> (REQUIRED)          Full path to the ImpliciX.Runtime project
  -a, --application <application> (REQUIRED)  Full path to the application main project
  -e, --entry <entry> (REQUIRED)              Application entry point (name of a class deriving from RuntimeModel)
  -v, --version <version> (REQUIRED)          Application version
  -o, --output <output> (REQUIRED)            Output zip file
  -?, -h, --help                              Show help and usage information
```

## Creating a device app from feeds
```
Description:
  Create the executable project for a given application from Azure Devops feeds

Usage:
  ImpliciX.Linker build-from-feeds [options]

Options:
  -r, --runtime <runtime> (REQUIRED)          Reference to the runtime feed <name>[:<version>]
  -a, --application <application> (REQUIRED)  Reference to the application feed <name>[:<version>]
  -e, --entry <entry> (REQUIRED)              Application entry point (name of a class deriving from RuntimeModel)
  -v, --version <version> (REQUIRED)          Application version
  -o, --output <output> (REQUIRED)            Output zip file
  -?, -h, --help                              Show help and usage information
```

## (ONGOING) Creating an Harmony package
Create the executable project for a given application from Azure Devops feeds
```
Description:
  Create an Harmony package

Usage:
  ImpliciX.Linker pack [options]

Options:
  -n, --name <name> (REQUIRED)        Package name
  -v, --version <version> (REQUIRED)  Package version
  -p, --part <part> (REQUIRED)        Part reference <id>,<version>,<path>
  -o, --output <output> (REQUIRED)    Output zip file
  -?, -h, --help                      Show help and usage information
```

## (TODO) Use docker for dotnet calls
The linking process uses dotnet command calls.
By default, the locally installed dotnet environment is used.
Optionally, an existing docker container can be used for these calls.
```
Options:
  -d, --docker <docker>  Container name for dotnet operations
```

