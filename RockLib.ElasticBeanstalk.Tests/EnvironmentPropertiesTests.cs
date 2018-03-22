using FluentAssertions;
using RockLib.ElasticBeanstalk;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

public class MapToEnvironmentVariables
{
    [Fact]
    public void ParsesARealFileAndSetsActualEnvironmentVariables()
    {
        try
        {
            var a = Environment.GetEnvironmentVariable("this.one.is.a.num");
            var b = Environment.GetEnvironmentVariable("test:variable");
            var c = Environment.GetEnvironmentVariable("test-var");

            a.Should().BeNull();
            b.Should().BeNull();
            c.Should().BeNull();

            EnvironmentProperties.MapToEnvironmentVariables("config.json");

            a = Environment.GetEnvironmentVariable("this.one.is.a.num");
            b = Environment.GetEnvironmentVariable("test:variable");
            c = Environment.GetEnvironmentVariable("test-var");

            a.Should().Be("1234");
            b.Should().Be("another one");
            c.Should().Be("a variable");
        }
        finally
        {
            Environment.SetEnvironmentVariable("this.one.is.a.num", null);
            Environment.SetEnvironmentVariable("test:variable", null);
            Environment.SetEnvironmentVariable("test-var", null);
        }
    }
}

public class Map
{
    [Fact]
    public void PassesPathToFileExists()
    {
        const string expectedPath = "foo_path";

        string pathValue = null;
        bool fileExists(string path) => (pathValue = path) != pathValue;
        EnvironmentProperties.Map(expectedPath, fileExists, null, null, null);
        pathValue.Should().Be(expectedPath);
    }

    public class IfFileExists
    {
        [Fact]
        public void PassesPathToReadAllText()
        {
            const string expectedPath = "foo_path";

            string pathValue = null;
            bool fileExists(string path) => true;
            string readAllText(string path) => pathValue = path;
            IEnumerable<(string, string)> getEnvironmentProperties(string raw) =>
                Enumerable.Empty<(string, string)>();
            EnvironmentProperties.Map(
                expectedPath, fileExists, readAllText, getEnvironmentProperties, null);
            pathValue.Should().Be(expectedPath);
        }

        [Fact]
        public void PassesAllTextToGetEnvironmentProperties()
        {
            const string allText = "all_text";

            string rawValue = null;
            bool fileExists(string path) => true;
            string readAllText(string path) => allText;
            IEnumerable<(string, string)> getEnvironmentProperties(string raw) =>
                (rawValue = raw) == rawValue ? Enumerable.Empty<(string, string)>() : null;
            EnvironmentProperties.Map(
                "foo_path", fileExists, readAllText, getEnvironmentProperties, null);
            rawValue.Should().Be(allText);
        }

        [Fact]
        public void PassesEachEnvironmentPropertyToSetEnvironmentVariable()
        {
            var expectedEnvironmentProperties = new List<(string, string)>
            {
                ("foo", "bar"),
                ("baz", "qux")
            };
            var actualEnvironmentProperties = new List<(string, string)>();

            bool fileExists(string path) => true;
            string readAllText(string path) => "all_text";
            IEnumerable<(string, string)> getEnvironmentProperties(string raw) => expectedEnvironmentProperties;
            void setEnvironmentVariable(string variable, string value) =>
                actualEnvironmentProperties.Add((variable, value));
            EnvironmentProperties.Map(
                "foo_path", fileExists, readAllText, getEnvironmentProperties, setEnvironmentVariable);
            actualEnvironmentProperties.Should().BeEquivalentTo(expectedEnvironmentProperties);
        }
    }

    public class IfFileDoesNotExist
    {
        [Fact]
        public void DoesNotCallReadAllText()
        {
            string pathValue = null;
            bool fileExists(string path) => false;
            string readAllText(string path) => "all_text";
            EnvironmentProperties.Map(
                "foo_path", fileExists, readAllText, null, null);
            pathValue.Should().BeNull();
        }

        [Fact]
        public void DoesNotCallGetEnvironmentProperties()
        {
            string rawValue = null;
            bool fileExists(string path) => false;
            string readAllText(string path) => "all_text";
            IEnumerable<(string, string)> getEnvironmentProperties(string raw) =>
                (rawValue = raw) == rawValue ? Enumerable.Empty<(string, string)>() : null;
            EnvironmentProperties.Map(
                "foo_path", fileExists, readAllText, getEnvironmentProperties, null);
            rawValue.Should().BeNull();
        }

