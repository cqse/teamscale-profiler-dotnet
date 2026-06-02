#pragma once
#include <string>
#include <atlbase.h>
#include <iostream>
#include <fstream>
#include <locale>
#include <codecvt>

namespace Profiler {
	/**
	 * Manages a log file on the file system.
	 * Unless mentioned otherwise, all methods in this class are thread-safe and perform their own synchronization.
	 */
	class FileLogBase
	{
	public:
		FileLogBase();
		virtual ~FileLogBase() noexcept;

		/** Closes the log. Further calls to logging methods will be ignored. */
		void shutdown();

		

	protected:
		/** File into which results are written. INVALID_HANDLE if the file has not been opened yet. */
		std::wofstream logFile;

		/** Synchronizes access to the log file. */
		CRITICAL_SECTION criticalSection;

		std::wstring_convert<std::codecvt_utf8_utf16<wchar_t>> converter;

		/**
		 * Create the log file. Must be the first method called on this object.
		 * This method is not thread-safe or reentrant.
		 */
		void createLogFile(std::string directory, std::string name);

		/** Writes the given string to the log file. */
		void writeToFile(const std::string& string);

		/** Writes the given wide string to the log file. */
		void writeWideToFile(const std::wstring& string);

		/** Writes the given wide string name-value pair to the log file. */
		void writeWideTupleToFile(const std::wstring& key, const std::wstring& value);

		/** Writes the given name-value pair to the log file. */
		void writeTupleToFile(const std::string& key, const std::string& value);

		/** Fills the given buffer with a string representing the current time. */
		std::string getFormattedCurrentTime();
	};
}


