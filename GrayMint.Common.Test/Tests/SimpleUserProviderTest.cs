using GrayMint.Common.AspNetCore.SimpleUserManagement;
using GrayMint.Common.AspNetCore.SimpleUserManagement.Dtos;
using GrayMint.Common.Exceptions;
using GrayMint.Common.Test.Helper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GrayMint.Common.Test.Tests;

[TestClass]
public class SimpleUserProviderTest : BaseControllerTest
{
    public class UserExData
    {
        public string? FatherName { get; set; }
    }

    [TestMethod]
    public async Task Crud()
    {
        // Create
        var simpleUserProvider = TestInit1.Scope.ServiceProvider.GetRequiredService<SimpleUserProvider>();
        var request = new UserCreateRequest<UserExData>
        {
            Email = $"{Guid.NewGuid()}@local",
            FirstName = Guid.NewGuid().ToString(),
            LastName = Guid.NewGuid().ToString(),
            Description = Guid.NewGuid().ToString(),
            ExData = new UserExData{FatherName = "dad"}
        };

        var user = await simpleUserProvider.Create(request);
        Assert.AreEqual(request.Email, user.Email);
        Assert.AreEqual(request.FirstName, user.FirstName);
        Assert.AreEqual(request.LastName, user.LastName);
        Assert.AreEqual(request.Description, user.Description);
        Assert.AreEqual(request.ExData.FatherName, user.ExData?.FatherName);
        Assert.IsNotNull(user.AuthCode);
        Assert.AreNotEqual(string.Empty, user.AuthCode.Trim());



        // Get
        var user2 = await simpleUserProvider.Get(user.UserId);
        Assert.AreEqual(user.Email, user2.Email);
        Assert.AreEqual(user.FirstName, user2.FirstName);
        Assert.AreEqual(user.LastName, user2.LastName);
        Assert.AreEqual(user.Description, user2.Description);
        Assert.AreEqual(user.AuthCode, user2.AuthCode);
        Assert.AreEqual(user.CreatedTime, user2.CreatedTime);
        Assert.AreEqual(user.UserId, user2.UserId);

        var user3 = await simpleUserProvider.GetByEmail(user.Email);
        Assert.AreEqual(user.UserId, user3.UserId);
        Assert.AreEqual(user.FirstName, user3.FirstName);

        // Update
        var updateRequest = new UserUpdateRequest()
        {
            FirstName = Guid.NewGuid().ToString(),
            LastName = Guid.NewGuid().ToString(),
            Description = Guid.NewGuid().ToString(),
            Email = $"{Guid.NewGuid()}@local"
        };
        await simpleUserProvider.Update(user.UserId, updateRequest);

        // Get
        var user4 = await simpleUserProvider.Get(user.UserId);
        Assert.AreEqual(user4.Email, updateRequest.Email.Value);
        Assert.AreEqual(user4.FirstName, updateRequest.FirstName.Value);
        Assert.AreEqual(user4.LastName, updateRequest.LastName.Value);
        Assert.AreEqual(user4.Description, updateRequest.Description.Value);
        Assert.AreEqual(user4.AuthCode, user.AuthCode);

        // Remove
        await simpleUserProvider.Remove(user.UserId);
        try
        {
            await simpleUserProvider.Get(user.UserId);
            Assert.Fail("NotExistsException was expected.");
        }
        catch (Exception ex)
        {
            Assert.IsTrue(NotExistsException.Is(ex));
        }
    }

    [TestMethod]
    public async Task Fail_Already_exist()
    {
        // Create
        var simpleUserProvider = TestInit1.Scope.ServiceProvider.GetRequiredService<SimpleUserProvider>();
        var request = new UserCreateRequest
        {
            Email = $"{Guid.NewGuid()}@local",
            FirstName = Guid.NewGuid().ToString(),
            LastName = Guid.NewGuid().ToString(),
            Description = Guid.NewGuid().ToString()
        };
        await simpleUserProvider.Create(request);

        // AlreadyExists exception
        try
        {
            await simpleUserProvider.Create(request);
            Assert.Fail("NotExistsException was expected.");
        }
        catch (Exception ex)
        {
            Assert.IsTrue(AlreadyExistsException.Is(ex));
        }
    }
}