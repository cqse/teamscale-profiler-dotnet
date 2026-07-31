#include "Ipc.h"
#include "zmq.h"
#include <chrono>

namespace Profiler {
	using namespace std::chrono_literals;

	constexpr int IPC_TIMEOUT_MS = 1000;
	constexpr int IPC_BUFFER_SIZE = 768;
	constexpr int IPC_LINGER = 0;

	/** How long to wait for the connection to the commander to be established. */
	constexpr long CONNECT_TIMEOUT_MS = 250;

	/** How often to look for the commander while we are not registered yet. */
	constexpr auto REGISTRATION_RETRY_INTERVAL = 1000ms;

	/** Upper bound of the random amount that is added to the interval between two registration attempts. */
	constexpr int REGISTRATION_RETRY_JITTER_MS = 500;

	// Message signaling test start event
	const std::string TEST_START = "start:";
	const std::string TEST_END = "end:";
	// Message requesting that the coverage collected so far is written to disk
	const std::string DUMP = "dump";

	Ipc::Ipc(Config* config, const std::function<void(std::string)>& testStartCallback, const std::function<void(std::string, std::string)>& testEndCallback, const std::function<void()>& dumpCallback, const std::function<void(std::string)>& infoCallback, const std::function<void(std::string)>& errorCallback) :
		zmqContext(zmq_ctx_new()),
		config(config),
		testStartCallback(testStartCallback),
		testEndCallback(testEndCallback),
		dumpCallback(dumpCallback),
		infoCallback(infoCallback),
		errorCallback(errorCallback),
		randomEngine(GetCurrentProcessId()),
		// starts the handler thread, so this must stay last
		handlerThread(std::make_unique<std::thread>(&Ipc::handlerThreadLoop, this))
	{
	}

	Ipc::~Ipc()
	{
		{
			std::lock_guard<std::mutex> lock(this->shutdownMutex);
			this->shutdown = true;
		}
		// wake the handler thread if it is waiting for the next registration attempt so we don't
		// delay the shutdown of the profiled process
		this->shutdownCondition.notify_all();
		if (this->handlerThread->joinable()) {
			this->handlerThread->join();
		}
		if (this->isRegistered) {
			// if we never reached a commander there is nobody to tell, and trying would only wait
			// for the connect timeout again
			this->request("profiler_disconnected");
		}
		if (this->zmqRequestSocket != nullptr) {
			zmq_close(this->zmqRequestSocket);
		}
		zmq_ctx_shutdown(this->zmqContext);
		zmq_ctx_term(this->zmqContext);
	}

	void Ipc::handlerThreadLoop() {
		std::string address = "";
		bool connectionErrorLogged = false;
		while (address.empty() && !this->shutdown) {
			std::string addressRequest = "register:" + std::to_string(GetCurrentProcessId());
			bool commanderWasReachable = false;
			address = this->request(addressRequest, &commanderWasReachable);
			if (!address.empty()) {
				break;
			}

			// only log this once. The profiler always looks for a commander, so for processes that are
			// profiled without one, this would otherwise fill up the trace file.
			if (!connectionErrorLogged) {
				if (commanderWasReachable) {
					// somebody is there but the handshake didn't work, which is a real problem
					logError("Registration at the commander failed, trying again.");
				}
				else {
					// there simply is no commander, which is the normal case when profiling without one
					infoCallback("No commander is listening at " + this->config->getTiaRequestSocket()
						+ ". Looking for one in the background.");
				}
				connectionErrorLogged = true;
			}
			waitForRetry(nextRetryInterval());
		}
		if (address.empty()) {
			// we are shutting down before we ever reached the commander
			return;
		}
		this->isRegistered = true;
		if (connectionErrorLogged) {
			// we reported that we couldn't reach the commander, so report that we eventually did
			infoCallback("Connected to the commander at " + this->config->getTiaRequestSocket());
		}
		handleMessage(getCurrentTestName());

		this->zmqReplySocket = zmq_socket(this->zmqContext, ZMQ_REP);
		zmq_setsockopt(this->zmqReplySocket, ZMQ_RCVTIMEO, &IPC_TIMEOUT_MS, sizeof(IPC_TIMEOUT_MS));
		zmq_setsockopt(this->zmqReplySocket, ZMQ_LINGER, &IPC_LINGER, sizeof(IPC_LINGER));

		if (!!zmq_bind(this->zmqReplySocket, address.c_str())) {
			zmq_close(this->zmqReplySocket);
			this->zmqReplySocket = nullptr;
			logError("Failed connecting to subscribe socket");
			return;
		}
		while (!this->shutdown) {
			std::array<char, IPC_BUFFER_SIZE> buf;
			int len = zmq_recv(this->zmqReplySocket, &buf, buf.size(), 0);
			if (len != -1) {
				std::string message(buf.data(), len);
				// the message is handled synchronously, i.e. the acknowledgement is only sent once
				// the requested action has been completed
				handleMessage(message);
				zmq_send(this->zmqReplySocket, "ack", 3, 0);
			}
		}
		zmq_close(this->zmqReplySocket);
	}

