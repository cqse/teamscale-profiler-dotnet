#pragma once

#include <string>

class StringUtils {
public:

	/** Removes the last part of the given file system path. */
	static std::string removeLastPartOfPath(std::string path);

	/** Whether the given value ends with the given suffix. */
	static bool endsWith(std::string const & value, std::string const & suffix);
};