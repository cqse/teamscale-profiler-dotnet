#include "Ipc.h"
#include "zmq.h"
#include <chrono>

namespace Profiler {
	using namespace std::chrono_literals;

	constexpr int IPC_TIMEOUT_MS = 1000;
	constexpr int IPC_BUFFER_SIZE = 768;
	constexpr int IPC_LINGER = 0;

	// Message signaling test start event
	const std::string TEST_START = "start:";
	const std::string TEST_END = "end:";

	Ipc::Ipc(Config* config, const std::function<void(std::string)>& testStartCallback, const std::function<void(std::string, std::string)>& testEndCallback, const std::function<void(std::string)>& errorCallback) :
		config(config),
		testStartCallback(testStartCallback),
		testEndCallback(testEndCallback),
		errorCallback(errorCallback),
		zmqContext(zmq_ctx_new()),
		handlerThread(std::make_unique<std::thread>(&Ipc::handlerThreadLoop, this))
	{
	}

	Ipc::~Ipc()
	{
		this->shutdown = true;
		if (this->handlerThread->joinable()) {
			this->handlerThread->join();
		}
		this->request("profiler_disconnected");
		if (this->zmqRequestSocket != nullptr) {
			zmq_close(this->zmqRequestSocket);
		}
		zmq_ctx_shutdown(this->zmqContext);
		zmq_ctx_term(this->zmqContext);
	}

	void Ipc::handlerThreadLoop() {
		std::string address = "";
		while (address.empty() && !this->shutdown) {
			std::string addressRequest = "register:" + std::to_string(GetCurrentProcessId());
			address = this->request(addressRequest);
			if (address.empty()) {
				std::this_thread::sleep_for(3000ms);
				logError("Connection failed, trying again.");
			}
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
	}

	std::string Ipc::getCurrentTestName()
	{
		std::string testnameRequest = "testname";
		return this->request(testnameRequest);
	}

	std::string Ipc::request(const std::string& message)
	{
		if (!initRequestSocket()) {
			return "";
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
			if (zmq_connect(this->zmqRequestSocket, this->config->getTiaRequestSocket().c_str()) == -1) {
				zmq_close(this->zmqRequestSocket);
				this->zmqRequestSocket = nullptr;
				logError("Failed connecting to request socket");
				return false;
			}
		}
		return true;
	}

	void Ipc::logError(const std::string& message) {
		std::string error = message + " (ZMQ Status: " + zmq_strerror(zmq_errno()) + ")";
		errorCallback(error);
	}

}

