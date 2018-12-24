using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.DirectoryServices.AccountManagement;

namespace UniFlowGW.Util
{
	public static class LDAPUtil
	{
		public static bool ValidateUser(string domain, string userName, string password)
		{
			//string domain = "LDAP://10.11.226.156/DC=cnmkt,DC=local";
			using (PrincipalContext pc = new PrincipalContext(ContextType.Domain, domain))
			{
				bool isValid = pc.ValidateCredentials(userName, password);
				return isValid;
			}

		}

	}
}
