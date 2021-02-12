#pragma once
#ifdef TIA
#include "config/Config.h"

#include <thread>
#include <atomic>

class Ipc
{
public:
	Ipc(Config* config, std::function<void(std::string)> testStartCallback, std::function<void(std::string, std::string)> testEndCallback, std::function<void(std::string)> errorCallback);
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
	std::function<void(std::string)> errorCallback;
	std::atomic<bool> shutdown = false;
	void handlerThreadLoop();
	void handleMessage(std::vector<std::string> frames);
	bool initRequestSocket();
	void logError(std::string message);
	std::string request(std::string message);
};
#endif
