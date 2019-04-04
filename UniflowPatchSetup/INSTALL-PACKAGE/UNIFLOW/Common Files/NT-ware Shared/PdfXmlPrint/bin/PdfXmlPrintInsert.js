//
// FILE: PdfXmlPrintInsert.js
//
// Description:
//  Wait for a set of hot folder files to be complete (after the main
//  file is picked up by the Hotfolder Monitor device agent) and process
//  the additional files.
//  The uniFLOW user must have a default LDAPLogin identity.
//  Identity types:
//   Multiple identities can be specified; the first match determines the user.
//   If none of the identies match, then optionally a new user can be created.
//  PDL types:
//   PDLs types "PDF" and "UniDrv" are recognised ("" translates to "PDF").
//   For unknown PDLs that can be printed directly can use type "Native".
//   Any other PDL types are interpreted as "Native".
//
// Run:
//  User Module 1 from a Printing workflow triggered by HotFolder Monitor.
//  The hot folder monitor must act on the XML files only.
//
// Argument: the JobID (GUID including curly brackets)!
//
// Copyright (C) 2016,2017 NT-ware Systemprg. GmbH
//
//
// Version: 1.4
// Date:    2017-10-04
// Author:  RobertG
// ITS:     MOMPS-40660,MOMPS-45728,MOMPS-44469,MOMPS-47758
//

// Version: 1.4(M)
// Date: 	2018-10-26
// Author:	Liu Xin
// Purpose:	Add Target PageSize setting and Add Native-In printer
var sVersion = "1.4";

////////////////////////
// SETTINGS --begin-- //
////////////////////////

// BEFORE FIRST RUN:
// Please set up all parameters accordingly to fit to your installation.

	// Timeout in seconds to wait for all files to be present
	// Make sure the timeout in User Module 1 is set larger than this timeout!
	var nTimeout = 50;

	// Whether or not to use the <JobFile> element to filnd the corresponding spool file.
	//  true: always use the XML file name (ignore <JobFile>).
	//  false: use the JobFile, if present.
	// If the files are renamed (e.g. by a remote hot folder monitor service), then 'true' should be used.
	var bOverruleJobFile = false;

	// Name of the uniFLOW printer that acts as the input queue.
	// All jobs will be inserted into this print queue.
//	var sInputPrinter = "SecInViaHot";
//	var sInputPrinter = "PdfXmlConfig";
	var sInputPrinter = "PdfXmlPrint-SecIn";
//	var sInputPrinter = "PdfXmlPrint-SecInRps";
//	var sInputPrinter = "PdfXmlRps-SecIn";

	// Optionally, native jobs can be inserted into an alternative into printer.
	// XML ticket preconditions:
	// -LDAPLogin identity is mandatory,
	// -Job file type must be "Native",
	// -<Ticket> must be absent.
	var bNativeJobsToInputPrinterAlt = true;
	var sInputPrinterAlt = "SecIn-Native";
//	var bNativeJobsToInputPrinterAlt = false;
//	var sInputPrinterAlt = "";

	// Creation of unknown user
	//  bAllowUserCreation false: no users will be created (always reject jobs from unknown users)
	//  bAllowUserCreation true: create user if unknown and requested in the XML ticket
	//  sDefaultGroup: if non-empty the user will be assigned to the specified Group (must already exist)
	//  bBudgetMonitoring: true to enable budget monitoring
	// No users are created if the Group doesn't exist in uniFLOW.
	// The user's display name is set to "<prefix><first identity>".
	// bAllowUserCreation must be set to false on the RPS.
	var bAllowUserCreation = true;
	var bBudgetMonitoring = false;
	var sDefaultGroup = "AutoRegisteredUsers";
	var sDisplayNamePrefix = "User-";

	// Tray-to-media assignments
	// Note: may also be available in 'Server Config > General Settings > PdfXmlPrint Tray-to-Media'.
	//  Comma-separated list of pairs of tray numbers and media names.
	//  Each pair looks like this: <TrayNumber> + ":" + <MediaName>.
	//  -TrayNumber: 1...99
	//  -MediaName: any uniFLOW media name
	var bServerConfigHasPrecedence = true;
	var sTrayToMedia = "";
//	var sTrayToMedia = "1:A4 Normal 80, 2:A4 Blue, 60:A3 Heavy";

	// Target Page Size assignment
	// Note:setting for the target page setting of output
	var sPageSize = "";
//	var sPageSize = "1"; //A4 
	// Logging to screen and/or to file
	var bLogToStdout = false;
	var bLogToFile = true;

	// The following variables may contain tokens %DATE%, %TIME%, %DAY%, %WEEKDAY%, %DATAFOLDER%, %JOBID%
	// Logging is cyclic if the %DAY% or %WEEKDAY% token is used (log files are overwritten).
	var sLogDir = "C:\\log";
//	var sLogDir = "%DATAFOLDER%\\XmlPrint\\Log";
	var sLogFile = "PdfXmlPrint-Insert-Day%DAY%.log";
//	var sLogFile = "%DATE%_%TIME%_Hot_%JOBID%.log";

////////////////////////
// SETTINGS  --end--  //
////////////////////////

var fso   = WScript.CreateObject("Scripting.FileSystemObject");
var shell = WScript.CreateObject("WScript.Shell");
var objJobPpp = WScript.CreateObject("DsPcSrv.PppIisIf");
var objJobTck = WScript.CreateObject("DsPcSrv.JobTicket");
var objRQMgmt = WScript.CreateObject("DsPcSrv.ReleaseQueueMgmt");
var objOProxy = WScript.CreateObject("DsPcSrv.DbObjectProxy");
var objCompIf = WScript.CreateObject("DsPcSrv.MomDocCompIf");
var objSimple = WScript.CreateObject("DsPcSrv.SimpleServerConfig");