        [Fact]
        public void DoesNotCallSetEnvironmentVariable()
        {
            var expectedEnvironmentProperties = new List<(string, string)>
            {
                ("foo", "bar"),
                ("baz", "qux")
            };
            var actualEnvironmentProperties = new List<(string, string)>();

            bool fileExists(string path) => false;
            string readAllText(string path) => "all_text";
            IEnumerable<(string, string)> getEnvironmentProperties(string raw) => expectedEnvironmentProperties;
            void setEnvironmentVariable(string variable, string value) =>
                actualEnvironmentProperties.Add((variable, value));
            EnvironmentProperties.Map(
                "foo_path", fileExists, readAllText, getEnvironmentProperties, setEnvironmentVariable);
            actualEnvironmentProperties.Should().BeEmpty();
        }
    }
}

public class GetEnvironmentProperties
{
    public class IfRawHasCorrectFormat
    {
        [Fact]
        public void TheEmbeddedEnvironmentPropertiesAreReturned()
        {
            var expectedProperties = new List<(string, string)> { ("foo", "bar"), ("baz", "qux") };
            var raw = @"{""iis"":{""env"":[""foo=bar"",""baz=qux""]}}";
            var actualProperties = EnvironmentProperties.GetEnvironmentProperties(raw);
            actualProperties.Should().BeEquivalentTo(expectedProperties);
        }
    }

    public class IfRawIsNull
    {
        [Fact]
        public void NoPropertiesAreReturned()
        {
            var actualProperties = EnvironmentProperties.GetEnvironmentProperties(null);
            actualProperties.Should().BeEmpty();
        }
    }

    public class IfRawIsEmpty
    {
        [Fact]
        public void NoPropertiesAreReturned()
        {
            var actualProperties = EnvironmentProperties.GetEnvironmentProperties("");
            actualProperties.Should().BeEmpty();
        }
    }

    public class IfRawDoesNotContainAJsonObject
    {
        [Theory]
        [InlineData("null")]
        [InlineData("[]")]
        [InlineData("123")]
        [InlineData("123.45")]
        [InlineData("true")]
        [InlineData(@"""abc""")]
        public void NoPropertiesAreReturned(string raw)
        {
            var actualProperties = EnvironmentProperties.GetEnvironmentProperties(raw);
            actualProperties.Should().BeEmpty();
        }
    }

    public class IfTheJsonObjectDoesNotHaveAnIisProperty
    {
        [Fact]
        public void NoPropertiesAreReturned()
        {
            var raw = @"{}";

            var actualProperties = EnvironmentProperties.GetEnvironmentProperties(raw);
            actualProperties.Should().BeEmpty();
        }
    }

    public class IfTheIisPropertyIsNotAJsonObject
    {
        [Theory]
        [InlineData("null")]
        [InlineData("[]")]
        [InlineData("123")]
        [InlineData("123.45")]
        [InlineData("true")]
        [InlineData(@"""abc""")]
        public void NoPropertiesAreReturned(string iis)
        {
            var raw = $@"{{""iis"":{iis}}}";

            var actualProperties = EnvironmentProperties.GetEnvironmentProperties(raw);
            actualProperties.Should().BeEmpty();
        }
    }

    public class IfTheIisPropertyDoesNotHaveAnEnvProperty
    {
        [Fact]
        public void NoPropertiesAreReturned()
        {
            var raw = @"{""iis"":{}}";

            var actualProperties = EnvironmentProperties.GetEnvironmentProperties(raw);
            actualProperties.Should().BeEmpty();
        }
    }

    public class IfTheEnvPropertyIsNotAJsonArray
    {
        [Theory]
        [InlineData("null")]
        [InlineData("{}")]
        [InlineData("123")]
        [InlineData("123.45")]
        [InlineData("true")]
        [InlineData(@"""abc""")]
        public void NoPropertiesAreReturned(string env)
        {
            var raw = $@"{{""iis"":{{""env"":{env}}}}}";

            var actualProperties = EnvironmentProperties.GetEnvironmentProperties(raw);
            actualProperties.Should().BeEmpty();
        }
    }

    public class IfAnEnvValueDoesNotContainAnEqualSign
    {
        [Fact]
        public void ItIsNotReturned()
        {
            var raw = @"{""iis"":{""env"":[""foobar"",""bazqux""]}}";

            var actualProperties = EnvironmentProperties.GetEnvironmentProperties(raw);
            actualProperties.Should().BeEmpty();
        }
    }

    public class IfAnEnvValueHasAnEmptyName
    {
        [Fact]
        public void ItIsNotReturned()
        {
            var raw = @"{""iis"":{""env"":[""=bar"",""=qux""]}}";

            var actualProperties = EnvironmentProperties.GetEnvironmentProperties(raw);
            actualProperties.Should().BeEmpty();
        }
    }
}
