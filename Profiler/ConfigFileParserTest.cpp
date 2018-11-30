#include <sstream>
#include "CppUnitTest.h"
#include "ConfigParser.h"

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

	TEST_METHOD(ValidSection)
	{
		ConfigFile file = parse(R"(
match:
  "foo.*":
    enabled: "0"
)");
		Assert::AreEqual(1, (int)file.sections.size(), L"number of sections");
		Assert::IsTrue(std::regex_match("foobar", file.sections[0].processRegex), L"expecting regex to match");
		Assert::AreEqual("0", file.sections[0].options["enabled"].c_str(), L"enabled option");
	}

	TEST_METHOD(ConfigOptionsCaseInsensitive)
	{
		ConfigFile file = parse(R"(
match:
  "foo.*":
    ENablED: "1"
)");
		Assert::AreEqual(1, (int)file.sections.size(), L"number of sections");
		Assert::AreEqual("1", file.sections[0].options["enabled"].c_str(), L"enabled option");
	}

	TEST_METHOD(SectionOrderPreserved)
	{
		ConfigFile file = parse(R"(
match:
  foo:
  zzz:
  "bar":
  "__123":
  zzz:
)");
		Assert::AreEqual(5, (int)file.sections.size(), L"number of sections");
		Assert::IsTrue(std::regex_match("foo", file.sections[0].processRegex), L"expecting section 0 match");
		Assert::IsTrue(std::regex_match("zzz", file.sections[1].processRegex), L"expecting section 1 match");
		Assert::IsTrue(std::regex_match("bar", file.sections[2].processRegex), L"expecting section 2 match");
		Assert::IsTrue(std::regex_match("__123", file.sections[3].processRegex), L"expecting section 3 match");
		Assert::IsTrue(std::regex_match("__123", file.sections[3].processRegex), L"expecting section 4 match");
	}

	TEST_METHOD(TypeConversion)
	{
		ConfigFile file = parse(R"(
match:
  "foo.*":
    enabled: true
    enabled2: 1
)");
		Assert::AreEqual(1, (int)file.sections.size(), L"number of sections");
		Assert::AreEqual("true", file.sections[0].options["enabled"].c_str(), L"enabled option");
		Assert::AreEqual("1", file.sections[0].options["enabled2"].c_str(), L"enabled option");
	}

private:

	ConfigFile parse(std::string content) {
		return ConfigParser::parse(std::stringstream(content));
	}
};