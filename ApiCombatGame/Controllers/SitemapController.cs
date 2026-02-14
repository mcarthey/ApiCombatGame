using System.Text;
using Microsoft.AspNetCore.Mvc;

namespace ApiCombatGame.Controllers;

[ApiExplorerSettings(IgnoreApi = true)]
public class SitemapController : Controller
{
    [HttpGet("sitemap.xml")]
    [ResponseCache(Duration = 3600)]
    public IActionResult Sitemap()
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";

        var urls = new (string path, string changefreq, string priority)[]
        {
            ("/", "weekly", "1.0"),
            ("/api-docs/v1", "weekly", "0.9"),
            ("/Auth/Register", "monthly", "0.8"),
            ("/Auth/Login", "monthly", "0.7"),
            ("/Privacy", "yearly", "0.3"),
        };

        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");

        foreach (var (path, changefreq, priority) in urls)
        {
            sb.AppendLine("  <url>");
            sb.AppendLine($"    <loc>{baseUrl}{path}</loc>");
            sb.AppendLine($"    <changefreq>{changefreq}</changefreq>");
            sb.AppendLine($"    <priority>{priority}</priority>");
            sb.AppendLine("  </url>");
        }

        sb.AppendLine("</urlset>");

        return Content(sb.ToString(), "application/xml", Encoding.UTF8);
    }
}
