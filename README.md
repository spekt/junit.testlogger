# JUnit Test Logger

JUnit xml report extension for [Visual Studio Test Platform](https://github.com/microsoft/vstest).

[![Build Status](https://travis-ci.com/spekt/junit.testlogger.svg?branch=master)](https://travis-ci.com/spekt/junit.testlogger)
[![Build Status](https://ci.appveyor.com/api/projects/status/gsiaqo5g4gfk76kq?svg=true)](https://ci.appveyor.com/project/spekt/junit-testlogger)
[![NuGet Downloads](https://img.shields.io/nuget/dt/JunitXml.TestLogger)](https://www.nuget.org/packages/JunitXml.TestLogger/)

## Packages

| Logger | Stable Package | Pre-release Package |
| ------ | -------------- | ------------------- |
| JUnit | [![NuGet](https://img.shields.io/nuget/v/JUnitXml.TestLogger.svg)](https://www.nuget.org/packages/JUnitXml.TestLogger/) | [![MyGet Pre Release](https://img.shields.io/myget/spekt/vpre/junitxml.testlogger.svg)](https://www.myget.org/feed/spekt/package/nuget/JunitXml.TestLogger) |

If you're looking for `Nunit`, `Xunit` or `appveyor` loggers, visit following repositories:

- <https://github.com/spekt/nunit.testlogger>
- <https://github.com/spekt/xunit.testlogger>
- <https://github.com/spekt/appveyor.testlogger>

## Usage

The JUnit Test Logger generates xml reports in the [Ant Junit Format](https://github.com/windyroad/JUnit-Schema), which the JUnit 5 repository refers to as the de-facto standard. While the generated xml complies with that schema, it does not contain values in every case. For example, the logger currently does not log any `properties`. Please [refer to a sample file](docs/assets/TestResults.xml) to see an example. If you find that the format is missing data required by your CI/CD system, please open an issue or PR.

1. Add a reference to the [JUnit Logger](https://www.nuget.org/packages/JUnitXml.TestLogger) nuget package in test project
2. Use the following command line in tests
```
> dotnet test --logger:junit
```
1. Test results are generated in the `TestResults` directory relative to the `test.csproj`

A path for the report file can be specified as follows:
```
> dotnet test --logger:"junit;LogFilePath=test-result.xml"
```

`test-result.xml` will be generated in the same directory as `test.csproj`.

**Note:** the arguments to `--logger` should be in quotes since `;` is treated as a command delimiter in shell.

All common options to the logger is documented [in the wiki][config-wiki]. E.g.
token expansion for `{assembly}` or `{framework}` in result file. If you are writing multiple
files to the same directory or testing multiple frameworks, these options can prevent
test logs from overwriting eachother.

[config-wiki]: https://github.com/spekt/testlogger/wiki/Logger-Configuration

### Customizing Output

There are several options to customize the output file name, location, and contents.

Platform Specific Recommendations:

- [GitLab CI/CD Recomendation](/docs/gitlab-recommendation.md)

After the logger name, command line arguments are provided as key/value pairs with the following general format. **Note** the quotes are required and key names are case sensitive. In case of issues, review the command line output for error messages.

```
> dotnet test --test-adapter-path:. --logger:"junit;key1=value1;key2=value2"
```

#### Log File Name and Path

You can control 

#### MethodFormat

This option alters the testcase name attribute. By default, this contains only the method. Class, will add the class to the name. Full, will add the assembly/namespace/class to the method. See [here](/docs/gitlab-recommendation.md) for an example of why this might be useful.

##### Allowed Values

- MethodFormat=Default
- MethodFormat=Class
- MethodFormat=Full

#### FailureBodyFormat

When set to default, the body will contain only the exception which is captured by vstest. Verbose will prepend the body with 'Expected X, Actual Y' similar to how it is displayed in the standard test output. 'Expected X, Actual Y' are normally only contained in the failure message. Additionally, Verbose will include standard output from the test in the failure message. See [here](/docs/gitlab-recommendation.md) for an example of why this might be useful.

##### Allowed Values

- FailureBodyFormat=Default
- FailureBodyFormat=Verbose

## License

MIT
