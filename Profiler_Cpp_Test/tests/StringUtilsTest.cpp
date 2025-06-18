#include <sstream>
#include "CppUnitTest.h"
#include "utils/StringUtils.h"

using namespace Profiler;
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

	TEST_METHOD(EqualsIgnoreCase)
	{
		Assert::IsTrue(StringUtils::equalsIgnoreCase("foo?1\\", "FoO?1\\"));
		Assert::IsTrue(StringUtils::equalsIgnoreCase("foo", "foo"));
		Assert::IsFalse(StringUtils::equalsIgnoreCase("foo", "bar"));
	}

	TEST_METHOD(LastPartOfPath)
	{
		Assert::AreEqual(std::string("test.exe"), StringUtils::getLastPartOfPath("C:\\foo\\bar\\test.exe"));
		Assert::AreEqual(std::string("bar"), StringUtils::getLastPartOfPath("C:\\foo\\bar"));
	}

	TEST_METHOD(RemoveLastPartOfPath)
	{
		Assert::AreEqual(std::string("C:\\foo\\bar"), StringUtils::removeLastPartOfPath("C:\\foo\\bar\\test.exe"));
		Assert::AreEqual(std::string("C:\\foo"), StringUtils::removeLastPartOfPath("C:\\foo\\bar"));
	}
};