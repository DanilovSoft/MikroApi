using System;
using System.Text;

namespace MikroApi
{
    public class MikroTikConnectionPoolBuilder
    {
        internal int? _minimumCount;
        internal int? _maximumCount;
        internal string _userName;
        internal string _password;
        internal string _host;
        internal int? _port;
        internal TimeSpan _lifetime;
        internal Encoding _encoding;

        public MikroTikConnectionPoolBuilder MinimumCount(int minimumCount)
        {
            _minimumCount = minimumCount;
            return this;
        }

        public MikroTikConnectionPoolBuilder MaximumCount(int maximumCount)
        {
            _maximumCount = maximumCount;
            return this;
        }

        public MikroTikConnectionPoolBuilder DefaultEncoding(Encoding encoding)
        {
            _encoding = encoding;
            return this;
        }

        public MikroTikConnectionPoolBuilder UserName(string userName)
        {
            _userName = userName;
            return this;
        }

        public MikroTikConnectionPoolBuilder Password(string password)
        {
            _password = password;
            return this;
        }

        public MikroTikConnectionPoolBuilder Address(string host, int port)
        {
            _host = host;
            _port = port;
            return this;
        }

        public MikroTikConnectionPoolBuilder Lifetime(TimeSpan lifetime)
        {
            _lifetime = lifetime;
            return this;
        }

        public MikroTikConnectionPool Build()
        {
            if (_userName == null)
                throw new ArgumentOutOfRangeException("userName");

            if (_password == null)
                throw new ArgumentOutOfRangeException("password");

            if (_host == null)
                throw new ArgumentOutOfRangeException("host");

            if (_port == null)
                throw new ArgumentOutOfRangeException("port");

            return new MikroTikConnectionPool(this);
        }
    }
}