var sScript = WScript.ScriptName;
var sRunDir = (""+WScript.ScriptFullName).replace(/[^\\]+$/,""); // (ends with backslash)
var dT0 = new Date();

var sJobID = "";
var sSourceFile = "";
var sJobName = "";
var sProviderID = "";
var sDir = "";
var sFile = "";
var sFileExt = "";
var sFileWithoutExt = "";
var bLogTokensReplaced = false;
var sDataFolder = ""; // "": not initialised, >"": uniFLOW/RPS Data folder with terminating backslash

// .jobprop
var sConfigString = "";
var sConfigFile = "";
var sPdlFile = "";
var sIdentity = "";
var bUseAlt = false;

/*
<MOMCONFIGURATION>
	<CATEGORIE type="40660004" stringid="PdfXmlPrint Tray-to-Media" name="PdfXmlPrint" objecttype="DsPcSrv.SimpleServerConfig" getmethod="GetProperty(strCategorie, strType)" setmethod="SetProperty(strCategorie, strType, strValue)">
		<PARAMETER name="Txt4" stringid="PdfXmlPrint Tray-to-Media Configuration" type="label" descriptionid="" attribute="" defaultvalue=""></PARAMETER>
		<PARAMETER name="IndividualTrays" stringid="Tray configuration" type="selectbox" descriptionid="" attribute="" defaultvalue="0"> 
			<FIXVALUE value="0" stringid="Use 'Media assignment for all Trays' setting"></FIXVALUE>
			<FIXVALUE value="1" stringid="Use 'Media for Tray 1..6' settings"></FIXVALUE>
		</PARAMETER>
		<PARAMETER name="TrayToMedia" stringid="Media assignment for all Trays" type="text" descriptionid="Comma-separated list of pairs of tray numbers and media names.&lt;BR&gt;Each pair looks like this: &amp;lt;TrayNumber&amp;gt; + &quot;:&quot; + &amp;lt;MediaName&amp;gt;.&lt;BR&gt;-TrayNumber: 1...99&lt;BR&gt;-MediaName: any uniFLOW media name&lt;BR&gt;Example: &quot;1:A4 Normal 80, 2:A4 Blue, 60:A3 Heavy&quot;." attribute="" defaultvalue=""></PARAMETER>
		<PARAMETER name="Tray1" stringid="Media for Tray 1" type="text" descriptionid="" attribute="" defaultvalue=""></PARAMETER>
		<PARAMETER name="Tray2" stringid="Media for Tray 2" type="text" descriptionid="" attribute="" defaultvalue=""></PARAMETER>
		<PARAMETER name="Tray3" stringid="Media for Tray 3" type="text" descriptionid="" attribute="" defaultvalue=""></PARAMETER>
		<PARAMETER name="Tray4" stringid="Media for Tray 4" type="text" descriptionid="" attribute="" defaultvalue=""></PARAMETER>
		<PARAMETER name="Tray5" stringid="Media for Tray 5" type="text" descriptionid="" attribute="" defaultvalue=""></PARAMETER>
		<PARAMETER name="Tray6" stringid="Media for Tray 6" type="text" descriptionid="MediaName: any uniFLOW media name&lt;BR&gt;Example: &quot;1:A4 Normal 80&quot;" attribute="" defaultvalue=""></PARAMETER>
	 </CATEGORIE>
</MOMCONFIGURATION>
*/

// reads ascii file; returns non-empty content on success
function ReadFile(sFile)
{
	var sContent = "";
	try
	{
		var f = fso.GetFile(sFile);
		var ts = f.OpenAsTextStream(1, 0); // 1=ForReading, 0=TristateFalse(=ASCII)
		sContent = ts.ReadAll();
		ts.Close();
	}
	catch(e){}
	return sContent;
}

// writes unicode string to file; returns true on error
function WriteUFile(sFile, sContent)
{
	var bError = true;
//	Echo("WriteUFile('"+sFile+"','"+sContent+"');");
	try
	{
		if (fso.FileExists(sFile)) fso.DeleteFile(sFile);
		var ts = fso.CreateTextFile(sFile, true, true); //Filename, Owerwrite (true/false), true for Unicode and false for ASCII
		ts.Write(sContent);
		ts.Close();
		bError = false;
	}
	catch(e)
	{
		Echo("WriteUFile() "+e.message);
	}
	return bError;
}

function HasWriteAccess(sFile)
{
	var bWritable = false;

	try
	{
		var f = fso.GetFile(sFile);
		var ts = f.OpenAsTextStream(8, 0); // 8=ForAppending, 0=TristateFalse(=ASCII)
		ts.Close();
		ts = null;
		f = null;
		bWritable = true;
	}
	catch(e){}

	return bWritable;
}

function DeleteFile(sFile)
{
	try
	{
		fso.DeleteFile(sFile,true);
	}
	catch(e){}
}

// Check if the directory exist, create (recursively) if necessary
function EnsureDirExists(sDir)
{
	if (!fso.FolderExists(sDir))
	{
		var sParent = "" + fso.GetParentFolderName(sDir);
		if (sParent.length > 3) EnsureDirExists(sParent);
		var d = fso.CreateFolder(sDir);
	}
}

function LPadZero(sStr, nLen)
{
	sStr = "00000000" + sStr;
	return sStr.substr(sStr.length - nLen, nLen);	
}

