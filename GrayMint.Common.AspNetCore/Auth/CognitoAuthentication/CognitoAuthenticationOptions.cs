using Microsoft.Extensions.Options;

namespace GrayMint.Common.AspNetCore.Auth.CognitoAuthentication;

public class CognitoAuthenticationOptions
{
    public string CognitoIssuer { get; set; } = default!;
    public string CognitoClientId { get; set; } = default!;
    public string CognitoRolePrefix { get; set; } = "cognito:";

    public void Validate()
    {
        var optionName = nameof(CognitoIssuer);
        if (string.IsNullOrEmpty(CognitoIssuer))
            throw new OptionsValidationException(optionName, typeof(string), new[] { string.Format(AppCommon.OptionsValidationMsgTemplate, optionName) });

        optionName = nameof(CognitoClientId);
        if (string.IsNullOrEmpty(CognitoClientId))
            throw new OptionsValidationException(optionName, typeof(string), new[] { string.Format(AppCommon.OptionsValidationMsgTemplate, optionName) });
    }
}