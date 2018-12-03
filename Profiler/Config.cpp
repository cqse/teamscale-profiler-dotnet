#include "Config.h"
#include "yaml-cpp/yaml.h"
#include "WindowsUtils.h"

void Config::load(std::string configFilePath, std::string processPath) {
	this->processPath = processPath;
	try {
		ConfigFile configFile = ConfigParser::parseFile(configFilePath);
	}
	catch (ConfigParsingException e) {
		// TODO how to handle?
	}

	// todo: still support old PROCESS handling?? i.e. disable if not suffix-matched and set?
	targetDir = getOption("targetdir");
	enabled = getBooleanOption("enabled");
	useLightMode = getBooleanOption("light_mode");
	logAssemblyFileVersion = getBooleanOption("assembly_file_version");
	logAssemblyPaths = getBooleanOption("assembly_paths");
	dumpEnvironment = getBooleanOption("dump_environment");
	ignoreExceptions = getBooleanOption("ignore_exceptions");
	startUploadDaemon = getBooleanOption("upload_daemon");

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
			options.insert(section.profilerOptions.begin(), section.profilerOptions.end());
		}
	}
}

std::string Config::getOption(std::string optionName) {
	std::string value = WindowsUtils::getConfigValueFromEnvironment(optionName);
	if (!value.empty()) {
		return value;
	}
	return options[optionName];
}

bool Config::getBooleanOption(std::string optionName) {
	std::string value = getOption(optionName);
	// true comes from the YAML files and 1 is used for the env options so we support both
	return value == "true" || value == "1";
}