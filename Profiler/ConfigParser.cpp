#include "ConfigParser.h"

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
	if (!matchSections.IsSequence()) {
		return configFile;
	}

	for (auto entry : matchSections) {
		parseMatchSection(entry, configFile);
	}

	return configFile;
}

void ConfigParser::parseMatchSection(YAML::Node &node, ConfigFile &configFile)
{
	ProcessSection section;

	std::string processRegex = node["process"].as<std::string>();
	section.processRegex = std::regex(processRegex, std::regex_constants::ECMAScript | std::regex_constants::icase);

	for (auto optionEntry : node["profiler"]) {
		std::string optionName = optionEntry.first.as<std::string>();
		std::string value = optionEntry.second.as<std::string>();
		section.profilerOptions[optionName] = value;
	}

	configFile.sections.push_back(section);
}