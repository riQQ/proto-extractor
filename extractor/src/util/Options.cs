using CommandLine;
using CommandLine.Text;
using System.Collections.Generic;

namespace protoextractor.util
{
	class Options
	{
		/* Location for resolving assembly dependancies */
		[Option("libPath", Required = true,
				HelpText = "The path for resolving input file dependancies.")]
		public string LibraryPath
		{
			get;
			set;
		}

		/* Directory where proto files will be written to */
		[Option("outPath", Required = true,
				HelpText = "The path where all compiled proto files will be written to.")]
		public string OutDirectory
		{
			get;
			set;
		}

		/* Runs the proto3 compiler instead of proto2 */
		[Option("proto3", Required = false, Default = false,
				HelpText = "Set this to true to produce proto3 syntax in output files.")]
		public bool Proto3Syntax
		{
			get;
			set;
		}

		/* List of (absolute path) input filenames */
		[Value(0)]
		public IEnumerable<string> InputFileNames
		{
			get;
			set;
		}

		// Set this to false on release
		[Option("debug", Required = false, Default = false,
				HelpText = "This switch allows debug print statements to work.")]
		public bool DebugMode
		{
			get;
			set;
		}

		[Option("log", Required = false, Default = "",
				HelpText = "The path to the file where all output will be redirected to.")]
		public string LogFile
		{
			get;
			set;
		}

		[Option("resolve-circular-dependancies", Required = false, Default = false,
				HelpText = "This switch enables automatic resolving of circular dependancies.")]
		public bool ResolveCircDependancies
		{
			get;
			set;
		}

		[Option("manual-package-file", Required = false, Default = "",
				HelpText = "Path to the `stove-proto-packaging.ini` file.")]
		public string ManualPackagingFile
		{
			get;
			set;
		}

		[Option("automatic-packaging", Required = false, Default = false,
				HelpText = "This switch enables automatic packaging of similar namespaces.")]
		public bool AutomaticPackaging
		{
			get;
			set;
		}

		[Option("resolve-name-collisions", Required = false, Default = false,
				HelpText = "This switch enables resolving name collisions automatically.")]
		public bool ResolveCollisions
		{
			get;
			set;
		}

		[Option("package-structure", Required = false, Default = false,
				HelpText = "This switch enables generating a package-style folder structure for output files.")]
		public bool PackageStructure
		{
			get;
			set;
		}

		[Usage(ApplicationAlias = "extractor")]
		public static IEnumerable<Example> Examples
		{
			get
			{
				yield return new Example("Full usage", new Options()
				{
					AutomaticPackaging = true,
					LibraryPath = "$(GAME_INSTALL)/lib",
					ManualPackagingFile = "$(REPO)/hsproto-packaging.ini",
					OutDirectory = "$(TARGET)/protos",
					Proto3Syntax = true,
					ResolveCircDependancies = true,
					ResolveCollisions = true,
					PackageStructure = true
				});
			}
		}
	}
}
