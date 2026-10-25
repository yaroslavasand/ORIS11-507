using System.Threading.Tasks;
using MyHttpServer.Controllers;
using Xunit;

namespace MyHttpServer.Tests;

public sealed class HomeworkControllerTests
{
    [Fact]
    public async Task Send_EmptyEmail_Returns400()
    {
        // Arrange: подготовка.
        var controller = new HomeworkController();

        // Act: вызываем проверяемый метод.
        var result = await controller.Send("");

        // Assert: проверяем результат.
        Assert.Equal(400, result.StatusCode);
    }
}