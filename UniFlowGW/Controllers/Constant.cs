using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace UniFlowGW.Controllers
{
	public static class Constant
	{

		public const string TokenURL = "https://qyapi.weixin.qq.com/cgi-bin/gettoken";

		public const string UserinfoURL = "https://qyapi.weixin.qq.com/cgi-bin/user/getuserinfo";

		public const string OAuthURL = "https://open.weixin.qq.com/connect/oauth2/authorize";

		public const string WxAccessTokenURL = "https://api.weixin.qq.com/sns/oauth2/access_token?appid={0}&secret={1}&code={2}&grant_type=authorization_code ";

		public const string WxUserInfoURL = "https://api.weixin.qq.com/sns/userinfo?access_token={0}&openid={1}";

		public const string LXValidSignURL = @"/longchat/app/v1/appplat/valid/{0}";

	}
}
