#include "StringUtils.h"
#include <Shlwapi.h>

/** Removes the last part of the given file system path. */
std::string StringUtils::removeLastPartOfPath(std::string path) {
	char* chars = _strdup(path.c_str());
	PathRemoveFileSpec(chars);
	std::string result(chars);
	free(chars);
	return result;
}

/** Whether the given value ends with the given suffix. */
bool StringUtils::endsWith(std::string const & value, std::string const & suffix)
{
	if (suffix.size() > value.size()) {
		return false;
	}
	return std::equal(suffix.rbegin(), suffix.rend(), value.rbegin());
}