	void Ipc::handleMessage(const std::string& message) {
		if (message.find(TEST_START) == 0) {
			this->testStartCallback(message.substr(TEST_START.length()));
		}
		else if (message.find(TEST_END) == 0) {
			// 21 = maximum length of long + 1
			size_t last = message.find_last_of(':', 21);
			std::string testIdentifier = message.substr(0, last);
			std::string duration = message.substr(last + 1);
			this->testEndCallback(testIdentifier.substr(TEST_END.length()), duration);
		}
		else if (message == DUMP) {
			this->dumpCallback();
		}
	}

	std::string Ipc::getCurrentTestName()
	{
		std::string testnameRequest = "testname";
		return this->request(testnameRequest);
	}

	std::string Ipc::request(const std::string& message, bool* commanderWasReachable)
	{
		if (commanderWasReachable != nullptr) {
			*commanderWasReachable = false;
		}
		if (!initRequestSocket()) {
			return "";
		}

		// Because of ZMQ_IMMEDIATE the socket only becomes writable once the connection to the commander
		// has actually been established, so this tells us whether there is a commander at all. Without
		// it we would send into the void and only notice when the receive below times out. We have to
		// wait a moment because ZeroMQ establishes the connection asynchronously.
		zmq_pollitem_t pollItem{ this->zmqRequestSocket, 0, ZMQ_POLLOUT, 0 };
		if (zmq_poll(&pollItem, 1, CONNECT_TIMEOUT_MS) <= 0) {
			// keep the socket, ZeroMQ keeps trying to establish the connection in the background.
			// A message we never sent also leaves the REQ socket ready to send again.
			return "";
		}
		if (commanderWasReachable != nullptr) {
			*commanderWasReachable = true;
		}

		zmq_send(this->zmqRequestSocket, message.data(), message.size(), 0);
		std::array<char, IPC_BUFFER_SIZE> buffer;
		int len = zmq_recv(this->zmqRequestSocket, buffer.data(), buffer.size(), 0);
		if (len < 0) {
			zmq_close(this->zmqRequestSocket);
			this->zmqRequestSocket = nullptr;
			return "";
		}

		return std::string(buffer.data(), len);
	}

	bool Ipc::initRequestSocket() {
		if (this->zmqRequestSocket == nullptr) {
			this->zmqRequestSocket = zmq_socket(this->zmqContext, ZMQ_REQ);
			if (!this->zmqRequestSocket) {
				logError("Failed to create ZMQ socket");
				return false;
			}
			zmq_setsockopt(this->zmqRequestSocket, ZMQ_RCVTIMEO, &IPC_TIMEOUT_MS, sizeof(IPC_TIMEOUT_MS));
			zmq_setsockopt(this->zmqRequestSocket, ZMQ_LINGER, &IPC_TIMEOUT_MS, sizeof(IPC_TIMEOUT_MS));
			// Only queue messages for connections that are established. This lets a send tell us whether
			// the commander is reachable, instead of ZeroMQ silently queueing for a peer that may never
			// appear. Must be set before connecting.
			constexpr int immediate = 1;
			zmq_setsockopt(this->zmqRequestSocket, ZMQ_IMMEDIATE, &immediate, sizeof(immediate));
			// Let ZeroMQ retry the connection at the same jittered rate at which we look for the
			// commander. The default of 100ms would have its I/O thread reconnecting far more often
			// than we ask, which is wasted work for the many processes profiled without a commander.
			const int reconnectInterval = static_cast<int>(nextRetryInterval().count());
			zmq_setsockopt(this->zmqRequestSocket, ZMQ_RECONNECT_IVL, &reconnectInterval, sizeof(reconnectInterval));
			if (zmq_connect(this->zmqRequestSocket, this->config->getTiaRequestSocket().c_str()) == -1) {
				zmq_close(this->zmqRequestSocket);
				this->zmqRequestSocket = nullptr;
				logError("Failed connecting to request socket");
				return false;
			}
		}
		return true;
	}

	void Ipc::waitForRetry(std::chrono::milliseconds duration) {
		std::unique_lock<std::mutex> lock(this->shutdownMutex);
		this->shutdownCondition.wait_for(lock, duration, [this] { return this->shutdown.load(); });
	}

	std::chrono::milliseconds Ipc::nextRetryInterval() {
		std::uniform_int_distribution<int> jitter(0, REGISTRATION_RETRY_JITTER_MS);
		return REGISTRATION_RETRY_INTERVAL + std::chrono::milliseconds(jitter(this->randomEngine));
	}

	void Ipc::logError(const std::string& message) {
		std::string error = message + " (ZMQ Status: " + zmq_strerror(zmq_errno()) + ")";
		errorCallback(error);
	}

}

