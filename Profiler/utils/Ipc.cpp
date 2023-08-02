#include "Ipc.h"
#include "zmq.h"

#define IPC_TIMEOUT_MS 1000
#define IPC_BUFFER_SIZE 255

// ZMQ strings are not zero-terminated, let's terminate these
// TODO: Profiler_disconnected is missing!
#define IPC_TERMINATE_STRING(buffer, len) buffer[len < IPC_BUFFER_SIZE ? len : IPC_BUFFER_SIZE - 1] = '\0'

// Message signaling test start event
const std::string TEST_START = "start";
const std::string TEST_END = "end";

Ipc::Ipc(Config* config, std::function<void(std::string)> testStartCallback, std::function<void(std::string)> testEndCallback, std::function<void(std::string)> errorCallback)
{
	this->config = config;
	this->testStartCallback = testStartCallback;
	this->testEndCallback = testEndCallback;
	this->errorCallback = errorCallback;
	this->zmqTimeout = IPC_TIMEOUT_MS;
	this->zmqContext = zmq_ctx_new();

	this->handlerThread = new std::thread(&Ipc::handlerThreadLoop, this);

	this->request("profiler_connected");
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
	this->zmqReplySocket = zmq_socket(this->zmqContext, ZMQ_REP);
	zmq_setsockopt(this->zmqReplySocket, ZMQ_RCVTIMEO, &zmqTimeout, sizeof(zmqTimeout));
	zmq_setsockopt(this->zmqReplySocket, ZMQ_LINGER, 0, sizeof(0));

	if (!!zmq_bind(this->zmqReplySocket, config->getTiaSubscribeSocket().c_str())) {
		zmq_close(this->zmqReplySocket);
		this->zmqReplySocket = NULL;
		logError("Failed connecting to subscribe socket");
		return;
	}

	std::string message;
	while (!this->shutdown) {
		char buf[IPC_BUFFER_SIZE];
		int bytes = zmq_recv(this->zmqReplySocket, &buf, IPC_BUFFER_SIZE, 0);
		
		if (bytes != -1) {
			IPC_TERMINATE_STRING(buf, bytes);
			handleMessage(message);
		}
	}
	zmq_close(this->zmqReplySocket);
} 

void Ipc::handleMessage(std::string message) {
	if (message.find(TEST_START) == 0) {
		this->testStartCallback(message.substr(TEST_START.length()));
	}
	else if (message.find(TEST_END) == 0) {

		this->testEndCallback(message.substr(TEST_END.length()));
	}
}

std::string Ipc::getCurrentTestName()
{
	return this->request("get_testname");
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
	return buffer;
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
	std::string error = message + " (ZMQ error: " + zmq_strerror(zmq_errno()) + ")";
	errorCallback(error);
}
