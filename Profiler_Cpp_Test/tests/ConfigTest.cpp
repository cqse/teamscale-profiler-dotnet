#include <sstream>
#include "CppUnitTest.h"
#include "Config.h"

using namespace Microsoft::VisualStudio::CppUnitTestFramework;

TEST_CLASS(ConfigTest)
{
public:

	TEST_METHOD(EmptyConfigAndNoEnvironment)
	{
		Config config = parse(R"()", [](std::string suffix) -> std::string { return "";});

		Assert::AreEqual(size_t(0), config.getProblems().size(), L"number of problems");
		Assert::AreEqual(true, config.isEnabled(), L"default value should be enabled");
		Assert::AreEqual(false, config.shouldIgnoreExceptions(), L"default value should be to not ignore exceptions");
		Assert::AreEqual(size_t(0), config.getEagerness(), L"default value should be no eagerness");
	}

	TEST_METHOD(EnvironmentMustBeRespected)
	{
		Config config = parse(R"()", [](std::string suffix) -> std::string {
			if (StringUtils::uppercase(suffix) == "IGNORE_EXCEPTIONS") {
				return "1";
			}
			return "";
		});

		Assert::AreEqual(true, config.shouldIgnoreExceptions(), L"should use value from environment");
	}

	TEST_METHOD(ConfigFileMustBeRespected)
	{
		Config config = parse(R"(
match:
  - process: ".*"
    profiler:
      ignore_exceptions: true
)", [](std::string suffix) -> std::string { return ""; });

		Assert::AreEqual(true, config.shouldIgnoreExceptions(), L"should use value from config");
	}

	TEST_METHOD(OnlyMatchingSectionsAreApplied)
	{
		Config config = parse(R"(
match:
  - process: ".*doesnt-match"
    profiler:
      ignore_exceptions: true
)", [](std::string suffix) -> std::string { return ""; });

		Assert::AreEqual(false, config.shouldIgnoreExceptions(), L"should be the default value");
	}

	TEST_METHOD(EnvironmentTrumpsConfig)
	{
		Config config = parse(R"(
match:
  - process: ".*"
    profiler:
      targetdir: config
)", [](std::string suffix) -> std::string {
			if (StringUtils::uppercase(suffix) == "TARGETDIR") {
				return "env";
			}
			return "";
		});

		Assert::AreEqual(std::string("env"), config.getTargetDir(), L"should use value from environment");
	}

	TEST_METHOD(LastMatchWins)
	{
		Config config = parse(R"(
match:
  - process: ".*"
    profiler:
      targetdir: first
  - process: ".*"
    profiler:
      targetdir: last
)", [](std::string suffix) -> std::string { return ""; });

		Assert::AreEqual(std::string("last"), config.getTargetDir(), L"Should use value from last matching section");
	}

	TEST_METHOD(ConfigProblemsMustBeLoggable)
	{
		Config config = parse(R"(/$&)", [](std::string suffix) -> std::string { return ""; });

		Assert::AreEqual(size_t(1), config.getProblems().size(), L"number of problems");
	}

	TEST_METHOD(OldProcessSelectionMustMatchSuffixCaseInsensitively)
	{
		Config config = parse(R"(/$&)", [](std::string suffix) -> std::string {
			if (StringUtils::uppercase(suffix) == "PROCESS") {
				return "proGRam.Exe";
			}
			return "";
		});

		Assert::IsTrue(config.isEnabled(), L"must be enabled for program.exe");
	}

	TEST_METHOD(OldProcessSelectionMustIgnoreProcessesThatDontMatch)
	{
		Config config = parse(R"(/$&)", [](std::string suffix) -> std::string {
			if (StringUtils::uppercase(suffix) == "PROCESS") {
				return "doesnt-match";
			}
			return "";
		});

		Assert::IsTrue(config.isEnabled(), L"must not be enabled for program.exe");
	}

private:

	Config parse(std::string yaml, EnvironmentVariableReader* reader) {
		Config config(reader);
		std::stringstream stream(yaml);
		config.load(stream, "c:\\company\\program.exe");
		return config;
	}
};