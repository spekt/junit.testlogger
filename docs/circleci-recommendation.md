# CircleCI Recommendation

## Summary

```yml
- run:
   command: |
     dotnet test ... --logger:"junit;LogFilePath=TestResults/test-result.xml;FailureBodyFormat=Verbose"
- store_test_results:
   path: TestResults/
```

## Details

CircleCI uses just a few pieces of the JUnit XML report to generate the displayed user interface.
<!--
Maintenance note:
Findings as of July 20 2023.
 - Only data in a <testcase> element is shown.
 - e.g. <system-out> from a <testcase> is shown.
 - The logger only puts console text (<system-out> and <system-err>) in the <testsuite> level (not into <testcase>) so CircleCI ignores it.
-->
For each failed test (i.e. for each JUnit `<testcase>` element containing a failure or an error),
CircleCI only shows the testcase's failure/error message, any text within the failure/error element, plus any `<system-out>` or `<system-err>` text.
It does not show the testcase's properties.
It does not show data from the testsuite.

By default, the logger records test console output in the JUnit testsuite,
which CircleCI ignores;
this can be improved upon by setting FailureBodyFormat when invoking the logger.
Setting MethodFormat provides no additional information or functionality.

### FailureBodyFormat

Setting `FailureBodyFormat=Verbose` when invoking the logger ensures that any test console output is visible in the CircleCI UI for (failed) tests.
This can provide important/useful information,
especially if the test failure message is insufficiently informative on its own.

- `Default`:

  ![FailureBodyFormat Default](assets/circleci-test-expanded-with-failure-default.png)

- `Verbose`:

  ![FailureBodyFormat Verbose](assets/circleci-test-expanded-with-failure-verbose.png)

### MethodFormat

CircleCI shows the class names of each failed test so setting `MethodFormat=Class` or `MethodFormat=Full` provides no further information on the CircleCI UI; the effect is purely cosmetic and is a matter of personal preference.

- `Default`:

  ![MethodFormat Default](assets/circleci-test-collapsed-with-methodname-default.png)

- `Class` and `Full`:

  ![MethodFormat Class](assets/circleci-test-collapsed-with-methodname-class.png)
