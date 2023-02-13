#include <sstream>
#include "CppUnitTest.h"
#include <chrono>
#include <cor.h>
#include <corprof.h>
#include "utils/functionID_set/functionID_set.h"
#include <iostream>
#include <set>

using namespace Microsoft::VisualStudio::CppUnitTestFramework;

TEST_CLASS(FunctionIDSetTest)
{
public:
	TEST_METHOD(SetPerformanceTest)
	{
		functionID_set testSet;
		std::chrono::steady_clock::time_point begin = std::chrono::steady_clock::now();
		for (unsigned int i = 0; i < 100'000'000; i++) {
			testSet.insert(std::rand());
		}
		std::chrono::steady_clock::time_point end = std::chrono::steady_clock::now();
		std::string message = "Time difference = " + std::to_string(std::chrono::duration_cast<std::chrono::microseconds>(end - begin).count()) + "[mikrosekunden]";
		Logger::WriteMessage(message.c_str());

		std::set<FunctionID> testSet2;
		std::chrono::steady_clock::time_point begin2 = std::chrono::steady_clock::now();
		for (unsigned int i = 0; i < 100'000'000; i++) {
			testSet2.insert(std::rand());
		}
		std::chrono::steady_clock::time_point end2 = std::chrono::steady_clock::now();
		std::string message2 = "Time difference = " + std::to_string(std::chrono::duration_cast<std::chrono::microseconds>(end2 - begin2).count()) + "[mikrosekunden]";
		Logger::WriteMessage(message2.c_str());
	}
};
