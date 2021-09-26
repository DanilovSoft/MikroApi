using System;
using System.Threading.Tasks;
using DanilovSoft.MikroApi;
using Xunit;

namespace UnitTests
{
    public class MikroApiTest
    {
        private const string Address = "10.0.0.1";
        private const int Port = 8728;
        private const string Login = "api";
        private const string Password = "api";

        [Fact]
        public void TestQuit()
        {
            using (var con = new MikroTikConnection())
            {
                con.Connect(Login, Password, Address, Port);
                bool success = con.Quit(2000);
                Assert.True(success);
            }
        }

        [Fact]
        public async Task TestQuitAsync()
        {
            using (var con = new MikroTikConnection())
            {
                await con.ConnectAsync(Login, Password, Address, Port);
                bool success = await con.QuitAsync(2000);
                Assert.True(success);
            }
        }

        [Fact]
        public async Task TestCancelListeners()
        {
            using (var con = new MikroTikConnection())
            {
                await con.ConnectAsync(Login, Password, Address, Port);
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
                await con.ConnectAsync(Login, Password, Address, Port);
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
                await con.ConnectAsync(Login, Password, Address, Port);

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
                await con.ConnectAsync(Login, Password, Address, Port);

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
