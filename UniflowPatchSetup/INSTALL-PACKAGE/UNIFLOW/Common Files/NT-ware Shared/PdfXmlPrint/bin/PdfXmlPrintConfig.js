//
// FILE: PdfXmlPrintConfig.js
//
// Description:
//  Read a .jobprop file containing an already validated configuration string
//  and sets the corresponding JT properties.
//  The .jobprop file is deleted.
//
// Run:
//  User Module 1 from the Printing workflow of a PDF job inserted by the
//  PdfXmlPrintInsert.js script.
//
// Argument: the JobID (GUID including curly brackets)!
//
// Copyright (C) 2017 NT-ware Systemprg. GmbH
//
//
// Version: 1.4
// Date:    2017-10-04
// Author:  RobertG
// ITS:     MOMPS-38793,MOMPS-45728,MOMPS-47758
//

// Version: 1.4(M)
// Date: 	2018-10-26
// Author:	Liu Xin
// Purpose:	Add Target PageSize setting and Add Native-In
var sVersion = "1.4";

////////////////////////
// SETTINGS --begin-- //
////////////////////////

// BEFORE FIRST RUN:
// Please set up all parameters accordingly to fit to your installation.

	var bLogToStdout = true;
	var bLogToFile = true;
	var sLogDir = "C:\\log";
	var sLogFile = "PdfXmlPrint-Config-all.log";

////////////////////////
// SETTINGS  --end--  //
////////////////////////

var fso = WScript.CreateObject("Scripting.FileSystemObject");
var objJobPpp = WScript.CreateObject("DsPcSrv.PppIisIf");
var objJobTck = WScript.CreateObject("DsPcSrv.JobTicket");

var sJobID = "";
var sScript = WScript.ScriptName;
var sRunDir = (""+WScript.ScriptFullName).replace(/[^\\]+$/,""); // (ends with backslash)
var dT0 = new Date();

function DeleteFile(sFile)
{
	try
	{
		fso.DeleteFile(sFile,true);
	}
	catch(e){}
}

// read unicode file; returns non-empty content on success
function ReadUFile(sFile)
{
	var sContent = "";
	try
	{
		var f = fso.GetFile(sFile);
		var ts = f.OpenAsTextStream(1, -1); // ForReading = 1, TristateTrue = -1 (Unicode)
		sContent = ts.ReadAll();
		ts.Close();
	}
	catch(e){}
	return sContent;
}

function PipeDecode(sVal)
{
	// replace all "_HH" sequences, where HH is a two digit upper case hex code, by characters "_" and "|"
	return sVal.replace(/_7C/g,"|").replace(/_5F/g,"_");
}

function AddCurlies(sVal)
{
	// change "guid" into "{guid}", but "" stays ""
	return sVal == "" ? "" : "{"+sVal+"}";
}

function LPadZero(sStr, nLen)
{
	sStr = "00000000" + sStr;
	return sStr.substr(sStr.length - nLen, nLen);	
}

var sLogJobID = "";
function Echo(sLine)
{
	if (bLogToStdout) WScript.StdOut.WriteLine(sLine);
	if (bLogToFile)
	{
		var d = new Date();
		var sDate = "" + d.getFullYear() + "-" + LPadZero(d.getMonth()+1,2) + "-" + LPadZero(d.getDate(),2);
		var sTime = LPadZero(d.getHours(),2) + ":" + LPadZero(d.getMinutes(),2) + ":" + LPadZero(d.getSeconds(),2);
		var sTS = sDate + " " + sTime + "." + LPadZero(d.getMilliseconds(),3);
		if (sLogJobID == "") sLogJobID = sJobID.substr(1,36).toLowerCase() + " ";
		
		sLine = sTS + " [Flow]  " + sLogJobID + sLine.replace(/\n?\n/, "\r\n+ ");
		
		var sFile = sLogDir + "\\" + sLogFile;
		if (fso.FileExists(sFile))
		{
			f = fso.GetFile(sFile);
			ts = f.OpenAsTextStream(8, -1); // 8: ForAppending, -1: TristateTrue (Unicode)
		}
		else
		{
			ts = fso.CreateTextFile(sFile, true, true); // Filename, true: Owerwrite, true: Unicode
			// header line in new log file
			sLine = sTS + " [Info]  " + sScript + "\r\n" + sLine;
		}
		ts.Write(sLine + "\r\n");
		ts.Close();
	}
}

