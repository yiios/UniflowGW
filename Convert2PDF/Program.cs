using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using OFFICECORE = Microsoft.Office.Core;
using POWERPOINT = Microsoft.Office.Interop.PowerPoint;
using EXCEL = Microsoft.Office.Interop.Excel;
using WORD = Microsoft.Office.Interop.Word;
using System.IO;
using Microsoft.Office.Core;

namespace Convert2PDFConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 3) Environment.Exit(-1);
            var input = args[0];
            var output = args[1];
            if (args[2] != "-landscape" && args[2] != "-portrait") Environment.Exit(-2);
            bool land = args[2] == "-landscape";
            if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(output))
                Environment.Exit(-1);
            var ext = Path.GetExtension(input).ToLower();
            if (ext == ".xls" || ext == ".xlsx")
                ConvertExcelToPdf(input, output, land);
            else if (ext == ".doc" || ext == ".docx" || ext == ".txt")
                ConvertWordToPdf(input, output);
            else if (ext == ".ppt" || ext == ".pptx")
                ConvertPowerPointToPdf(input, output);
            else
                Environment.Exit(-2);
        }

        private static bool ConvertPowerPointToPdf(string sourcePath, string targetPath)
        {
            POWERPOINT.Application pptApp = null;
            POWERPOINT.Presentation objPresSet = null;
            try
            {
                pptApp = new POWERPOINT.Application();
                //pptApp.Visible = OFFICECORE.MsoTriState.msoFalse;
                pptApp.DisplayAlerts = POWERPOINT.PpAlertLevel.ppAlertsNone;

                objPresSet = pptApp.Presentations.Open(sourcePath + "::IN_VA_LI______D", MsoTriState.msoTrue, MsoTriState.msoTrue, MsoTriState.msoFalse);
                //info.AppendFormat("文件: {0}, 共{1}页", sourcePath, objPresSet.Slides.Count);
                objPresSet.SaveAs(targetPath, POWERPOINT.PpSaveAsFileType.ppSaveAsPDF);
            }
            catch
            {
                return false;
            }
            finally
            {
                if (objPresSet != null)
                {
                    objPresSet.Close();
                    objPresSet = null;
                }
                if (pptApp != null)
                {
                    pptApp.Quit();
                    pptApp = null;
                }
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            return true;
        }

        private static bool ConvertExcelToPdf(string sourcePath, string targetPath, bool land)
        {
            bool result;
            object missing = Type.Missing;

            EXCEL.XlFixedFormatType targetType = EXCEL.XlFixedFormatType.xlTypePDF;
            EXCEL.Application application = null;
            EXCEL.Workbook workBook = null;
            try
            {
                application = new EXCEL.Application();
                application.Visible = false;
                application.DisplayAlerts = false;
                string target = sourcePath.Replace(".xlsx", ".pdf");
                object type = targetType;
                workBook = application.Workbooks.Open(sourcePath,
                    AddToMru: false, ReadOnly: true, Password: "IN_VA_LI______D");
                foreach (EXCEL.Worksheet sheet in workBook.Sheets)
                {
                    sheet.PageSetup.Orientation = land ?
                        EXCEL.XlPageOrientation.xlLandscape : EXCEL.XlPageOrientation.xlPortrait;
                }
                workBook.ExportAsFixedFormat(targetType, targetPath,
                    EXCEL.XlFixedFormatQuality.xlQualityStandard,
                    IncludeDocProperties: true,
                    IgnorePrintAreas: false);
                result = true;
            }
            catch
            {
                result = false;
            }
            finally
            {
                if (workBook != null)
                {
                    workBook.Close(SaveChanges: false);
                    workBook = null;
                }
                if (application != null)
                {
                    application.Quit();
                    application = null;
                }
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            return result;
        }

        private static int ConvertWordToPdf(string sourcePath, string targetPath)
        {
            int result = -1;
            object paramMissing = Type.Missing;

            WORD.Application wordApplication = null;
            WORD.Document wordDocument = null;
            try
            {
                object source = sourcePath;
                wordApplication = new WORD.Application();
                wordApplication.Visible = false;
                wordApplication.DisplayAlerts = WORD.WdAlertLevel.wdAlertsNone;
                try
                {
                    wordDocument = wordApplication.Documents.OpenNoRepairDialog(
                        source, AddToRecentFiles: false,
                        ReadOnly: true, PasswordDocument: "IN_VA_LI______D");
                }
                catch
                {
                    return result;
                }
                if (wordDocument != null)
                {
                    WORD.WdStatistic stat = WORD.WdStatistic.wdStatisticPages;
                    int num = wordDocument.ComputeStatistics(stat);

                    wordDocument.ExportAsFixedFormat(targetPath, WORD.WdExportFormat.wdExportFormatPDF, IncludeDocProps: true);
                    result = num;
                }
            }
            finally
            {
                if (wordDocument != null)
                {
                    wordDocument.Close(SaveChanges: false);
                    wordDocument = null;
                }
                if (wordApplication != null)
                {
                    wordApplication.Quit(SaveChanges: false);
                    wordApplication = null;
                }
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            return result;
        }
    }
}
