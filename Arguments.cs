using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text.RegularExpressions;

namespace TelnetUpdate
{
    public class Arguments
    {
        private readonly StringDictionary _parameters;

        public Arguments(IEnumerable<string> args)
        {
            _parameters = new StringDictionary();
            var regex = new Regex("^-{1,2}|^/|=|:", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            var regex1 = new Regex("^['\"]?(.*?)['\"]?$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            string str = null;
            foreach (var arg in args)
            {
                var strArrays = regex.Split(arg, 3);
                switch (strArrays.Length)
                {
                    case 1:
                    {
                        if (str != null)
                        {
                            if (!_parameters.ContainsKey(str))
                            {
                                strArrays[0] = regex1.Replace(strArrays[0], "$1");
                                _parameters.Add(str, strArrays[0]);
                            }

                            str = null;
                        }

                        break;
                    }
                    case 2:
                    {
                        if (str != null && !_parameters.ContainsKey(str)) _parameters.Add(str, "true");
                        str = strArrays[1];
                        break;
                    }
                    case 3:
                    {
                        if (str != null && !_parameters.ContainsKey(str)) _parameters.Add(str, "true");
                        str = strArrays[1];
                        if (!_parameters.ContainsKey(str))
                        {
                            strArrays[2] = regex1.Replace(strArrays[2], "$1");
                            _parameters.Add(str, strArrays[2]);
                        }

                        str = null;
                        break;
                    }
                }
            }

            if (str != null && !_parameters.ContainsKey(str)) _parameters.Add(str, "true");
        }

        public string this[string param] => _parameters[param];
    }
}