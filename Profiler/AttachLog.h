#pragma once
#include "FileLogBase.h"

class AttachLog: public FileLogBase
{
public:
	void createLogFile(std::string path);

	void logAttach(std::string processId, std::string executablePath);

	void logDetach(std::string processId, std::string executablePath);
};

