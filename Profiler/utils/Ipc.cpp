#ifdef TIA
#include "Ipc.h"

#include <zmq.h>

#define IPC_TIMEOUT_MS 250
#define IPC_BUFFER_SIZE 255

// ZMQ strings are not zero-terminated, let's terminate these
#define IPC_TERMINATE_STRING(buffer, len) buffer[len < IPC_BUFFER_SIZE ? len : IPC_BUFFER_SIZE - 1] = '\0'

Ipc::Ipc(Config* config, std::function<void(std::string)> testChangedCallback)
{
	this->config = config;
	this->testChangedCallback = testChangedCallback;
	this->zmqTimeout = IPC_TIMEOUT_MS;
	this->zmqContext = zmq_ctx_new();

	this->handlerThread = new std::thread(&Ipc::handlerThreadLoop, this);

	this->request("profiler_connected");
}

Ipc::~Ipc()
{
	this->shutdown = true;
	this->handlerThread->join();

	this->request("profiler_disconnected");
	if (this->zmqRequestSocket != NULL) {
		zmq_close(this->zmqRequestSocket);
	}

	zmq_ctx_destroy(this->zmqContext);
}

void Ipc::handlerThreadLoop() {
	this->zmqSubscribeSocket = zmq_socket(this->zmqContext, ZMQ_SUB);
	zmq_setsockopt(this->zmqSubscribeSocket, ZMQ_SUBSCRIBE, "", 0);
	zmq_setsockopt(this->zmqSubscribeSocket, ZMQ_RCVTIMEO, &zmqTimeout, sizeof(zmqTimeout));
	zmq_connect(this->zmqSubscribeSocket, config->getTiaSubscribeSocket().c_str());
	char buffer[IPC_BUFFER_SIZE];
	while (!this->shutdown) {
		int len = zmq_recv(this->zmqSubscribeSocket, &buffer, IPC_BUFFER_SIZE - 1, 0);
		if (len >= 0) {
			IPC_TERMINATE_STRING(buffer, len);
			this->testChangedCallback(buffer);
		}
	}
	zmq_close(this->zmqSubscribeSocket);
}

std::string Ipc::getCurrentTestName()
{
	return this->request("get_testname");
}

std::string Ipc::request(std::string message)
{
	initRequestSocket();

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

void Ipc::initRequestSocket() {
	if (this->zmqRequestSocket == NULL) {
		this->zmqRequestSocket = zmq_socket(this->zmqContext, ZMQ_REQ);
		zmq_setsockopt(this->zmqRequestSocket, ZMQ_RCVTIMEO, &zmqTimeout, sizeof(zmqTimeout));
		zmq_setsockopt(this->zmqRequestSocket, ZMQ_LINGER, &zmqTimeout, sizeof(zmqTimeout));
		zmq_connect(this->zmqRequestSocket, this->config->getTiaRequestSocket().c_str());
	}
}
#endif
