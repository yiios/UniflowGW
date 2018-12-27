using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using WebSocketSharp;

namespace UniFlowGW
{

	public class Client
	{

		static WebSocket ws = new WebSocket("wss://wwopenhw.exmail.qq.com");
		Timer timer = new Timer(30000);



		Dictionary<String, PrintJob> reqJobList = new Dictionary<String, PrintJob>();

		IConfiguration configuration;
		readonly ILogger<Client> _logger;
		public Client(IConfiguration configuration, ILogger<Client> logger)
		{
			this.configuration = configuration;
			this._logger = logger;
			ws.OnOpen += (sender, e) => OnOpen(sender, e);
			ws.OnMessage += (sender, e) => OnMessage(sender, e);
			ws.OnError += (sender, e) => OnError(sender, e);
			ws.OnClose += (sender, e) => OnClose(sender, e);
			timer.Elapsed += (sender, e) => { Ping(); };
		}


		#region public method


		public void ConnectWSS()
		{
			ws.Connect();

		}


		public void RegisterNetWork()
		{
			var sn = "XTR03183";
			var timestamp = ((Int32)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds).ToString();
			var nonce = "123456";
			var secretNo = "101d7a8c8d6db9842a493b40642107a4";
			var type = "register";
			var paramArray = new List<string> { sn, secretNo, timestamp, nonce, type };
			paramArray.Sort(StringComparer.Ordinal);
			var sign = "";
			using (SHA1Managed sha1 = new SHA1Managed())
			{
				var result = String.Join("", paramArray.ToArray());
				sign = Hash(result);
			}
			string reqId = Guid.NewGuid().ToString();
			var model = new RegisterRequestModel
			{
				cmd = "register",
				headers = new Headers { req_id = reqId },
				body = new RegisterRequestBody { sn = sn, device_signature = sign, nonce = nonce, timestamp = timestamp }

			};
			var json = UniFlowGW.Util.JsonHelper.SerializeObject(model);
			_logger.LogInformation(json);
			ws.Send(json);
			//启动心跳
			this.StartTimer();
		}

		public void ActiveDevie(string activeCode)
		{
			string reqId = Guid.NewGuid().ToString();
			var model = new ActiveRequestModel
			{
				cmd = "active",
				headers = new Headers { req_id = reqId },
				body = new ActiveRequestBody { active_code = activeCode }

			};
			var json = UniFlowGW.Util.JsonHelper.SerializeObject(model);
			_logger.LogInformation(json);
			ws.Send(json);
		}

		public void SubCrop(string secret)
		{
			string reqId = Guid.NewGuid().ToString();
			var model = new SubCropRequestModel
			{
				cmd = "subscribe_corp",
				headers = new Headers { req_id = reqId },
				body = new SubCropRequestBody { secret = secret, firmware_version = "1.0" }

			};
			var json = UniFlowGW.Util.JsonHelper.SerializeObject(model);
			_logger.LogInformation(json);
			ws.Send(json);
		}


		public void Ping()
		{
			string reqId = Guid.NewGuid().ToString();
			var model = new PingRequestModel
			{
				cmd = "ping",
				headers = new Headers { req_id = reqId },
			};
			var json = UniFlowGW.Util.JsonHelper.SerializeObject(model);
			_logger.LogInformation(json);

			ws.Send(json);
		}


		public void GetJobList()
		{
			string reqId = Guid.NewGuid().ToString();
			var model = new JobRequestModel
			{
				cmd = "printer/get_job_list",
				headers = new Headers { req_id = reqId },
				body = new JobRequestBody { status = 0 }  //获取未打印文件列表
			};
			var json = UniFlowGW.Util.JsonHelper.SerializeObject(model);
			_logger.LogInformation(json);
			ws.Send(json);
		}

		public void GetPrintFile(string userId, string jobId, string reqId)
		{

			var model = new FileRequestModel
			{
				cmd = "printer/download_file",
				headers = new Headers { req_id = reqId },
				body = new FileRequestBody { jobid = jobId, format_version = "1" }

			};
			var json = UniFlowGW.Util.JsonHelper.SerializeObject(model);
			_logger.LogInformation(json);
			ws.Send(json);
		}


