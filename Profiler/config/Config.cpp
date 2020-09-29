#include "Config.h"
#include "utils/WindowsUtils.h"
#include <exception>

std::string Config::getDefaultConfigPath()
{
	std::string profilerDllPath = WindowsUtils::getConfigValueFromEnvironment("PATH");
	std::string profilerDllDirectory = StringUtils::removeLastPartOfPath(profilerDllPath);
	return profilerDllDirectory + "\\Profiler.yml";
}

void Config::load(std::string configFilePath, std::string processPath, bool logProblemIfConfigFileDoesNotExist) {
	this->processPath = processPath;
	this->configPath = configFilePath;

	if (WindowsUtils::isFile(configFilePath)) {
		std::ifstream stream(configFilePath);
		if (stream.fail()) {
			problems.push_back("Failed to open the config file " + configFilePath + " for reading");
			// we must still load the values from the environment in this case so we don't return here
		}
		else {
			loadYamlConfig(stream);
		}
	}
	else if (logProblemIfConfigFileDoesNotExist) {
		problems.push_back("The config file " + configFilePath + " does not exist");
	}

	setOptions();
}

void Config::load(std::istream& configFileContents, std::string processPath) {
	this->processPath = processPath;
	loadYamlConfig(configFileContents);
	setOptions();
}

void Config::loadYamlConfig(std::istream& configFileContents) {
	ConfigFile configFile;
	try {
		configFile = ConfigParser::parse(configFileContents);
	}
	catch (const std::exception& e) {
		problems.push_back(std::string("Failed to parse the config file: ") + e.what());
		return;
	}
	catch (...) {
		problems.push_back(std::string("Failed to parse the config file. The reason is unknown"));
		return;
	}

	for (ProcessSection& section : configFile.sections) {
		if (sectionMatches(section)) {
			relevantConfigFileSections.insert(relevantConfigFileSections.begin(), section);
		}
	}
}

bool Config::sectionMatches(ProcessSection& section) {
	if (!std::regex_match(processPath, section.executablePathRegex)) {
		return false;
	}

	std::string executableName = StringUtils::getLastPartOfPath(processPath);
	return section.caseInsensitiveExecutableName.empty() || StringUtils::equalsIgnoreCase(section.caseInsensitiveExecutableName, executableName);
}

void Config::setOptions()
{
	targetDir = getOption("targetdir");
	enabled = getBooleanOption("enabled", true);
	useLightMode = getBooleanOption("light_mode", true);
	logAssemblyFileVersion = getBooleanOption("assembly_file_version", false);
	logAssemblyPaths = getBooleanOption("assembly_paths", false);
	dumpEnvironment = getBooleanOption("dump_environment", false);
	ignoreExceptions = getBooleanOption("ignore_exceptions", false);
	startUploadDaemon = getBooleanOption("upload_daemon", false);
#ifdef TIA
	tiaEnabled = getBooleanOption("tia", false);
	tiaSubscribeSocket = getOption("tia_subscribe_socket");
	if (tiaSubscribeSocket.empty()) {
		tiaSubscribeSocket = "tcp://127.0.0.1:7145"; // 7145 for leet TIA-Socket :)
	}

	tiaRequestSocket = getOption("tia_request_socket");
	if (tiaSubscribeSocket.empty()) {
		tiaSubscribeSocket = "tcp://127.0.0.1:7146";
	}
#endif
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
	std::string processSuffix = environmentVariableReader("process");
	if (!processSuffix.empty() && !StringUtils::endsWithCaseInsensitive(processPath, processSuffix)) {
		enabled = false;
	}
}

std::string Config::getOption(std::string optionName) {
	std::string value = environmentVariableReader(optionName);
	if (!value.empty()) {
		return value;
	}

	for (ProcessSection section : relevantConfigFileSections) {
		if (section.profilerOptions.find(optionName) != section.profilerOptions.end()) {
			return section.profilerOptions[optionName];
		}
	}
	return "";
}

bool Config::getBooleanOption(std::string optionName, bool defaultValue) {
	std::string value = getOption(optionName);
	if (value.empty()) {
		return defaultValue;
	}

	// true comes from the YAML files and 1 is used for the env options so we support both
	return value == "true" || value == "1";
}
