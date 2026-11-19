using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace Locator_Fallback.Utilities;

public static class LocatorResolver
{
    private static readonly object FailureLogLock = new();

    public static IWebElement FindVisibleElement(
        IWebDriver driver,
        IReadOnlyList<By> locators,
        string testCaseName,
        string elementName,
        string locatorListName,
        Action<string> writeTestOutput,
        TimeSpan timeoutPerLocator,
        bool requireEnabled = true,
        string? workDirectory = null)
    {
        ArgumentNullException.ThrowIfNull(driver);
        ArgumentNullException.ThrowIfNull(locators);
        ArgumentException.ThrowIfNullOrWhiteSpace(testCaseName);
        ArgumentException.ThrowIfNullOrWhiteSpace(locatorListName);
        ArgumentNullException.ThrowIfNull(writeTestOutput);

        if (locators.Count == 0)
            throw new ArgumentException("Locator list cannot be empty.", nameof(locators));
        if (timeoutPerLocator <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(timeoutPerLocator), "The locator timeout must be greater than zero.");

        writeTestOutput($"Test case: {testCaseName}");
        writeTestOutput($"***************** {locatorListName} **************");

        for (var index = 0; index < locators.Count; index++)
        {
            var locator = locators[index];
            try
            {
                var wait = new WebDriverWait(driver, timeoutPerLocator);
                var element = wait.Until(d =>
                {
                    foreach (var candidate in d.FindElements(locator))
                    {
                        try
                        {
                            if (candidate.Displayed && (!requireEnabled || candidate.Enabled))
                                return candidate;
                        }
                        catch (StaleElementReferenceException)
                        {
                            writeTestOutput($"[RETRY] {elementName}: element became stale for {locator}");
                        }
                    }

                    return null;
                });

                if (element is not null)
                {
                    writeTestOutput($"locator {index + 1} - {locator} - Identified locator");
                    return element;
                }
            }
            catch (WebDriverTimeoutException)
            {
                LogFailure(testCaseName, locatorListName, index, locator, "not found before timeout", writeTestOutput, workDirectory);
            }
            catch (StaleElementReferenceException)
            {
                LogFailure(testCaseName, locatorListName, index, locator, "element became stale", writeTestOutput, workDirectory);
            }
            catch (InvalidSelectorException exception)
            {
                LogFailure(testCaseName, locatorListName, index, locator, $"invalid selector: {exception.Message}", writeTestOutput, workDirectory);
            }
        }

        throw new NoSuchElementException($"Could not locate the {elementName} using any of the {locators.Count} configured locators.");
    }

    private static void LogFailure(
        string testCaseName,
        string locatorListName,
        int index,
        By locator,
        string reason,
        Action<string> writeTestOutput,
        string? workDirectory)
    {
        var failure = $"locator {index + 1} - {locator} - failed to identify ({reason})";
        writeTestOutput(failure);

        var directory = Path.Combine(workDirectory ?? AppContext.BaseDirectory, "Logs", "LocatorFailures");
        var filePath = Path.Combine(directory, $"{SanitizeFileName(testCaseName)}.log");
        var logEntry = $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz} | {locatorListName} | {failure}{Environment.NewLine}";

        lock (FailureLogLock)
        {
            Directory.CreateDirectory(directory);
            if (!File.Exists(filePath))
                File.WriteAllText(filePath, $"Test case: {testCaseName}{Environment.NewLine}");

            File.AppendAllText(filePath, logEntry);
        }

        writeTestOutput($"Locator failure log: {filePath}");
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalidCharacters = Path.GetInvalidFileNameChars();
        return string.Concat(fileName.Select(character =>
            invalidCharacters.Contains(character) ? '_' : character));
    }
}
