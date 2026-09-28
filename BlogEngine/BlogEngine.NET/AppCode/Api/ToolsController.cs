using App_Code;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Net;
using System.Security.Principal;

[ApiController]
[Route("api/[controller]")]
public class ToolsController : ControllerBase
{
    private static object _SyncRoot = new object();
    private readonly IWebHostEnvironment _env;

    public ToolsController(IWebHostEnvironment env)
    {
        _env = env;
    }

    public IActionResult Get(string id)
    {
        if (id == "check1")
        {
            var result = string.Format("ASP.NET application Identity is: {0}", Environment.UserName);
            return Ok(result);
        }
        else if (id == "trust")
        {
            // ASP.NET Core runs in full trust always; there is no CAS trust level concept
            var data = new { success = true, msg = "Trust level: Full (ASP.NET Core)" };
            return Ok(data);
        }
        else
        {
            string fileName = GetFileName(id);
            var processed = Process(fileName);

            if (string.IsNullOrEmpty(processed))
            {
                var data = new { success = true, msg = "Can create and delete file " + fileName };
                return Ok(data);
            }
            else
            {
                var data = new { success = false, msg = "Error creating file " + fileName };
                return Ok(data);
            }             
        }
    }

    #region Private methods

    private string Process(string file)
    {
        string processed = WriteToFile(file);

        if (!string.IsNullOrEmpty(processed))
            return processed;
        
        return DeleteFile(file);
    }

    private string WriteToFile(string file)
    {
        lock (_SyncRoot)
        {
            try
            {
                using (FileStream fs = new FileStream(file, FileMode.Create))
                {
                    using (StreamWriter sw = new StreamWriter(fs))
                    {
                        sw.WriteLine(@"*-*-*");
                        sw.Close();
                        fs.Close();

                        return "";
                    }
                }
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }
    }

    private string DeleteFile(string file)
    {
        try
        {
            System.IO.File.Delete(file);
            return "";
        }
        catch (Exception e)
        {
            return e.Message;
        }
    }

    private string GetFileName(string dir)
    {
        var contentRoot = _env.ContentRootPath;

        if (dir == "root")
            return Path.Combine(contentRoot, "tmp.txt");

        if (dir == "data")
            return Path.Combine(contentRoot, "App_Data", "tmp.txt");

        return Path.Combine(contentRoot, dir, "tmp.txt");
    }

    #endregion
}
