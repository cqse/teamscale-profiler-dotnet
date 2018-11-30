#include "ConfigParser.h"
#include "yaml-cpp/yaml.h"

ConfigFile ConfigParser::parse(std::istream& stream) {
	try {
		return parseUnsafe(stream);
	}
	catch (YAML::Exception e) {
		std::string reason = e.what();
		throw ConfigParsingException("Parsing the YAML config file failed at line " + std::to_string(e.mark.line) +
			", column " + std::to_string(e.mark.column) + ": " + reason);
	}
}

ConfigFile ConfigParser::parseUnsafe(std::istream& stream) {
	ConfigFile configFile;

	YAML::Node rootNode = YAML::Load(stream);

	YAML::Node matchSections = rootNode["match"];
	if (!matchSections.IsMap()) {
		return configFile;
	}

	for (auto entry : matchSections) {
		std::string key(entry.first.as<std::string>());

		ProcessSection section;
		section.processRegex = std::regex(key, std::regex_constants::ECMAScript | std::regex_constants::icase);

		for (auto optionEntry : entry.second) {
			std::string optionName = optionEntry.first.as<std::string>();
			std::string value = optionEntry.second.as<std::string>();
			section.options[optionName] = value;
		}

		configFile.sections.push_back(section);
	}

	return configFile;
}

ConfigFile ConfigParser::parseFile(std::string filePath) {
	std::ifstream stream(filePath);
	if (stream.fail()) {
		throw ConfigParsingException("Unable to open YAML config file " + filePath + " for reading");
	}
	return parse(stream);
}