#include "FileSystemUtils.h"
#include <Shlwapi.h>

/** Removes the last part of the given file system path. */
std::string FileSystemUtils::removeLastPartOfPath(std::string path) {
	char* chars = _strdup(path.c_str());
	PathRemoveFileSpec(chars);
	std::string result(chars);
	free(chars);
	return result;
}