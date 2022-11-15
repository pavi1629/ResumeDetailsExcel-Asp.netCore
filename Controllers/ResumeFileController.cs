using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using SampleCore.Core.IServices;
using SampleCore.Core.Model;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.Data.OleDb;
using Microsoft.Extensions.FileProviders;
using Microsoft.Azure.Documents;
using ExcelDataReader;
using System.Diagnostics;

namespace ResumeDetails.Controllers
{
    public class ResumeFileController : Controller
    {
        //SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["con"].ConnectionString);
        //private OleDbConnection Econ;
        private readonly IResumeServices _ResumeDataServices;
        public ResumeFileController(IResumeServices services)
        {
            _ResumeDataServices = services;
        }
        #region Inserting a New Resume File
               
        [HttpGet]
        public IActionResult InsertFile()
        {
            return View();
        }
        #endregion
        #region List the Resume Details 
        [HttpGet]
        public IActionResult ReadList()
        {
            var data = _ResumeDataServices.ReadList();
            return View(data);
        }
        #endregion
        #region Accessing Through Excel Datas To database
              
        [HttpPost]
        public async Task<IActionResult> FileUpload(IFormFile file,Resume ResumeDatas)
        {
            if (file == null || file.Length == 0)
                return Content("file not selected");

            var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot\\Resume_Datas",
                        file.FileName);

            using (var stream = new FileStream(path, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            ResumeDatas.Resumes = path;
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            using (var stream = System.IO.File.Open(path, FileMode.Open, FileAccess.Read))
            {
                using (var reader = ExcelReaderFactory.CreateReader(stream))
                {
                    while (reader.Read()) //Each row of the file
                    {                        
                        ResumeDatas.FullName = reader.GetValue(0).ToString();
                        ResumeDatas.sslc = reader.GetValue(1).ToString();
                        ResumeDatas.Hsc = reader.GetValue(2).ToString();
                        ResumeDatas.CGPA = reader.GetValue(3).ToString();
                        ResumeDatas.Interest = reader.GetValue(4).ToString();
                        ResumeDatas.Skills = reader.GetValue(5).ToString();
                    }
                }
            }
            _ResumeDataServices.InsertFile(ResumeDatas);
            return RedirectToAction("ReadList");
        }
        #endregion
        #region Download The Excel Details            
        public async Task<IActionResult> Download(string filename)
        {
            if (filename == null)

                return Content("filename not present");

            var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot\\Resume_Datas", filename);

            var memory = new MemoryStream();
            using (var stream = new FileStream(path, FileMode.Open))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;
            
            return File(memory, GetContentType(path), Path.GetFileName(path));
        }
        private string GetContentType(string path)
        {
            var types = GetMimeTypes();
            var ext = Path.GetExtension(path).ToLowerInvariant();
            return types[ext];
        }

        private Dictionary<string, string> GetMimeTypes()
        {
            return new Dictionary<string, string>
            {              
                {".pdf", "application/pdf"},                
                {".xls", "application/vnd.ms-excel"},
                {".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"},               
            };
        }
        #endregion
        #region Partial view              
        public IActionResult Excel(int ResumeId)
        {
            var data = _ResumeDataServices.Excel(ResumeId);
            return PartialView(data);

        }
        #endregion


    }

}


