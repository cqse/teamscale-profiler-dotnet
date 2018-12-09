#pragma once
#include <string>
#include <regex>
#include <fstream>
#include "utils/StringUtils.h"
#include "yaml-cpp/yaml.h"
#include "utils/Testing.h"

/** A process-specific config section. */
struct ProcessSection {
	std::regex processRegex;
	CaseInsensitiveStringMap profilerOptions;
};

/** The parsed YAML config. */
struct ConfigFile {
	std::vector<ProcessSection> sections;
};

/** Thrown if config parsing fails. */
class ConfigParsingException : public std::runtime_error {
public:
	ConfigParsingException(std::string message) : std::runtime_error(message) {};
};

/**
  * Parses a config YAML file into structs and handles potential errors.
  *
  * Example:
  *
  *   match:
  *     - process: ".*prog1.exe"
  *       profiler:
  *         opt1: 1
  *         opt2: "uiae"
  */
class ConfigParser
{
public:
	/** Parses the given stream as a YAML config file. Throws ConfigParsingException if parsing fails. */
	static EXPOSE_TO_CPP_TESTS ConfigFile parse(std::istream& stream);

private:
	static ConfigFile parseUnsafe(std::istream& stream);
	static void parseMatchSection(YAML::Node &node, ConfigFile &configFile);
};
