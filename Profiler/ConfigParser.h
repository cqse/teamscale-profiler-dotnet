#pragma once
#include <string>
#include <regex>
#include <fstream>
#include "StringUtils.h"

/** A process-specific config section. */
struct ProcessSection {
	std::regex processRegex;
	CaseInsensitiveStringMap options;
};

/** The parsed YAML config. */
struct ConfigFile {
	std::vector<ProcessSection> sections;
};

/** Thrown if config parsing fails. */
class ConfigParsingException : std::runtime_error {
public:
	ConfigParsingException(std::string message) : std::runtime_error(message) {};
};

/**
  * Parses a config YAML file into structs and handles potential errors.
  *
  * Example:
  *
  *   match:
  *     ".*prog1":
  *       opt1: 1
  *       opt2: uiae
  */
class __declspec(dllexport) ConfigParser
{
public:
	/** Parses the given stream as a YAML config file. Throws ConfigParsingException if parsing fails. */
	static ConfigFile parse(std::istream& stream);

	/** Parses the given file as a YAML config file. Throws ConfigParsingException if parsing fails. */
	static ConfigFile parseFile(std::string filePath);

private:
	static ConfigFile parseUnsafe(std::istream& stream);
};