function Main()
{
	var sRet = "";
	if (WScript.Arguments.length > 0)
	{
		sJobID = WScript.Arguments.Item(0);
		objJobTck.JobId = sJobID;

		try
		{
			Echo("Reading .jobprop");

			// The job name must contain the full path of the .config file
			sConfigFile = objJobPpp.GetJobProperty(sJobID, "JobName");
			objJobTck.SetValue("OldJobName", sConfigFile);
			var sConfig = "";
			if (sConfigFile.search(/\.jobprop$/) != -1)
			{
				if (fso.FileExists(sConfigFile))
				{
					sConfig = ReadUFile(sConfigFile);
					DeleteFile(sConfigFile);
				}
			}
			if (sConfig != "")
			{
				Echo(sConfig);
				var aConfig = sConfig.split("|");
				// The first value is a version.
				// This allows future upgrades while having a partly processed hot folder.
				switch (aConfig[0])
				{
					case "1":
						if (aConfig.length == 12)
						{
							var sXmlIdentityType  = PipeDecode(aConfig[1]);
							var sXmlIdentityValue = PipeDecode(aConfig[2]);
							var sXmlJobName       = PipeDecode(aConfig[3]);
							var sXmlJobPDL        = PipeDecode(aConfig[4]);
							var sXmlTimestamp     = PipeDecode(aConfig[5]);
							var sTckCopies        = PipeDecode(aConfig[6]);
							var sTckDuplex        = PipeDecode(aConfig[7]);
							var sTckColorMode     = PipeDecode(aConfig[8]);
							var sTckStaple        = PipeDecode(aConfig[9]);
							var sTckPunch         = PipeDecode(aConfig[10]);
							var sTckPageSize      = PipeDecode(aConfig[11]);
							// Store the values in the job session container
							objJobTck.SetValue("XmlPrintConfig", "1");
							objJobTck.SetValue("HotFolderIdType",  sXmlIdentityType);
							objJobTck.SetValue("HotFolderUser",    sXmlIdentityValue);
							objJobTck.SetValue("HotFolderJobName", sXmlJobName);
							objJobTck.SetValue("HotFolderJobPDL",  sXmlJobPDL);
							objJobTck.SetValue("HotFolderJobTS",   sXmlTimestamp);
							objJobTck.SetValue("CopyCount",     sTckCopies);
							objJobTck.SetValue("DIF_Duplex",    sTckDuplex);
							objJobTck.SetValue("DIF_Color",     sTckColorMode);
							objJobTck.SetValue("DIF_Stapling",  sTckStaple);
							objJobTck.SetValue("DIF_HolePunch", sTckPunch);
							objJobTck.SetValue("PageSize", sTckPageSize);
						}
						else
						{
							sRet = "ERROR .jobprop string corrupt";
						}
						break;
					case "2":
						if (aConfig.length >= 14 || aConfig.length <= 15)
						{
							var sOwnerUserID      = PipeDecode(aConfig[1]);
							var sOwnerGroupID     = PipeDecode(aConfig[2]);
							var sOwnerCcID        = PipeDecode(aConfig[3]);
							var sOwnerIsNew       = PipeDecode(aConfig[4]);
							var sXmlJobName       = PipeDecode(aConfig[5]);
							var sXmlJobPDL        = PipeDecode(aConfig[6]);
							var sXmlTimestamp     = PipeDecode(aConfig[7]);
							var sTckCopies        = PipeDecode(aConfig[8]);
							var sTckDuplex        = PipeDecode(aConfig[9]);
							var sTckColorMode     = PipeDecode(aConfig[10]);
							var sTckStaple        = PipeDecode(aConfig[11]);
							var sTckPunch         = PipeDecode(aConfig[12]);
							var sTckPageSize      = PipeDecode(aConfig[13]); 
							var sTckMedia         = "";							
							if (aConfig.length > 14) sTckMedia = PipeDecode(aConfig[14]);

							// Store the values in the job session container
							objJobTck.SetValue("XmlPrintConfig", "2");
							objJobTck.SetValue("JobOwnerUserID",   sOwnerUserID);
							objJobTck.SetValue("JobOwnerGroupID",  sOwnerGroupID);
							objJobTck.SetValue("JobOwnerCcID",     sOwnerCcID);
							objJobTck.SetValue("JobOwnerIsNew",    sOwnerIsNew);
							objJobTck.SetValue("HotFolderJobName", sXmlJobName);
							objJobTck.SetValue("HotFolderJobPDL",  sXmlJobPDL);
							objJobTck.SetValue("HotFolderJobTS",   sXmlTimestamp);
							objJobTck.SetValue("CopyCount",     sTckCopies);
							objJobTck.SetValue("DIF_Duplex",    sTckDuplex);
							objJobTck.SetValue("DIF_Color",     sTckColorMode);
							objJobTck.SetValue("DIF_Stapling",  sTckStaple);
							objJobTck.SetValue("DIF_HolePunch", sTckPunch);
							objJobTck.SetValue("PageSize", sTckPageSize);							
							objJobTck.SetValue("DIF_MediaType", sTckMedia);						
						}
						else
						{
							sRet = "ERROR .jobprop string corrupt";
						}
						break;
					default:
						sRet = "ERROR .jobprop incompatible version";
						break;
				}

				if (sRet == "") sRet = "OK";
			}
			else
			{
				sRet = "ERROR .jobprop file not found";
			}
		}
		catch(e)
		{
			sRet = "ERROR "+e.message;
		}

		Echo(sRet);
		objJobTck.SetValue("ScriptStatus", sRet);
	}
}

Main();
