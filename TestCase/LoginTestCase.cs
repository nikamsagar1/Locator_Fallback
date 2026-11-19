using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Android;
using OpenQA.Selenium.Support.UI;
using Locator_Fallback.Utilities;

namespace Locator_Fallback.TestCase;

[TestFixture]
[NonParallelizable]
public sealed class LoginTestCase
{
    #region Variables

    private static AppiumDriver? _driver;
    private const string Username = "trainer@way2automation.com";
    private const string Password = "way2automation";
    private const string DeviceName = "Android";
    private const string AndroidDeviceId = "RZCX20NPDYM";

    #endregion

    #region App URLs

    private static readonly Uri AppiumServerUrl = new("http://127.0.0.1:4723");
    private const string AppPath = @"E:\AS2986\Mobile_Automation_RMS\APKs\automation-practice.apk";
    private const string AndroidAppPackage = "com.way2automation.medishop";
    private const string AndroidAppActivity = "com.example.MainActivity";
    // private const string IosBundleId = "com.apexnetworks.rms";

    private static AppiumDriver CreateDriver()
    {
        if (!Platform.Equals("Android", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("This test is currently configured for Android only.");
        }

        var options = new AppiumOptions { PlatformName = "Android" };
        options.DeviceName = DeviceName;
        options.AddAdditionalAppiumOption("udid", AndroidDeviceId);
        options.AddAdditionalAppiumOption("autoGrantPermissions", true);

        if (!string.IsNullOrWhiteSpace(AppPath)) options.App = AppPath;

        options.AutomationName = "UiAutomator2";
        if (string.IsNullOrWhiteSpace(AppPath))
        {
            options.AddAdditionalAppiumOption("appPackage", AndroidAppPackage);
            if (!string.IsNullOrWhiteSpace(AndroidAppActivity))
                options.AddAdditionalAppiumOption("appActivity", AndroidAppActivity);
        }

        return new AndroidDriver(AppiumServerUrl, options);
    }

    #endregion

    #region Platform Selection

    private const string Platform = "Android"; // iOS setup is intentionally omitted for now.

    #endregion

    #region Locator Lists

    private static readonly IReadOnlyList<By> AndroidUsernameLocators =
    [
        MobileBy.AndroidUIAutomator("new UiSelector().className(\"android.widget.EditText\").instance(0)"),
        By.XPath("//android.widget.EditText[1]"),
        MobileBy.AndroidUIAutomator("new UiSelector().resourceId(\"email_id\")"),
        By.XPath("//android.widget.EditText[@resource-id=\"email_id\"]")
    ];

    private static readonly IReadOnlyList<By> AndroidPasswordLocators =
    [
        MobileBy.AndroidUIAutomator("new UiSelector().className(\"android.widget.EditText\").instance(1)"),
        By.XPath("//android.widget.EditText[2]"),
        MobileBy.AndroidUIAutomator("new UiSelector().resourceId(\"password_id\")"),
        By.XPath("//android.widget.EditText[@resource-id=\"password_id\"]")
    ];

    private static readonly IReadOnlyList<By> AndroidLoginButtonLocators =
    [
        MobileBy.AndroidUIAutomator("new UiSelector().className(\"android.widget.Button\").instance(2)"),
        MobileBy.AndroidUIAutomator("new UiSelector().description(\"Sign In\")"),
        By.XPath("//android.widget.Button[@content-desc=\"Sign In\"]"),
        By.XPath("//android.view.View[@resource-id=\"signin_button\"]/android.widget.Button")
    ];

    private static readonly By InvalidCredentialsMessages =
        By.XPath("//*[contains(@text, 'Invalid credentials') or contains(@content-desc, 'Invalid credentials')]");

    #endregion

    [SetUp]
    public void SetUp()
    {
        _driver = CreateDriver();
        Driver.Manage().Timeouts().ImplicitWait = TimeSpan.Zero;
    }

    [Test]
    public void Login_WithInvalidCredentials_DisplaysErrorsForBothFields()
    {
        SendKeys(AndroidUsernameLocators, Username, "username field", nameof(AndroidUsernameLocators));
        SendKeys(AndroidPasswordLocators, "invalid-password", "password field", nameof(AndroidPasswordLocators));
        FindVisibleElement(AndroidLoginButtonLocators, "Sign In button", nameof(AndroidLoginButtonLocators)).Click();

        var errorsVisible = new WebDriverWait(Driver, TimeSpan.FromSeconds(10)).Until(driver =>
            driver.FindElements(InvalidCredentialsMessages).Count(element => element.Displayed) >= 2);

        Assert.That(errorsVisible, Is.True, "Invalid credentials should be displayed for both fields.");
        Assert.That(IsElementDisplayed(Driver, LoginEmailLocator), Is.True, "The email field should remain visible after a failed login.");
    }

    [Test]
    public void Login_WithValidCredentials_HidesEmailField()
    {
        SendKeys(AndroidUsernameLocators, Username, "username field", nameof(AndroidUsernameLocators));
        SendKeys(AndroidPasswordLocators, Password, "password field", nameof(AndroidPasswordLocators));
        FindVisibleElement(AndroidLoginButtonLocators, "Sign In button", nameof(AndroidLoginButtonLocators)).Click();

        var loginFormClosed = new WebDriverWait(Driver, TimeSpan.FromSeconds(15)).Until(driver =>
            !IsElementDisplayed(driver, LoginEmailLocator));

        Assert.That(loginFormClosed, Is.True, "The email field should disappear after a successful login.");
    }

    [TearDown]
    public void TearDown()
    {
        var driver = _driver;
        if (driver is null) return;

        try { driver.Quit(); }
        finally { _driver = null; }
    }

    private static AppiumDriver Driver => _driver ?? throw new InvalidOperationException("The Appium driver has not been initialized.");

    private static By LoginEmailLocator => AndroidUsernameLocators[AndroidUsernameLocators.Count - 1];

    private static bool IsElementDisplayed(IWebDriver driver, By locator) =>
        driver.FindElements(locator).Any(element => element.Displayed);

    private static IWebElement SendKeys(IReadOnlyList<By> locators, string text, string elementName, string locatorListName)
    {
        ArgumentNullException.ThrowIfNull(text);
        var element = FindVisibleElement(locators, elementName, locatorListName: locatorListName);
        element.Clear();
        element.SendKeys(text);
        return element;
    }

    private static IWebElement FindVisibleElement(IReadOnlyList<By> locators, string elementName, string locatorListName, int timeoutInSeconds = 3, bool requireEnabled = true)
    {
        return LocatorResolver.FindVisibleElement(
            Driver,
            locators,
            TestContext.CurrentContext.Test.Name,
            elementName,
            locatorListName,
            message => TestContext.Out.WriteLine(message),
            TimeSpan.FromSeconds(timeoutInSeconds),
            requireEnabled,
            TestContext.CurrentContext.WorkDirectory);
    }
}
