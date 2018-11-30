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
		Assert::IsTrue(file.sections.empty());
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
		Assert::AreEqual(1, (int)file.sections.size());
	}

private:

	ConfigFile parse(std::string content) {
		return ConfigParser::parse(std::stringstream(content));
	}
};