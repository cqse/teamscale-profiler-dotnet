#ifdef TIA
#include "Ipc.h"

#include <zmq.h>

Ipc::Ipc(Config* config)
{
	this->zmqContext = zmq_ctx_new();
	this->zmqRequestSocket = zmq_socket(this->zmqContext, ZMQ_REQ);
	this->zmqSubscribeSocket = zmq_socket(this->zmqContext, ZMQ_SUB);

	zmq_connect(this->zmqRequestSocket, config->getTiaRequestSocket().c_str());
	zmq_connect(this->zmqSubscribeSocket, config->getTiaSubscribeSocket().c_str());

	this->request("profiler_connected");
}

Ipc::~Ipc()
{
	this->request("profiler_disconnected");
	zmq_close(this->zmqSubscribeSocket);
	zmq_close(this->zmqRequestSocket);
	zmq_ctx_destroy(this->zmqContext);
}

char* Ipc::getCurrentTestName()
{
	return this->request("get_testname");
}

char* Ipc::request(const char* message)
{
	int bufferSize = 256;
	char* buffer = new char[bufferSize];
	zmq_send(this->zmqRequestSocket, message, strlen(message), 0);
	int len = zmq_recv(this->zmqRequestSocket, buffer, bufferSize - 1, 0);
	// terminate string with zero
	buffer[len < bufferSize ? len : bufferSize - 1] = '\0';
	return buffer;
}
#endif
