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
	std::string getCurrentTestName();
private:
	int zmqTimeout;
	void* zmqContext = NULL;
	void* zmqRequestSocket = NULL;
	void* zmqSubscribeSocket = NULL;
	Config* config = NULL;
	std::thread* handlerThread = NULL;
	std::function<void(std::string)> testChangedCallback;
	std::atomic<bool> shutdown = false;
	void handlerThreadLoop();
	void initRequestSocket();
	std::string request(std::string message);
};
#endif