// Log to file and/or stdout
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

		var sFile = sLogDir + "\\" + sLogFile;
		if (!bLogTokensReplaced)
		{
			if (sDataFolder == "" && sLogDir.search(/%DATAFOLDER%/) != -1) return;

			// Compose the file name of the logfile 
			var sTime = sTime.replace(/:/g, "");
			var sDay = "" + LPadZero(d.getDate(),2);
			var sWeekday = "" + (d.getDay()==0 ? 7 : d.getDay()+1); // Mon=1...Sun=7
			var bCyclic = sLogDir.search(/%(WEEK)?DAY%/) != -1 || sLogFile.search(/%(WEEK)?DAY%/) != -1;
			sLogDir  = sLogDir.replace(/%DATE%/, sDate).replace(/%TIME%/, sTime).replace(/%DAY%/, sDay).replace(/%WEEKDAY%/, sWeekday).replace(/%JOBID%/, sJobID).replace(/%DATAFOLDER%/, sDataFolder).replace(/\\$/, "");
			sLogFile = sLogFile.replace(/%DATE%/, sDate).replace(/%TIME%/, sTime).replace(/%DAY%/, sDay).replace(/%WEEKDAY%/, sWeekday).replace(/%JOBID%/, sJobID);
			bLogTokensReplaced = true;

			EnsureDirExists(sLogDir);
			sFile = sLogDir + "\\" + sLogFile;
			if (bCyclic)
			{
				// Delete old, but only if older than 48 hours (overwrite)
				if (fso.FileExists(sFile))
				{
					var f = fso.GetFile(sFile);
					var fDelta = (d - f.DateLastModified)/(60*60*1000);
					if (fDelta > 48)
					{
						try
						{
							fso.DeleteFile(sFile);
						}
						catch(e){}
					}
				}
			}
			sLogJobID = sJobID.substr(1,36).toLowerCase() + " ";
		}
		sLine = sTS + " [Flow]  " + sLogJobID + sLine.replace(/\r?\n/g,"\r\n+ ") + "\r\n";
	
		if (fso.FileExists(sFile))
		{
			f = fso.GetFile(sFile);
			ts = f.OpenAsTextStream(8, -1); // 8: ForAppending, -1: TristateTrue (Unicode)
		}
		else
		{
			ts = fso.CreateTextFile(sFile, true, true); // Filename, true: Owerwrite, true: Unicode
			// header line in new log file
			sLine = sTS + " [Info]  " + sScript +" v" + sVersion + "\r\n" + sLine;
		}
		ts.Write(sLine);
		ts.Close();
	}
}

function PipeEncode(sVal)
{
	// replace all "_" and "|" characters by "_HH", where HH is a two digit upper case hex code
	return sVal.replace(/_/g,"_5F").replace(/\|/g,"_7C");
}

function StripCurlies(sVal)
{
	// change "{guid}" into "guid", but "" stays ""
	return sVal == "" ? "" : sVal.substr(1,sVal.length-2);
}

function StripNullGuid(sVal)
{
	// change "{00}" into "", but leave "{guid}" intact
	return sVal == "{00000000-0000-0000-0000-000000000000}" ? "" : sVal;
}