		public void ReportPrintFileStatus(StatusReportRequestBody statusBody)
		{
			string reqId = Guid.NewGuid().ToString();
			var model = new StatusReportRequestModel
			{
				cmd = "printer/report_job_status",
				headers = new Headers { req_id = reqId },
				body = statusBody,
			};
			var json = UniFlowGW.Util.JsonHelper.SerializeObject(model);
			_logger.LogInformation(json);
			ws.Send(json);
		}

		#endregion

		#region event

		public void OnOpen(object sender, EventArgs e)
		{
			_logger.LogDebug("Connected!");
		}

		public void OnError(object sender, WebSocketSharp.ErrorEventArgs e)
		{
			_logger.LogError(e.Message, e.Exception);
		}


		public void OnClose(object sender, CloseEventArgs e)
		{
			_logger.LogDebug("Closed!");
			timer = null;
			ws.Connect();
			this.RegisterNetWork();

		}

		public void OnMessage(object sender, MessageEventArgs e)
		{


			if (e.IsBinary)
			{
				var jobId = "";
				try
				{
					_logger.LogInformation("Binary data length:" + e.RawData.Length);

					var reqLength = BitConverter.ToInt32(e.RawData.Skip(4).Take(4).ToArray());
					var reqId = System.Text.Encoding.UTF8.GetString(e.RawData.Skip(8).Take(reqLength).ToArray());
					var dataLength = BitConverter.ToInt32(e.RawData.Skip(8 + reqLength).Take(4).ToArray());//.Split("-").Select(x => Convert.ToByte(x, 16)).ToArray();\
					var data = e.RawData.Skip(12 + reqLength).Take(dataLength).ToArray();
					if (!reqJobList.ContainsKey(reqId))
					{
						return;
					}
					var userId = reqJobList[reqId].userid;
					jobId = reqJobList[reqId].jobid;
					var filePath = Path.GetTempFileName() + ".pdf";
					using (var stream = new MemoryStream(data))
					{
						using (var fileStream = File.Create(filePath))
						{
							stream.CopyTo(fileStream);
						}
					}
					MoveToOutput(userId, filePath);

					ReportPrintFileStatus(new StatusReportRequestBody { jobid = jobId, status = 1 });
				}
				catch (Exception ex)
				{
					ReportPrintFileStatus(new StatusReportRequestBody { jobid = jobId, status = 2, errcode = "-1", errmsg = "System error！" });
					_logger.LogError(ex.Message, ex);
				}

			}
			else if (e.IsText)
			{
				try
				{
					_logger.LogInformation(e.Data);
					var jsonStr = e.Data;
					JObject json = JObject.Parse(jsonStr);
					if (null != json["body"])
					{
						if (null != json["body"]["active_code"])
						{
							var registerResponseModel = Util.JsonHelper.DeserializeJsonToObject<RegisterResponseModel>(jsonStr);
							var activeCode = registerResponseModel.body.active_code;
							ActiveDevie(activeCode);

						}
						else if (null != json["body"]["secret"])
						{
							var activeResponseModel = Util.JsonHelper.DeserializeJsonToObject<ActiveResponseModel>(jsonStr);
							var secret = activeResponseModel.body.secret;
							SubCrop(secret);

						}
						else if (null != json["body"]["jobid_list"])
						{
							this.GetJobList();

						}
						else if (null != json["body"]["printer_job_list"])
						{
							var jobResponseModel = Util.JsonHelper.DeserializeJsonToObject<JobResponseModel>(jsonStr);
							var jobList = jobResponseModel.body.printer_job_list;

							foreach (var job in jobList)
							{
								//ReportPrintFileStatus(new StatusReportRequestBody { jobid = job.jobid, status = 1 });
								string reqId = Guid.NewGuid().ToString();
								reqJobList.Add(reqId, job);
								GetPrintFile(job.userid, job.jobid, reqId);
							}
						}
						else if (null == json["errcode"])
						{


						}
					}
				}
				catch (Exception ex)
				{
					_logger.LogError(ex.Message, ex);
				}
			}

		}

		#endregion

		#region private method

		private void StartTimer()
		{
			timer.Start();
		}

		static string Hash(string input)
		{
			var hash = (new SHA1Managed()).ComputeHash(Encoding.UTF8.GetBytes(input));
			return string.Join("", hash.Select(b => b.ToString("x2")).ToArray());
		}

