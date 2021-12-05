using App;
using Xunit;

namespace Tests
{
    public class LineParsingUnitTests
    {
        [Fact]
        public void ParseMessageTest()
        {
            const string text = "some message";
            var line = Line.Parse(text);
            var message = Assert.IsType<Line.Message>(line);
            Assert.Equal("some message", message.Value);
        }
        
        [Fact]
        public void ParseEnterRoomTest()
        {
            const string text = "/enter room";
            var line = Line.Parse(text);
            var command = Assert.IsType<Line.EnterRoomCommand>(line);
            Assert.Equal("room", command.Name);
        }
        
        [Fact]
        public void ParseLeaveRoomTest()
        {
            const string text = "/leave";
            var line = Line.Parse(text);
            Assert.IsType<Line.LeaveRoomCommand>(line);
        }
        
        [Fact]
        public void ParseExitTest()
        {
            const string text = "/exit";
            var line = Line.Parse(text);
            Assert.IsType<Line.ExitCommand>(line);
        }

        [Theory]
        [InlineData("/bad-command", "Unknown command")]
        [InlineData("/enter a b", "/enter command requires exactly one argument")]
        [InlineData("/enter", "/enter command requires exactly one argument")]
        [InlineData("/leave a", "/leave command does not have arguments")]
        [InlineData("/exit a", "/exit command does not have arguments")]
        public void ParseErrorTest(string text, string expectedReason)
        {
            var line = Line.Parse(text);
            var invalidLine = Assert.IsType<Line.InvalidLine>(line);
            Assert.Equal(expectedReason, invalidLine.Reason);
        }
    }
}