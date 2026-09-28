using App_Code;
using BlogEngine.Core;
using BlogEngine.Core.API.BlogML;
using BlogEngine.Core.Providers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Linq;
using System.Net;

[ApiController]
[Route("api/[controller]")]
public class UploadController : ControllerBase
{
    private readonly IWebHostEnvironment _env;

    public UploadController(IWebHostEnvironment env)
    {
        _env = env;
    }

    [HttpPost]
    public IActionResult Post(string action, string dirPath = "")
    {
        WebUtils.CheckRightsForAdminPostPages(false);

        var file = Request.Form.Files.Count > 0 ? Request.Form.Files[0] : null;
        action = action.ToLowerInvariant();

        if (file != null && file.Length > 0)
        {
            var dirName = string.Format("/{0}/{1}", DateTime.Now.ToString("yyyy"), DateTime.Now.ToString("MM"));
            var fileName = new FileInfo(file.FileName).Name; // to work in IE and others

            // iOS sends all images as "image.jpg" or "image.png"
            fileName = fileName.Replace("image.jpg", DateTime.Now.ToString("yyyyMMddHHmmssfff") + ".jpg");
            fileName = fileName.Replace("image.png", DateTime.Now.ToString("yyyyMMddHHmmssfff") + ".png");

            var root = Blog.CurrentInstance.StorageLocation + Utils.FilesFolder;

            dirPath = dirPath.SanitizePath(root);
            
            if (!string.IsNullOrEmpty(dirPath))
                dirName = dirPath;

            if (action == "filemgr" || action == "file")
            {
                string[] ImageExtensnios = { ".jpg", ".png", ".jpeg", ".tiff", ".gif", ".bmp" };

                if (ImageExtensnios.Any(x => fileName.ToLower().Contains(x.ToLower())))
                    action = "image";
                else
                    action = "file";
            }

            var dir = new BlogEngine.Core.FileSystem.Directory();
            var retUrl = "";

            if (action == "import")
            {
                if (Security.IsAdministrator)
                {
                    return ImportBlogML(file);
                }
            } 
            if (action == "profile")
            {
                if (Security.IsAuthorizedTo(Rights.EditOwnUser))
                {
                    // upload profile image
                    dir = BlogService.GetDirectory("/avatars");
                    var dot = fileName.LastIndexOf(".");
                    var ext = dot > 0 ? fileName.Substring(dot) : "";
                    if (User.Identity.Name.Contains("/") || User.Identity.Name.Contains(@"\"))
                        throw new ApplicationException("Invalid character detected in UserName");
                    var profileFileName = User.Identity.Name + ext;

                    var imgPath = Path.Combine(_env.ContentRootPath, dir.FullPath.TrimStart('~', '/').Replace('/', Path.DirectorySeparatorChar), profileFileName);
                    
                    // Save the uploaded file, then create a thumbnail
                    // REVIEW: System.Drawing is not available on all platforms in .NET Core.
                    // Using stream copy as a cross-platform approach. 
                    // For production thumbnail generation, consider using ImageSharp.
                    var imgDir = Path.GetDirectoryName(imgPath);
                    if (!System.IO.Directory.Exists(imgDir))
                        System.IO.Directory.CreateDirectory(imgDir);

                    using (var stream = new FileStream(imgPath, FileMode.Create))
                    {
                        file.CopyTo(stream);
                    }

                    return Created("", profileFileName);
                }
            }
            if (action == "image")
            {
                if (Security.IsAuthorizedTo(Rights.EditOwnPosts))
                {
                    dir = BlogService.GetDirectory(dirName);
                    var uploaded = BlogService.UploadFile(file.OpenReadStream(), fileName, dir, true);
                    return Created("", uploaded.AsImage.ImageUrl);
                }
            }
            if (action == "file")
            {
                if (Security.IsAuthorizedTo(Rights.EditOwnPosts)) 
                {
                    dir = BlogService.GetDirectory(dirName);
                    var uploaded = BlogService.UploadFile(file.OpenReadStream(), fileName, dir, true);
                    retUrl = uploaded.FileDownloadPath + "|" + fileName + " (" + BytesToString(uploaded.FileSize) + ")";
                    return Created("", retUrl);
                }
            }
            if (action == "video")
            {
                if (Security.IsAuthorizedTo(Rights.EditOwnPosts))
                {
                    // default media folder
                    var mediaFolder = "Custom/Media";

                    // get the mediaplayer extension and use it's folder
                    var mediaPlayerExtension = BlogEngine.Core.Web.Extensions.ExtensionManager.GetExtension("MediaElementPlayer");
                    mediaFolder = mediaPlayerExtension.Settings[0].GetSingleValue("folder");

                    var folder = Path.Combine(_env.ContentRootPath, mediaFolder.Replace('/', Path.DirectorySeparatorChar));

                    UploadVideo(folder, file, fileName);

                    return Created("", fileName);
                }
            }
        }
        return BadRequest();
    }

    #region Private methods

    IActionResult ImportBlogML(IFormFile file)
    {
        if (file != null && file.Length > 0)
        {
            var reader = new BlogReader();
            using (var rdr = new StreamReader(file.OpenReadStream()))
            {
                reader.XmlData = rdr.ReadToEnd();
            }

            if (reader.Import())
            {
                return Ok();
            }
        }
        return StatusCode(500);
    }

    static string BytesToString(long byteCount)
    {
        string[] suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB" };
        if (byteCount == 0)
            return "0" + suf[0];
        long bytes = Math.Abs(byteCount);
        int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
        double num = Math.Round(bytes / Math.Pow(1024, place), 1);
        return (Math.Sign(byteCount) * num).ToString() + suf[place];
    }

    private void UploadVideo(string folderPath, IFormFile file, string fileName)
    {
        if (!System.IO.Directory.Exists(folderPath))
        {
            System.IO.Directory.CreateDirectory(folderPath);
        }
        var filePath = Path.Combine(folderPath, fileName);
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            file.CopyTo(stream);
        }
    }

    #endregion
}
