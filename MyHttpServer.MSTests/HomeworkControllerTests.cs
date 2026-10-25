using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MyHttpServer.Controllers;

namespace MyHttpServer.MSTests;

[TestClass]
public sealed class HomeworkControllerTests
{
    [TestMethod]
    public async Task Send_EmptyEmail_Returns400()
    {
        // Arrange: подготовка.
        var controller = new HomeworkController();

        // Act: передаём пустую почту.
        var result = await controller.Send("");

        // Assert: проверяем код результата.
        Assert.AreEqual(400, result.StatusCode);
    }

    [TestMethod]
    [DataRow(" ")]
    [DataRow("   ")]
    [DataRow("\t")]
    [DataRow("\n")]
    public async Task Send_WhitespaceEmail_Returns400(
        string email)
    {
        // Arrange.
        var controller = new HomeworkController();

        // Act.
        var result = await controller.Send(email);

        // Assert.
        Assert.AreEqual(400, result.StatusCode);
    }

    [TestMethod]
    [DataRow("не-почта")]
    [DataRow("user@")]
    [DataRow("@example.com")]
    [DataRow("user@@example.com")]
    public async Task Send_InvalidEmail_Returns400(
        string email)
    {
        // Arrange.
        var controller = new HomeworkController();

        // Act.
        var result = await controller.Send(email);

        // Assert.
        Assert.AreEqual(400, result.StatusCode);
    }
}