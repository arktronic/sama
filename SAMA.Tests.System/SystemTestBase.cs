using System.Collections.Concurrent;
using Microsoft.Playwright;

namespace SAMA.Tests.System;

/// <summary>
/// Base class for system tests using Playwright.
/// Each test class gets its own app instance and database schema for parallel execution.
/// </summary>
public abstract class SystemTestBase
{
    private static readonly ConcurrentDictionary<Type, TestClassContext> _contexts = new();

    private IPlaywright _playwright = null!;
    private IBrowser _browser = null!;
    private TestClassContext? _context;

    protected IPage Page { get; private set; } = null!;

    protected string BaseUrl => _context?.BaseUrl ?? throw new InvalidOperationException("Test not initialized.");

    internal static void Cleanup()
    {
        foreach (var context in _contexts.Values)
        {
            context.Dispose();
        }

        _contexts.Clear();
    }

    [TestInitialize]
    public virtual async Task TestInitializeAsync()
    {
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true,
        });
        _context = await GetOrCreateContextAsync();
        Page = await _browser.NewPageAsync(new BrowserNewPageOptions
        {
            IgnoreHTTPSErrors = true,
        });
    }

    [TestCleanup]
    public virtual async Task TestCleanupAsync()
    {
        await Page.CloseAsync();
        await _browser.DisposeAsync();
        _playwright.Dispose();
    }

    protected async Task LoginAsync(string email = "admin@example.com", string password = "TestPassword123!")
    {
        await Page.GotoAsync($"{BaseUrl}/Account/Login");
        await Page.FillAsync("input[name='Input.Email']", email);
        await Page.FillAsync("input[name='Input.Password']", password);
        await Page.ClickAsync("button[type='submit']");
        await Page.WaitForURLAsync($"{BaseUrl}/**");
    }

    private async Task<TestClassContext> GetOrCreateContextAsync()
    {
        var testClassType = GetType();

        if (_contexts.TryGetValue(testClassType, out var existing))
        {
            return existing;
        }

        var context = await TestClassContext.CreateAsync();

        if (!_contexts.TryAdd(testClassType, context))
        {
            // Another thread created it first, dispose ours and use theirs
            context.Dispose();
            return _contexts[testClassType];
        }

        return context;
    }
}
