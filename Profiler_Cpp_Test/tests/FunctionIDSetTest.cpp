#include <sstream>
#include "CppUnitTest.h"
#include <chrono>
#include <cor.h>
#include <corprof.h>
#include "utils/functionID_set/functionID_set.h"
#include <iostream>
#include <set>
#include <vector>

using namespace Microsoft::VisualStudio::CppUnitTestFramework;

TEST_CLASS(FunctionIDSetTest)
{
public:
	TEST_METHOD(SetPerformanceTest)
	{
		std::vector<int> vec;
		for (int i = 0; i < 10'000'000; i++) {
			vec.push_back((std::rand() + 1) * (std::rand() + 1));
		}
		std::vector<int> vec2;
		for (int i = 0; i < 10'000'000; i++) {
			vec.push_back((std::rand() + 1) * (std::rand() + 1));
		}
		functionID_set testSet;
		std::chrono::steady_clock::time_point begin = std::chrono::steady_clock::now();
		for (int i : vec) {
			testSet.insert(i);
		}
		int matches = 0;
		for (int i : vec2) {
			matches += testSet.contains(i);
		}
		std::chrono::steady_clock::time_point end = std::chrono::steady_clock::now();
		std::string message = "Time difference = " + std::to_string(std::chrono::duration_cast<std::chrono::microseconds>(end - begin).count()) + " [mikrosekunden]\n";
		std::string message2 = "Matches: " + std::to_string(matches) + "\n";
		Logger::WriteMessage(message.c_str());
		Logger::WriteMessage(message2.c_str());

		std::set<FunctionID> testSet2;
		std::chrono::steady_clock::time_point begin2 = std::chrono::steady_clock::now();
		for (int i : vec) {
			testSet2.insert(i);
		}
		int matches2 = 0;
		for (int i : vec2) {
			matches2 += testSet2.find(i) != testSet2.end();
		}
		std::chrono::steady_clock::time_point end2 = std::chrono::steady_clock::now();
		std::string message3 = "Time difference = " + std::to_string(std::chrono::duration_cast<std::chrono::microseconds>(end2 - begin2).count()) + " [mikrosekunden]\n";
		std::string message4 = "Matches: " + std::to_string(matches2) + "\n";
		Logger::WriteMessage(message3.c_str());
		Logger::WriteMessage(message4.c_str());
	}
};
