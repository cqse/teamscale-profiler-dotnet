#include "Config.h"
#include "yaml-cpp/yaml.h"
#include "WindowsUtils.h"

void Config::load(std::string configFilePath, std::string processPath) {
	std::ifstream stream(configFilePath);
	if (stream.fail()) {
		// TODO how to handle? default values??
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
	catch (ConfigParsingException e) {
		// TODO how to handle?
	}

	targetDir = getOption("targetdir");
	enabled = getBooleanOption("enabled", true);
	useLightMode = getBooleanOption("light_mode", false);
	logAssemblyFileVersion = getBooleanOption("assembly_file_version", false);
	logAssemblyPaths = getBooleanOption("assembly_paths", false);
	dumpEnvironment = getBooleanOption("dump_environment", false);
	ignoreExceptions = getBooleanOption("ignore_exceptions", false);
	startUploadDaemon = getBooleanOption("upload_daemon", false);

	try {
		eagerness = static_cast<size_t>(std::stoi(getOption("eagerness")));
	}
	catch (...) {
		// fall back to no eagerness for invalid values
		eagerness = 0;
		// TODO warn/log??
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