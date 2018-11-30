#include "Config.h"
#include "yaml-cpp/yaml.h"
#include "WindowsUtils.h"

Config::Config(std::string configFilePath, std::string processName)
	: processName(processName)
{
	try {
		ConfigFile configFile = ConfigParser::parseFile(configFilePath);
	}
	catch (ConfigParsingException e) {
		// TODO how to handle?
	}
}

void Config::apply(ConfigFile configFile) {
	for (ProcessSection section : configFile.sections) {
		if (std::regex_match(processName, section.processRegex)) {
			options.insert(section.options.begin(), section.options.end());
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