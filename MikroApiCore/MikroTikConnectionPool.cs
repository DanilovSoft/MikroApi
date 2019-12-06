using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ObjectPoolCore;

namespace MikroApi
{
    public class MikroTikConnectionPool
    {
        ObjectPool<MikroTikConnectionSlot> _pool;

        public static MikroTikConnectionPoolBuilder Create()
        {
            return new MikroTikConnectionPoolBuilder();
        }

        internal MikroTikConnectionPool(MikroTikConnectionPoolBuilder builder)
        {
            _pool = ObjectPool.Create<MikroTikConnectionSlot>()
                .MinSize(builder._minimumCount.Value)
                .MaxSize(builder._maximumCount.Value)
                .Lifetime(builder._lifetime)
                .BeforeTake(BeforeTake)
                .Creator(() => CreateConnection(builder._host, builder._port.Value, builder._userName, builder._password, builder._encoding))
                .Destroyer(x => x.DisposeInnerObject())
                .Build();
        }

        static MikroTikConnectionSlot CreateConnection(string host, int port, string login, string password, Encoding encoding)
        {
            MikroTikConnectionSlot con = new MikroTikConnectionSlot(encoding);
            try
            {
                con.Connect(host, port, login, password);
            }
            catch
            {
                con.Dispose();
                throw;
            }

            return con;
        }

        static void BeforeTake(BeforeTakeArgs<MikroTikConnectionSlot> args)
        {
            try
            {
                args.Object.Command("/put")
                    .GetResponse();
            }
            catch
            {
                args.Destroy = true;
            }
        }

        public MikroTikConnection Take()
        {
            var item = _pool.Take();
            item.Object.PooledObject = item;
            return item.Object;
        }
    }
}
