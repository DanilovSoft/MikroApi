using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Security.Cryptography;

namespace MikroApi
{
    public class MikroTikConnection : IDisposable
	{
		byte[] _readBuffer;
        Encoding _encoding;
		NetworkStream _nstream;
        MemoryStream _requestBuffer;

        public MikroTikConnection(Encoding encoding) : this()
        {
            if (encoding != null)
                _encoding = encoding;
        }

        public MikroTikConnection()
        {
            _readBuffer = new byte[1024 * 4];
            _requestBuffer = new MemoryStream();
            _encoding = Encoding.UTF8;
        }

        public void Connect(string hostname, int port, string login, string password)
		{
            TcpClient tcp = new TcpClient();
            tcp.ConnectAsync(hostname, port).GetAwaiter().GetResult();
            _nstream = tcp.GetStream();

            Login(login, password);
        }

		void Login(string login, string password)
		{
			AppendLine("/login");
            MikroTikResponse resp = GetResponse();
            string hash = resp[0]["ret"];
            string response = EncodePassword(password, hash);

            AppendLine("/login")
			    .Attribute("name", login)
			    .Attribute("response", response);
			    GetResponse();
		}

        public MikroTikConnection Command(string command)
        {
            string formatted = string.Join("/", command.Split(' '));
            return AppendLine(formatted);
        }

        public MikroTikConnection Query(string query)
        {
            return AppendLine($"?{query}");
        }

        public MikroTikConnection Query(string query, string value)
        {
            return AppendLine($"?{query}={value}");
        }

        public MikroTikConnection Query(string query, params string[] values)
        {
            string joined = string.Join(",", values);
            return AppendLine($"?{query}={joined}");
        }

        public MikroTikConnection Attribute(string name)
        {
            return AppendLine($"={name}=");
        }

        public MikroTikConnection Attribute(string name, string value)
        {
            return AppendLine($"={name}={value}");
        }

        public MikroTikConnection Attribute(string name, params string[] values)
        {
            string joined = string.Join(",", values);
            return AppendLine($"={name}={joined}");
        }

        public MikroTikConnection Attribute(string name, IEnumerable<string> values)
        {
            string joined = string.Join(",", values);
            return AppendLine($"={name}={joined}");
        }

        public MikroTikConnection AppendLine(string message)
		{
			byte[] data = _encoding.GetBytes(message);
			byte[] size = EncodeLength((uint)data.Length);
            _requestBuffer.Write(size, 0, size.Length);
            _requestBuffer.Write(data, 0, data.Length);
            return this;
		}

        public T[] Array<T>(Func<MikroTikResponseValues, T> selector)
        {
            return List(selector)
                .ToArray();
        }

        public T[] Array<T>() where T : new()
        {
            var resp = GetResponse();

            T[] items = new T[resp.Count];

            var binder = new DataBinder<T>();
            for (int i = 0; i < resp.Count; i++)
            {
                items[i] = binder.Bind(resp[i]);
            }

            return items;
        }

        public T[] Array<T>(T @object)
        {
            var mapper = new AnonymousObjectMapper<T>();
            var resp = GetResponse();
            T[] array = new T[resp.Count];
            for (int i = 0; i < resp.Count; i++)
            {
                array[i] = mapper.ReadObject(resp[i]);
            }
            return array;
        }

        public List<T> List<T>(Func<MikroTikResponseValues, T> selector)
        {
            return GetResponse()
                .Select(x => selector(x))
                .ToList();
        }

        public MikroTikResponse GetResponse()
		{
            _requestBuffer.WriteByte(0);
            _requestBuffer.Position = 0;
            _requestBuffer.CopyTo(_nstream);
            _requestBuffer.SetLength(0);
            MikroTikResponse response = Read();
            return response;
        }

        public string ScalarOrDefault()
        {
            var resp = GetResponse();
            if (resp.Count > 0)
            {
                return resp[0].First().Value;
            }
            else
                return null;
        }

        public string Scalar(string name)
        {
            var resp = GetResponse();
            return resp[0][name];
        }

		MikroTikResponse Read()
		{
			bool trap = false;
			bool done = false;
			bool fatal = false;
			string fatalMessage = null;
            MikroTikResponse sentences = new MikroTikResponse();
			var words = new MikroTikResponseValues();
		
			int count;
			while (true)
			{
				Receive(_readBuffer, 0, 1);
				if (_readBuffer[0] == 0)
				{
					if (words.Count > 0)
						sentences.Add(words);

					if (fatal)
						throw new MikroTikFatalException(fatalMessage);

					if (trap)
						ThrowTrap(sentences[0]);

					if (done)
						break;

					words = new MikroTikResponseValues();
					continue;
				}
				else
					count = GetSize(_readBuffer);

				byte[] buf = _readBuffer;

				if(count > buf.Length)
					buf = new byte[count];

				Receive(buf, 0, count);

				string word = _encoding.GetString(buf, 0, count);
				if (word == "!re")
					continue;

				if (!done && word == "!done")
				{
					done = true;
					continue;
				}

				if (!fatal && word == "!fatal")
				{
					fatal = true;
					continue;
				}

				if (!trap && word == "!trap")
				{
					trap = true;
					continue;
				}

				var pos = word.IndexOf('=', 1);
				if (pos >= 0)
				{
					var key = word.Substring(1, pos - 1);
					if (!words.ContainsKey(key))
						words.Add(key, word.Substring(pos + 1));
				}
				else if (fatal)
					fatalMessage = word;
			}

			return sentences;
		}