function FormatSearch(sVal)
{
	return sVal.replace(/'/g,"''");
}

function NVL(sVal)
{
	if (sVal === undefined) return "";
	if (sVal == null) return "";
	return "" + sVal;
}

function SmartGetRegKey(sName,sDefault)
{
	var sRegPathApsWow = "HKEY_LOCAL_MACHINE\\Software\\Wow6432Node\\NT-ware\\Mom\\MomAps";
	var sRegPathMomWow = "HKEY_LOCAL_MACHINE\\Software\\Wow6432Node\\NT-ware\\Mom";
	var sRegPathApsNative = "HKEY_LOCAL_MACHINE\\Software\\NT-ware\\Mom\\MomAps";
	var sRegPathMomNative = "HKEY_LOCAL_MACHINE\\Software\\NT-ware\\Mom";

	try
	{
		return "" + shell.RegRead(sRegPathApsWow+"\\"+sName);
	}
	catch(e){}
	try
	{
		return "" + shell.RegRead(sRegPathMomWow+"\\"+sName);
	}
	catch(e){}
	try
	{
		return "" + shell.RegRead(sRegPathApsNative+"\\"+sName);
	}
	catch(e){}
	try
	{
		return "" + shell.RegRead(sRegPathMomNative+"\\"+sName);
	}
	catch(e){}

	return sDefault;
}

function HasFileArrived(sWaitForFile, bArrivedFilePdl)
{
	if (bArrivedFilePdl) return true;

	if (fso.FileExists(sWaitForFile))
	{
		Echo("FileExists:true");
		if (HasWriteAccess(sWaitForFile))
		{
			Echo("HasWriteAccess:true");
			WScript.Sleep(200);
			return true;
		}
	}
	return false;
}

// Pre-process the XML file (which is the spool file)
// -Read the file and parse the XML contents.
// -Compose the setting for uniFLOW.
// -Place the settings in the job session container as "JT" properties.
//
// XML ticket structure:
// <MOMJOB>
//  <Identity type="LDAPLogin">...</Identity>     <!-- empty or absent: reject job -->
//  <Identity type="CardNumber">...</Identity>    <!-- optional: additional identities can be specified -->
//  <Identity type="PINCode">...</Identity>       <!-- optional: additional identities can be specified -->
//  <IfUnknown>Reject|Create</IfUnknown>          <!-- empty or absent: use Reject -->
//  <JobFile type="PDF">...</JobFile>             <!-- empty or absent: use xml file name and .pdf -->
//  <JobName>...</JobName>                        <!-- empty or absent: use job file name -->
//  <Distribution>...</Distribution>              <!-- empty or absent: 0 -->
//  <Timestamp>...</Timestamp>                    <!-- format, if present: yyyy-mm-dd hh:mm:ss -->
//  <Ticket>                            <!-- optional -->
//   <Copies>1|...</Copies>                      <!-- empty or absent: 1 -->
//   <ColorMode>Color|BW|Auto</ColorMode>        <!-- empty or absent: Auto -->
//   <Duplex>Simplex|LongEdge|ShortEdge</Duplex> <!-- empty or absent: Simplex -->
//   <Staple>0|1|2</Staple>                      <!-- empty or absent: 0 -->
//   <Punch>0|2|3|4</Punch>                      <!-- empty or absent: 0 -->
//   <PageSize>0|1|2</PageSize>                  <!-- empty or absent: 1 -->
//   <Media>...</Media>                          <!-- empty or absent: use default media type -->
//   <Tray>1|2|3|4</Tray>                        <!-- empty or absent: use default media type -->
//  </Ticket>
// </MOMJOB>
//
// Minimum ticket:
// <MOMJOB>
//  <Identity type="LDAPLogin">...</Identity>     <!-- empty or absent: reject job -->
// </MOMJOB>
//
// <Identity> element: type can be LDAPLogin, CardNumber, PINCode, TIC, SMTPMailAddress, Login
//  Login means any identity if the Login category (identification only, ignored for user creation).
//  More than one identity element can be specified (processed top-to-bottom).
// JobFile element: if type argument is absent, then PDF is assumed.
// Media element: has precedence over Tray element.
// Tray element: media selection using tray-to-media 
//
// The accompanying example workflow may accept several values for the <Identity> type=... argument:
//  LDAPLogin|Login|CardNumber|PINCode|TIC|SMTPMailAddress
// ('Login' represents all identity types in the Login category.)
//
// <JobFile> element: type can be PDF, UD, NATIVE.
// The workflow will convert PDF files into a Universal Driver job.
// The workflow will assume type UniDrv is a Universal Driver job (including some Canon Generic drivers, see manual).
// The workflow will assume type Native is any native (non-UD) PDL; the <Ticket> element will be ignored.
//
function PreProcessFileXml(sXmlFile)
{
	var sRet = "OK";
	var sErrFile = sXmlFile.replace(/^.*[:\\\/]/, ""); // for error messages

	var sXmlIdentityType = "";
	var sXmlIdentityValue = "";
	var aXmlIdentityType = [];
	var aXmlIdentityValue = [];
	var bIdentityAsType = false;
	var sXmlIfUnknown = "";
	var sXmlJobName = "";
	var sXmlJobFile = "";
	var sXmlJobPDL = "";
	var sXmlTimestamp = "";
	var sTckCopies = "";
	var sTckColorMode = "";
	var sTckDuplex = "";
	var sTckStaple = "";
	var sTckPunch = "";
	var sTckMedia = "";
	var sTckTray = "";
	var sTckPageSize = "";
	var bHasTicket = false;

	// Parse the XML data and store the results in the above-declared variables.
	Echo("PreProcessFileXml();");
	try
	{
		// Camel casing for the accepted identity types/categories
		var sAcceptedTypes = ",Login,LDAPLogin,CardNumber,PINCode,TIC,SMTPMailAddress,";

		var xmlDoc = WScript.CreateObject("Msxml2.DOMDocument.3.0");
		xmlDoc.load(sXmlFile);
		if (xmlDoc.parseError.errorCode != 0)
		{
			var sError = xmlDoc.parseError;
			sRet = "ERROR " + sFile + ": " + sError;
		}
		else
		{
			if(!xmlDoc.hasChildNodes())
			{
				sRet = "ERROR " + sErrFile + ": No child nodes found";
			}
			else
			{
				var xmlTopNode = xmlDoc.getElementsByTagName("MOMJOB")[0];
				if (xmlTopNode == null)
				{
					strResult = "ERROR " + sErrFile + ": Root element missing";
				}
				else
				{
					var bError = false;
					var bSubError = false;
					for (var nNode = 0; nNode < xmlTopNode.childNodes.length; nNode++)
					{
						var xmlNode = xmlTopNode.childNodes[nNode];
						if (xmlNode.nodeType == 1) // ELEMENT_NODE
						{
							switch (xmlNode.nodeName)
							{
								case "Identity":
									sXmlIdentityType = xmlNode.getAttribute("type");
									if (sXmlIdentityType == null) sXmlIdentityType = "";
									sXmlIdentityValue = xmlNode.text;
									bError = sXmlIdentityType == "" || sXmlIdentityValue == "";
									if (!bError)
									{
										// apply the right camel case, since we might need to create identities
										var patt = new RegExp(","+sXmlIdentityType+",", "i");
										var capt = patt.exec(sAcceptedTypes);
										if (capt != null) sXmlIdentityType = capt[0].substr(1,capt[0].length-2);

										aXmlIdentityType[aXmlIdentityType.length] = sXmlIdentityType;
										aXmlIdentityValue[aXmlIdentityValue.length] = sXmlIdentityValue;
									}
									break;
								case "IfUnknown":
									sXmlIfUnknown = xmlNode.text;
									bSubError = sXmlIfUnknown.replace(/^((Reject)|(Create))?$/, "") != "";
									break;
								case "JobFile":
									sXmlJobPDL = xmlNode.getAttribute("type");
									if (sXmlJobPDL == null) sXmlJobPDL = "";
									// upper case and strip " _-/" characters, thus Pcl/Xl becomes PCLXL
									sXmlJobPDL = sXmlJobPDL.replace(/[_ \-\/]/g, "").toUpperCase();
									Echo("sXmlJobPDL is " + sXmlJobPDL + " !")
									sXmlJobFile = sDir + xmlNode.text;
									break;
								case "JobName":
									sXmlJobName = xmlNode.text;
									break;
								case "Timestamp":
									// is either "yyyy-MM-dd HH:mm:ss" or "yyyy-MM-dd'T'HH:mm:ss.SSS'Z'"
									// output only "yyyy-MM-dd HH:mm:ss", timezone is ignored
									sXmlTimestamp = xmlNode.text;
									sXmlTimestamp = sXmlTimestamp.toUpperCase();
									if (sXmlTimestamp.length >= 23 && sXmlTimestamp.substr(10,1) == "T")
										sXmlTimestamp = sXmlTimestamp.substr(0,10)+" "+sXmlTimestamp.substr(11,8);
									break;
								case "Ticket":
									bHasTicket = true;
									for (var nSubNode = 0; nSubNode < xmlNode.childNodes.length; nSubNode++)
									{
										var xmlSubNode = xmlNode.childNodes[nSubNode];
										if (xmlSubNode.nodeType == 1) // ELEMENT_NODE
										{
											switch (xmlSubNode.nodeName)
											{
												case "Copies":
													sTckCopies = xmlSubNode.text;
													bSubError = !/^([1-9]\d?\d?)?$/.test(sTckCopies);
													break;
												case "ColorMode":
													sTckColorMode = xmlSubNode.text;
													bSubError = !/^((Color)|(BW)|(Auto))?$/.test(sTckColorMode);
													break;
												case "Duplex":
													sTckDuplex = xmlSubNode.text;
													bSubError = !/^((Simplex)|(LongEdge)|(ShortEdge))?$/.test(sTckDuplex);
													break;
												case "Staple":
													sTckStaple = xmlSubNode.text;
													bSubError = !/^[012]?$/.test(sTckStaple);
													break;
												case "Punch":
													sTckPunch = xmlSubNode.text;
													bSubError = !/^[0234]?$/.test(sTckPunch);
													break;
												case "Media":
													sTckMedia = xmlSubNode.text;
													break;
												case "Tray":
													sTckTray = xmlSubNode.text;
													bSubError = !/^([1-9]\d?)?$/.test(sTckTray);
													break;
												case "PageSize":
													sTckPageSize = xmlSubNode.text;
													bSubError = !/^[012]?$/.test(sTckPageSize);
												default: // ignore any unknown subnodes
											}
											if (bSubError) sRet = "ERROR " + sErrFile + ": Element <" + xmlSubNode.nodeName + "> invalid";
											if (bSubError) break;
										}
									}
									break;
								default: // ignore any unknown nodes
							}
							if (bError) sRet = "ERROR " + sErrFile + ": Element <" + xmlNode.nodeName + "> invalid";
							if (bError || bSubError) break;
						}
					}
				}
			}
		}

		// Check mandatory elements
		if (sRet == "OK" && aXmlIdentityValue.length == 0) sRet = "ERROR " + sErrFile + ": Element <Identity> missing";
		Echo("intermediate sRet:'"+sRet+"'");

		// Fill in default for any empty or absent type arguments
		if (sRet == "OK")
		{
			if (aXmlIdentityType[0] == "") aXmlIdentityType[0] = "Login";
			if (sXmlJobPDL == "") sXmlJobPDL = "PDF";
		}

		// Validate PDL type
		var sDefaultExt = "";
		if (sRet == "OK")
		{
			switch (sXmlJobPDL)
			{
				case "PDF":    sDefaultExt = ".pdf"; break;
				case "UNIDRV": sDefaultExt = ".SPL"; break;
				case "NATIVE": sDefaultExt = ".SPL"; break;
				default:
					// interpreted as NATIVE
					sDefaultExt = ".SPL";
					//sRet = "ERROR <JobFile> type argument: invalid PDL type '" + sXmlJobPDL + "'.";
			}
		}

		// Fill in default for any empty or absent values
		if (sRet == "OK")
		{
			if (sXmlIfUnknown == "") sXmlIfUnknown = "Reject";
			if (sXmlJobName == "") sXmlJobName = sFile;
			if (sXmlJobFile == "" || bOverruleJobFile) sXmlJobFile = sDir + sFileWithoutExt + sDefaultExt;
			if (sTckCopies == "") sTckCopies = "1";
			if (sTckColorMode == "") sTckColorMode = "Auto";
			if (sTckDuplex == "") sTckDuplex = "Simplex";
			if (sTckStaple == "") sTckStaple = "0";
			if (sTckPunch == "") sTckPunch = "0";
			if (sTckMedia != "") sTckTray = "";
			if (sTckPageSize == "") sTckPageSize = "1";			
		}

		if (sRet == "OK" && sTckTray != "")
		{
			var bIsInvalid = true;
			try
			{
				//// The printer must have a DIF assigned, otherwise no tray configuration is available.
				//objOProxy.LoadObject(sProviderID);
				//if (objOProxy.GetProperty("ModelName") > "")
				//{
				//}
				if (bServerConfigHasPrecedence)
				{
					
					if (NVL(objSimple.GetProperty("PdfXmlPrint","IndividualTrays")) == "1")
					{
						sTrayToMedia =
							"1:"  + NVL(objSimple.GetProperty("PdfXmlPrint","Tray1")) +
							",2:" + NVL(objSimple.GetProperty("PdfXmlPrint","Tray2")) +
							",3:" + NVL(objSimple.GetProperty("PdfXmlPrint","Tray3")) +
							",4:" + NVL(objSimple.GetProperty("PdfXmlPrint","Tray4")) +
							",5:" + NVL(objSimple.GetProperty("PdfXmlPrint","Tray5")) +
							",6:" + NVL(objSimple.GetProperty("PdfXmlPrint","Tray6"));
					}
					else
					{
						sTrayToMedia = NVL(objSimple.GetProperty("PdfXmlPrint","TrayToMedia"));
					}
				}
			}
			catch(e){}
			sTrayToMedia = ("," + sTrayToMedia + ",").replace(/ *: */g,":").replace(/ *, */g,",");
			var p = sTrayToMedia.search(new RegExp("," + sTckTray + ":"));
			if (p != -1)
			{
				sTckMedia = sTrayToMedia.substr(p + sTckTray.length + 2);
				sTckMedia = sTckMedia.substr(0, sTckMedia.search(/,/));
			}
			bIsInvalid = p == -1 || sTckMedia == "";
			if (bIsInvalid)
				sRet = "ERROR Invalid tray number: " + sTckTray;
		}

		if (sRet == "OK" && sTckMedia != "")
		{
			var bIsInvalid = true;
			try
			{
				// MediaVisibilities: 0=Normal, 1=HiddenFromUser, 2=HiddenFromOperator, 3=HiddenFromOfficeUser, 4=HiddenFromProfessionalUser
				// For selection via the <Media> element the visibility MUST be Normal.
				var rsData = objCompIf.GetMediaTypesAsDataSet();
				var sSql = "SELECT * FROM xyz WHERE MediaName = N'" + FormatSearch(sTckMedia) + "'";
				if (sTckTray == "") sSql += " AND MediaVisibilities = 0";
				rsData.Open(sSql, "");
				bIsInvalid = rsData.EOF;
				rsData.Close();
			}
			catch(e){}
			if (bIsInvalid) sRet = "ERROR Invalid media type: " + sTckMedia + (sTckTray == "" ? "" : " (Tray #" + sTckTray + ")");
		}

		// Use the input data
		if (sRet == "OK")
		{
			// Convert input values into uniFLOW values
			switch (sTckColorMode)
			{
				case "Color": sTckColorMode = "on";   break;
				case "BW":    sTckColorMode = "off";  break;
				case "Auto":  sTckColorMode = "auto"; break;
			}
			switch (sTckPunch)
			{
				case "0": sTckPunch = "Off";   break;
				case "2": sTckPunch = "Two";   break;
				case "3": sTckPunch = "Three"; break;
				case "4": sTckPunch = "Four";  break;
			}
			switch (sTckStaple)
			{
				case "0": sTckStaple = "Off";        break;
				case "1": sTckStaple = "LeftUpper";  break;
				case "2": sTckStaple = (sTckDuplex == "ShortEdge" ? "DoubleTop" : "DoubleLeft"); break;
			}
			switch (sTckDuplex)
			{
				case "Simplex":   sTckDuplex = "Simplex";  break;
				case "LongEdge":  sTckDuplex = "Tumble";   break; // mind: swapped in uniFLOW
				case "ShortEdge": sTckDuplex = "NoTumble"; break; // mind: swapped in uniFLOW
			}
			bAllowUserCreation = bAllowUserCreation && sXmlIfUnknown == "Create";

			// attempt to identify the user
			var sIsNew = "0";
			var sUserID = "";
			var sGroupID = "";
			var sCcID = "";
			var nIdx = 0;
			while (sUserID == "" && sRet == "OK" && nIdx < aXmlIdentityValue.length)
			{
				switch (aXmlIdentityType[nIdx])
				{
					case "Login":
						sUserID = objRQMgmt.IdentifyUserByIdentityType(2,"",aXmlIdentityValue[nIdx],"");
						break;
					case "LDAPLogin":
					case "CardNumber":
					case "PINCode":
					case "TIC":
					case "SMTPMailAddress":
						bIdentityAsType = true;
						sUserID = objRQMgmt.IdentifyUserByIdentityType(0,aXmlIdentityType[nIdx],aXmlIdentityValue[nIdx],"");
						break;
					default:
						sRet = "ERROR Invalid identity type: "+aXmlIdentityType[nIdx];
						break;
				}
				nIdx++;
			}
			if (sUserID == "")
			{
				if (bAllowUserCreation && bIdentityAsType)
				{
					if (sDefaultGroup != "")
					{
						try
						{
							var sSql = "SELECT ID FROM ServiceConsumer_T WHERE UserTypeEx = 4097 AND Visibility = 0 AND ";
							if (sDefaultGroup.length == 38 && /^\{[0-9a-fA-F\-]+\}$/.test(sDefaultGroup))
								sSql += "ID = '" + FormatSearch(sDefaultGroup) + "'";
							else
								sSql += "Name = N'" + FormatSearch(sDefaultGroup) + "'";
							var rsData = objOProxy.DoDatabaseQuery(sSql);
							rsData.Open("SELECT * FROM xyz", "");
							if (!rsData.EOF) sGroupID = NVL(rsData.Fields("ID").Value);
							rsData.Close();
							if (sGroupID == "") sRet = "ERROR Default Group: does not exist";
						}
						catch(e)
						{
							sRet = "ERROR Default Group: lookup failed";
						}
					}
					if (sRet == "OK")
					{
						sIsNew = "1";
						var sNiceName = sDisplayNamePrefix + aXmlIdentityValue[0];
						sUserID = objOProxy.CreateObject("CServiceConsumer:User");
						Echo("New user: "+sUserID);
						objOProxy.LoadObject(sUserID);
						objOProxy.SetProperty("Name",sNiceName);
						if (sGroupID != "") objOProxy.SetProperty("DefaultGroupID",sGroupID);
						if (sCcID != "")    objOProxy.SetProperty("DefaultCostCenter",sCcID);
						objOProxy.SetProperty("BudgetEnabled",bBudgetMonitoring ? "1" : "0");
						if (bBudgetMonitoring)
						{
							objOProxy.SetProperty("BudgetResetType",          "0");
							objOProxy.SetProperty("BudgetInitialBalance",     "0.00");
							objOProxy.SetProperty("BudgetResetInterval",      "0");
							objOProxy.SetProperty("BudgetExceededBehaviour",  "2"); // block prints and copies
							objOProxy.SetProperty("BudgetNotificationMethod", "3"); // 0:no message, 1:e-mail, 3:only log entry
							objOProxy.SetProperty("BudgetWarningBalance",     "0.00");
						}
						objOProxy.Commit();
						var objIdentityMgmt = WScript.CreateObject("DsPcSrv.ConsumerIdentityContainerMgmt");
						try
						{
							objIdentityMgmt.LoadIdentitiesFrom(sUserID);
							for (var i = 0; i < aXmlIdentityValue.length; i++)
							{
								if (aXmlIdentityType[i] != "Login")
									objIdentityMgmt.AddIdentity(aXmlIdentityType[i],aXmlIdentityValue[i]);
							}
							objIdentityMgmt.StoreIdentitiesTo(sUserID);
							Echo("User successfully created: "+sUserID);
						}
						catch(e)
						{
							var sErrorDesc = "";
							var sErrorCode = "";
							var rsError = objIdentityMgmt.GetLastError();
							var sSql = "SELECT * FROM ConsumerIdentities WHERE 1=1";
							rsError.Open(sSql,"");
							if (!rsError.EOF)
							{
								sErrorDesc = rsError.Fields("ErrorDesc").value;
								sErrorCode = rsError.Fields("ErrorID").value;
							}
							rsError.Close();
							sRet = "ERROR Adding identities to new user: "+sErrorCode+" "+sErrorDesc;
						}
						finally
						{
							try
							{
								objIdentityMgmt.Clear();
							}
							catch(e){}
						}
					}
				}
				else if (bAllowUserCreation && !bIdentityAsType)
				{
					sRet = "ERROR User cannot be created: no typed identities specified";
				}
			}
			else
			{
				// retrieve the job owner details
				Echo("User: "+sUserID);
				objOProxy.LoadObject(sUserID);
				sGroupID  = objOProxy.GetProperty("DefaultGroupID");
				sCcID     = objOProxy.GetProperty("DefaultCostCenter");
				sIdentity = objOProxy.GetProperty("Login");
			}

			if (sUserID != "")
			{
				sGroupID = StripNullGuid(sGroupID);
				sCcID    = StripNullGuid(sCcID);
			}

			var sEcho = "";
			var sLogin2ndChoice = "";
			for (var i = 0; i < aXmlIdentityValue.length; i++)
			{
				sEcho += ", "+aXmlIdentityType[i]+":"+aXmlIdentityValue[i];
				if (sIdentity == "" && aXmlIdentityType[i] == "LDAPLogin") sIdentity = aXmlIdentityValue[i];
				if (sLogin2ndChoice == "" && aXmlIdentityType[i] == "Login") sLogin2ndChoice = aXmlIdentityValue[i];
			}
//			if (sIdentity == "") sIdentity = sLogin2ndChoice;

			Echo("JobOwnerIdentities:'"+sEcho.substr(2)+"'");
			Echo("JobOwnerUserID:'"+sUserID+"'");
			Echo("JobOwnerGroupID:'"+sGroupID+"'");
			Echo("JobOwnerCcID:'"+sCcID+"'");
			Echo("JobOwnerIsNew:'"+sIsNew+"'");
			Echo("HotFolderJobName:'"+sXmlJobName+"'");
			Echo("HotFolderJobFile:'"+sXmlJobFile+"'");
			Echo("HotFolderJobPDL:'"+sXmlJobPDL+"'");
			Echo("HotFolderJobTS:'"+sXmlTimestamp+"'");
			if (sXmlJobPDL == "PDF" || sXmlJobPDL == "UNIDRV")
			{
				Echo("CopyCount:'"+sTckCopies+"'");
				Echo("DIF_Duplex:'"+sTckDuplex+"'");
				Echo("DIF_Color:'"+sTckColorMode+"'");
				Echo("DIF_Stapling:'"+sTckStaple+"'");
				Echo("DIF_HolePunch:'"+sTckPunch+"'");
				Echo("DIF_Media:'"+sTckMedia+"'"+(sTckTray == "" ? "" : " (Tray #"+sTckTray+")"));
				Echo("PageSize:'"+sTckPageSize+"'");				
			}

			if (sUserID == "")
			{
				if (sRet == "OK") sRet = "ERROR User unknown";
			}

			bUseAlt = bNativeJobsToInputPrinterAlt /*&& !bHasTicket */&& sXmlJobPDL == "NATIVE" && sInputPrinterAlt != "";
			if (bUseAlt)
			{
				Echo("InputPrinterAlt:true");
				sJobName = sXmlJobName;
			}

			if (sIdentity == "" && bUseAlt)
			{
				if (sRet == "OK") sRet = "ERROR Cannot insert print job due to unavailable LDAPLogin identity";
			}
			if (sRet == "OK")
			{
				// store configuration in global variables for later use
				sConfigFile = sDir + sFileWithoutExt + ".jobprop";
				sConfigString = "2|"+ // version
					PipeEncode(sUserID)+"|"+
					PipeEncode(sGroupID)+"|"+
					PipeEncode(sCcID)+"|"+
					PipeEncode(sIsNew)+"|"+
					PipeEncode(sXmlJobName)+"|"+
					PipeEncode(sXmlJobPDL)+"|"+
					PipeEncode(sXmlTimestamp)+"|"+
					PipeEncode(sTckCopies)+"|"+
					PipeEncode(sTckDuplex)+"|"+
					PipeEncode(sTckColorMode)+"|"+
					PipeEncode(sTckStaple)+"|"+
					PipeEncode(sTckPunch)+"|"+
					PipeEncode(sTckPageSize)+"|"+					
					PipeEncode(sTckMedia);
				sPdlFile = sXmlJobFile;
			}
		}
	}
	catch(e)
	{
		sRet = "ERROR " + e.message;
	}
	if (sPdlFile == "") sPdlFile = sXmlJobFile;

	return sRet;
}

// Post-process the XML file (which is the spool file)
// Fills sLDAPLogin with the identified user's login name
function PostProcessFileXml(sXmlFile)
{
	var sRet = "OK";

	// write the .jobprop file
	//if (!bUseAlt)
	{
		Echo("PostProcessFileXml();");
		Echo("sConfigString:" + sConfigString);
		if (WriteUFile(sConfigFile, sConfigString)) sRet = "ERROR Cannot write .jobprop file";
	}

	return sRet;
}

// Post-process the PDF/UD/Native file (which isn't the spool file)
function PostProcessFilePdl(sPdlFile)
{
	var sRet = "OK";

	// insert pdf print job with the correct job owner and the job name containing the full path of the .jobprop file.
	Echo("PostProcessFilePdl();");
	try
	{
		if (bUseAlt)
		{
			//Echo("InsertPrintJob('"+sPdlFile+"','"+sInputPrinterAlt+"','"+sIdentity+"','"+sJobName+"',false)");
			//var newJobID = objJobTck.InsertPrintJob(sPdlFile, sInputPrinterAlt, sIdentity, sJobName, false);
			Echo("InsertPrintJob('"+sPdlFile+"','"+sInputPrinterAlt+"','"+sIdentity+"','"+sConfigFile+"',false)");
			var newJobID = objJobTck.InsertPrintJob(sPdlFile, sInputPrinterAlt, sIdentity, sConfigFile, false);			
			Echo("InsertPrintJob:"+newJobID);
			WScript.Sleep(200);
		}
		else
		{
			Echo("InsertPrintJob('"+sPdlFile+"','"+sInputPrinter+"','"+sIdentity+"','"+sConfigFile+"',false)");
			var newJobID = objJobTck.InsertPrintJob(sPdlFile, sInputPrinter, sIdentity, sConfigFile, false);
			Echo("InsertPrintJob:"+newJobID);
			WScript.Sleep(200);
		}
		if (newJobID == "")
		{
			Echo("InsertPrintJob: No job inserted for input printer "+sInputPrinter);
			sRet = "ERROR InsertPrintJob: No job inserted";
		}
	}
	catch(e)
	{
		sRet = "ERROR InsertPrintJob: "+e.message;
	}

	return sRet;
}

function Main()
{
	var sRet = "";
	if (WScript.Arguments.length > 0)
	{
		sJobID = WScript.Arguments.Item(0);
		if (nTimeout < 3) nTimeout = 3;
		sDataFolder = SmartGetRegKey("DATAFOLDER","");

		try
		{
			objJobTck.JobId = sJobID;

			// Pre processing
			// JobName is assumed to contain the original filename (full path) of file that is now the spool file
			sProviderID = objJobPpp.GetJobProperty(sJobID, "ServiceProvider");
			sSourceFile = objJobPpp.GetJobProperty(sJobID, "$SourceFile");
			sSpoolFile = objJobPpp.GetJobProperty(sJobID, "SpoolFile");
			sJobName = objJobPpp.GetJobProperty(sJobID, "JobName");
			sJobName = sJobName.replace(/\\\\/g,"\\");
			if (sSourceFile == "") sSourceFile = sJobName;
			sDir = sSourceFile.replace(/[^:\\\/]+$/, ""); // ends with backslash
			sFile = sSourceFile.replace(/^.*[:\\\/]/, "");
			if (sFile.search(/\./) != -1) sFileExt = sFile.replace(/^.*\./, ".").toLowerCase(); // ".ext" with dot
			sFileWithoutExt = sFile.replace(/(\.[^.]*)?$/, ""); // ".ext" removed, if present
			Echo("sProviderID:'"+sProviderID+"'");
			Echo("sSourceFile:'"+sSourceFile+"'");
			Echo("sSpoolFile:'"+sSpoolFile+"'");
			Echo("sJobName:'"+sJobName+"'");
			Echo("sDir:'"+sDir+"'");
			Echo("sFile:'"+sFile+"'");

			if (sSourceFile == "")
			{
				sRet = "ERROR Cannot access job session container";
			}
			else if (sFileExt != ".xml")
			{
				sRet = "ERROR Hot folder monitor triggered on the wrong file type";
			}
			else if (sInputPrinter == "")
			{
				sRet = "ERROR No input printer configured";
			}
			else
			{
				// Where is the spool file?
				var sWaitForFileXml = sSpoolFile; // path+file

				sRet = PreProcessFileXml(sWaitForFileXml); // xml file (ticket)
				var sWaitForFilePdl = sPdlFile;

				var bArrivedFileXml = true;
				var bArrivedFilePdl = false;
				var bAllFilesArrived = false;
				if (sRet == "OK")
				{
					// Further file(s) to wait for
					Echo("sWaitForFile1:'"+sWaitForFileXml+"'");
					Echo("sWaitForFile2:'"+sWaitForFilePdl+"'");

					var nDelay = 0;
					while (nTimeout > 0 && sRet == "OK")
					{
						Echo("nTimeout:'"+nTimeout+"'");
						try
						{
							bArrivedFilePdl = HasFileArrived(sWaitForFilePdl, bArrivedFilePdl);

							bAllFilesArrived = bArrivedFileXml && bArrivedFilePdl;
							if (bAllFilesArrived)
							{
								sRet = "OK";
								break;
							}
						}
						catch(e)
						{
							sRet = "ERROR " + e.message;
							break;
						}
						// Wait with an increasing interval (but don't exceed nTimeout)
						if (nDelay < 10) nDelay++;
						if (nDelay > nTimeout) nDelay = nTimeout;
						nTimeout -= nDelay;
						WScript.Sleep(1000 * nDelay);
					}
					if (nTimeout == 0 && sRet == "") sRet = "TIMEOUT";
	
					// Post processing
					if (sRet == "OK") sRet = PostProcessFileXml(sWaitForFileXml); // xml file (ticket)
					if (sRet == "OK") sRet = PostProcessFilePdl(sWaitForFilePdl); // pdf file (spool file)
				}
	
				// Delete all files, apart from the spool file
				if (sWaitForFilePdl != "") DeleteFile(sWaitForFilePdl);
				if (sConfigFile != "" && sRet != "OK") DeleteFile(sConfigFile);
			}

			// "OK": condition met, "TIMEOUT": failed after timeout, "ERROR"+" "+msg: error,
			// empty: call configuration error
			objJobTck.SetValue("ScriptStatus", sRet);
			Echo("ScriptStatus:'"+sRet+"'");
		}
		catch(e)
		{
			Echo("EXCEPTION " + e.message);
		}
	}
}

Main();
