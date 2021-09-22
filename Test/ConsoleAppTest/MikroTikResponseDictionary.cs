using System.Collections.Generic;

namespace MikroApi
{
    public class MikroTikResponseValues : Dictionary<string, string>
    {
       

        public string this[string key, bool nullIfNotExist]
        {
            get
            {
                string value;
                if (TryGetValue(key, out value))
                    return value;

                return null;
            }
        }

       
    }
}
