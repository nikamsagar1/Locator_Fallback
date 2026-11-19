# Dynamic Locator Fallback – UI Automation

A lightweight and reusable C# implementation of Dynamic Locator Fallback for Web and Mobile UI Automation using Selenium and Appium.

This project demonstrates how automation frameworks can automatically try predefined alternative locator strategies when the primary locator fails, improving test resilience and reducing unnecessary locator maintenance after UI changes.

## Contents

- [Overview](#overview)
- [Quick Summary](#quick-summary)
- [Architecture and Execution Flow](#architecture-and-execution-flow)
- [How It Works](#how-it-works)
- [Implemented Components](#implemented-components)
- [Locator Results](#locator-results)
- [Failure Logs](#failure-logs)
- [Runtime Impact](#runtime-impact)
- [Practical Time-Planning Examples](#practical-time-planning-examples)
- [Usage Example](#usage-example)
- [Limitations and Safe Use](#limitations-and-safe-use)
- [Best Practices](#best-practices)
- [Rollout Plan](#rollout-plan)
- [Roadmap](#roadmap)
- [Prerequisites and Folder Structure](#prerequisites-and-folder-structure)
- [Public Repository and Security Notes](#public-repository-and-security-notes)
- [Build and Run](#build-and-run)
- [Licence](#licence)

---

## Overview

UI automation scripts frequently fail after application upgrades because existing element locators may no longer identify the intended elements.

This can result in unnecessary test failures and increased maintenance effort, even when the application functionality is working correctly.

Dynamic Locator Fallback addresses this challenge by allowing multiple predefined locator strategies for the same UI element.

Instead of depending on a single locator, the resolver attempts alternative locators in a predefined order until it identifies an element that satisfies the configured conditions.

The implementation is designed as a reusable utility that can be integrated into Web and Mobile UI Automation frameworks.

**Supported automation technologies:**
- Selenium WebDriver – Web UI Automation
- Appium – Mobile UI Automation

The current repository includes an Appium login demonstration. Selenium integration can use the same generic resolver when implemented and verified against a web test.

---

## Quick Summary

| Topic | Information |
|---|---|
| Project | Dynamic Locator Fallback |
| Language | C# |
| Automation | Selenium / Appium |
| Shared implementation | `Utilities/LocatorResolver.cs` |
| Main test integration | `TestCase/LoginTestCase.cs` |
| Wait type | `WebDriverWait` polling |
| Current timeout | 3 seconds per locator |
| Selection behavior | First visible locator, optionally requiring it to be enabled |
| Success output | Printed in NUnit output |
| Failure output | Written only when a locator fails |
| Log location | `<NUnit work directory>/Logs/LocatorFailures/<TestCaseName>.log` |

**Important:** The resolver uses ordered fallback attempts. It does not automatically generate new locators or guarantee that an alternative locator identifies the correct element. The configured locators must be reviewed and validated.

---

## Architecture and Execution Flow

The locator fallback process follows this execution flow:

```text
             Test Script
                  |
                  v
           Locator Resolver
                  |
                  v
          Try Primary Locator
                  |
             Element Found?
              /       \
            Yes        No
             |          |
             v          v
          Return    Try Next Locator
          Element        |
             |       Element Found?
             |        /       \
             |      Yes        No
             |       |          |
             |       v          v
             |    Return     Try Next
             |    Element    Locator
             |       |          |
             v       v          v
             Test Continues  All Locators
                             Exhausted
                                 |
                                 v
                         Throw Exception
```

### Main Components

1. **Test Script:** Supplies the ordered list of locators.
2. **Locator Resolver:** Evaluates each locator using the configured timeout.
3. **Fallback Mechanism:** Attempts the next locator when the current locator fails.
4. **Logging:** Records locator failures and successful identification in test output.
5. **Test Execution:** Continues with the identified element or reports failure if all locators are exhausted.

---

## How It Works

The locator fallback process follows these steps:

1. The test passes the driver, ordered locator list, test-case name, element name, locator-list name, output writer, and timeout to `LocatorResolver.FindVisibleElement`.
2. The resolver prints the test-case name and locator-list heading.
3. It tries the first locator.
4. For that locator, it waits for an element that is:
   - Visible.
   - Enabled, unless `requireEnabled` is set to `false`.
5. If a matching element is found, the resolver prints `Identified locator` and returns it.
6. If a locator fails, the failure is recorded in NUnit output and, when applicable, in a failure log.
7. The resolver moves to the next locator and repeats the process.
8. If every locator fails, it throws `NoSuchElementException`.

Fallback attempts are sequential. A later locator is not checked until the earlier locator's timeout or failure handling has finished.

> Stale elements are retried. If an element becomes stale while checking candidates, the resolver reports a retry and continues waiting.

---

## Implemented Components

### Locator Resolver

`Utilities/LocatorResolver.cs` contains the shared `FindVisibleElement` implementation.

Callers provide:

| Argument | Purpose |
|---|---|
| `driver` | Active Appium or Selenium driver |
| `locators` | Ordered list of fallback locators |
| `testCaseName` | Test name used in output and log filename |
| `elementName` | Human-readable element name used in messages |
| `locatorListName` | Name of the locator list shown in NUnit output |
| `writeTestOutput` | Callback for printing messages |
| `timeoutPerLocator` | Maximum wait time for each individual locator |
| `requireEnabled` | Whether the located element must be enabled; defaults to `true` |
| `workDirectory` | Optional NUnit work directory used for log output |

The resolver:

- Uses `WebDriverWait` instead of `Thread.Sleep`.
- Stops at the first visible matching element.
- Requires the element to be enabled by default.
- Supports stale-element retries.
- Reports invalid selectors separately.
- Throws `NoSuchElementException` only after all configured locators fail.

### Login Test Integration

`TestCase/LoginTestCase.cs` demonstrates the current Appium integration.

The test fixture:

- Configures ordered locator lists for username, password, and Sign In button.
- Calls `LocatorResolver.FindVisibleElement` through a local wrapper.
- Uses `TestContext.CurrentContext.Test.Name` as the test-case name.
- Uses `TestContext.Out.WriteLine` as the NUnit output writer.
- Uses `TestContext.CurrentContext.WorkDirectory` for failure-log storage.
- Uses a 3-second timeout per locator.

Other test fixtures can reuse `LocatorResolver.FindVisibleElement` and provide their own test-case name, element name, and locator-list name.

---

## Locator Results

For each locator list, NUnit output includes:

- Test-case name.
- Actual locator-list name.
- Numbered result for every attempted locator.

### Example: Primary Locator Failed, Fallback Succeeded

```text
Test case: Login_WithInvalidCredentials_DisplaysErrorsForBothFields

***************** AndroidUsernameLocators **************

locator 1 - ByAndroidUIAutomator(...) - failed to identify (not found before timeout)

locator 2 - By.XPath: ... - failed to identify (not found before timeout)

locator 3 - By.XPath: ... - Identified locator
```

After an identified locator, no remaining locators in that list are attempted.

This makes it easier to understand whether a test used its primary locator or depended on an alternative locator.

---

## Failure Logs

Only failed attempts are written to files.

### Where logs are created

```text
<NUnit work directory>/Logs/LocatorFailures/<TestCaseName>.log
```

For example, `<NUnit work directory>` may be TestResults or NUnit's configured work directory.

### What a log contains

Each log file starts with:

```text
Test case: <TestCaseName>
```

Each failure entry includes:

| Value | Description |
|---|---|
| Timestamp | Date, time, and timezone offset |
| Locator-list name | Configured list containing the failed locator |
| Locator number | Position in the ordered list |
| Selector | Selenium/Appium locator expression |
| Failure reason | Timeout, stale element, or invalid selector details |
| Failure text | Message printed in NUnit output |

Example entry:

```text
2026-11-19 12:46:56 +05:30 | AndroidUsernameLocators | locator 1 - ByAndroidUIAutomator(...) - failed to identify (not found before timeout)
```

The same failure is also printed in NUnit output, along with the full log-file path:

```text
Locator failure log: <full-log-file-path>
```

If a test has no locator failures, it does not create a failure log.

---

## Runtime Impact

Fallback attempts are sequential, so each failed locator may add execution time.

### Runtime calculation

```text
Maximum additional wait time =
    Number of failed locator attempts
    x Timeout per locator
```

The current resolver uses a 3-second timeout per locator and does not add an explicit fixed sleep.

For example, if the first two locators fail and the third succeeds:

```text
2 failed attempts x 3 seconds

= Up to 6 seconds additional wait time
```

This is a maximum planning estimate, not a guaranteed execution duration.

Actual execution time may be lower because a locator can succeed before its timeout expires.

It may also include additional overhead from:
- Appium/Selenium communication.
- Locator polling.
- Element visibility and enabled-state checks.
- Logging operations.

### Why avoid Thread.Sleep?

Avoid adding fixed sleeps after failed locator attempts.

- `WebDriverWait` already polls for the element.
- Additional sleeps increase execution time.
- Sleep time is added on top of the configured wait timeout.
- The additional overhead multiplies across failed locators and elements.

---

## Practical Time-Planning Examples

### Current implementation

Assume:
- Timeout per locator: 3 seconds.
- First two locators fail.
- Third locator succeeds.

```text
2 failed attempts x 3 seconds

= Up to 6 seconds additional wait time
```

### Bad practice to avoid: Wait plus fixed sleep

The following is an example of an inefficient implementation, not the current project configuration.

Assume:
- Five locator options.
- First two locators fail.
- Each locator waits for 10 seconds.
- An additional 5-second sleep occurs after each failure.

```text
2 failed attempts x (10-second wait + 5-second sleep)

= 2 x 15 seconds

= Up to 30 seconds additional wait time
```

The successful locator and the remaining test execution add their normal execution time.

**Recommendation:** Use controlled explicit waits and avoid fixed sleeps in fallback logic.

---

## Usage Example

The following example demonstrates how to define an ordered locator list and resolve the username field.

```csharp
IReadOnlyList<By> usernameLocators =
[
    MobileBy.AndroidUIAutomator(
        "new UiSelector().className(\"android.widget.EditText\").instance(0)"),

    By.XPath("//android.widget.EditText[1]")
];

IWebElement username = LocatorResolver.FindVisibleElement(
    Driver,
    usernameLocators,
    TestContext.CurrentContext.Test.Name,
    "username field",
    nameof(usernameLocators),
    message => TestContext.Out.WriteLine(message),
    TimeSpan.FromSeconds(3),
    requireEnabled: true,
    TestContext.CurrentContext.WorkDirectory);

// Continue with the identified element
username.SendKeys("testuser");
```

Add locators from the most stable and specific selector to the least stable selector.

The returned element is the first visible match that also satisfies the configured `requireEnabled` condition.

For Web UI Automation, use Selenium-compatible locator strategies such as:

```csharp
IReadOnlyList<By> usernameLocators =
[
    By.Id("username"),
    By.Name("username"),
    By.CssSelector("input[name='username']"),
    By.XPath("//input[@type='text']")
];
```

Both examples illustrate the locator strategies. The generic Selenium example requires a verified Selenium test integration before it should be considered an implemented repository feature.

---

## Limitations and Safe Use

A fallback locator may match a different element that happens to satisfy the selector.

It can therefore hide a genuine UI regression or allow the test to continue with the wrong control.

Consider the following limitations:

- A fallback does not guarantee correct element identification.
- Multiple locator attempts can increase execution time.
- Poorly designed XPath expressions may be fragile.
- Alternative locators may also become invalid after application upgrades.
- Fallback logic cannot resolve changes in application behaviour, business logic, missing elements, or application crashes.
- A successful locator does not automatically mean that the application is functioning correctly.

### Safe Usage Guidelines

- Review failure logs after every UI upgrade.
- Promote a working fallback to the primary locator when it is stable.
- Remove locators that repeatedly identify the wrong or unintended element.
- Prefer stable test IDs and accessibility IDs supplied by the development team.
- Validate that all fallback locators identify the same intended element.
- Avoid broad selectors that may match multiple controls.

**Important:** Locator fallback is a resilience mechanism, not a replacement for a stable application accessibility contract.

The long-term solution is for development teams to provide stable test IDs and accessibility identifiers.

Fallback logic helps reduce short-term automation breakage while application locator stability is being improved.

---

## Best Practices

1. Start with the most stable locator.
2. Prefer accessibility IDs and resource IDs where available.
3. Keep fallback locators specific to the intended element.
4. Avoid unnecessary locator alternatives.
5. Configure reasonable timeouts.
6. Do not use fixed sleeps after failed locator attempts.
7. Log fallback usage and review it regularly.
8. Remove obsolete or unreliable selectors.
9. Validate the identified element before performing important actions.
10. Treat repeated fallback usage as a signal for locator maintenance.

---

## Rollout Plan

A safe rollout should use the following stages:

1. **Pilot:** Test the resolver on a representative set of stable flows.
2. **Collect data:** Review failure logs and compare locator success rates.
3. **Optimize:** Keep only selectors that provide value and remove consistently failing options.
4. **Migrate:** Update suites in controlled batches.
5. **Monitor:** Track total execution time and failure-log volume during each batch.
6. **Review:** Remove fallback overhead or refine locators for unstable elements.

Review failure logs after each UI upgrade and promote a working fallback to primary when appropriate.

Treat the log as a cleanup signal: the fallback exists to provide resilience, not to remain permanently ahead of the application team's stable IDs.

---

## Roadmap

Potential future improvements:

- Cache the working locator per element so subsequent lookups can start with the known-working selector.
- Add a summary report listing fallback usage across a complete test run.
- Introduce configurable timeouts for individual fallback locators.
- Add optional structured JSON logging for reporting and trend analysis.
- Extend and verify Selenium Web UI Automation integration.
- Add more Android and iOS examples.
- Introduce fallback success-rate reporting.

---

## Prerequisites and Folder Structure

### Prerequisites

- .NET SDK 10.0 or a compatible SDK for `net10.0`.
- NUnit version matching the project file.
- NUnit3TestAdapter version matching the project file.
- Microsoft.NET.Test.Sdk version matching the project file.
- Appium.WebDriver version matching the project file.
- A reachable Appium server.
- An Android device or emulator with the Appium UiAutomator2 driver.
- Target APK and application package/activity configuration.

Use the versions in `Locator_Fallback.csproj` as the source of truth.

### Folder Structure

```text
Locator_Fallback/
|-- Locator_Fallback.csproj
|-- README.md
|-- LOCATOR_FALLBACK_GUIDE.md
|-- APKs/
|-- AppiumInspectorJson/
|-- TestCase/
|   `-- LoginTestCase.cs
`-- Utilities/
    `-- LocatorResolver.cs
```

---

## Public Repository and Security Notes

Before publishing this repository or sharing execution logs:

- Do not commit APK files.
- Do not commit real device IDs, credentials, signing material, tokens, or private Appium endpoints.
- Keep internal project scale, cost, and execution estimates private unless your organization approves their publication.
- Use environment variables or user secrets for device-specific configuration.
- Review generated logs for application names, element text, and other sensitive test data.
- Add `.gitignore` rules for APK files and generated `Logs/` directories.
- Ensure that any code shared publicly is owned by you or approved for publication.

The repository is intended to demonstrate a reusable automation pattern. Project-specific paths and environment details should be sanitized before publication.

---

## Build and Run

Run commands from the `Locator_Fallback` project directory.

### Build

```powershell
dotnet build .\Locator_Fallback.csproj
```

### Run the fallback test

```powershell
dotnet test .\Locator_Fallback.csproj --filter "FullyQualifiedName~Locator_Fallback.TestCase.LoginTestCase.Login_WithInvalidCredentials_DisplaysErrorsForBothFields"
```

The test command requires:

- A reachable Appium server.
- A configured Android device.
- A valid Appium connection.
- The test application's APK and device settings.

Locator-failure files are produced only when tests execute and encounter failed locator attempts.

---

## Licence

No licence is currently included.

Add an appropriate open-source licence before publishing or allowing reuse of this repository.

MIT is one possible permissive licence, but the repository owner must be authorized to choose it and provide the required copyright notice.
