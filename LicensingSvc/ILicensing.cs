using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;

namespace Licensing
{
    [ServiceContract]
    public interface ILicensing
    {

        [OperationContract]
        Response CheckLicense(Request data);

        [OperationContract]
        Response RegisterLicense(Request data);

    }

    [DataContract]
    public class Request
    {
        [DataMember]
        public int Version { get; set; }
        [DataMember]
        public byte[] Data { get; set; }
        [DataMember]
        public byte[] Token { get; set; }
    }

    [DataContract]
    public class Response
    {
        [DataMember]
        public int Version { get; set; }
        [DataMember]
        public byte[] Data { get; set; }
        [DataMember]
        public byte[] Signature { get; set; }
    }
}
