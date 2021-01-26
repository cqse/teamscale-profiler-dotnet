#pragma once
#ifdef TIA
#include "config/Config.h"

#include <thread>
#include <atomic>

class Ipc
{
public:
	Ipc(Config* config, std::function<void(std::string)> testStartCallback, std::function<void(std::string, std::string)> testEndCallback);
	~Ipc();
	std::string getCurrentTestName();
private:
	int zmqTimeout;
	void* zmqContext = NULL;
	void* zmqRequestSocket = NULL;
	void* zmqSubscribeSocket = NULL;
	Config* config = NULL;
	std::thread* handlerThread = NULL;
	std::function<void(std::string)> testStartCallback;
	std::function<void(std::string, std::string)> testEndCallback;
	std::atomic<bool> shutdown = false;
	void handlerThreadLoop();
	void handleMessage(std::vector<std::string> frames);
	void initRequestSocket();
	std::string request(std::string message);
};
#endif
