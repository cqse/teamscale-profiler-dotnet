#pragma once
#include <thread>
#include <atomic>
#include <chrono>
#include <condition_variable>
#include <functional>
#include <memory>
#include <mutex>
#include <string>

namespace Profiler {
	/*
	* Inter Process Communication class that handles communication between the commander and the profiler.
	*/
	class Ipc
	{
	public:
		/**
		 * The callbacks the Ipc invokes when the commander sends a message. testStart, testEnd and dump
		 * may be left unset to mark a feature that is not active for this profiled process, in which
		 * case the messages the commander sends for it are ignored, e.g. testStart and testEnd are only
		 * set when TIA is enabled. info and error must always be set.
		 */
		struct Callbacks {
			std::function<void(std::string)> testStart;
			std::function<void(std::string, std::string)> testEnd;
			std::function<void()> dump;
			std::function<void(std::string)> info;
			std::function<void(std::string)> error;
		};

		/** Starts communicating with the commander that is expected to listen at the given address. */
		Ipc(const std::string& requestSocketAddress, Callbacks callbacks);
		~Ipc();
		/*
		 * Returns the name of the currently running test when in testwise coverage mode.
		 */
		std::string getCurrentTestName();
	private:
		void* zmqContext = nullptr;
		void* zmqRequestSocket = nullptr;
		void* zmqReplySocket = nullptr;

		/** The address at which the commander listens for our requests. */
		std::string requestSocketAddress;

		Callbacks callbacks;
		std::atomic<bool> shutdown = false;

		/** Whether we ever successfully registered with the commander. */
		bool isRegistered = false;

		/** Used to cancel sleep when shutting down the profiler so the shutdown is not delayed. */
		std::mutex shutdownMutex;
		std::condition_variable shutdownCondition;

		/**
		 * Must be declared last: the thread it starts uses all the other members, which are only
		 * guaranteed to be initialized if they are declared before it.
		 */
		std::unique_ptr<std::thread> handlerThread;

		void handlerThreadLoop();

		/**
		 * Repeatedly tries to register at the commander until that succeeds or the profiler shuts down.
		 * Returns the address at which the commander expects us to listen for its messages, or an empty
		 * string if we shut down before ever reaching it.
		 */
		std::string registerAtCommander();

		void handleMessage(const std::string& message);
		bool initRequestSocket();
		void logError(const std::string& message);

		/** Waits for the given duration or until the profiler shuts down, whichever comes first. */
		void waitBeforeRetry(std::chrono::milliseconds duration);

		/** The outcome of a request to the commander. */
		enum class RequestResult {
			/** The commander answered. */
			Answered,
			/** No commander was reachable, which is the normal case when profiling without one. */
			Unreachable,
			/** A commander was there but did not answer in time, which is a real problem. */
			NotAnswered,
		};

		struct Response {
			RequestResult result;
			/** The commander's answer. Only set if result is Answered. */
			std::string answer;
		};

		/** Sends the given message to the commander and returns its answer along with the outcome. */
		Response request(const std::string& message);
	};

}
