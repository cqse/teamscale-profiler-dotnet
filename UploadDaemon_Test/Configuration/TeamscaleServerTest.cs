using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace UploadDaemon.Configuration
{
    [TestFixture]
    public class TeamscaleServerTest
    {
        private const string YAML_USER = "yaml-user";
        private const string YAML_ACCESS_KEY = "yaml-access-key";
        private const string ENVIRONMENT_USER = "environment-user";
        private const string ENVIRONMENT_ACCESS_KEY = "environment-access-key";

        private string originalUsername;
        private string originalAccessKey;

        /// <summary>
        /// The environment variables are process-global, so we must remember whatever the
        /// developer/build machine has set and start from a clean slate in every test.
        /// </summary>
        [SetUp]
        public void ClearEnvironmentVariables()
        {
            originalUsername = Environment.GetEnvironmentVariable(TeamscaleServer.USERNAME_ENVIRONMENT_VARIABLE);
            originalAccessKey = Environment.GetEnvironmentVariable(TeamscaleServer.ACCESS_KEY_ENVIRONMENT_VARIABLE);
            SetEnvironmentVariables(null, null);
        }

        [TearDown]
        public void RestoreEnvironmentVariables()
        {
            SetEnvironmentVariables(originalUsername, originalAccessKey);
        }

        [Test]
        public void EnvironmentVariablesProvideTheCredentialsIfTheYamlDoesNotContainThem()
        {
            SetEnvironmentVariables(ENVIRONMENT_USER, ENVIRONMENT_ACCESS_KEY);

            TeamscaleServer server = ServerWithoutCredentials();
            Assert.Multiple(() =>
            {
                Assert.That(server.Username, Is.EqualTo(ENVIRONMENT_USER), "username");
                Assert.That(server.AccessKey, Is.EqualTo(ENVIRONMENT_ACCESS_KEY), "access key");
            });

            IEnumerable<string> errors = server.Validate();
            Assert.That(errors, Is.Empty, "credentials from the environment must not raise any errors");
        }

        [Test]
        public void EnvironmentVariablesTakePrecedenceOverTheYamlProperties()
        {
            SetEnvironmentVariables(ENVIRONMENT_USER, ENVIRONMENT_ACCESS_KEY);

            TeamscaleServer server = ServerWithCredentials();

            Assert.Multiple(() =>
            {
                Assert.That(server.Username, Is.EqualTo(ENVIRONMENT_USER), "username");
                Assert.That(server.AccessKey, Is.EqualTo(ENVIRONMENT_ACCESS_KEY), "access key");
            });
        }

        [Test]
        public void BlankEnvironmentVariablesAreTreatedAsUnset()
        {
            SetEnvironmentVariables("", "   ");

            TeamscaleServer server = ServerWithCredentials();

            Assert.Multiple(() =>
            {
                Assert.That(server.Username, Is.EqualTo(YAML_USER), "username");
                Assert.That(server.AccessKey, Is.EqualTo(YAML_ACCESS_KEY), "access key");
            });
        }

        [Test]
        public void YamlWithoutCredentialsIsInvalidIfTheEnvironmentVariablesAreNotSet()
        {
            Exception exception = Assert.Throws<Config.InvalidConfigException>(() => Config.Read(@"
                match:
                  - profiler:
                      targetdir: C:\test
                    uploader:
                      pdbDirectory: C:\pdbs
                      revisionFile: C:\revision
                      teamscale:
                        url: url
                        project: project
                        partition: partition
            ").CreateConfigForProcess("foo.exe"));

            Assert.Multiple(() =>
            {
                Assert.That(exception.Message, Does.Contain(TeamscaleServer.USERNAME_ENVIRONMENT_VARIABLE), "username error hints at the environment variable");
                Assert.That(exception.Message, Does.Contain(TeamscaleServer.ACCESS_KEY_ENVIRONMENT_VARIABLE), "access key error hints at the environment variable");
            });
        }

        private static void SetEnvironmentVariables(string username, string accessKey)
        {
            Environment.SetEnvironmentVariable(TeamscaleServer.USERNAME_ENVIRONMENT_VARIABLE, username);
            Environment.SetEnvironmentVariable(TeamscaleServer.ACCESS_KEY_ENVIRONMENT_VARIABLE, accessKey);
        }

        /// <summary>
        /// A server as it results from a YAML config that provides the credentials itself.
        /// </summary>
        private static TeamscaleServer ServerWithCredentials()
        {
            TeamscaleServer server = ServerWithoutCredentials();
            server.Username = YAML_USER;
            server.AccessKey = YAML_ACCESS_KEY;
            return server;
        }

        /// <summary>
        /// A server as it results from a YAML config that omits the credentials.
        /// </summary>
        private static TeamscaleServer ServerWithoutCredentials()
        {
            return new TeamscaleServer
            {
                Url = "url",
                Project = "project",
                Partition = "partition",
            };
        }
    }
}
