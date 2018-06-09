using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using OfficeOpenXml;
using System.Web.Mvc;
using Quiniela.Models;
using Microsoft.AspNet.Identity;

namespace Quiniela.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {

        public ActionResult Index()
        {
            QuinielaGolEntities db = new QuinielaGolEntities();
            
            //update Matches States Here

            int nearDay = db.Match.Where(x => x.Status == 0).FirstOrDefault().Date.Value.Day; //.Select(x => x.Date.Value.Day as )

            List<Match> nextMatches = db.Match.Where(x => x.Date.Value.Day == nearDay).ToList<Match>();
            List<Prediction> usersPredictions = db.Prediction.ToList<Prediction>();
            List<Ranking> usersRank = db.Ranking.ToList<Ranking>();

            //Set UsersRank Points Here

            var model = new indexModels();
            model.matches = nextMatches;
            //model.predictions = usersPredictions;
            model.ranking = usersRank.OrderByDescending(o => o.Points).ToList();

            return View(model);
        }

        public ActionResult Prediction()
        {
            return View();
        }

        public JsonResult UploadFile()
        {
            string UserId = User.Identity.GetUserId();
            QuinielaGolEntities db = new QuinielaGolEntities();
            bool alreadyUploaded = db.Prediction.Select(x => x.UserId).Contains(UserId);
            DateTime limitDay = db.Match.FirstOrDefault().Date.Value; 

            if (DateTime.Today >= limitDay)
            {
                return Json(new { status = "Se alcanzó la fecha límite." }, JsonRequestBehavior.AllowGet);
            }
            else {
                if (alreadyUploaded)
                {

                    var toDelete = db.Prediction.Where(x => x.UserId == UserId).ToList<Prediction>();
                    db.Prediction.RemoveRange(toDelete); 
                }
                if (Request.Files.Count > 0)
                {
                    try
                    {
                        List<Prediction> predictions = new List<Prediction>();
                        HttpFileCollectionBase file = Request.Files;
                        if ((file != null) && (file.Count > 0))
                        {
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

                                predictions.Add(predict);
                            }
                            db.Prediction.AddRange(predictions);
                            db.SaveChanges();
                        }
                        return Json(new { status = "OK" }, JsonRequestBehavior.AllowGet);
                    }
                    catch (Exception ex)
                    {
                        return Json(new { status = "Error en formato" }, JsonRequestBehavior.AllowGet);
                    }

                }
            }

            return Json("No se ha cargado ningun archivo.", JsonRequestBehavior.AllowGet);
        }

        public ActionResult Matches()
        {
            QuinielaGolEntities db = new QuinielaGolEntities();
            List<Match> matches = new List<Match>();
            matches = db.Match.ToList<Match>();

            return View(matches);
        }
    }
}