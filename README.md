# Proto-extractor

A C# program to extract [Protocol Buffer definitions](https://developers.google.com/protocol-buffers/)
compiled with [Google Protos](https://github.com/google/protobuf)
or [SilentOrbit](https://silentorbit.com/protobuf/).

# Usage

Compile the program.
Give it the library files you want to decompile and if you want proto2 or proto3 syntax.

The program will do the following actions automatically:

* resolve circular dependancies;
* resolve name collisions;
* generates proto2 syntax output.

Basic usage example: 
```bash
proto-extractor --libPath "%HS_LOCATION%/Hearthstone_Data/Managed" 
--outPath "./proto-out" 
"%HS_LOCATION%/Hearthstone_Data/Managed/Assembly-CSharp.dll" 
"%HS_LOCATION%/Hearthstone_Data/Managed/Assembly-CSharp-firstpass.dll" 
```

Extended usage example:

> This command is used to generate protobuffer files for [HS proto repository](https://github.com/HearthSim/hsproto)

```bash
proto-extractor --libPath "%HS_LOCATION%/Hearthstone_Data/Managed" 
--outPath "./proto-out" --proto3 --automatic-packaging --manual-package-file "%REPO%/hscode-proto-packaging.ini"
"%HS_LOCATION%/Hearthstone_Data/Managed/Assembly-CSharp.dll" 
"%HS_LOCATION%/Hearthstone_Data/Managed/Assembly-CSharp-firstpass.dll" 
```

> The section `Options` explains all parameters. The executable will write parameter parsing errors on standard outstream.

## Options

### Proto3

The option `--proto3` will use the proto3 compiler to generate .proto files with protobuffer 3 syntax.

> Defaults to False.

### Resolve circular dependancies

The option `--resolve-circular-dependancies` will run a processor object that detects and solves circular dependancies for you. Both circular dependancies between types and namespaces are detected.

> Defaults to True.

### Resolve name collisions

The option `--resolve-name-collisions` will run a processor object that detects and solves all kinds of name collisions for you. See the processor code for more details.

> Defaults to True.

### Automatic packaging

The option `--automatic-packaging` will try to group namespaces under the same namespace if their names show similarities. The used algorithm is longest substring matching, with anchorpoint at the beginning of the string. Half-words are cut to the nearest namespace component.

> Defaults to False.

### Manual packaging

The option `--manual-package-file "PATH-TO-INI-FILE"` can be used manually move content of namespaces or specific types to other/new namespaces. See the file `hscode-proto-packaging.ini` in the root of the repo for examples. 

It's important to keep the order of processing algorithms in mind! We shouldn't try to manually compensate the behaviour of the automatic dependancy resolver. This has to do with trying to keep a consistent layout of outputted protobuffer files regarding the absolute dependancy on the source material.

> Defaults to "" (empty string) -> nothing will happen.

## Order of processing algorithms

The execution order of processing algorithms is always as follows:

1. Manual packaging of namespaces;
2. Resolve circular dependancies;
2. Automatic packaging of namespaces;
3. Resolving name collisions.

# Proto file extraction from binaries

At the root of the repository you'll find a python3 script that extracts protobuffer files from arbitrary binary files. 

>**This only works on proto files which have been compiled with the Google Protobuffer compiler tool (protoc)!** Use the proto-extractor project for hearthstone related proto files.

## Usage

The only requirement for this script is the [Google Protobuffer package](https://pypi.python.org/pypi/protobuf) which you can install through pip. A quick setup guide is given below.

1. Install virtualenv; `pip3 install virtualenv`
2. Create a virtual environment inside the CWD; `py -3 -m virtualenv ./.env`
3. Activate the environment; `./env/Scripts/activate`
4. Install protobuf package; `pip install protobuf`
5. Run the proto extraction script; `python protobin_to_proto.py -o [out-dir] [inputfile [inputfiles ..]]`
6. Extracted proto files can be found inside [out-dir], the script will output any found, valid filename.

# License

Proto-extractor is licensed under the terms of the MIT license.
The full license text is available in the `LICENSE` file.

# Community

proto-extractor is a [HearthSim](http://hearthsim.info) project. All development
happens on our IRC channel `#hearthsim` on [Freenode](https://freenode.net).

Contributions are welcome. Make sure to read through the `CONTRIBUTING.md` first.
