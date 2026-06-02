#pragma once
#include <string>
#include <regex>
#include <fstream>
#include "utils/StringUtils.h"
#include "yaml-cpp/yaml.h"
#include "utils/Testing.h"

namespace Profiler {
	/** A process-specific config section. */
	struct ProcessSection {
		/** Regular expression that must match the full process path for the section to be applied. Defaults to ".*" in case the user didn't set this field explicitly. */
		std::regex executablePathRegex;
		/** Name of the executable to which this section applies or the empty string if this field should be ignored. Must be compared case-insensitively. */
		std::string caseInsensitiveExecutableName;
		/** The profiler options to apply if this section matches the profiled process. */
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
		static ProcessSection parseMatchSection(YAML::Node& node);
	};
}

