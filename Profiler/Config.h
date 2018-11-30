#pragma once
#include <string>
#include "ConfigParser.h"

/**
  * Manages config settings from both the environment and a config file.
  * Settings from the environment always win.
  */
class Config
{
public:
	Config(std::string configFilePath, std::string processName);

	/** The directory to which to write the trace file. */
	std::string targetDir() {
		return getOption("targetdir");
	}

	/** Whether to profile at all. */
	bool isEnabled() {
		return getOption("enabled") == "1";
	}

	/** Whether to use light mode or force rejitting of prejitted assemblies. */
	bool shouldUseLightMode() {
		return getOption("light_mode") == "1";
	}

	/** Whether to log the assembly file versions of all loaded assemblies. */
	bool shouldLogAssemblyFileVersion() {
		return getOption("assembly_file_version") == "1";
	}

	/** Whether to log the assembly file paths of all loaded assemblies. */
	bool shouldLogAssemblyPaths() {
		return getOption("assembly_paths") == "1";
	}

	/** Whether to log all environment variables in the trace file. */
	bool shouldDumpEnvironment() {
		return getOption("dump_environment") == "1";
	}

	/** Whether exceptions in the profiler code should be swallowed. */
	bool shouldIgnoreExceptions() {
		return getOption("ignore_exceptions") == "1";
	}

	/** Whether to eagerly log trace data. */
	int eagerness() {
		try {
			return std::stoi(getOption("eagerness"));
		}
		catch (...) {
			// fall back to no eagerness for invalid values
			return 0;
		}
	}

private:

	std::string processName;
	CaseInsensitiveStringMap options;

	void apply(ConfigFile configFile);
	std::string getOption(std::string key);
	bool getBooleanOption(std::string key);
};
