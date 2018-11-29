#include "Config.h"
#include "yaml-cpp/yaml.h"
#include "WindowsUtils.h"

Config::Config(std::string configFilePath, std::string processName)
	: configFilePath(configFilePath), processName(processName)
{
	try {
		ConfigFile configFile = parse();
	}
	catch (YAML::Exception e) {
		// TODO how to handle?
	}
}

ConfigFile Config::parse() {
	ConfigFile configFile;

	YAML::Node rootNode = YAML::LoadFile(configFilePath);

	YAML::Node matchSections = rootNode["match"];
	if (!matchSections.IsMap()) {
		return configFile;
	}

	for (auto entry : matchSections) {
		std::string key(entry.first.as<std::string>());

		ProcessSection section;
		section.processRegex = std::regex(key, std::regex_constants::ECMAScript | std::regex_constants::icase);

		for (auto optionEntry : entry.second) {
			section.options[entry.first.as<std::string>()] = entry.second.as<std::string>();
		}

		configFile.sections.push_back(section);
	}

	return configFile;
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
