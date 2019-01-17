#include <sstream>
#include "CppUnitTest.h"
#include "config/ConfigParser.h"

using namespace Microsoft::VisualStudio::CppUnitTestFramework;

TEST_CLASS(ConfigParserTest)
{
public:

	TEST_METHOD(ValidEmptyConfig)
	{
		ConfigFile file = parse(R"()");
		Assert::IsTrue(file.sections.empty(), L"expecting no sections");
	}

	TEST_METHOD(InvalidYamlFile)
	{
		Assert::ExpectException<ConfigParsingException>([this] {
			parse(R"(/$&)");
		});
	}

	TEST_METHOD(InvalidType)
	{
		Assert::ExpectException<ConfigParsingException>([this] {
			parse(R"(
match:
  - executablePathRegex: "foo.*"
    profiler:
      - invalidEntry1
      - invalidEntry2
)");
		});
	}

	TEST_METHOD(ValidSection)
	{
		ConfigFile file = parse(R"(
match:
  - executablePathRegex: "foo.*"
    profiler:
      enabled: "0"
)");
		Assert::AreEqual(1, (int)file.sections.size(), L"number of sections");
		Assert::IsTrue(std::regex_match("foobar", file.sections[0].executablePathRegex), L"expecting regex to match");
		Assert::AreEqual("0", file.sections[0].profilerOptions["enabled"].c_str(), L"enabled option");
	}

	TEST_METHOD(ConfigOptionsCaseInsensitive)
	{
		ConfigFile file = parse(R"(
match:
  - executablePathRegex: "foo.*"
    profiler:
      ENablED: "1"
)");
		Assert::AreEqual(1, (int)file.sections.size(), L"number of sections");
		Assert::AreEqual("1", file.sections[0].profilerOptions["enabled"].c_str(), L"enabled option");
	}

	TEST_METHOD(TypeConversion)
	{
		ConfigFile file = parse(R"(
match:
  - executablePathRegex: "foo.*"
    profiler:
      enabled: true
      enabled2: 1
)");
		Assert::AreEqual(1, (int)file.sections.size(), L"number of sections");
		Assert::AreEqual("true", file.sections[0].profilerOptions["enabled"].c_str(), L"enabled option");
		Assert::AreEqual("1", file.sections[0].profilerOptions["enabled2"].c_str(), L"enabled option");
	}

	TEST_METHOD(FileNameAttributeAccepted)
	{
		ConfigFile file = parse(R"(
match:
  - executableName: foo.exe
)");
		Assert::AreEqual(1, (int)file.sections.size(), L"number of sections");
		Assert::AreEqual("foo.exe", file.sections[0].caseInsensitiveExecutableName.c_str(), L"executable name");
	}

private:

	ConfigFile parse(std::string content) {
		return ConfigParser::parse(std::stringstream(content));
	}
};