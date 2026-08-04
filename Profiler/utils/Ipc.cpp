#include "Ipc.h"
#include "zmq.h"
#include <array>
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

	// Message signaling test start event
	const std::string TEST_START = "start:";
	const std::string TEST_END = "end:";
	// Message requesting that the coverage collected so far is written to disk
	const std::string DUMP = "dump";
	// Message the commander sends before a request to check whether we are still responsive
	const std::string ALIVE_CHECK = "alive";

	Ipc::Ipc(const std::string& requestSocketAddress, Callbacks callbacks) :
		zmqContext(zmq_ctx_new()),
		requestSocketAddress(requestSocketAddress),
		callbacks(std::move(callbacks)),
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
			this->request("profiler_disconnected");
		}
		if (this->zmqRequestSocket != nullptr) {
			zmq_close(this->zmqRequestSocket);
		}
		zmq_ctx_shutdown(this->zmqContext);
		zmq_ctx_term(this->zmqContext);
	}

	std::string Ipc::registerAtCommander() {
		std::string address = "";
		bool connectionHintLogged = false;
		while (!this->shutdown) {
			std::string addressRequest = "register:" + std::to_string(GetCurrentProcessId());
			Response response = this->request(addressRequest);
			if (response.result == RequestResult::Answered) {
				address = response.answer;
				break;
			}

			// only log this once. The profiler always looks for a commander, so for processes that are
			// profiled without one, this would otherwise fill up the trace file.
			if (!connectionHintLogged) {
				if (response.result == RequestResult::NotAnswered) {
					logError("Registration at the commander failed, trying again.");
				}
				else {
					this->callbacks.info("No commander is listening at " + this->requestSocketAddress
						+ ". Looking for one in the background.");
				}
				connectionHintLogged = true;
			}
			waitBeforeRetry(REGISTRATION_RETRY_INTERVAL);
		}
		if (!address.empty()) {
			// we reported that we couldn't reach the commander, so report that we eventually did
			this->callbacks.info("Connected to the commander at " + this->requestSocketAddress);
		}
		return address;
	}

	void Ipc::handlerThreadLoop() {
		std::string address = registerAtCommander();
		if (address.empty()) {
			// we are shutting down before we ever reached the commander
			return;
		}
		this->isRegistered = true;
		if (this->callbacks.testStart) {
			handleMessage(getCurrentTestName());
		}

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
		if (message == ALIVE_CHECK) {
			return;
		}
		// messages whose callback is not set belong to a feature that is inactive for this process
		if (this->callbacks.testStart && message.find(TEST_START) == 0) {
			this->callbacks.testStart(message.substr(TEST_START.length()));
		}
		else if (this->callbacks.testEnd && message.find(TEST_END) == 0) {
			// 21 = maximum length of long + 1
			size_t last = message.find_last_of(':', 21);
			std::string testIdentifier = message.substr(0, last);
			std::string duration = message.substr(last + 1);
			this->callbacks.testEnd(testIdentifier.substr(TEST_END.length()), duration);
		}
		else if (this->callbacks.dump && message == DUMP) {
			this->callbacks.dump();
		}
	}

	std::string Ipc::getCurrentTestName()
	{
		std::string testnameRequest = "testname";
		return this->request(testnameRequest).answer;
	}

	Ipc::Response Ipc::request(const std::string& message)
	{
		if (!initRequestSocket()) {
			return { RequestResult::Unreachable };
		}

		// Wait for the socket to become writable. Thanks to ZMQ_IMMEDIATE that only happens once the
		// connection to the commander is established, so this is our check for whether a commander
		// exists. Without it, we would send into the void and only notice once the receive below times
		// out. 
		zmq_pollitem_t pollItem{ this->zmqRequestSocket, 0, ZMQ_POLLOUT, 0 };
		if (zmq_poll(&pollItem, 1, CONNECT_TIMEOUT_MS) <= 0) {
			// keep the socket, ZeroMQ keeps trying to establish the connection in the background.
			return { RequestResult::Unreachable };
		}

		zmq_send(this->zmqRequestSocket, message.data(), message.size(), 0);
		std::array<char, IPC_BUFFER_SIZE> buffer;
		int len = zmq_recv(this->zmqRequestSocket, buffer.data(), buffer.size(), 0);
		if (len < 0) {
			zmq_close(this->zmqRequestSocket);
			this->zmqRequestSocket = nullptr;
			return { RequestResult::NotAnswered };
		}

		return { RequestResult::Answered, std::string(buffer.data(), len) };
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

			constexpr int reconnectInterval = static_cast<int>(REGISTRATION_RETRY_INTERVAL.count());
			zmq_setsockopt(this->zmqRequestSocket, ZMQ_RECONNECT_IVL, &reconnectInterval, sizeof(reconnectInterval));
			if (zmq_connect(this->zmqRequestSocket, this->requestSocketAddress.c_str()) == -1) {
				zmq_close(this->zmqRequestSocket);
				this->zmqRequestSocket = nullptr;
				logError("Failed connecting to request socket");
				return false;
			}
		}
		return true;
	}

	void Ipc::waitBeforeRetry(std::chrono::milliseconds duration) {
		std::unique_lock<std::mutex> lock(this->shutdownMutex);
		this->shutdownCondition.wait_for(lock, duration, [this] { return this->shutdown.load(); });
	}

	void Ipc::logError(const std::string& message) {
		std::string error = message + " (ZMQ Status: " + zmq_strerror(zmq_errno()) + ")";
		this->callbacks.error(error);
	}

}

