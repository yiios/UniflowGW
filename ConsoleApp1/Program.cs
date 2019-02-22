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
                ServiceEndpoint = "http://localhost:60504/Licensing.svc"
            };
            var res = checker.RegisterLicense("AAAAABBBBBCCCCCDDDDDEEEEE");
            Console.WriteLine($"{res.State}: {res.Message}");
        }
    }
}
