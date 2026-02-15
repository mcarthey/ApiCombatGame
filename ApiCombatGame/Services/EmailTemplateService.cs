using System.Net;
using ApiCombatGame.Services.Interfaces;

namespace ApiCombatGame.Services;

public class EmailTemplateService : IEmailTemplateService
{
    public string Render(string title, string bodyHtml, string? preheader = null)
    {
        var preheaderBlock = string.IsNullOrEmpty(preheader)
            ? ""
            : $"""<span style="display:none;font-size:1px;color:#f8f9fa;line-height:1px;max-height:0;max-width:0;opacity:0;overflow:hidden;">{Encode(preheader)}</span>""";

        return $$"""
<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="utf-8"/>
<meta name="viewport" content="width=device-width, initial-scale=1.0"/>
<title>{{Encode(title)}}</title>
<style>
  body { margin:0; padding:0; background:#f2f4f8; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; }
  .wrapper { width:100%; background:#f2f4f8; padding:32px 0; }
  .card { max-width:580px; margin:0 auto; background:#ffffff; border-radius:12px; overflow:hidden; box-shadow:0 1px 3px rgba(0,0,0,0.08); }
  .header { background:#2563eb; padding:24px 32px; text-align:center; }
  .header h1 { margin:0; color:#ffffff; font-size:20px; font-weight:700; letter-spacing:-0.02em; }
  .header .subtitle { margin:4px 0 0; color:rgba(255,255,255,0.85); font-size:13px; }
  .body { padding:28px 32px; color:#1f2937; font-size:15px; line-height:1.6; }
  .body h2 { margin:0 0 16px; font-size:18px; color:#111827; }
  .body p { margin:0 0 12px; }
  .meta-table { width:100%; border-collapse:collapse; margin:16px 0; }
  .meta-table td { padding:8px 12px; font-size:14px; border-bottom:1px solid #e5e7eb; }
  .meta-table td:first-child { font-weight:600; color:#374151; width:90px; white-space:nowrap; }
  .meta-table td:last-child { color:#4b5563; }
  .message-box { background:#f8fafc; border:1px solid #e2e8f0; border-radius:8px; padding:16px; margin:16px 0; font-size:14px; color:#334155; white-space:pre-wrap; line-height:1.6; }
  .cta { display:inline-block; background:#2563eb; color:#ffffff !important; padding:10px 24px; border-radius:8px; text-decoration:none !important; font-weight:600; font-size:14px; margin:8px 0; }
  .footer { padding:20px 32px; text-align:center; font-size:12px; color:#9ca3af; border-top:1px solid #f3f4f6; }
  .footer a { color:#6b7280; text-decoration:underline; }
</style>
</head>
<body>
{{preheaderBlock}}
<div class="wrapper">
  <div class="card">
    <div class="header">
      <h1>&#9876; API Combat Game</h1>
      <div class="subtitle">The API IS the game</div>
    </div>
    <div class="body">
      {{bodyHtml}}
    </div>
    <div class="footer">
      <p>&copy; {{DateTime.UtcNow.Year}} API Combat Game &mdash; <a href="https://apicombat.com">apicombat.com</a></p>
      <p style="margin-top:4px;">Built by <a href="https://learnedgeek.com">LearnedGeek</a></p>
    </div>
  </div>
</div>
</body>
</html>
""";
    }

    public string ContactNotification(string senderName, string senderEmail, string subject, string message)
    {
        var body = $"""
<h2>New Contact Form Submission</h2>
<table class="meta-table">
  <tr><td>From</td><td>{Encode(senderName)}</td></tr>
  <tr><td>Email</td><td><a href="mailto:{Encode(senderEmail)}">{Encode(senderEmail)}</a></td></tr>
  <tr><td>Subject</td><td>{Encode(subject)}</td></tr>
</table>
<div class="message-box">{Encode(message)}</div>
<p style="font-size:13px; color:#6b7280;">Reply directly to this email to respond to the sender (Reply-To is set).</p>
""";

        return Render($"Contact: {subject}", body, $"New message from {senderName}");
    }

    public string ContactThankYou(string senderName, string subject)
    {
        var body = $"""
<h2>Thanks for reaching out, {Encode(senderName)}!</h2>
<p>We received your message about <strong>{Encode(subject)}</strong> and will get back to you as soon as possible &mdash; usually within 1&ndash;2 business days.</p>
<p>In the meantime, feel free to explore the API documentation or jump into a battle:</p>
<p style="text-align:center;">
  <a href="https://apicombat.com/api-docs/v1" class="cta" style="color:#ffffff !important; text-decoration:none !important;">Explore the API &rarr;</a>
</p>
<p style="font-size:13px; color:#6b7280;">You&rsquo;re receiving this because you submitted a contact form on apicombat.com. No further emails will be sent unless you reach out again.</p>
""";

        return Render("We got your message!", body, "Thanks for contacting API Combat Game");
    }

    private static string Encode(string value) => WebUtility.HtmlEncode(value);
}
