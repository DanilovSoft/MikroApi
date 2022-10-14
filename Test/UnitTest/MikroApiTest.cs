using System.Threading.Tasks;
using DanilovSoft.MikroApi;
using Xunit;

namespace UnitTests;

public class MikroApiTest
{
    private const string Address = "10.0.0.1";
    private const int Port = 8728;
    private const string Login = "api";
    private const string Password = "KLEZM00D";
    private const bool UseSsl = false;

    [Fact]
    public void Quit_WaitACK_Success()
    {
        using var con = new MikroTikConnection();
        con.Connect(Login, Password, Address, UseSsl, Port);
        var success = con.Quit(2000);
        Assert.True(success);
    }

    [Fact]
    public async Task QuitAsync_WaitACK_Success()
    {
        using var con = new MikroTikConnection();
        await con.ConnectAsync(Login, Password, Address, UseSsl, Port);
        var success = await con.QuitAsync(2000);
        Assert.True(success);
    }

    [Fact]
    public async Task CancelListeners_WaitACK_Success()
    {
        using var con = new MikroTikConnection();
        await con.ConnectAsync(Login, Password, Address, UseSsl, Port);
        var task = Task.Run(() => con.CancelListeners());
        var success = task.Wait(3000);
        Assert.True(success);
    }

    [Fact]
    public async Task CancelListenersAsync_WaitACK_Success()
    {
        using var con = new MikroTikConnection();
        await con.ConnectAsync(Login, Password, Address, UseSsl, Port);
        var task = con.CancelListenersAsync();
        var success = task.Wait(3000);
        Assert.True(success);
    }

    [Fact]
    public async Task CancelListener_WaitACK_Success()
    {
        using var con = new MikroTikConnection();
        await con.ConnectAsync(Login, Password, Address, UseSsl, Port);

        var listener = con.Command("/ping")
            .Attribute("address", "SERV.LAN")
            .Proplist("time")
            .Listen();

        var task = Task.Run(() => listener.Cancel());
        var success = task.Wait(3000);
        Assert.True(success);
    }

    [Fact]
    public async Task CancelListenerAsync_WaitACK_Success()
    {
        using var con = new MikroTikConnection();
        await con.ConnectAsync(Login, Password, Address, UseSsl, Port);

        var listener = con.Command("/ping")
            .Attribute("address", "SERV.LAN")
            .Proplist("time")
            .Listen();

        var task = listener.CancelAsync();
        var success = task.Wait(3000);
        Assert.True(success);
    }
}
