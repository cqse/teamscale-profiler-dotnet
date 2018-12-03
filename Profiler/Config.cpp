#include "Config.h"
#include "yaml-cpp/yaml.h"
#include "WindowsUtils.h"
#include <exception>

void Config::load(std::string configFilePath, std::string processPath) {
	std::ifstream stream(configFilePath);
	if (stream.fail()) {
		problems.push_back("Failed to open the config file " + configFilePath + " for reading");
		// we must still load the values from the environment in this case
		loadValues();
		return;
	}

	load(stream, processPath);
}

void Config::load(std::istream& configFileContents, std::string processPath) {
	this->processPath = processPath;
	try {
		ConfigFile configFile = ConfigParser::parse(configFileContents);
		apply(configFile);
	}
	catch (const std::exception& e) {
		problems.push_back(std::string("Failed to parse the config file: ") + e.what());
		// continue loading the values from the environment in this case
	}
	catch (...) {
		problems.push_back(std::string("Failed to parse the config file. The reason is unknown"));
		// continue loading the values from the environment in this case
	}

	loadValues();
}

void Config::loadValues()
{
	targetDir = getOption("targetdir");
	enabled = getBooleanOption("enabled", true);
	useLightMode = getBooleanOption("light_mode", false);
	logAssemblyFileVersion = getBooleanOption("assembly_file_version", false);
	logAssemblyPaths = getBooleanOption("assembly_paths", false);
	dumpEnvironment = getBooleanOption("dump_environment", false);
	ignoreExceptions = getBooleanOption("ignore_exceptions", false);
	startUploadDaemon = getBooleanOption("upload_daemon", false);

	std::string eagernessValue = getOption("eagerness");
	if (eagernessValue.empty()) {
		eagerness = 0;
	}
	else {
		try {
			eagerness = static_cast<size_t>(std::stoi(eagernessValue));
		}
		catch (...) {
			problems.push_back("Invalid eagerness value configured: " + eagernessValue + ". Using the default of no eagerness instead");
		}
	}

	disableProfilerIfProcessSuffixDoesntMatch();
}

void Config::disableProfilerIfProcessSuffixDoesntMatch() {
	std::string processSuffix = WindowsUtils::getConfigValueFromEnvironment("process");
	if (!processSuffix.empty() && !StringUtils::endsWithCaseInsensitive(processPath, processSuffix)) {
		enabled = false;
	}
}

void Config::apply(ConfigFile configFile) {
	for (ProcessSection section : configFile.sections) {
		if (std::regex_match(processPath, section.processRegex)) {
			for (auto entry : section.profilerOptions) {
				options[entry.first] = entry.second;
			}
		}
	}
}

std::string Config::getOption(std::string optionName) {
	std::string value = environmentVariableReader(optionName);
	if (!value.empty()) {
		return value;
	}
	return options[optionName];
}

bool Config::getBooleanOption(std::string optionName, bool defaultValue) {
	std::string value = getOption(optionName);
	if (value.empty()) {
		return defaultValue;
	}

	// true comes from the YAML files and 1 is used for the env options so we support both
	return value == "true" || value == "1";
}