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

	/** Loads the config from the given YAML file and applies all sections that apply to the given process name. */
	void load(std::string configFilePath, std::string processName);

	/** The directory to which to write the trace file. */
	std::string targetDir() {
		return getOption("targetdir");
	}

	/** Whether to profile at all. */
	bool isEnabled() {
		return getBooleanOption("enabled");
	}

	/** Whether to use light mode or force rejitting of prejitted assemblies. */
	bool shouldUseLightMode() {
		return getBooleanOption("light_mode");
	}

	/** Whether to log the assembly file versions of all loaded assemblies. */
	bool shouldLogAssemblyFileVersion() {
		return getBooleanOption("assembly_file_version");
	}

	/** Whether to log the assembly file paths of all loaded assemblies. */
	bool shouldLogAssemblyPaths() {
		return getBooleanOption("assembly_paths");
	}

	/** Whether to log all environment variables in the trace file. */
	bool shouldDumpEnvironment() {
		return getBooleanOption("dump_environment");
	}

	/** Whether exceptions in the profiler code should be swallowed. */
	bool shouldIgnoreExceptions() {
		return getBooleanOption("ignore_exceptions");
	}

	/** Whether the profiler should start the upload daemon on startup. */
	bool shouldStartUploadDaemon() {
		return getBooleanOption("UPLOAD_DAEMON");
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
