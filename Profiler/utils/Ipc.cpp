#include "Ipc.h"
#include "zmq.h"
#include <chrono>

using namespace std::chrono_literals;

#define IPC_TIMEOUT_MS 1000
#define IPC_BUFFER_SIZE 768
#define IPC_LINGER 0

// ZMQ strings are not zero-terminated, let's terminate these
#define IPC_TERMINATE_STRING(buffer, len) buffer[len < IPC_BUFFER_SIZE ? len : IPC_BUFFER_SIZE - 1] = '\0'

// Message signaling test start event
const std::string TEST_START = "start:";
const std::string TEST_END = "end:";

Ipc::Ipc(Config* config, std::function<void(std::string)> testStartCallback, std::function<void(std::string, std::string)> testEndCallback, std::function<void(std::string)> errorCallback)
{
	this->config = config;
	this->testStartCallback = testStartCallback;
	this->testEndCallback = testEndCallback;
	this->errorCallback = errorCallback;
	this->zmqTimeout = IPC_TIMEOUT_MS;
	this->zmqLinger = IPC_LINGER;
	this->zmqContext = zmq_ctx_new();

	this->handlerThread = new std::thread(&Ipc::handlerThreadLoop, this);
}

Ipc::~Ipc()
{
	this->shutdown = true;
	if (this->handlerThread->joinable()) {
		this->handlerThread->join();
	}
	this->request("profiler_disconnected");
	if (this->zmqRequestSocket != NULL) {
		zmq_close(this->zmqRequestSocket);
	}
	zmq_ctx_shutdown(this->zmqContext);
	zmq_ctx_term(this->zmqContext);
}

void Ipc::handlerThreadLoop() {
	std::string address = "";
	while (address == "" && !this->shutdown) {
		address = this->request("register:" + std::to_string(GetCurrentProcessId()));
		if (address == "") {
			std::this_thread::sleep_for(3000ms);
			logError("Connection failed, trying again.");
		}
	}
	handleMessage(getCurrentTestName());

	this->zmqReplySocket = zmq_socket(this->zmqContext, ZMQ_REP);
	zmq_setsockopt(this->zmqReplySocket, ZMQ_RCVTIMEO, &zmqTimeout, sizeof(zmqTimeout));
	zmq_setsockopt(this->zmqReplySocket, ZMQ_LINGER, &zmqLinger, sizeof(zmqLinger));

	if (!!zmq_bind(this->zmqReplySocket, address.c_str())) {
		zmq_close(this->zmqReplySocket);
		this->zmqReplySocket = NULL;
		logError("Failed connecting to subscribe socket");
		return;
	}

	while (!this->shutdown) {
		char buf[IPC_BUFFER_SIZE];
		int len = zmq_recv(this->zmqReplySocket, &buf, IPC_BUFFER_SIZE, 0);
		if (len != -1) {
			IPC_TERMINATE_STRING(buf, len);
			std::string message(buf);
			handleMessage(message);
			zmq_send(this->zmqReplySocket, "ack", 3, 0);
		}
	}
	zmq_close(this->zmqReplySocket);
}

void Ipc::handleMessage(std::string message) {
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
	return this->request("testname");
}

std::string Ipc::request(std::string message)
{
	if (!initRequestSocket()) {
		return "";
	}

	char buffer[IPC_BUFFER_SIZE];
	zmq_send(this->zmqRequestSocket, message.c_str(), message.length(), 0);
	int len = zmq_recv(this->zmqRequestSocket, &buffer, IPC_BUFFER_SIZE - 1, 0);
	if (len < 0) {
		zmq_close(this->zmqRequestSocket);
		this->zmqRequestSocket = NULL;
		return "";
	}
	
	IPC_TERMINATE_STRING(buffer, len);
	return std::string(buffer);
}

bool Ipc::initRequestSocket() {
	if (this->zmqRequestSocket == NULL) {
		this->zmqRequestSocket = zmq_socket(this->zmqContext, ZMQ_REQ);
		zmq_setsockopt(this->zmqRequestSocket, ZMQ_RCVTIMEO, &zmqTimeout, sizeof(zmqTimeout));
		zmq_setsockopt(this->zmqRequestSocket, ZMQ_LINGER, &zmqTimeout, sizeof(zmqTimeout));
		if (!!zmq_connect(this->zmqRequestSocket, this->config->getTiaRequestSocket().c_str())) {
			zmq_close(this->zmqRequestSocket);
			this->zmqRequestSocket = NULL;
			logError("Failed connecting to request socket");
			return false;
		}
	}
	return true;
}

void Ipc::logError(std::string message) {
	std::string error = message + " (ZMQ Status: " + zmq_strerror(zmq_errno()) + ")";
	errorCallback(error);
}