		private void MoveToOutput(string uid, string tmpfile)
		{
			Guid guid = Guid.NewGuid();
			var outdoc = guid.ToString() + Path.GetExtension(tmpfile);
			var ticket = Template.ticketPdf
				.Replace("$USERID$", uid)
				.Replace("$PATH$", outdoc)
				.Replace("$FILENAME$", outdoc.ToString())
				.Replace("$COPIES$", "1")
				.Replace("$COLORMODE$", "BW")
				.Replace("$PAPERSIZE$", "1");
			var tempxml = tmpfile + ".xml";
			System.IO.File.WriteAllText(tempxml, ticket);

			var targetPaths = this.configuration["TaskTargetPath"];
			foreach (var targetPath in targetPaths.Split(';'))
			{
				var targetdoc = Path.Combine(targetPath.Trim(), outdoc);
				var targetxml = Path.Combine(targetPath.Trim(), guid.ToString() + ".xml");
				System.IO.File.Copy(tmpfile, targetdoc);
				System.IO.File.Copy(tempxml, targetxml);
			}

			System.IO.File.Delete(tmpfile);
			System.IO.File.Delete(tempxml);
		}

		#endregion



	}

	#region model
	public class BaseWssRequestModel
	{
		public string cmd { get; set; }
		public Headers headers { get; set; }
	}

	public class BaseWssResponseModel
	{
		public Headers headers { get; set; }
		public string errcode { get; set; }
		public string errmsg { get; set; }
	}

	public class Headers
	{
		public string req_id { get; set; }
	}

	public class RegisterRequestBody
	{
		public string device_signature { get; set; }
		public string nonce { get; set; }
		public string timestamp { get; set; }
		public string sn { get; set; }
	}

	public class RegisterResponseBody
	{
		public string active_code { get; set; }
	}

	public class RegisterRequestModel : BaseWssRequestModel
	{
		public RegisterRequestBody body { get; set; }
	}

	public class RegisterResponseModel : BaseWssResponseModel
	{
		public RegisterResponseBody body { get; set; }
	}


	public class ActiveRequestBody
	{
		public string active_code { get; set; }
	}

	public class ActiveResponseBody
	{
		public string secret { get; set; }
	}

	public class ActiveRequestModel : BaseWssRequestModel
	{
		public ActiveRequestBody body { get; set; }
	}

	public class ActiveResponseModel : BaseWssResponseModel
	{
		public ActiveResponseBody body { get; set; }
	}


	public class SubCropRequestBody
	{
		public string secret { get; set; }
		public string firmware_version { get; set; }
	}


	public class SubCropRequestModel : BaseWssRequestModel
	{
		public SubCropRequestBody body { get; set; }
	}

	public class SubCropResponseModel : BaseWssResponseModel
	{

	}

	public class PingRequestModel : BaseWssRequestModel
	{

	}


	public class JobRequestBody
	{
		public string userid { get; set; }
		public int status { get; set; }
		public int offset { get; set; }
		public int limit { get; set; }
		public List<string> jobid_list { get; set; }
	}


	public class JobResponseBody
	{
		public List<PrintJob> printer_job_list { get; set; }
	}

	public class PrintJob
	{
		public string userid { get; set; }
		public int createtime { get; set; }
		public int page_size { get; set; }
		public int status { get; set; }
		public string jobid { get; set; }
		public string state { get; set; }
		public List<Setting> setting_list { get; set; }

	}

	public class Setting
	{
		public string key { get; set; }
		public List<string> value { get; set; }
	}


	public class JobRequestModel : BaseWssRequestModel
	{
		public JobRequestBody body { get; set; }
	}

	public class JobResponseModel : BaseWssResponseModel
	{
		public JobResponseBody body { get; set; }
	}


	public class FileRequestBody
	{
		public string jobid { get; set; }
		public int offset { get; set; }
		public int limit { get; set; }
		public string format_version { get; set; }
	}

	public class FileRequestModel : BaseWssRequestModel
	{
		public FileRequestBody body { get; set; }
	}

	public class StatusReportRequestBody
	{
		public string jobid { get; set; }
		public int status { get; set; }
		public string errcode { get; set; }
		public string errmsg { get; set; }
	}

	public class StatusReportRequestModel : BaseWssRequestModel
	{
		public StatusReportRequestBody body { get; set; }
	}
	#endregion
}
