using System;
using System.Threading.Tasks;
using DanilovSoft.MikroApi;
using Xunit;

namespace UnitTests
{
    public class MikroApiTest
    {
        private const string Address = "192.168.88.1";
        private const int Port = 8728;
        private const string Login = "api";
        private const string Password = "api";

        [Fact]
        public void TestQuit()
        {
            using (var con = new MikroTikConnection())
            {
                con.Connect(Address, Port, Login, Password);
                bool success = con.Quit(2000);
                Assert.True(success);
            }
        }

        [Fact]
        public async Task TestQuitAsync()
        {
            using (var con = new MikroTikConnection())
            {
                await con.ConnectAsync(Address, Port, Login, Password);
                bool success = await con.QuitAsync(2000);
                Assert.True(success);
            }
        }

        [Fact]
        public async Task TestCancelListeners()
        {
            using (var con = new MikroTikConnection())
            {
                await con.ConnectAsync(Address, Port, Login, Password);
                var task = Task.Run(() => con.CancelListeners());
                bool success = task.Wait(3000);
                Assert.True(success);
            }
        }

        [Fact]
        public async Task TestCancelListenersAsync()
        {
            using (var con = new MikroTikConnection())
            {
                await con.ConnectAsync(Address, Port, Login, Password);
                var task = con.CancelListenersAsync();
                bool success = task.Wait(3000);
                Assert.True(success);
            }
        }

        [Fact]
        public async Task TestCancelListener()
        {
            using (var con = new MikroTikConnection())
            {
                await con.ConnectAsync(Address, Port, Login, Password);

                var listener = con.Command("/ping")
                    .Attribute("address", "SERV.LAN")
                    .Proplist("time")
                    .Listen();

                var task = Task.Run(() => listener.Cancel());
                bool success = task.Wait(3000);
                Assert.True(success);
            }
        }

        [Fact]
        public async Task TestCancelListenerAsync()
        {
            using (var con = new MikroTikConnection())
            {
                await con.ConnectAsync(Address, Port, Login, Password);

                var listener = con.Command("/ping")
                    .Attribute("address", "SERV.LAN")
                    .Proplist("time")
                    .Listen();

                var task = listener.CancelAsync();
                bool success = task.Wait(3000);
                Assert.True(success);
            }
        }
    }
}
