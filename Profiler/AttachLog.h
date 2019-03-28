#pragma once
#include "FileLogBase.h"

class AttachLog: public FileLogBase
{
public:
	void createLogFile(std::string path);

	void logAttach();

	void logDetach();
};

