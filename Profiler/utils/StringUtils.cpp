#include "StringUtils.h"
#include <Shlwapi.h>

namespace Profiler {
	std::string StringUtils::removeLastPartOfPath(std::string path) {
		char* chars = _strdup(path.c_str());
		PathRemoveFileSpec(chars);
		std::string result(chars);
		free(chars);
		return result;
	}

	std::string StringUtils::getLastPartOfPath(std::string path) {
		char* chars = _strdup(path.c_str());
		char* fileName = PathFindFileName(chars);
		std::string result(fileName);
		free(chars);
		return result;
	}

	std::string StringUtils::uppercase(std::string const& value)
	{
		std::string result(value);
		std::transform(result.begin(), result.end(), result.begin(), ::toupper);
		return result;
	}

	bool StringUtils::equalsIgnoreCase(std::string const& value1, std::string const& value2) {
		return uppercase(value1) == uppercase(value2);
	}

	bool StringUtils::endsWithCaseInsensitive(std::string const& value, std::string const& suffix)
	{
		if (suffix.length() > value.length()) {
			return false;
		}

		std::string suffixUppercased = uppercase(suffix);
		std::string valueUppercased = uppercase(value);

		for (size_t i = 0; i < suffix.length(); i++) {
			if (suffixUppercased[suffix.length() - i - 1] != valueUppercased[valueUppercased.length() - i - 1]) {
				return false;
			}
		}
		return true;
	}
}

