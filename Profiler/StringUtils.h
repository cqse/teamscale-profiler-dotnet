#pragma once

#include <string>
#include <algorithm>
#include <map>

/** Utility functions for strings. */
class StringUtils {
public:

	/** Removes the last part of the given file system path. */
	static std::string removeLastPartOfPath(std::string path);

	/** Whether the given value ends with the given suffix. */
	static bool endsWith(std::string const & value, std::string const & suffix);

	/** Compares strings regardless of their casing. */
	struct CaseInsensitiveComparator {
		bool operator() (const std::string& s1, const std::string& s2) const {
			std::string str1(s1.length(), ' ');
			std::string str2(s2.length(), ' ');
			std::transform(s1.begin(), s1.end(), str1.begin(), ::tolower);
			std::transform(s2.begin(), s2.end(), str2.begin(), ::tolower);
			return  str1 < str2;
		}
	};
};

/** A map string -> string that ignores the casing of its keys. */
typedef std::map<std::string, std::string, StringUtils::CaseInsensitiveComparator> CaseInsensitiveStringMap;
