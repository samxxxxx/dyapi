
using DYService.Users;
using Moq;

namespace TestProject
{
    public class UnitTest1
    {
        [Fact]
        public void Test1()
        {
            var mock = new Mock<IUserService>();
            var model = new DYService.Data.RegisterModel
            {
                UserName = "Test",
            };
            mock.Setup(lib => lib.RegisterAsync(model))
                .Returns(async (DYService.Data.LogonTokenResult x) => x.Success == true);

            var lib = mock.Object;
            var result = lib.RegisterAsync(model);


        }
    }
}