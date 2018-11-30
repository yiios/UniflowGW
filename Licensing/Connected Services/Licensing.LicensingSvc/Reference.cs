//------------------------------------------------------------------------------
// <自动生成>
//     此代码由工具生成。
//     //
//     对此文件的更改可能导致不正确的行为，并在以下条件下丢失:
//     代码重新生成。
// </自动生成>
//------------------------------------------------------------------------------

namespace Licensing.LicensingSvc
{
    using System.Runtime.Serialization;
    
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("dotnet-svcutil", "1.0.0.1")]
    [System.Runtime.Serialization.DataContractAttribute(Name="Request", Namespace="http://schemas.datacontract.org/2004/07/Licensing")]
    public partial class Request : object
    {
        
        private byte[] DataField;
        
        private byte[] TokenField;
        
        private int VersionField;
        
        [System.Runtime.Serialization.DataMemberAttribute()]
        public byte[] Data
        {
            get
            {
                return this.DataField;
            }
            set
            {
                this.DataField = value;
            }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute()]
        public byte[] Token
        {
            get
            {
                return this.TokenField;
            }
            set
            {
                this.TokenField = value;
            }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute()]
        public int Version
        {
            get
            {
                return this.VersionField;
            }
            set
            {
                this.VersionField = value;
            }
        }
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("dotnet-svcutil", "1.0.0.1")]
    [System.Runtime.Serialization.DataContractAttribute(Name="Response", Namespace="http://schemas.datacontract.org/2004/07/Licensing")]
    public partial class Response : object
    {
        
        private byte[] DataField;
        
        private byte[] SignatureField;
        
        private int VersionField;
        
        [System.Runtime.Serialization.DataMemberAttribute()]
        public byte[] Data
        {
            get
            {
                return this.DataField;
            }
            set
            {
                this.DataField = value;
            }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute()]
        public byte[] Signature
        {
            get
            {
                return this.SignatureField;
            }
            set
            {
                this.SignatureField = value;
            }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute()]
        public int Version
        {
            get
            {
                return this.VersionField;
            }
            set
            {
                this.VersionField = value;
            }
        }
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("dotnet-svcutil", "1.0.0.1")]
    [System.ServiceModel.ServiceContractAttribute(ConfigurationName="Licensing.LicensingSvc.ILicensing")]
    public interface ILicensing
    {
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ILicensing/CheckLicense", ReplyAction="http://tempuri.org/ILicensing/CheckLicenseResponse")]
        System.Threading.Tasks.Task<Licensing.LicensingSvc.CheckLicenseResponse> CheckLicenseAsync(Licensing.LicensingSvc.CheckLicenseRequest request);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ILicensing/RegisterLicense", ReplyAction="http://tempuri.org/ILicensing/RegisterLicenseResponse")]
        System.Threading.Tasks.Task<Licensing.LicensingSvc.RegisterLicenseResponse> RegisterLicenseAsync(Licensing.LicensingSvc.RegisterLicenseRequest request);
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("dotnet-svcutil", "1.0.0.1")]
    [System.ServiceModel.MessageContractAttribute(WrapperName="CheckLicense", WrapperNamespace="http://tempuri.org/", IsWrapped=true)]
    public partial class CheckLicenseRequest
    {
        
        [System.ServiceModel.MessageBodyMemberAttribute(Namespace="http://tempuri.org/", Order=0)]
        public Licensing.LicensingSvc.Request data;
        
        public CheckLicenseRequest()
        {
        }
        
        public CheckLicenseRequest(Licensing.LicensingSvc.Request data)
        {
            this.data = data;
        }
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("dotnet-svcutil", "1.0.0.1")]
    [System.ServiceModel.MessageContractAttribute(WrapperName="CheckLicenseResponse", WrapperNamespace="http://tempuri.org/", IsWrapped=true)]
    public partial class CheckLicenseResponse
    {
        
        [System.ServiceModel.MessageBodyMemberAttribute(Namespace="http://tempuri.org/", Order=0)]
        public Licensing.LicensingSvc.Response CheckLicenseResult;
        
        public CheckLicenseResponse()
        {
        }
        
        public CheckLicenseResponse(Licensing.LicensingSvc.Response CheckLicenseResult)
        {
            this.CheckLicenseResult = CheckLicenseResult;
        }
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("dotnet-svcutil", "1.0.0.1")]
    [System.ServiceModel.MessageContractAttribute(WrapperName="RegisterLicense", WrapperNamespace="http://tempuri.org/", IsWrapped=true)]
    public partial class RegisterLicenseRequest
    {
        
        [System.ServiceModel.MessageBodyMemberAttribute(Namespace="http://tempuri.org/", Order=0)]
        public Licensing.LicensingSvc.Request data;
        
        public RegisterLicenseRequest()
        {
        }
        
        public RegisterLicenseRequest(Licensing.LicensingSvc.Request data)
        {
            this.data = data;
        }
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("dotnet-svcutil", "1.0.0.1")]
    [System.ServiceModel.MessageContractAttribute(WrapperName="RegisterLicenseResponse", WrapperNamespace="http://tempuri.org/", IsWrapped=true)]
    public partial class RegisterLicenseResponse
    {
        
        [System.ServiceModel.MessageBodyMemberAttribute(Namespace="http://tempuri.org/", Order=0)]
        public Licensing.LicensingSvc.Response RegisterLicenseResult;
        
        public RegisterLicenseResponse()
        {
        }
        
        public RegisterLicenseResponse(Licensing.LicensingSvc.Response RegisterLicenseResult)
        {
            this.RegisterLicenseResult = RegisterLicenseResult;
        }
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("dotnet-svcutil", "1.0.0.1")]
    public interface ILicensingChannel : Licensing.LicensingSvc.ILicensing, System.ServiceModel.IClientChannel
    {
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("dotnet-svcutil", "1.0.0.1")]
    public partial class LicensingClient : System.ServiceModel.ClientBase<Licensing.LicensingSvc.ILicensing>, Licensing.LicensingSvc.ILicensing
    {
        
    /// <summary>
    /// 实现此分部方法，配置服务终结点。
    /// </summary>
    /// <param name="serviceEndpoint">要配置的终结点</param>
    /// <param name="clientCredentials">客户端凭据</param>
    static partial void ConfigureEndpoint(System.ServiceModel.Description.ServiceEndpoint serviceEndpoint, System.ServiceModel.Description.ClientCredentials clientCredentials);
        
        public LicensingClient() : 
                base(LicensingClient.GetDefaultBinding(), LicensingClient.GetDefaultEndpointAddress())
        {
            this.Endpoint.Name = EndpointConfiguration.BasicHttpBinding_ILicensing.ToString();
            ConfigureEndpoint(this.Endpoint, this.ClientCredentials);
        }
        
        public LicensingClient(EndpointConfiguration endpointConfiguration) : 
                base(LicensingClient.GetBindingForEndpoint(endpointConfiguration), LicensingClient.GetEndpointAddress(endpointConfiguration))
        {
            this.Endpoint.Name = endpointConfiguration.ToString();
            ConfigureEndpoint(this.Endpoint, this.ClientCredentials);
        }
        
        public LicensingClient(EndpointConfiguration endpointConfiguration, string remoteAddress) : 
                base(LicensingClient.GetBindingForEndpoint(endpointConfiguration), new System.ServiceModel.EndpointAddress(remoteAddress))
        {
            this.Endpoint.Name = endpointConfiguration.ToString();
            ConfigureEndpoint(this.Endpoint, this.ClientCredentials);
        }
        
        public LicensingClient(EndpointConfiguration endpointConfiguration, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(LicensingClient.GetBindingForEndpoint(endpointConfiguration), remoteAddress)
        {
            this.Endpoint.Name = endpointConfiguration.ToString();
            ConfigureEndpoint(this.Endpoint, this.ClientCredentials);
        }
        
        public LicensingClient(System.ServiceModel.Channels.Binding binding, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(binding, remoteAddress)
        {
        }
        
        public System.Threading.Tasks.Task<Licensing.LicensingSvc.CheckLicenseResponse> CheckLicenseAsync(Licensing.LicensingSvc.CheckLicenseRequest request)
        {
            return base.Channel.CheckLicenseAsync(request);
        }
        
        public System.Threading.Tasks.Task<Licensing.LicensingSvc.RegisterLicenseResponse> RegisterLicenseAsync(Licensing.LicensingSvc.RegisterLicenseRequest request)
        {
            return base.Channel.RegisterLicenseAsync(request);
        }
        
        public virtual System.Threading.Tasks.Task OpenAsync()
        {
            return System.Threading.Tasks.Task.Factory.FromAsync(((System.ServiceModel.ICommunicationObject)(this)).BeginOpen(null, null), new System.Action<System.IAsyncResult>(((System.ServiceModel.ICommunicationObject)(this)).EndOpen));
        }
        
        public virtual System.Threading.Tasks.Task CloseAsync()
        {
            return System.Threading.Tasks.Task.Factory.FromAsync(((System.ServiceModel.ICommunicationObject)(this)).BeginClose(null, null), new System.Action<System.IAsyncResult>(((System.ServiceModel.ICommunicationObject)(this)).EndClose));
        }
        
        private static System.ServiceModel.Channels.Binding GetBindingForEndpoint(EndpointConfiguration endpointConfiguration)
        {
            if ((endpointConfiguration == EndpointConfiguration.BasicHttpBinding_ILicensing))
            {
                System.ServiceModel.BasicHttpBinding result = new System.ServiceModel.BasicHttpBinding();
                result.MaxBufferSize = int.MaxValue;
                result.ReaderQuotas = System.Xml.XmlDictionaryReaderQuotas.Max;
                result.MaxReceivedMessageSize = int.MaxValue;
                result.AllowCookies = true;
                return result;
            }
            throw new System.InvalidOperationException(string.Format("找不到名称为“{0}”的终结点。", endpointConfiguration));
        }
        
        private static System.ServiceModel.EndpointAddress GetEndpointAddress(EndpointConfiguration endpointConfiguration)
        {
            if ((endpointConfiguration == EndpointConfiguration.BasicHttpBinding_ILicensing))
            {
                return new System.ServiceModel.EndpointAddress("http://localhost:60504/Licensing.svc");
            }
            throw new System.InvalidOperationException(string.Format("找不到名称为“{0}”的终结点。", endpointConfiguration));
        }
        
        private static System.ServiceModel.Channels.Binding GetDefaultBinding()
        {
            return LicensingClient.GetBindingForEndpoint(EndpointConfiguration.BasicHttpBinding_ILicensing);
        }
        
        private static System.ServiceModel.EndpointAddress GetDefaultEndpointAddress()
        {
            return LicensingClient.GetEndpointAddress(EndpointConfiguration.BasicHttpBinding_ILicensing);
        }
        
        public enum EndpointConfiguration
        {
            
            BasicHttpBinding_ILicensing,
        }
    }
}
