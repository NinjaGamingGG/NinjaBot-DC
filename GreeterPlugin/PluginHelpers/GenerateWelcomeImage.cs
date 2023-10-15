using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace GreeterPlugin.PluginHelpers;

[SuppressMessage("Interoperability", "CA1416:Plattformkompatibilität überprüfen")]
public static class GenerateWelcomeImage
{
    public static async Task Generator(string username, string avatarUrl, string welcomeText, int memberCount, string backgroundUrl, bool roundedAvatar, double offsetX, double offsetY, string welcomeCardPath ,bool whiteCorner = true )
    {
        var baseBitmap = new Bitmap(Path.Combine(GreeterPlugin.StaticPluginDirectory,backgroundUrl));
        var baseGraphic = Graphics.FromImage(baseBitmap);
        
        await AddImage(avatarUrl, roundedAvatar, baseGraphic, offsetY, offsetX, whiteCorner);
        AddText(username,welcomeText,memberCount, baseGraphic);
        
        var memoryStream = new MemoryStream();
        baseBitmap.Save(memoryStream,ImageFormat.Png);

        var file = new FileStream(welcomeCardPath, FileMode.Create, FileAccess.Write);
        memoryStream.WriteTo(file);

        file.Close();

    }
    
    private static async Task AddImage(string imageUrl,bool roundedAvatar, Graphics baseGraphic,double offsetY, double offsetX, bool whiteCorner)
    {


        var avatarBitmap = await GetUserAvatar(imageUrl);

        var whiteOverlay = new Bitmap(650, 650);
        
        using (var graph = Graphics.FromImage(whiteOverlay))
        {
            var imageSize = new Rectangle(0,0,whiteOverlay.Width,whiteOverlay.Height);
            graph.FillRectangle(Brushes.White, imageSize);
        }
        
        
        
        
        avatarBitmap = ResizeImage(avatarBitmap, new Size(640, 640));
        

        if (roundedAvatar)
        {
            whiteOverlay = OvalImage(whiteOverlay);
            
            avatarBitmap = OvalImage(avatarBitmap);
        }

        var avatarOffsetY = baseGraphic.DpiY / 2.0f + avatarBitmap.Height / 2.0f - offsetY;

        var imagePoint = new PointF((float)offsetX+15, (float)avatarOffsetY+15);
        var overlayPoint = new PointF((float)offsetX+10.5f, (float)avatarOffsetY+10.5f);
        
        if (whiteCorner)
            baseGraphic.DrawImage(whiteOverlay, overlayPoint);
        baseGraphic.DrawImage(avatarBitmap, imagePoint);
    }

    private static Image ResizeImage(Image imgToResize, Size size)
    {
        return new Bitmap(imgToResize, size);
    }
    
    private static async Task<Image> GetUserAvatar(string avatarUrl)
    {
        var httpClient = new HttpClient();

        var res = await httpClient.GetAsync(avatarUrl);

        var bytes = await res.Content.ReadAsByteArrayAsync();
        
        return Image.FromStream(new MemoryStream(bytes));
    }
    
    private static void AddText(string username,string welcomeText, int memberCount, Graphics baseGraphic)
    {
        var fontMainText = new Font("Tahoma", 60);
        var fontSubText = new Font("Tahoma", 40);
        var brushMainText = Brushes.White;
        var brushSubText = Brushes.White;
        var pointMainText = new PointF(850,400);
        var pointSubText = new PointF(1000,650);
        

        
        var boundingBoxPoint = new PointF(800, 150);
        var boundingBox = new RectangleF(boundingBoxPoint, new SizeF(750, 450));

        if (welcomeText.Contains("{username}"))
            welcomeText = welcomeText.Replace("{username}", username);
        
        baseGraphic.DrawString(welcomeText, fontMainText, brushMainText,boundingBox, new StringFormat(){Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Far, Trimming = StringTrimming.EllipsisWord});
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