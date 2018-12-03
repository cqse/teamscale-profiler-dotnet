#include "StringUtils.h"
#include <Shlwapi.h>

std::string StringUtils::removeLastPartOfPath(std::string path) {
	char* chars = _strdup(path.c_str());
	PathRemoveFileSpec(chars);
	std::string result(chars);
	free(chars);
	return result;
}

std::string StringUtils::uppercase(std::string const & value)
{
	std::string result(value);
	std::transform(result.begin(), result.end(), result.begin(), ::toupper);
	return result;
}

bool StringUtils::endsWithCaseInsensitive(std::string const & value, std::string const & suffix)
{
	if (suffix.size() > value.size()) {
		return false;
	}

	return std::equal(suffix.rbegin(), suffix.rend(), value.rbegin(), [](char first, char second) { return toupper(first) < toupper(second); });
}