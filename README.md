# JUnit Test Logger

JUnit xml report extension for [Visual Studio Test Platform](https://github.com/microsoft/vstest).

[![Build Status](https://travis-ci.com/spekt/junit.testlogger.svg?branch=master)](https://travis-ci.com/spekt/junit.testlogger)
[![Build status](https://ci.appveyor.com/api/projects/status/0hw9s9ib60rw46om?svg=true)](https://ci.appveyor.com/project/spekt/junit-testlogger)

## Packages

| Logger | Stable Package | Pre-release Package |
| ------ | -------------- | ------------------- |
| JUnit | <!-- [![NuGet](https://img.shields.io/nuget/v/JUnitXml.TestLogger.svg)](https://www.nuget.org/packages/JUnitXml.TestLogger/) --> TODO | [![MyGet Pre Release](https://img.shields.io/myget/spekt/vpre/junitxml.testlogger.svg)](https://www.myget.org/feed/spekt/package/nuget/JunitXml.TestLogger) |

If you're looking for `Nunit`, `Xunit` or `appveyor` loggers, visit following repositories:

- <https://github.com/spekt/nunit.testlogger>
- <https://github.com/spekt/xunit.testlogger>
- <https://github.com/spekt/appveyor.testlogger>

## Usage

JUnit logger can generate xml reports in the JUnit format. The JUnit format seems to be variable across different implementations, with the JUnit 5 repository referring to the [Ant Junit Format](https://github.com/windyroad/JUnit-Schema) as a de-facto standard. This project does not implement that standard exactly. Please [refer to a sample file](docs/assets/TestResults.xml) to see an example of which parts of the format have been implemented. If you find that this format is missing some feature required by your CI/CD system, please open an issue.

### Default Behavior

1. Add a reference to the [JUnit Logger](https://www.nuget.org/packages/JUnitXml.TestLogger) nuget package in test project
2. Use the following command line in tests

```
> dotnet test --test-adapter-path:. --logger:junit
```

3. Test results are generated in the `TestResults` directory relative to each `test.csproj` file.

### Customizing Output

There are several options to customize the output file name, location, and contents.

Platform Specific Recommendations:

- [GitLab CI/CD Recomendation](/docs/gitlab-recommendation.md)

After the logger name, command line arguments are provided as key/value pairs with the following general format. **Note** the quotes are required and key names are case sensitive. In case of issues, review the command line output for error messages.

```
> dotnet test --test-adapter-path:. --logger:"junit;key1=value1;key2=value2"
```

#### LogFilePath

Specify the path and file relative to the .csproj file for each test project. <br> **Note** This option is not compatible with LogFileName or TestRunDirectory

##### Examples

- LogFilePath=artifacts\\test-output.xml
- LogFilePath=artifacts\\{framework}\\{assembly}-test-results.xml
- LogFilePath=..\\artifacts\\{assembly}-test-results.xml

#### LogFileName

Specify the file name. Unless the TestRunDirectory is specified the file will be stored in the default TestResult directory, relative to the test csproj file.

##### Examples

- LogFileName=test-output.xml
- LogFileName={framework}-{assembly}-test-results.xml

#### TestRunDirectory

Specify the directory, absolute or relative to the test csproj file. If the file name is not specified, it will be TestResults.xml

##### Examples

- TestRunDirectory=artifacts\\
- TestRunDirectory=artifacts\\{framework}\

#### MethodFormat

This option alters the testcase name attribute. By default, this contains only the method. Class, will add the class to the name. Full, will add the assembly/namespace/class to the method. See [here](/docs/gitlab-recommendation.md) for an example of why this might be useful.

##### Allowed Values

- MethodFormat=Default
- MethodFormat=Class
- MethodFormat=Full

#### FailureBodyFormat

When set to default, the body will contain only the exception which is captured by vstest. Verbose will prepend the body with 'Expected X, Actual Y' similar to how it is displayed in the standard test output. 'Expected X, Actual Y' are normally only contained in the failure message. See [here](/docs/gitlab-recommendation.md) for an example of why this might be useful.

##### Allowed Values

- FailureBodyFormat=Default
- FailureBodyFormat=Verbose

### Saving Multiple Result Files In One Directory

By default, every test project generates an xml report with the same directory and file name. The tokens {framework} and {assembly} may be placed anywhere in the directory or file names to customize the output location. This is **critical**, when multiple test reports will be written to the same directory, as in the following example. Otherwise, the files would use identical names, and the second output file written would overwrite the first.

```
Consider following project structure:
.\Solution\MySolution.sln
     |____\TestLib1\TestLib1.csproj
     |____\TestLib2\TestLib2.csproj

Run Tests:
 > dotnet test --test-adapter-path:. --logger:"junit;LogFilePath=..\artifacts\{assembly}-test-results.xml"

New Files:
.\Solution\
     |____\artifacts
          |____\TestLib1-test-results.xml
          |____\TestLib2-test-results.xml
```

## License

MIT
