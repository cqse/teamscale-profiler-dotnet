#pragma once

#include <string>

class FileSystemUtils {
public:

	/** Removes the last part of the given file system path. */
	static std::string removeLastPartOfPath(std::string path);
};