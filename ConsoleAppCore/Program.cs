using DanilovSoft.MikroApi;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleAppCore
{
    class Program
    {
        private static void Main()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            // Support localized comments.
            MikroTikConnection.DefaultEncoding = Encoding.GetEncoding("windows-1251"); // RUS

            using (var con = new MikroTikConnection())
            {
                con.Connect("10.0.0.1", 8728, "api", "api", RouterOsVersion.Post_v6_43);

                var arp = con
                    .Command("/ip arp print")
                    .Send();

                // Отправляет запрос без получения результата.
                var listener = con.Command("/ping")
                    .Attribute("address", "SERV.LAN")
                    .Attribute("interval", "1")
                    .Attribute("count", "4")
                    .Proplist("time")
                    .Listen();

                // сервер будет присылать результат каждую секунду.
                while (!listener.IsComplete)
                {
                    MikroTikResponseFrame result;
                    try
                    {
                        result = listener.Listen();
                    }
                    catch (MikroTikDoneException)
                    {

                        break;
                    }

                    Console.WriteLine(result);

                    listener.Cancel(true);
                }

                var logListener = con.Command("/log listen")
                    .Listen();

                //var logs = con.Command("/log print")
                //    .Send();

                //var logs2 = logs.ToList<Log>();

                //var dict = logs2.ToDictionary(x => x.Id, x => x);

                //logs.ToList()

                while (!logListener.IsComplete)
                {
                    try
                    {
                        logListener.Listen(1000);
                    }
                    catch (TimeoutException)
                    {

                    }

                    var entry = logListener.Listen();
                    Console.WriteLine(entry);
                }

                // Вытащить активные сессии юзера.
                var activeSessionsResult = con.Command("/ip hotspot active print")
                    .Proplist(".id")
                    .Query("user", "2515")
                    .Send();

                string[] activeSessions = activeSessionsResult.ScalarArray(".id");
                Thread.Sleep(-1);

                MikroTikResponse resultPrint = con.Command("/interface print")
                    .Query("name", "sfp1")
                    //.Proplist("comment", "name")
                    .Send();

                resultPrint.Scalar("");
                resultPrint.ScalarList();
                resultPrint.ScalarOrDefault();

                var sw = Stopwatch.StartNew();
                for (int i = 0; i < 500_000; i++)
                {
                    resultPrint.ToArray(new { });
                    resultPrint.ScalarList<int>();
                    var row = resultPrint.ToList<Row>();

                }
                sw.Stop();
                Trace.WriteLine(sw.Elapsed);
                Console.WriteLine("OK");
                Thread.Sleep(-1);

                // Команда выполняется 20 сек.
                var task = con.Command("/delay")
                    .Attribute("delay-time", "20")
                    .Send();

                // Tell router we are done.
                con.Quit(1000);
            }
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
                MikroTikResponseFrame result;
                try
                {
                    result = listener.Listen();
                    if (cancellationToken.IsCancellationRequested)
                    {
                        listener.Cancel();
                    }
                }
                catch (MikroTikCommandInterruptedException)
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
                MikroTikResponseFrame result;
                try
                {
                    result = listener.Listen();
                }
                catch (MikroTikCommandInterruptedException)
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

    class Row
    {
        [MikroTikProperty("name")]
        public string Name { get; set; }

        [MikroTikProperty("mtu")]
        public string mtu { get; set; }
    }
}
