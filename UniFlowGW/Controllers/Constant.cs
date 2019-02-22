using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace UniFlowGW.Controllers
{
	public static class Constant
	{
	}

    public static class SessionKeys
    {
        public const string CurrentPrinterSN = nameof(CurrentPrinterSN);
        public const string BindId = nameof(BindId);
        public const string ExternId = nameof(ExternId);
        public const string Type = nameof(Type);
        public const string LdapLoginId = nameof(LdapLoginId);
		public const string NoLoginBindIdValue = "<NoLoginBindIdValue>";

		public static void SetBindId(this ISession session, string value)
            => session.SetString(BindId, value);

        public static string GetBindId(this ISession session)
            => session.GetString(BindId);

        public static void SetLdapLoginId(this ISession session, string value)
            => session.SetString(LdapLoginId, value);

        public static string GetLdapLoginId(this ISession session)
            => session.GetString(LdapLoginId);

        public static void SetCurrentPrinterSN(this ISession session, string value)
            => session.SetString(CurrentPrinterSN, value);

        public static string GetCurrentPrinterSN(this ISession session)
            => session.GetString(CurrentPrinterSN);

        public static void SetExternId(this ISession session, string externId, string type)
            => session.SetString(ExternId, $"{externId}|||{type}");

        public static (string, string) GetExternId(this ISession session)
        {
            var text = session.GetString(ExternId);
            if (string.IsNullOrEmpty(text) || !text.Contains("|||"))
                return (null, null);
            var parts = text.Split("|||");
            if (parts.Length < 2)
                return (null, null);
            return (parts[0], parts[1]);
        }

		public static bool IsNoLoginBind(this string bindId)
			=> NoLoginBindIdValue == bindId;
    }

    public static class AdminSessionKeys
    {
        public const string AdminLoginUser = nameof(AdminLoginUser);

        public static void SetAdminLoginUser(this ISession session, string value)
            => session.SetString(AdminLoginUser, value);
        public static string GetAdminLoginUser(this ISession session)
            => session.GetString(AdminLoginUser);

        public static bool HasAdminLogin(this ISession session)
            => !string.IsNullOrEmpty(session.GetString(AdminLoginUser));
    }
}
