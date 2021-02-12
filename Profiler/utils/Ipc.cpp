#ifdef TIA
#include "Ipc.h"

#include <zmq.h>

#define IPC_TIMEOUT_MS 250
#define IPC_BUFFER_SIZE 255

// ZMQ strings are not zero-terminated, let's terminate these
#define IPC_TERMINATE_STRING(buffer, len) buffer[len < IPC_BUFFER_SIZE ? len : IPC_BUFFER_SIZE - 1] = '\0'

Ipc::Ipc(Config* config, std::function<void(std::string)> testStartCallback, std::function<void(std::string, std::string)> testEndCallback, std::function<void(std::string)> errorCallback)
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

	zmq_ctx_destroy(this->zmqContext);
}

void Ipc::handlerThreadLoop() {
	this->zmqSubscribeSocket = zmq_socket(this->zmqContext, ZMQ_SUB);
	zmq_setsockopt(this->zmqSubscribeSocket, ZMQ_SUBSCRIBE, "test:", 0);
	zmq_setsockopt(this->zmqSubscribeSocket, ZMQ_RCVTIMEO, &zmqTimeout, sizeof(zmqTimeout));
	if (!!zmq_connect(this->zmqSubscribeSocket, config->getTiaSubscribeSocket().c_str())) {
		zmq_close(this->zmqSubscribeSocket);
		this->zmqSubscribeSocket = NULL;
		logError("Failed connecting to subscribe socket");
		return;
	}

	/// Alternative way to read messages
	//char buffer[IPC_BUFFER_SLOTS][IPC_BUFFER_SIZE];
	//while (!this->shutdown) {
	//	int len = zmq_recv(this->zmqSubscribeSocket, &buffer[0], IPC_BUFFER_SIZE - 1, 0);
	//	if (len >= 0) {
	//		IPC_TERMINATE_STRING(buffer[0], len);
	//		this->testStartCallback(buffer[0]);
	//	}
	//}

	std::vector<std::string> frames;
	zmq_msg_t message;
	while (!this->shutdown) {
		zmq_msg_init(&message);
		do {
			int len = zmq_msg_recv(&message, this->zmqSubscribeSocket, 0);
			if (len >= 0) {
				frames.push_back(std::string((char*)zmq_msg_data(&message), zmq_msg_size(&message)));
			}
		} while (zmq_msg_more(&message));

		if (!frames.empty()) {
			handleMessage(frames);
			frames.clear();
		}
		zmq_msg_close(&message);
	}
	zmq_close(this->zmqSubscribeSocket);
}

void Ipc::handleMessage(std::vector<std::string> frames) {
	if (frames[0] == "test:start" && frames.size() > 1) {
		this->testStartCallback(frames[1]);
	}
	else if (frames[0] == "test:end") {
		std::string result = "";
		if (frames.size() > 1) {
			result = frames[1];
		}

		std::string message = "";
		if (frames.size() > 2) {
			message = frames[2];
		}

		this->testEndCallback(result, message);
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
#endif
