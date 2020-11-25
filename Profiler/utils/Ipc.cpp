#ifdef TIA
#include "Ipc.h"

#include <zmq.h>

#define IPC_TIMEOUT_MS 250
#define IPC_BUFFER_SIZE 255

// ZMQ strings are not zero-terminated, let's terminate these
#define IPC_TERMINATE_STRING(buffer, len) buffer[len < IPC_BUFFER_SIZE ? len : IPC_BUFFER_SIZE - 1] = '\0'

Ipc::Ipc(Config* config, std::function<void(std::string)> testChangedCallback)
{
	this->testChangedCallback = testChangedCallback;
	this->zmqContext = zmq_ctx_new();

	this->handlerThread = new std::thread(&Ipc::handlerThreadLoop, this, config);

	this->zmqRequestSocket = zmq_socket(this->zmqContext, ZMQ_REQ);
	zmq_connect(this->zmqRequestSocket, config->getTiaRequestSocket().c_str());
	this->request("profiler_connected");
}

Ipc::~Ipc()
{
	this->shutdown = true;
	this->handlerThread->join();

	this->request("profiler_disconnected");
	zmq_close(this->zmqRequestSocket);
	zmq_ctx_destroy(this->zmqContext);
}

void Ipc::handlerThreadLoop(Config* config) {
	this->zmqSubscribeSocket = zmq_socket(this->zmqContext, ZMQ_SUB);
	int timeout = IPC_TIMEOUT_MS;
	zmq_setsockopt(this->zmqSubscribeSocket, ZMQ_SUBSCRIBE, "", 0);
	zmq_setsockopt(this->zmqSubscribeSocket, ZMQ_RCVTIMEO, &timeout, sizeof(timeout));
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

char* Ipc::getCurrentTestName()
{
	return this->request("get_testname");
}

char* Ipc::request(const char* message)
{
	char* buffer = new char[IPC_BUFFER_SIZE];
	zmq_send(this->zmqRequestSocket, message, strlen(message), 0);
	int len = zmq_recv(this->zmqRequestSocket, buffer, IPC_BUFFER_SIZE - 1, 0);
	if (len < 0) {
		return NULL;
	}

	IPC_TERMINATE_STRING(buffer, len);
	return buffer;
}
#endif
