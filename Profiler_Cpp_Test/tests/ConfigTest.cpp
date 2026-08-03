#include <sstream>
#include <vector>
#include "CppUnitTest.h"
#include "config/Config.h"

using namespace Profiler;
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
		Assert::AreEqual(true, config.shouldUseLightMode(), L"should use light mode");
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

	TEST_METHOD(UseLightModeByDefault)
	{
		Config config = parse("", emptyEnvironment);

		Assert::AreEqual(true, config.shouldUseLightMode(), L"should use light mode");
	}

	TEST_METHOD(DisableLightModeInConfigFile)
	{
		Config config = parse(R"(
match:
  - executablePathRegex: ".*"
    profiler:
        light_mode: false
)", emptyEnvironment);

		Assert::AreEqual(false, config.shouldUseLightMode(), L"should not use light mode when overwritten in config file");
	}

	TEST_METHOD(MustWarnAboutUnknownProfilerOption)
	{
		Config config = parse(R"(
match:
  - executablePathRegex: ".*"
    profiler:
      light_mdoe: false
)", emptyEnvironment);

		Assert::AreEqual(size_t(1), config.getWarnings().size(), L"number of warnings");
		Assert::AreEqual(size_t(0), config.getProblems().size(), L"a misspelled option must not be fatal");
		Assert::AreEqual(true, config.shouldUseLightMode(), L"the misspelled option must still be ignored");
	}

	TEST_METHOD(MustNotWarnAboutKnownProfilerOptions)
	{
		Config config = parse(R"(
match:
  - executablePathRegex: ".*"
    profiler:
      LIGHT_MODE: false
      Ignore_Exceptions: true
)", emptyEnvironment);

		Assert::AreEqual(size_t(0), config.getWarnings().size(), L"options must be recognized regardless of their casing");
		Assert::AreEqual(false, config.shouldUseLightMode(), L"should not use light mode");
	}

	TEST_METHOD(MustWarnOnlyOnceAboutTheSameUnknownOption)
	{
		Config config = parse(R"(
match:
  - executablePathRegex: ".*"
    profiler:
      light_mdoe: false
  - executablePathRegex: ".*\\program.exe"
    profiler:
      light_mdoe: true
)", emptyEnvironment);

		Assert::AreEqual(size_t(1), config.getWarnings().size(), L"the same option must not be reported per section");
	}

	TEST_METHOD(MustNotWarnAboutUnknownOptionsInNonMatchingSections)
	{
		Config config = parse(R"(
match:
  - executableName: "other.exe"
    profiler:
      light_mdoe: false
)", emptyEnvironment);

		Assert::AreEqual(size_t(0), config.getWarnings().size(), L"sections of other processes must not be inspected");
	}

	TEST_METHOD(MustNotWarnAboutUploaderSection)
	{
		// The uploader section is consumed by the UploadDaemon and not by the profiler,
		// so none of its options may be reported as unknown.
		Config config = parse(R"(
match:
  - executablePathRegex: ".*"
    profiler:
      light_mode: false
    uploader:
      pdbDirectory: '@AssemblyDir'
      revisionFile: '@AssemblyDir\revision.txt'
)", emptyEnvironment);

		Assert::AreEqual(size_t(0), config.getWarnings().size(), L"the uploader section must not be inspected");
		Assert::AreEqual(false, config.shouldUseLightMode(), L"the profiler section must still be applied");
	}

	TEST_METHOD(AllSupportedOptionsMustBeRecognized)
	{
		// This list documents all options the profiler supports. It is deliberately duplicated here and
		// not in the profiler itself: the profiler derives the supported options from the ones it queries,
		// so it can never go stale. This test detects when an option is no longer queried unconditionally
		// from Config::setOptions, which would make the profiler warn about a perfectly valid option.
		const std::vector<std::string> supportedOptions = {
			"targetdir", "enabled", "light_mode", "assembly_file_version", "assembly_paths",
			"dump_environment", "ignore_exceptions", "upload_daemon", "tga", "tia",
			"tia_request_socket", "eagerness"
		};

		std::stringstream yaml;
		yaml << "match:\n  - executablePathRegex: \".*\"\n    profiler:\n";
		for (const std::string& option : supportedOptions) {
			yaml << "      " << option << ": \"1\"\n";
		}

		Config config = parse(yaml.str(), emptyEnvironment);

		Assert::AreEqual(size_t(0), config.getWarnings().size(), describe(config.getWarnings()).c_str());
	}

private:

	/** Turns the given messages into an assertion message so a failure names the offending options. */
	static std::wstring describe(const std::vector<std::string>& messages) {
		std::string description = "unexpected warnings:";
		for (const std::string& message : messages) {
			description += " " + message;
		}
		return std::wstring(description.begin(), description.end());
	}

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