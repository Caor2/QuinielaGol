using System;
using System.IO;
using System.Data;
using System.Reflection;
using ClosedXML.Excel;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using OfficeOpenXml;
using System.Web.Mvc;
using Quiniela.Models;
using Microsoft.AspNet.Identity;

namespace Quiniela.Controllers
{

    public class HomeController : Controller
    {
        private QuinielaGolEntities db = new QuinielaGolEntities();


        public void UpdateMatchEstatus() {
            List<Match> todayMatches = db.Match.Where(x => x.Date.Value.Day == DateTime.Today.Day).ToList<Match>();

            DateTime timeRightNow = DateTime.Now;
            DateTime endOfTheMatch, startOfTheMatch;
            
            //update Matches States:
            //Status 0:toBePlayed
            //Status 1:onProcess
            //Status 2:Finished
            
            if (todayMatches.Count > 0)
            {
                foreach (Match m in todayMatches)
                {
                    endOfTheMatch = m.Date.Value.AddHours(-4);
                    startOfTheMatch = m.Date.Value.AddHours(-6);
                    if ( m.Status != 2) {
                        if (timeRightNow > endOfTheMatch) {
                            m.Status = 2;
                        }
                        else if (timeRightNow > startOfTheMatch)
                        {
                            m.Status = 1;
                        }
                        db.Entry(m).State = System.Data.Entity.EntityState.Modified;
                        db.SaveChanges();
                    }
                }
            }
        }

        public ActionResult Index()
        {
            UpdateMatchEstatus();
            List<Match> todayMatches = db.Match.Where(x => x.Date.Value.Day == DateTime.Today.Day).ToList<Match>();
            
            int nearDay = db.Match.Where(x => x.Status == 0 && x.Date.Value.Day != DateTime.Today.Day).FirstOrDefault().Date.Value.Day;
            List<Match> nextMatches = db.Match.Where(x => x.Date.Value.Day == nearDay).ToList<Match>();
            foreach (Match m in nextMatches)
            {
                m.Date = m.Date.Value.AddHours(-6);
            }
            
            foreach (Match match in todayMatches)
            {
                match.Date = match.Date.Value.AddHours(-6);
            }

            //List<Prediction> usersPredictions = db.Prediction.ToList<Prediction>();
            List<Ranking> usersRank = db.Ranking.ToList<Ranking>();

            indexModels model = new indexModels();
            model.matches = todayMatches.OrderBy(x => x.Date).ToList();
            //model.predictions = usersPredictions;
            model.ranking = usersRank.OrderByDescending(o => o.Points).ToList();

            return View(model);
        }

        [Authorize]
        public ActionResult Prediction()
        {
            string _user = User.Identity.GetUserId();
            List<Prediction> userPredicts = db.Prediction.Where(x => x.UserId == _user).ToList();

            DateTime limitDay = (db.Match.FirstOrDefault().Date.Value).AddHours(-6);
            DateTime todayIs = DateTime.Now;
            PredictionModels model = new PredictionModels();

            if (todayIs >= limitDay) {
                model.allowUpload = false;
            }
            else {
                model.allowUpload = true;
            }

            model.userPredicts = userPredicts;

            return View(model);
        }

        public JsonResult UploadFile()
        {
            string UserId = User.Identity.GetUserId();
            bool alreadyUploaded = db.Prediction.Select(x => x.UserId).Contains(UserId);
            DateTime limitDay = (db.Match.FirstOrDefault().Date.Value).AddHours(-6);
            DateTime todayIs = DateTime.Now;

            if (Request.Files.Count > 0 && todayIs < limitDay)
            {
                try
                {
                    List<Prediction> predicts = new List<Prediction>();
                    HttpFileCollectionBase file = Request.Files;
                    if ((file != null) && (file.Count > 0))
                    {
                        if (alreadyUploaded)
                        {
                            var toDelete = db.Prediction.Where(x => x.UserId == UserId).ToList<Prediction>();
                            db.Prediction.RemoveRange(toDelete);
                        }
                        //string fileName = file.FileName;
                        //string fileContentType = file.ContentType;
                        byte[] fileBytes = new byte[Request.ContentLength];
                        var data = Request.InputStream.Read(fileBytes, 0, Convert.ToInt32(Request.ContentLength));
                        // var usersList = new List<Users>();
                        //using (var package = new ExcelPackage())
                        var package = new ExcelPackage(Request.InputStream);
                        ExcelWorksheet workSheet = package.Workbook.Worksheets.FirstOrDefault();

                        var cells = workSheet.Cells;

                        for (int i = 2; i <= 49; i++)
                        {
                            Prediction predict = new Prediction()
                            {
                                MatchId = i - 1,
                                UserId = User.Identity.GetUserId(),
                                LocalGoals = int.Parse(cells["B" + i].Text),
                                VisitorGoals = int.Parse(cells["C" + i].Text)
                            };

                            predicts.Add(predict);
                        }
                        
                        db.Prediction.AddRange(predicts);
                        db.SaveChanges();
                        Response.Redirect("/Home/Prediction");

                    }
                    return Json(new { status = "OK" }, JsonRequestBehavior.AllowGet);
                }
                catch (Exception ex)
                {
                    return Json(new { status = "Error en formato" }, JsonRequestBehavior.AllowGet);
                }

            }
            return Json("No se ha cargado ningun archivo.", JsonRequestBehavior.AllowGet);
        }

