using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Drawing.Drawing2D;
using Serilog;

namespace GreeterPlugin.PluginHelpers;

[SuppressMessage("Interoperability", "CA1416:Plattformkompatibilität überprüfen")]
public static class GenerateWelcomeImage
{
    public static async void Generator(string username, string avatarUrl, string welcomeText, int memberCount, string backgroundUrl, bool roundedAvatar, double offsetX, double offsetY, string welcomeCardPath )
    {
        var baseBitmap = new Bitmap(backgroundUrl);
        var baseGraphic = Graphics.FromImage(baseBitmap);
        

        
        await AddImage(avatarUrl, roundedAvatar, baseGraphic, offsetY, offsetX, username);
        AddText(username,welcomeText,memberCount, baseGraphic);
        
        baseBitmap.Save(welcomeCardPath);
        

    }
    
    private static async Task AddImage(string imageUrl,bool roundedAvatar, Graphics baseGraphic,double offsetY, double offsetX, string username)
    {
        var avatarLocalPath = Path.Combine(GreeterPlugin.StaticPluginDirectory,"temp",$"avatar-{username}.png") ;
        
        await GetUserAvatar(imageUrl, avatarLocalPath);
        
        var avatarBitmap = new Bitmap(avatarLocalPath);
        
        avatarBitmap = ResizeImage(avatarBitmap, new Size(640, 640));
        

        if (roundedAvatar)
        {
            avatarBitmap = OvalImage(avatarBitmap);
        }

        var avatarOffsetY = baseGraphic.DpiY / 2 + avatarBitmap.Height / 2 - offsetY;
        var avatarOffsetX = offsetX;
        
        var image = new PointF((float)avatarOffsetX, (float)avatarOffsetY);
        
        baseGraphic.DrawImage(avatarBitmap, image);

        if (!IsFileLocked.Check(avatarLocalPath, 10))
        {
            File.Delete(avatarLocalPath);
        }
        else
        {
            Log.Error("[Greeter Plugin] Failed to delete local avatar, file appears to be locked! Filepath: {FilePath}", avatarLocalPath);
            
        } 
    

    }
    
    private static Bitmap ResizeImage(Bitmap imgToResize, Size size)
    {
        try
        {
            var bitmap = new Bitmap(size.Width, size.Height);
            using (var graphics = Graphics.FromImage((Image)bitmap))
            {
                graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                graphics.DrawImage(imgToResize, 0, 0, size.Width, size.Height);
            }
            return bitmap;
        }
        catch 
        { 
            Log.Error("Failed to resize image");
            return imgToResize; 
        }
    }
    
    private static async Task GetUserAvatar(string avatarUrl, string localPath)
    {
        var httpClient = new HttpClient();

        var res = await httpClient.GetAsync(avatarUrl);

        var bytes = await res.Content.ReadAsByteArrayAsync();

        using var image = Image.FromStream(new MemoryStream(bytes));
        image.Save(localPath);
    }
    
    private static void AddText(string username,string welcomeText, int memberCount, Graphics baseGraphic)
    {
        var fontMainText = new Font("Tahoma", 40);
        var fontSubText = new Font("Tahoma", 30);
        var brushMainText = Brushes.White;
        var brushSubText = Brushes.White;
        var pointMainText = new PointF(850,400);
        var pointSubText = new PointF(1000,500);

        if (welcomeText.Contains("{username}"))
            welcomeText = welcomeText.Replace("{username}", username);
    
        baseGraphic.DrawString(welcomeText, fontMainText, brushMainText, pointMainText);
    
        baseGraphic.DrawString($"Member: #{memberCount}", fontSubText, brushSubText, pointSubText);
    }
    
    private static Bitmap OvalImage(Image img) {
        var bmp = new Bitmap(img.Width, img.Height);
        
        using var gp = new GraphicsPath();
        gp.AddEllipse(0, 0, img.Width, img.Height);
        
        using var gr = Graphics.FromImage(bmp);
        gr.SetClip(gp);
        gr.DrawImage(img, Point.Empty);

        return bmp;
    }
    



}