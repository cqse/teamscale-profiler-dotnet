#pragma once
#ifdef TIA
#include "config/Config.h"

#include <thread>
#include <atomic>

class Ipc
{
public:
	Ipc(Config* config, std::function<void(std::string)> testChangedCallback);
	~Ipc();
	char* getCurrentTestName();
private:
	void* zmqContext;
	void* zmqRequestSocket;
	void* zmqSubscribeSocket;
	std::thread* handlerThread;
	std::atomic<bool> shutdown = false;
	void handlerThreadLoop(Config* config);
	char* request(const char* message);
	std::function<void(std::string)> testChangedCallback;
};
#endif
