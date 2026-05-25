using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace kg.ValheimEnchantmentSystem.Misc;

public static class DiscordWebhook
{
    private static readonly Regex HtmlTagRegex = new("<.*?>", RegexOptions.Compiled);

    public static void TrySend(string link, string msg)
    {
        msg = HtmlTagRegex.Replace(msg ?? string.Empty, "**");
        if (!Uri.TryCreate(link, UriKind.Absolute, out _)) return;

        Task.Run(async () =>
        {
            try
            {
                string json = JSON.ToJSON(new Dictionary<string, string> { { "content", msg } });
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(link);
                request.ContentType = "application/json";
                request.Method = "POST";

                using (StreamWriter streamWriter = new(await request.GetRequestStreamAsync()))
                {
                    await streamWriter.WriteAsync(json);
                }

                using WebResponse _ = await request.GetResponseAsync();
            }
            catch (Exception ex)
            {
                Utils.print($"Discord webhook send failed: {ex.Message}", ConsoleColor.Red);
            }
        });
    }
}