		void Receive(byte[] buffer, int offset, int count)
		{
			int n;
			while ((count -= n = _nstream.Read(buffer, offset, count)) > 0)
			{
				if (n == 0) 
					throw new MikroTikDisconnectException();

				offset += n;
			}
		}

		void ThrowTrap(Dictionary<string, string> pairs)
		{
			string v;
            TrapCategory? category = null;
			if (pairs.TryGetValue("category", out v))
				category = (TrapCategory)int.Parse(v);

			string message;
			if (!pairs.TryGetValue("message", out message))
				message = null;

			throw new MikroTikTrapException(category, message);
		}

		byte[] EncodeLength(uint len)
		{
			if (len < 128)
			{
                byte[] tmp = BitConverter.GetBytes(len);
				return new byte[] { tmp[0] };
			}
			if (len < 16384)
			{
				byte[] tmp = BitConverter.GetBytes(len | 0x8000);
				return new byte[] { tmp[1], tmp[0] };
			}
			if (len < 0x200000)
			{
				byte[] tmp = BitConverter.GetBytes(len | 0xC00000);
				return new byte[] { tmp[2], tmp[1], tmp[0] };
			}
			if (len < 0x10000000)
			{
				byte[] tmp = BitConverter.GetBytes(len | 0xE0000000);
				return new byte[] { tmp[3], tmp[2], tmp[1], tmp[0] };
			}
			else
			{
				byte[] tmp = BitConverter.GetBytes(len);
				return new byte[] { 0xF0, tmp[3], tmp[2], tmp[1], tmp[0] };
			}
		}

		string EncodePassword(string password, string hash)
		{
			byte[] bHash = Enumerable.Range(0, hash.Length).Where(x => x % 2 == 0).Select(x => Convert.ToByte(hash.Substring(x, 2), 16)).ToArray();
			byte[] pass = _encoding.GetBytes(password);
			byte[] buf = new byte[pass.Length + bHash.Length + 1];

            System.Array.Copy(pass, 0, buf, 1, pass.Length);
            System.Array.Copy(bHash, 0, buf, pass.Length + 1, bHash.Length);

            using (var md5 = MD5.Create())
            {
                return "00" + string.Join("", md5.ComputeHash(buf).Select(x => x.ToString("X2")));

                //md5.TransformBlock(new byte[] { 0 }, 0, 1, null, 0);
                //md5.TransformBlock(pass, 0, pass.Length, pass, 0);
                //md5.TransformFinalBlock(bHash, 0, bHash.Length);
                //return "00" + string.Join("", md5.Hash.Select(x => x.ToString("x2")));
            }

            //using (var md5 = new MD5CryptoServiceProvider())
            //{
            //    var cHash = md5.ComputeHash(buf);
            //    return "00" + string.Join("", cHash.Select(x => x.ToString("x2")));
            //}
		}

		int GetSize(byte[] buf)
		{
			if (buf[0] < 128)
			{
				return buf[0];
			}
			else if (buf[0] < 192)
			{
				Receive(buf, 1, 1);

				int v = 0;
				for (int i = 0; i < 2; i++)
					v = (v << 8) + buf[i];

				return v ^ 0x8000;
			}
			else if (buf[0] < 224)
			{
				Receive(buf, 1, 2);

				int v = 0;
				for (int i = 0; i < 3; i++)
					v = (v << 8) + buf[i];

				return v ^ 0xC00000;
			}
			else if (buf[0] < 240)
			{
				Receive(buf, 1, 3);

				int v = 0;
				for (int i = 0; i < 4; i++)
					v = (v << 8) + buf[i];

				return (int)(v ^ 0xE0000000);
			}
			else if (buf[0] == 240)
			{
				Receive(buf, 0, 4);

				int v = 0;
				for (int i = 0; i < 4; i++)
					v = (v << 8) + buf[i];

				return v;
			}
			else
				throw new MikroTikUnknownLengthException();
		}

		public void Dispose()
		{
			DisposeManaged();
            GC.SuppressFinalize(this);
		}

        protected virtual void DisposeManaged()
        {
            _requestBuffer.Dispose();
            _nstream?.Dispose();
          
            _nstream = null;
            _requestBuffer = null;
        }
	}
}
