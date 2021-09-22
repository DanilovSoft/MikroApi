using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
