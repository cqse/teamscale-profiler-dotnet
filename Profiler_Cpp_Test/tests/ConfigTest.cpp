#include <sstream>
#include "CppUnitTest.h"
#include "config/Config.h"

using namespace Microsoft::VisualStudio::CppUnitTestFramework;

TEST_CLASS(ConfigTest)
{
public:

	TEST_METHOD(EmptyConfigAndNoEnvironment)
	{
		Config config = parse(R"()", emptyEnvironment);

		Assert::AreEqual(size_t(0), config.getProblems().size(), L"number of problems");
		Assert::AreEqual(true, config.isProfilingEnabled(), L"default value should be enabled");
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
  - executablePathRegex: ".*"
    profiler:
      ignore_exceptions: true
)", emptyEnvironment);

		Assert::AreEqual(true, config.shouldIgnoreExceptions(), L"should use value from config");
	}

	TEST_METHOD(OnlyMatchingSectionsAreApplied)
	{
		Config config = parse(R"(
match:
  - executablePathRegex: ".*doesnt-match"
    profiler:
      ignore_exceptions: true
)", emptyEnvironment);

		Assert::AreEqual(false, config.shouldIgnoreExceptions(), L"should be the default value");
	}

	TEST_METHOD(NoProcessMatchingFieldMeansMatchAnyProcess)
	{
		Config config = parse(R"(
match:
  - profiler:
      ignore_exceptions: true
)", emptyEnvironment);

		Assert::AreEqual(true, config.shouldIgnoreExceptions(), L"should be the config value");
	}

	TEST_METHOD(MatchingExecutableNameMustBeCaseInsensitive)
	{
		Config config = parse(R"(
match:
  - executableName: ProGRAM.exE
    profiler:
      ignore_exceptions: true
)", emptyEnvironment);

		Assert::AreEqual(true, config.shouldIgnoreExceptions(), L"should be the config value");
	}

	TEST_METHOD(IfBothExecutableNameAndRegexAreGivenBothMustMatch)
	{
		Config config = parse(R"(
match:
  - executableName: program.exe
    executablePathRegex: .*doesnt-match
    profiler:
      ignore_exceptions: true
)", emptyEnvironment);

		Assert::AreEqual(false, config.shouldIgnoreExceptions(), L"case 1: should be the default value");

		config = parse(R"(
match:
  - executableName: doesnt-match.exe
    executablePathRegex: .*program.exe
    profiler:
      ignore_exceptions: true
)", emptyEnvironment);

		Assert::AreEqual(false, config.shouldIgnoreExceptions(), L"case 2: should be the default value");

		config = parse(R"(
match:
  - executableName: program.exe
    executablePathRegex: .*program.exe
    profiler:
      ignore_exceptions: true
)", emptyEnvironment);

		Assert::AreEqual(true, config.shouldIgnoreExceptions(), L"should be the config value");
	}

	TEST_METHOD(EnvironmentTrumpsConfig)
	{
		Config config = parse(R"(
match:
  - executablePathRegex: ".*"
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
  - executablePathRegex: ".*"
    profiler:
      targetdir: first
  - executablePathRegex: ".*"
    profiler:
      targetdir: last
)", emptyEnvironment);

		Assert::AreEqual(std::string("last"), config.getTargetDir(), L"Should use value from last matching section");
	}

	TEST_METHOD(MatchingPathSeparators)
	{
		Config config = parse(R"(
match:
  - executablePathRegex: .*\\program.exe
    profiler:
      targetdir: backward
  - executablePathRegex: .*/program.exe
    profiler:
      targetdir: forward
)", emptyEnvironment);

		Assert::AreEqual(std::string("backward"), config.getTargetDir(), L"Should match paths using backward slashes");
	}

	TEST_METHOD(ConfigProblemsMustBeLoggable)
	{
		Config config = parse(R"(/$&)", emptyEnvironment);

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

		Assert::IsTrue(config.isProfilingEnabled(), L"must be enabled for program.exe");
	}

	TEST_METHOD(OldProcessSelectionMustIgnoreProcessesThatDontMatch)
	{
		Config config = parse(R"()", [](std::string suffix) -> std::string {
			if (StringUtils::uppercase(suffix) == "PROCESS") {
				return "doesnt-match";
			}
			return "";
		});

		Assert::IsFalse(config.isProfilingEnabled(), L"must not be enabled for program.exe");
	}

	TEST_METHOD(MustNotThrowExceptionIfConfigFileDoesNotExist)
	{
		Config config = Config([](std::string suffix) -> std::string {
			if (StringUtils::uppercase(suffix) == "TARGETDIR") {
				return "env";
			}
			return "";
		});

		config.load("z:\\file\\that\\doesnt\\exist123.yml", "process.exe", true);

		Assert::AreEqual(std::string("env"), config.getTargetDir(), L"must still load environment");
		Assert::AreEqual(size_t(1), config.getProblems().size(), L"must log a problem for the nonexisting file");
	}

private:

	Config parse(std::string yaml, EnvironmentVariableReader* reader) {
		Config config(reader);
		std::stringstream stream(yaml);
		config.load(stream, "c:\\company\\program.exe");
		return config;
	}

	static std::string emptyEnvironment(std::string suffix) {
		return "";
	};
};