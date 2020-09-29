#pragma once
#ifdef TIA
#include "config/Config.h"

class Ipc
{
public:
	Ipc(Config *config);
	~Ipc();
	char* getCurrentTestName();
private:
	void* zmqContext;
	void* zmqRequestSocket;
	void* zmqSubscribeSocket;
	char* request(const char* message);
};
#endif
