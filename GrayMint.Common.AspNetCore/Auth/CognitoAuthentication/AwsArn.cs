using System.Text;

namespace GrayMint.Common.AspNetCore.Auth.CognitoAuthentication;

public class AwsArn
{
    public string Partition { get; set; }
    public string Service { get; set; }
    public string Region { get; set; }
    public string Account { get; set; }
    public string? ResourceType { get; set; }
    public string ResourceId { get; set; }

    public AwsArn(string arn)
    {
        arn = arn.Trim().Replace('/', ':');
        var items = arn.Split(':');
        Partition = items[1];
        Service = items[2];
        Region = items[3];
        Account = items[4];
        ResourceType = items.Length == 6 ? null : items[5] ;
        ResourceId = items.Length == 6 ? items[5] : items[6];
    }

    public override string ToString()
    {
        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.Append("arn:");
        stringBuilder.Append(this.Partition);
        stringBuilder.Append(":");
        stringBuilder.Append(this.Service);
        stringBuilder.Append(":");
        stringBuilder.Append(this.Region);
        stringBuilder.Append(":");
        stringBuilder.Append(this.Account);
        stringBuilder.Append(":");
        stringBuilder.Append(this.ResourceId);
        return stringBuilder.ToString();
    }
}