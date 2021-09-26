using DanilovSoft.MikroApi;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleAppCore
{
    class Program
    {
        private static async Task Main()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            // Support localized comments.
            MikroTikConnection.DefaultEncoding = Encoding.GetEncoding("windows-1251"); // RUS

            using (var con = new MikroTikConnection(Encoding.GetEncoding("windows-1251")))
            {
                con.Connect("api_dbg", "debug_password", "10.0.0.1");

                var leases = con.Command("/ip dhcp-server lease print")
                    .Query("disabled", "false") // filter
                    .Proplist("address", "mac-address", "host-name", "status")
                    .Send();

                con.Quit(1000);
            }

            //// Отправляет запрос без получения результата.
            //var listener = con.Command("/ping")
            //    .Attribute("address", "SERV.LAN")
            //    .Attribute("interval", "1")
            //    .Attribute("count", "4")
            //    .Proplist("time")
            //    .Listen();

            //// сервер будет присылать результат каждую секунду.
            //while (!listener.IsComplete)
            //{
            //    MikroTikResponseFrame result;
            //    try
            //    {
            //        result = listener.ListenNext();
            //    }
            //    catch (MikroTikDoneException)
            //    {

            //        break;
            //    }

            //    Console.WriteLine(result);

            //    listener.Cancel(true);
            //}

            //var logListener = con.Command("/log listen")
            //    .Listen();

            //while (!logListener.IsComplete)
            //{
            //    try
            //    {
            //        logListener.ListenNext();
            //    }
            //    catch (TimeoutException)
            //    {

            //    }

            //    var entry = logListener.ListenNext();
            //    Console.WriteLine(entry);
            //}

            //// Вытащить активные сессии юзера.
            //var activeSessionsResult = con.Command("/ip hotspot active print")
            //    .Proplist(".id")
            //    .Query("user", "2515")
            //    .Send();

            //string[] activeSessions = activeSessionsResult.ScalarArray(".id");
            //Thread.Sleep(-1);



            //resultPrint.Scalar("");
            //resultPrint.ScalarList();
            //resultPrint.ScalarOrDefault();

            //var sw = Stopwatch.StartNew();
            //for (int i = 0; i < 500_000; i++)
            //{
            //    resultPrint.ToArray(new { });
            //    resultPrint.ScalarList<int>();
            //    var row = resultPrint.ToList<InterfaceDto>();

            //}
            //sw.Stop();
            //Trace.WriteLine(sw.Elapsed);
            //Console.WriteLine("OK");
            //Thread.Sleep(-1);

            // Команда выполняется 20 сек.
            //var task = con.Command("/delay")
            //    .Attribute("delay-time", "20")
            //    .Send();

            // Tell router we are done.
            //con.Quit(1000);
        }

        class Log
        {
            [MikroTikProperty(".id")]
            public string Id { get; set; }
            [MikroTikProperty("time")]
            public string Time { get; set; }
            [MikroTikProperty("message")]
            public string Message { get; set; }
            [MikroTikProperty("topics")]
            public string Topics { get; set; }
        }

        private static void PingHostAsync(MikroTikConnection con, string host, int intervalSec, CancellationToken cancellationToken)
        {
            var listener = con.Command("/ping")
                    .Attribute("address", "SERV.LAN")
                    .Attribute("interval", intervalSec.ToString())
                    .Proplist("time")
                    .Listen();

            while (!listener.IsComplete)
            {
                MikroTikResponseFrameDictionary result;
                try
                {
                    result = listener.ListenNext();
                    if (cancellationToken.IsCancellationRequested)
                    {
                        listener.Cancel();
                    }
                }
                catch (MikroApiCommandInterruptedException)
                {
                    // Операция прервана по запросу Cancel
                    return;
                }
                catch (Exception)
                {
                    // Обрыв соединения.
                    return;
                }

                Console.WriteLine(result);
            }
        }

        private static void ListenInterfaces(MikroTikFlowCommand command)
        {
            var listener = command.Listen();

            while (!listener.IsComplete)
            {
                MikroTikResponseFrameDictionary result;
                try
                {
                    result = listener.ListenNext();
                }
                catch (MikroApiCommandInterruptedException)
                {
                    // Операция прервана по запросу Cancel
                    return;
                }
                catch (Exception)
                {
                    // Обрыв соединения.
                    return;
                }

                Console.WriteLine(result);
            }
        }
    }

    class InterfaceDto
    {
        [MikroTikProperty("name")]
        public string Name { get; set; }

        [MikroTikProperty("mtu")]
        public string Mtu { get; set; }

        [MikroTikProperty("comment")]
        public string Comment { get; set; }
    }
}
