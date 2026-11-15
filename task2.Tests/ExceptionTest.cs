using task2.Exceptions;
using Xunit;

namespace task2.Tests;

public class ExceptionTests
{
    [Fact]
    public void NotFoundException_ShouldSetMessage()
    {
        var exception = new NotFoundException("Not found");
        Assert.Equal("Not found", exception.Message);
    }

    [Fact]
    public void BadRequestException_ShouldSetMessage()
    {
        var exception = new BadRequestException("Bad request");
        Assert.Equal("Bad request", exception.Message);
    }
}