#pragma once
#include "config/Config.h"

#include <thread>
#include <atomic>
#include <functional>

namespace Profiler {
	/*
	* Inter Process Communication class that handles communication between the commander and the profiler.
	*/
	class Ipc
	{
	public:
		Ipc(Config* config, const std::function<void(std::string)>& testStartCallback, const std::function<void(std::string, std::string)>& testEndCallback, const std::function<void(std::string)>& errorCallback);
		~Ipc();
		/*
		 * Returns the name of the currently running test when in testwise coverage mode.
		 */
		std::string getCurrentTestName();
	private:
		void* zmqContext = nullptr;
		void* zmqRequestSocket = nullptr;
		void* zmqReplySocket = nullptr;
		Config* config = nullptr;
		std::unique_ptr<std::thread> handlerThread;
		std::function<void(std::string)> testStartCallback;
		std::function<void(std::string, std::string)> testEndCallback;
		std::function<void(std::string)> errorCallback;
		std::atomic<bool> shutdown = false;
		void handlerThreadLoop();
		void handleMessage(const std::string& message);
		bool initRequestSocket();
		void logError(const std::string& message);
		std::string request(const std::string& message);
	};

}
