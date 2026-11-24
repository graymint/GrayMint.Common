using System.Diagnostics.CodeAnalysis;
using System.Web;
using Microsoft.AspNetCore.Components;

namespace GrayMint.Common.Blazor.Utils;

public static class NavigationManagerExtensions
{
    extension(NavigationManager navigationManager)
    {
        public string ReplaceQueryValue(string key, string? value)
        {
            return navigationManager.ReplaceQueryValue(new Dictionary<string, string?> { { key, value } });
        }

        public string ReplaceQueryValue(Dictionary<string, string?> keyValues)
        {
            var uri = navigationManager.ToAbsoluteUri(navigationManager.Uri);
            var queryString = HttpUtility.ParseQueryString(uri.Query);
            foreach (var keyValue in keyValues) {
                queryString.Remove(keyValue.Key);
                if (!string.IsNullOrWhiteSpace(keyValue.Value))
                    queryString.Add(keyValue.Key, keyValue.Value);
            }

            var url = uri.GetLeftPart(UriPartial.Path).TrimEnd('/') + "?" + queryString;
            return url.TrimEnd('/', '?');
        }

        public void SetQueryValue(string key, string? value)
        {
            var url = navigationManager.ReplaceQueryValue(key, value);
            navigationManager.NavigateTo(url, false, true);
        }

        public void SetQueryValue(Dictionary<string, string?> keyValues)
        {
            var url = navigationManager.ReplaceQueryValue(keyValues);
            navigationManager.NavigateTo(url, false, true);
        }

        public string RemoveQueryStringByKey(string key)
        {
            var uri = navigationManager.ToAbsoluteUri(navigationManager.Uri);
            var newQueryString = HttpUtility.ParseQueryString(uri.Query);
            newQueryString.Remove(key);

            var pagePathWithoutQueryString = uri.GetLeftPart(UriPartial.Path);

            return newQueryString.Count > 0
                ? $"{pagePathWithoutQueryString}?{newQueryString}"
                : pagePathWithoutQueryString;
        }

        public T GetQueryString<T>(string key)
        {
            return navigationManager.TryGetQueryString<T>(key, out var value)
                ? value
                : throw new KeyNotFoundException($"{key} does not exist in query string!");
        }

        public bool TryGetQueryString<T>(string key, [NotNullWhen(true)] out T? value)
        {
            var uri = navigationManager.ToAbsoluteUri(navigationManager.Uri);
            var queryParams = HttpUtility.ParseQueryString(uri.Query);
            var valueString = queryParams[key];
            value = default;

            if (valueString == null)
                return false;

            if (typeof(T) == typeof(int) && int.TryParse(valueString, out var intValue)) {
                value = (T)(object)intValue;
                return true;
            }

            if (typeof(T) == typeof(string)) {
                value = (T)(object)valueString;
                return true;
            }

            if (typeof(T) == typeof(Guid?) || typeof(T) == typeof(Guid)) {
                value = (T)(object)Guid.Parse(valueString);
                return true;
            }

            if (typeof(T) == typeof(decimal) && decimal.TryParse(valueString, out var valueAsDecimal)) {
                value = (T)(object)valueAsDecimal;
                return true;
            }

            value = default;
            return false;
        }
    }
}