        public ActionResult Matches()
        {
            List<Match> matches = new List<Match>();
            matches = db.Match.ToList<Match>();

            foreach (Match m in matches)
            {
                m.Date = m.Date.Value.AddHours(-6);
            }

            return View(matches.OrderBy(x => x.Date).ToList());
        }

        public ActionResult AnotherPredicts(string searching)
        {
            List<Prediction> otherPredicts = db.Prediction.Where(x => x.UserId == searching).ToList();
            return View(otherPredicts.OrderBy(x => x.Match.Date).ToList());
        }

        public ActionResult FilteredPredicts(int searchFor)
        {
            List<Prediction> predictsForMatch = db.Prediction.Where(x => x.MatchId == searchFor).ToList();
            return View(predictsForMatch);
        }

        // Excel Area -----------------------------------------------------------------------------------

        public ActionResult downloadPredictions()
        {
            var wb = new XLWorkbook();
            List<Prediction> predList = db.Prediction.ToList();
            List<string> already = new List<string>();
            System.IO.Stream spreadsheetStream = new System.IO.MemoryStream();

            if (predList.Count > 0)
            {
                int i = 4;
                foreach (var item in predList)
                {
                    string usrName = item.AspNetUsers.UserName;

                    if (!already.Contains(usrName))
                    {
                        i = 4;
                        already.Add(usrName);
                        wb.AddWorksheet(usrName);
                        wb.Worksheet(usrName).Cell("B2").Value = usrName;
                        wb.Worksheet(usrName).Cell("B3").Value = "Local";
                        wb.Worksheet(usrName).Cell("C3").Value = "GL";
                        wb.Worksheet(usrName).Cell("D3").Value = "GV";
                        wb.Worksheet(usrName).Cell("E3").Value = "Visitante";

                        //Sheet Styles
                        //Ranges
                        var rngTable = wb.Worksheet(usrName).Range("B2:E51");
                        var rngTitle = wb.Worksheet(usrName).Range("B2:E2");
                        var rngDates = rngTable.Range("D3:D5"); // The address is relative to rngTable (NOT the worksheet)
                        var rngHeaders = rngTable.Range("A2:D2"); // The address is relative to rngTable (NOT the worksheet)

                        rngHeaders.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        rngHeaders.Style.Font.Bold = true;

                        //wb.Worksheet(usrName).Columns(1,5).AdjustToContents();
                        rngTable.Style.Font.FontName = "Arial";
                        rngTable.Style.Font.FontSize = 12;
                        rngHeaders.Style.Font.FontSize = 14;
                        rngHeaders.Style.Font.FontColor = XLColor.White;
                        rngHeaders.Style.Fill.BackgroundColor = XLColor.Gray;

                        rngTable.Cell(1, 1).Style.Font.Bold = true;
                        rngHeaders.Style.Font.FontSize = 16;
                        rngTitle.Style.Font.FontColor = XLColor.White;
                        rngTable.Cell(1, 1).Style.Fill.BackgroundColor = XLColor.SteelBlue;
                        rngTable.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                        rngTable.Row(1).Merge(); // We could've also used: rngTable.Range("A1:E1").Merge()

                        //Add a thick outside border
                        rngTable.Style.Border.OutsideBorder = XLBorderStyleValues.Thick;

                    }
                    wb.Worksheet(usrName).Cell("B" + i).Value = item.Match.Local;
                    wb.Worksheet(usrName).Cell("C" + i).Value = item.LocalGoals;
                    wb.Worksheet(usrName).Cell("D" + i).Value = item.VisitorGoals;
                    wb.Worksheet(usrName).Cell("E" + i).Value = item.Match.Visitor;
                    i++;
                }
                //Adjust the colums sizes
                foreach (var item in already)
                {
                    var ws = wb.Worksheet(item);
                    ws.Column(1).AdjustToContents();
                    ws.Column(2).AdjustToContents();
                    ws.Column(5).AdjustToContents();
                }

                wb.SaveAs(spreadsheetStream);
                spreadsheetStream.Position = 0;
            }
            else {
                Response.Redirect("/Home/Index");
            }
            return new FileStreamResult(spreadsheetStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet") { FileDownloadName = "UQ_Results_All.xlsx" };
        }
    }
}