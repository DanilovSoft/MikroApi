# MikroApi
```csharp
using (var con = new MikroTikConnection())
{
    con.Connect("login", "password", "192.168.88.1");

    var leases = con.Command("/ip dhcp-server lease print")
        .Query("disabled", "false") // filter
        .Proplist("address", "mac-address", "host-name", "status")
        .Send();

    con.Quit(1000);
}
```
