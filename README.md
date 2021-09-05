# JUnit Test Logger

JUnit xml report extension for [Visual Studio Test Platform](https://github.com/microsoft/vstest).

[![Build Status](https://travis-ci.com/spekt/junit.testlogger.svg?branch=master)](https://travis-ci.com/spekt/junit.testlogger)
[![Build Status](https://ci.appveyor.com/api/projects/status/gsiaqo5g4gfk76kq?svg=true)](https://ci.appveyor.com/project/spekt/junit-testlogger)
[![NuGet Downloads](https://img.shields.io/nuget/dt/JunitXml.TestLogger)](https://www.nuget.org/packages/JunitXml.TestLogger/)

## Packages

| Logger | Stable Package | Pre-release Package |
| ------ | -------------- | ------------------- |
| JUnit | [![NuGet](https://img.shields.io/nuget/v/JUnitXml.TestLogger.svg)](https://www.nuget.org/packages/JUnitXml.TestLogger/) | [![MyGet Pre Release](https://img.shields.io/myget/spekt/vpre/junitxml.testlogger.svg)](https://www.myget.org/feed/spekt/package/nuget/JunitXml.TestLogger) |

If you're looking for `NUnit`, `xUnit` or `appveyor` loggers, visit following repositories:

- <https://github.com/spekt/nunit.testlogger>
- <https://github.com/spekt/xunit.testlogger>
- <https://github.com/spekt/appveyor.testlogger>

## Usage

The JUnit Test Logger generates xml reports in the [Jenkins JUnit Format](https://github.com/junit-team/junit5/blob/main/platform-tests/src/test/resources/jenkins-junit.xsd), which the JUnit 5 repository refers to as the de-facto standard. While the generated xml complies with that schema, it does not contain values in every case. For example, the logger currently does not log any `properties`. Please [refer to a sample file](docs/assets/TestResults.xml) to see an example. If you find that the format is missing data required by your CI/CD system, please open an issue or PR.

To use the logger, follow these steps:

1. Add a reference to the [JUnit Logger](https://www.nuget.org/packages/JUnitXml.TestLogger) nuget package in test project
2. Use the following command line in tests
```
> dotnet test --logger:junit
```
3. Test results are generated in the `TestResults` directory relative to the `test.csproj`

A path for the report file can be specified as follows:
```
> dotnet test --logger:"junit;LogFilePath=test-result.xml"
```

`test-result.xml` will be generated in the same directory as `test.csproj`.

**Note:** the arguments to `--logger` should be in quotes since `;` is treated as a command delimiter in shell.

All common options to the logger are documented [in the wiki][config-wiki]. E.g.
token expansion for `{assembly}` or `{framework}` in result file. If you are writing multiple
files to the same directory or testing multiple frameworks, these options can prevent
test logs from over-writing eachother.

[config-wiki]: https://github.com/spekt/testlogger/wiki/Logger-Configuration

### Junit Output Notes

The xml reports will have standard output (stdout) and standard error (stderr) from the test output. Stdout is logged on a per-testcase basis, but stderr

#### NUnit

On a testcase failure, NUnit will redirect stderr to stdout for each `TestCase`. Because of this, both stdout and stderr will be captured in the JUnit xml file within the `<system-out></system-out>` tags for each test case. It will also be capture within the `<system-out></system-out>` tags at the `<testsuite>` level.

##### NUnit Example
```csharp
[TestCase]
public void TestThatFailsWithStdErr()
{
    Console.WriteLine("Output to stderr", Console.Error);
    Assert.Fail();
}
```
Console Output
```
  Failed TestThatFailsWithStdErr() [71 ms]
  Stack Trace:
     at NUnit.UnitTests.Tests.TestThatFailsWithStdErr() in /home/runner/work/dotnet_test_output_reporting/dotnet_test_output_reporting/NUnit.UnitTests/UnitTest1.cs:line 42

  Standard Output Messages:
 Output to stderr
```

```xml
<testcase classname="NUnit.UnitTests.Tests" name="TestThatFailsWithStdOutAndStdErr()" time="0.0004670">
  <failure type="failure">at NUnit.UnitTests.Tests.TestThatFailsWithStdOutAndStdErr() in D:\Developer\dotnet_test_output_reporting\NUnit.UnitTests\UnitTest1.cs:line 50</failure>
  <system-out>Output to stdout
Output to stderr

</system-out>
</testcase>
```

#### xUnit

On a testcase failure, by default xUnit will not log any stdout or stderr to the console. Stdout from each `Fact` will be captured in the JUnit xml within the `<system-out></system-out>` tags at the `testsuite` level.

##### xUnit Example
```csharp
[Fact]
public void TestThatFailsWithStdErr()
{
    Console.WriteLine("Output to stderr", Console.Error);
    Assert.True(false, "test failure");
}
```
```
Expected: True
Actual:   False
  Stack Trace:
     at XUnit.UnitTests.UnitTest1.TestThatFailsWithStdErr() in /home/runner/work/dotnet_test_output_reporting/dotnet_test_output_reporting/XUnit.UnitTests/UnitTest1.cs:line 41
  Failed XUnit.UnitTests.UnitTest1.TestThatFailsWithStdOutAndStdErr [< 1 ms]
  Error Message:
   test failure
```
```xml
<testcase classname="XUnit.UnitTests.UnitTest1" name="TestThatFailsWithStdErr" time="0.0030326">
  <failure type="failure" message="test failure&#xD;&#xA;Expected: True&#xD;&#xA;Actual:   False">at XUnit.UnitTests.UnitTest1.TestThatFailsWithStdErr() in D:\Developer\dotnet_test_output_reporting\XUnit.UnitTests\UnitTest1.cs:line 41</failure>
</testcase>
```


#### MsTest

#### MsTest Example
```csharp

```
```
Failed TestThatFailsWithStdErr [< 1 ms]
Error Message:
 Assert.Fail failed. 
Stack Trace:
   at MsTest.UnitTests.UnitTest1.TestThatFailsWithStdErr() in /home/runner/work/dotnet_test_output_reporting/dotnet_test_output_reporting/MsTest.UnitTests/UnitTest1.cs:line 42

Standard Output Messages:
Output to stderr
```

### Customizing Junit XML Contents

There are several options to customize how the junit xml is populated. These options exist to
provide additional control over the xml file so that the logged test results can be optimized for different CI/CD systems.

Platform Specific Recommendations:

- [GitLab CI/CD Recomendation](/docs/gitlab-recommendation.md)
- [Jenkins Recomendation](/docs/jenkins-recommendation.md) 
- [CircleCI Recomendation](/docs/circleci-recommendation.md)

After the logger name, command line arguments are provided as key/value pairs with the following general format. **Note** the quotes are required and key names are case sensitive.

```
> dotnet test --test-adapter-path:. --logger:"junit;key1=value1;key2=value2"
```

#### MethodFormat

This option alters the `testcase name` attribute. By default, this contains only the method. Class, will add the class to the name. Full, will add the assembly/namespace/class to the method. 

We recommend this option for [GitLab](/docs/gitlab-recommendation.md) users.

##### Allowed Values

- MethodFormat=Default
- MethodFormat=Class
- MethodFormat=Full

#### FailureBodyFormat

When set to default, the body of a `failure` element will contain only the exception which is captured by vstest. Verbose will prepend the body with 'Expected X, Actual Y' similar to how it is displayed in the standard test output. 'Expected X, Actual Y' are normally only contained in the failure message. Additionally, Verbose will include standard output from the test in the failure message. 

We recommend this option for [GitLab](/docs/gitlab-recommendation.md) users.

##### Allowed Values

- FailureBodyFormat=Default
- FailureBodyFormat=Verbose

## License

MIT
