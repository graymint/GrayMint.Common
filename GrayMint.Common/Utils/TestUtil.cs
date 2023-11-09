using GrayMint.Common.Client;
using GrayMint.Common.Exceptions;
using System.Net;

namespace GrayMint.Common.Utils;

public static  class TestUtil
{
    public static async Task<bool> WaitForValue<TValue>(object? expectedValue, Func<TValue?> valueFactory, int timeout = 5000)
    {
        const int waitTime = 100;
        for (var elapsed = 0; elapsed < timeout; elapsed += waitTime)
        {
            if (Equals(expectedValue, valueFactory()))
                return true;

            await Task.Delay(waitTime);
        }

        return false;
    }

    public static async Task<bool> WaitForValue<TValue>(object? expectedValue, Func<Task<TValue?>> valueFactory, int timeout = 5000)
    {
        const int waitTime = 100;
        for (var elapsed = 0; elapsed < timeout; elapsed += waitTime)
        {
            if (Equals(expectedValue, await valueFactory()))
                return true;

            await Task.Delay(waitTime);
        }

        return false;
    }

    private static void AssertEquals(object? expected, object? actual, string? message)
    {
        message ??= "Unexpected Value";
        if (!Equals(expected, actual))
            throw new Exception($"{message}. Expected: {expected}, Actual: {actual}");
    }

    public static async Task AssertEqualsWait<TValue>(object? expectedValue, Func<TValue?> valueFactory, 
        string? message = null, int timeout = 5000)
    {
        await WaitForValue(expectedValue, valueFactory, timeout);
        AssertEquals(expectedValue, valueFactory(), message);
    }

    public static async Task AssertEqualsWait<TValue>(object? expectedValue, Func<Task<TValue?>> valueFactory, 
        string? message = null, int timeout = 5000)
    {
        await WaitForValue(expectedValue, valueFactory, timeout);
        AssertEquals(expectedValue, await valueFactory(), message);
    }

    public static Task AssertApiException(HttpStatusCode expectedStatusCode, Task task, string? message = null)
    {
        return AssertApiException((int)expectedStatusCode, task, message);
    }

    public static async Task AssertApiException(int expectedStatusCode, Task task, string? message = null)
    {
        try
        {
            await task;
            throw new Exception($"Expected {expectedStatusCode} but was OK. {message}");
        }
        catch (ApiException ex)
        {
            if (ex.StatusCode != expectedStatusCode)
                throw new Exception($"Expected {expectedStatusCode} but was {ex.StatusCode}. {message}");
        }
    }

    public static Task AssertApiException<T>(Task task, string? message = null)
    {
        return AssertApiException(typeof(T).Name, task, message);
    }

    public static async Task AssertApiException(string expectedExceptionType, Task task, string? message = null)
    {
        try
        {
            await task;
            throw new Exception($"Expected {expectedExceptionType} exception but was OK. {message}");
        }
        catch (ApiException ex)
        {
            if (ex.ExceptionTypeName != expectedExceptionType)
                throw new Exception($"Expected {expectedExceptionType} but was {ex.GetType().Name}. {message}");
        }
    }

    public static async Task AssertNotExistsException(Task task, string? message = null)
    {
        try
        {
            await task;
            throw new Exception($"Expected kind of {nameof(NotExistsException)} but was OK. {message}");
        }
        catch (Exception ex)
        {
            if (!NotExistsException.Is(ex))
                throw new Exception($"Expected kind of {nameof(NotExistsException)} but was {ex.GetType().Name}. {message}");
        }
    }

    public static async Task AssertAlreadyExistsException(Task task, string? message = null)
    {
        try
        {
            await task;
            throw new Exception($"Expected kind of {nameof(AlreadyExistsException)} but was OK. {message}");
        }
        catch (Exception ex)
        {
            if (!AlreadyExistsException.Is(ex))
                throw new Exception($"Expected kind of {nameof(AlreadyExistsException)} but was {ex.GetType().Name}. {message}");
        }
    }
}