# Locator Fallback Guide

This guide explains how locator fallback works in `Locator_Fallback`, what you will see in test output, and how it affects test execution time.

## Contents

- [Quick Summary](#quick-summary)
- [How It Works](#how-it-works)
- [Implemented Components](#implemented-components)
- [Locator Results](#locator-results)
- [Failure Logs](#failure-logs)
- [Runtime Impact](#runtime-impact)
- [Practical Time-Planning Examples](#practical-time-planning-examples)
- [Usage Example](#usage-example)
- [Limitations and Safe Use](#limitations-and-safe-use)
- [Rollout Plan](#rollout-plan)
- [Roadmap](#roadmap)
- [Prerequisites and Folder Structure](#prerequisites-and-folder-structure)
- [Public Repository and Security Notes](#public-repository-and-security-notes)
- [Build and Run](#build-and-run)
- [Licence](#licence)
- [Licence](#licence)

---

## Quick Summary

| Topic | Information |
| --- | --- |
| Shared implementation | `Utilities/LocatorResolver.cs` |
| Main test integration | `TestCase/LoginTestCase.cs` |
| Wait type | `WebDriverWait` polling; no fixed `Thread.Sleep` |
| Current timeout | 3 seconds for each locator |
| Selection behavior | Uses the first visible locator, optionally requiring it to be enabled |
| Success log | Printed in NUnit output |
| Failure log | Written only when a locator fails |
| Log location | `<NUnit work directory>\Logs\LocatorFailures\<TestCaseName>.log` |

**Use the locator resolver when an element may have more than one valid selector.** Locators are tried in the configured order until an element is found.

---

## How It Works

The locator fallback process follows these steps:

1. The test passes the driver, ordered locator list, test-case name, element name, locator-list name, output writer, and timeout to `LocatorResolver.FindVisibleElement`.
2. The resolver prints the test-case name and locator-list heading.
3. It tries the first locator.
4. For that locator, it waits for an element that is:
   - Visible; and
   - Enabled, unless `requireEnabled` is set to `false`.
5. If a matching element is found, the resolver prints `Identified locator` and returns it.
6. If a locator fails, it is recorded in NUnit output and, if needed, in a failure log.
7. The resolver moves to the next locator and repeats the process.
8. If every locator fails, it throws `NoSuchElementException`.

Fallback attempts are sequential. A later locator is not checked until the earlier locator timeout or failure handling has finished.

> Stale elements are retried. If an element becomes stale while checking candidates, the resolver reports a retry and continues waiting.

---

## Implemented Components

### Locator Resolver

[`Utilities/LocatorResolver.cs`](Utilities/LocatorResolver.cs) contains the shared `FindVisibleElement` implementation.

Callers provide:

| Argument | Purpose |
| --- | --- |
| `driver` | The active Appium or Selenium driver |
| `locators` | Ordered list of fallback locators |
| `testCaseName` | Test name used in output and the log file name |
| `elementName` | Human-readable element name used in messages |
| `locatorListName` | Name of the locator list shown in NUnit output |
| `writeTestOutput` | Callback for printing messages |
| `timeoutPerLocator` | Maximum wait time for each individual locator |
| `requireEnabled` | Whether the located element must be enabled; defaults to `true` |
| `workDirectory` | Optional NUnit work directory used for log output |

The resolver:

- Uses `WebDriverWait` instead of `Thread.Sleep`.
- Stops at the first visible element.
- Requires the element to be enabled by default.
- Supports stale-element retries.
- Reports invalid selectors separately.
- Throws `NoSuchElementException` only after every configured locator has failed.

### Login Test Integration

[`TestCase/LoginTestCase.cs`](TestCase/LoginTestCase.cs) demonstrates the current integration.

The test fixture:

- Configures ordered locator lists for the username, password, and Sign In button.
- Calls `LocatorResolver.FindVisibleElement` through a local wrapper.
- Uses `TestContext.CurrentContext.Test.Name` as the test-case name.
- Uses `TestContext.Out.WriteLine` as the NUnit output writer.
- Uses `TestContext.CurrentContext.WorkDirectory` for failure-log storage.
- Uses a 3-second timeout per locator.

Other test fixtures can reuse `LocatorResolver.FindVisibleElement` and provide their own test-case name, element name, and locator-list name.

---

## Locator Results

For each locator list, NUnit output includes:

- The test-case name.
- The actual locator-list name.
- A numbered result for every attempted locator.

Example:

```text
Test case: Login_WithInvalidCredentials_DisplaysErrorsForBothFields
***************** AndroidUsernameLocators **************
locator 1 - ByAndroidUIAutomator(...) - failed to identify (not found before timeout)
locator 2 - By.XPath: ... - failed to identify (not found before timeout)
locator 3 - By.XPath: ... - Identified locator
```

After an identified locator, no remaining locators in that list are attempted.

---

## Failure Logs

Only failed attempts are written to files.

### Where logs are created

```text
<NUnit work directory>\Logs\LocatorFailures\<TestCaseName>.log
```

For example, `<NUnit work directory>` may be TestResults or NUnit's configured work directory.

### What a log contains

Each log file starts with:

```text
Test case: <TestCaseName>
```

Each failure entry includes:

| Value | Description |
| --- | --- |
| Timestamp | Date, time, and timezone offset |
| Locator-list name | The configured list containing the failed locator |
| Locator number | Its position in the ordered list |
| Selector | The Selenium/Appium locator expression |
| Failure reason | Timeout, stale element, or invalid selector details |
| Failure text | Printed in the same format as NUnit output |

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

Fallback attempts are sequential, so each failed locator may add the timeout duration for that locator.

### Formula

```text
Extra time per element =
    failed locator attempts
    x (wait timeout per locator + explicit sleep after each failure)
```

The current resolver does not add an explicit 5-second sleep. `LoginTestCase` uses a 3-second timeout for each locator.

Therefore, if the first two locator attempts fail for a current implementation element:

```text
2 failed attempts x 3 seconds = up to 6 seconds extra
```

This 6-second result excludes small logging and Appium response overhead.

Avoid adding fixed sleeps when implementing fallback behavior:

- `WebDriverWait` already polls for the element.
- Each extra sleep is multiplied by every failed locator attempt.
- Sleep time is an additional cost on top of the configured timeout.

---

## Practical Time-Planning Examples

### Current implementation

The current implementation uses a 3-second timeout and no fixed sleep:

```text
2 failed attempts x 3 seconds = up to 6 seconds extra per element
```

### Bad practice to avoid: wait plus fixed sleep

This example is for planning worse implementations that combine waits and fixed sleeps. It does not describe the current project.

Assume:

- Five locator options.
- The first two locators fail.
- Each locator waits 10 seconds.
- An additional 5-second sleep occurs after each failure.

```text
2 failed attempts x (10-second wait + 5-second sleep)
= 2 x 15 seconds
= up to 30 seconds extra per element
```

The successful locator and the rest of the test add their normal execution time.

Do not use fixed sleeps in fallback locators. `WebDriverWait` already polls for the element, and combining waits with sleeps can quickly add a large cost. For repository-specific or employer-internal execution estimates, record the number of affected elements and use the formula in a private planning document rather than publishing internal scale or performance commitments.

---

## Usage Example

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
```

Add locators from the most stable and specific selector to the least stable selector. The returned element is the first visible match that also satisfies `requireEnabled`.

---

## Limitations and Safe Use

A fallback may match a different element that happens to satisfy the selector. It can therefore hide a real UI regression or allow the test to continue with the wrong control.

- Review failure logs after each UI upgrade.
- Promote a working fallback to the primary locator when it is stable.
- Remove locators that repeatedly identify the wrong or unintended element.
- Prefer stable test IDs and accessibility IDs supplied by the development team.
- Locators are temporary resilience, not a replacement for a stable application accessibility contract.

The real fix is for developers to provide stable test IDs or accessibility IDs. Fallback logic reduces short-term breakage while the application is being improved.

---

## Rollout Plan

A safe rollout should use the following stages:

1. **Pilot:** Test the resolver on a representative set of stable flows.
2. **Collect data:** Review failure logs and compare locator success rates.
3. **Optimize:** Keep only selectors that provide value and remove consistently failing options.
4. **Migrate:** Update suites in controlled batches.
5. **Monitor:** Track total execution time and failure-log volume during each batch.
6. **Review:** Remove fallback overhead or refine locators for unstable elements.

Review failure logs after each UI upgrade and promote a working fallback to primary when appropriate. Treat the log as a cleanup signal: the fallback exists to buy time, not to remain permanently ahead of the application team's stable IDs.

---

## Roadmap

- Cache the working locator per element so later lookups start with the known-working selector.
- Add a summary report listing fallback use across a complete run.
- Set a shorter timeout for low-priority or last-resort locators.
- Add optional structured JSON logs for reporting and trend analysis.

---

## Prerequisites and Folder Structure

### Prerequisites

- .NET SDK 10.0 or a compatible SDK for `net10.0`.
- NUnit 5.0.0.
- NUnit3TestAdapter 6.3.1.
- Microsoft.NET.Test.Sdk 18.12.0.
- Appium.WebDriver 9.0.0.
- A reachable Appium server.
- An Android device or emulator with the Appium UiAutomator2 driver.
- The target APK and application package/activity configuration.

Use the versions in `Locator_Fallback.csproj` as the source of truth and document server or driver version additions when they affect compatibility.

### Folder structure

```text
Locator_Fallback/
|-- Locator_Fallback.csproj
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

Before publishing this repository or sharing its logs:

- Do not commit APK files.
- Do not commit real device IDs, credentials, signing material, tokens, or private Appium endpoints.
- Keep internal scale, cost, and execution estimates private unless your organization approves them for publication.
- Add environment variables or user secrets for device-specific configuration.
- Review generated logs for application names, element text, and other sensitive test data.
- Add a `.gitignore` rule for APK files and generated `Logs/` directories.

The guide is intended to show a reusable pattern, but project-specific paths and environment details may need to be sanitized for a public repository.

---

## Build and Run

Run commands from the `Locator_Fallback` project directory.

### Build

```powershell
dotnet build .\Locator_Fallback.csproj --no-restore
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

No licence is currently included. Add an appropriate licence before publishing or allowing reuse of this repository.

A common permissive choice is the MIT Licence, but the licence owner must be authorized to choose it and to add the required copyright notice.
