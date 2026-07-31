#pragma once
#include "config/Config.h"

#include <thread>
#include <atomic>
#include <chrono>
#include <condition_variable>
#include <functional>
#include <mutex>
#include <random>

namespace Profiler {
	/*
	* Inter Process Communication class that handles communication between the commander and the profiler.
	*/
	class Ipc
	{
	public:
		Ipc(Config* config, const std::function<void(std::string)>& testStartCallback, const std::function<void(std::string, std::string)>& testEndCallback, const std::function<void()>& dumpCallback, const std::function<void(std::string)>& infoCallback, const std::function<void(std::string)>& errorCallback);
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
		std::function<void(std::string)> testStartCallback;
		std::function<void(std::string, std::string)> testEndCallback;
		std::function<void()> dumpCallback;
		std::function<void(std::string)> infoCallback;
		std::function<void(std::string)> errorCallback;
		std::atomic<bool> shutdown = false;

		/** Whether we ever successfully registered with the commander. */
		bool isRegistered = false;

		/** Used to abort the wait between two registration attempts as soon as the profiler shuts down. */
		std::mutex shutdownMutex;
		std::condition_variable shutdownCondition;

		/** Used to jitter the interval between two registration attempts. */
		std::mt19937 randomEngine;

		/**
		 * Must be declared last: the thread it starts uses all the other members, which are only
		 * guaranteed to be initialized if they are declared before it.
		 */
		std::unique_ptr<std::thread> handlerThread;

		void handlerThreadLoop();
		void handleMessage(const std::string& message);
		bool initRequestSocket();
		void logError(const std::string& message);

		/** Waits for the given duration or until the profiler shuts down, whichever comes first. */
		void waitForRetry(std::chrono::milliseconds duration);

		/**
		 * Returns how long to wait before looking for the commander again. The interval is jittered so
		 * that many profiled processes that start at the same time don't probe in lockstep.
		 */
		std::chrono::milliseconds nextRetryInterval();

		/**
		 * Sends the given message to the commander and returns its answer, or an empty string if the
		 * commander could not be reached or did not answer in time. If commanderWasReachable is given,
		 * it is set to whether we had a connection to the commander at all, which tells a missing
		 * commander apart from one that is there but does not answer.
		 */
		std::string request(const std::string& message, bool* commanderWasReachable = nullptr);
	};

}
