using Licensing;
using System;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            var checker = new LicenseChecker()
            {
                RsaPublicKey = "<RSAKeyValue><Modulus>m3LXSaTvDCh3Zd5KkhRozpgjEXVy1K8xByECwQYebU+Yk4LJAqs4qEIQ0lp" +
            "2bp1rZ5lbrsvr5WOSjod/1RzMKagT2CtiaqsmSgwbvBSKpMi2SYjdeZrYNnwFKdQOro+3K46KfBxliR2" +
            "LcBV9kl24LfoFeeSKzI3UtB5yf80kLSE=</Modulus><Exponent>AQAB</Exponent></RSAKeyValu" +
            "e>",
                Password = "DLOCR@2018",
                ServiceEndpoint = "http://0802-liyinan:802/Licensing.svc"
            };
            var res = checker.RegisterLicense("1111122222333334444455555");
            Console.WriteLine($"{res.State}: {res.Message}");
        }
    }
}
