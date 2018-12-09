#pragma once
#include <string>
#include <fstream>
#include "ConfigParser.h"
#include "utils/Testing.h"

/** Abstracts reading a config value from the environment so the Config class is unit-testable. */
typedef std::string EnvironmentVariableReader(std::string suffix);

/**
  * Manages config settings from both the environment and a config file.
  * Settings from the environment always win.
  */
class Config
{
public:

	Config(EnvironmentVariableReader* _environmentVariableReader) : environmentVariableReader(_environmentVariableReader) {}

	/** Loads the config from the given YAML file and applies all sections that apply to the given profiled process path. */
	void EXPOSE_TO_CPP_TESTS load(std::string configFilePath, std::string processPath, bool logProblemIfConfigFileDoesNotExist);

	/** Loads the config from the given YAML stream and applies all sections that apply to the given profiled process path. */
	void EXPOSE_TO_CPP_TESTS load(std::istream& configFileContents, std::string processPath);

	/** Returns any problems encountered while loading the config, e.g. to log them. */
	std::vector<std::string> getProblems() {
		return problems;
	}

	/** The directory to which to write the trace file. */
	std::string getTargetDir() {
		return targetDir;
	}

	/** Whether to profile at all. */
	bool isEnabled() {
		return enabled;
	}

	/** Whether to use light mode or force rejitting of prejitted assemblies. */
	bool shouldUseLightMode() {
		return useLightMode;
	}

	/** Whether to log the assembly file versions of all loaded assemblies. */
	bool shouldLogAssemblyFileVersion() {
		return logAssemblyFileVersion;
	}

	/** Whether to log the assembly file paths of all loaded assemblies. */
	bool shouldLogAssemblyPaths() {
		return logAssemblyPaths;
	}

	/** Whether to log all environment variables in the trace file. */
	bool shouldDumpEnvironment() {
		return dumpEnvironment;
	}

	/** Whether exceptions in the profiler code should be swallowed. */
	bool shouldIgnoreExceptions() {
		return ignoreExceptions;
	}

	/** Whether the profiler should start the upload daemon on startup. */
	bool shouldStartUploadDaemon() {
		return startUploadDaemon;
	}

	/** Whether to eagerly log trace data. */
	size_t getEagerness() {
		return eagerness;
	}

private:

	std::string processPath;
	CaseInsensitiveStringMap options;
	EnvironmentVariableReader* environmentVariableReader;
	std::vector<std::string> problems;

	bool enabled;
	std::string targetDir;
	bool useLightMode;
	bool logAssemblyFileVersion;
	bool logAssemblyPaths;
	bool dumpEnvironment;
	bool ignoreExceptions;
	bool startUploadDaemon;
	size_t eagerness;

	void apply(ConfigFile configFile);
	std::string getOption(std::string key);
	bool getBooleanOption(std::string key, bool defaultValue);
	void loadValues();
	void loadYamlConfig(std::istream& configFileContents);

	/** Backwards compatibility: disables the profiler if the suffix in the COR_PROFILER_PROCESS environment variable doesn't match the profiled process.  */
	void disableProfilerIfProcessSuffixDoesntMatch();
};
