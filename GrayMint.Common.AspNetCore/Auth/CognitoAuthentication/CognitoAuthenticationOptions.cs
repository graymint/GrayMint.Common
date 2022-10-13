using Microsoft.Extensions.Options;

namespace GrayMint.Common.AspNetCore.Auth.CognitoAuthentication;

public class CognitoAuthenticationOptions
{
    public string CognitoArn { get; set; } = default!;
    public string CognitoClientId { get; set; } = default!;
    public TimeSpan CacheTimeout { get; set; } = TimeSpan.FromMinutes(10);

    public string CognitoRolePrefix { get; set; } = "cognito:";

    public void Validate()
    {
        var optionName = nameof(CognitoArn);
        if (string.IsNullOrEmpty(CognitoArn))
            throw new OptionsValidationException(optionName, typeof(string), new[] { string.Format(AppCommon.OptionsValidationMsgTemplate, optionName) });

        optionName = nameof(CognitoClientId);
        if (string.IsNullOrEmpty(CognitoClientId))
            throw new OptionsValidationException(optionName, typeof(string), new[] { string.Format(AppCommon.OptionsValidationMsgTemplate, optionName) });
    }
}