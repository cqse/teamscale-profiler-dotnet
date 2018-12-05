#include <sstream>
#include "CppUnitTest.h"
#include "StringUtils.h"

using namespace Microsoft::VisualStudio::CppUnitTestFramework;

TEST_CLASS(StringUtilsTest)
{
public:

	TEST_METHOD(PathMustEndWithProgramName)
	{
		Assert::IsTrue(StringUtils::endsWithCaseInsensitive("\\VBOXSVR\\proj\\teamscale-profiler-dotnet\\test-data\\test-programs\\ProfilerTestee.exe", "profilerTesTEE.EXE"));
	}

	TEST_METHOD(SimpleTestForEndsWith)
	{
		Assert::IsTrue(StringUtils::endsWithCaseInsensitive("foobar", "bar"));
	}

	TEST_METHOD(SimpleTestForEndsWithCaseInsensitive)
	{
		Assert::IsTrue(StringUtils::endsWithCaseInsensitive("foobar", "Bar"));
	}

	TEST_METHOD(Uppercase)
	{
		Assert::AreEqual(std::string("BLA\\BLU_"), StringUtils::uppercase("bla\\Blu_"));
	}
};