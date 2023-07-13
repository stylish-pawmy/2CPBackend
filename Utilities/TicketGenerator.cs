namespace _2cpbackend.Utilities;

using ImageMagick;
using QRCoder;
using Azure.Storage.Blobs;

using _2cpbackend.Models;

public class TicketGenerator
{
    private readonly IConfiguration _configuration;

    public TicketGenerator(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public static byte[] WriteEventInfo(string outputImagePath, Event eventData, ApplicationUser user)
    {
        //Blob Storage
        var templateBlobConnectionString = "DefaultEndpointsProtocol=https;AccountName=2cpbackendstore;AccountKey=4o932Fjoevj8lyAtU7533t1VE6oI0x8oBCvUCNRZOsL9UaBBtFNeqmQqKoZTqatnErkjhSuFtEEP+ASt7zQ5bw==;EndpointSuffix=core.windows.net";

        var blobServiceClient = new BlobServiceClient(templateBlobConnectionString);
        var containerClient = blobServiceClient.GetBlobContainerClient("templates");
        var blobClient = containerClient.GetBlobClient("TicketTemplate.png");

        // Download the font file from Azure Blob Storage
        var fontBlobClient = containerClient.GetBlobClient("Montserrat.ttf");
        var fontFilePath = Path.Combine(Path.GetTempPath(), "Montserrat.ttf");
        fontBlobClient.DownloadTo(fontFilePath);

        // Read the template image using Magick.NET
        using var imageTemplate = new MemoryStream();
        blobClient.DownloadTo(imageTemplate);
        imageTemplate.Seek(0, SeekOrigin.Begin);

        using var stream = new MemoryStream();
        using (var templateImage = new MagickImage(imageTemplate))
        {
            // Create a new MagickDraw object to write text
                /*-----------------TITLE----------------------*/
                // Set the text settings
                var settings = new MagickReadSettings
                {
                    Font = fontFilePath,
                    FontPointsize = 50,
                    FillColor = MagickColors.Black,
                    TextGravity = Gravity.West,
                    FontWeight = FontWeight.Normal,
                    BackgroundColor = MagickColors.Transparent,
                };                
                // Draw the text on the image
                using (var caption = new MagickImage($"label: {eventData.Title}", settings))
                {
                    // Calculate the position for the text
                    var textX = 160;
                    var textY = 90;

                    templateImage.Composite(caption, textX, textY, CompositeOperator.Over);
                }

                /*-----------------Pice----------------------*/
                // Set the text settings
                settings = new MagickReadSettings
                {
                    Font = fontFilePath,
                    FontPointsize = 20,
                    FillColor = MagickColors.Black,
                    TextGravity = Gravity.West,
                    FontWeight = FontWeight.Bold,
                    BackgroundColor = MagickColors.Transparent,
                };                

                // Draw the text on the image
                var priceText = $"DZD{eventData.Price}";
                if (eventData.Price == 0) priceText = "FREE";
                using (var caption = new MagickImage($"label: {priceText}", settings))
                {
                    // Calculate the position for the text
                    var textX = 803;
                    var textY = 230;

                    templateImage.Composite(caption, textX, textY, CompositeOperator.Over);
                }
                /*-----------------Date----------------------*/
                settings.FillColor = new MagickColor("#6600ccff");
                using (var caption = new MagickImage($"label: {eventData.DateAndTime.ToString("MMMM dd, yyyy")}", settings))
                {
                    // Calculate the position for the text
                    var textX = 150;
                    var textY = 350;

                    templateImage.Composite(caption, textX, textY, CompositeOperator.Over);
                }

                /*-----------------Time----------------------*/
                settings.FillColor = new MagickColor("#00cc99ff");
                using (var caption = new MagickImage($"label: {eventData.DateAndTime.ToString("h:mm tt")}", settings))
                {
                    // Calculate the position for the text
                    var textX = 400;
                    var textY = 350;

                    templateImage.Composite(caption, textX, textY, CompositeOperator.Over);
                }

                /*-----------------Location----------------------*/
                settings.FillColor = new MagickColor("#f4c802ff");
                var locationText = $"{eventData.Location.X}°W, {eventData.Location.Y}°N";
                using (var caption = new MagickImage($"label: {locationText}", settings))
                {
                    // Calculate the position for the text
                    var textX = 600;
                    var textY = 350;

                    templateImage.Composite(caption, textX, textY, CompositeOperator.Over);
                }

                /*----------------QRCode-----------------------*/
                // Generate the QR code image
                var qrCodeImage = GenerateQRCode(user, eventData); // Adjust the width and height as needed
                qrCodeImage.Resize(398, 398);

                // Calculate the position for the QR code
                var posX = 1151;
                var posY = 51;

                // Compose the QR code on top of the template image
                templateImage.Composite(qrCodeImage, posX, posY, CompositeOperator.Over);

            /*-------------------Save the modified image------------------------------*/
            templateImage.Format = MagickFormat.Png;
            templateImage.Write(stream);

            return stream.ToArray();
        }
    }

    public static MagickImage GenerateQRCode(ApplicationUser user, Event eventData)
    {
        var information = $"First Name: {user.FirstName}\n"
                        + $"Last Name: {user.LastName}\n"
                        + $"Birth Date: {user.BirthDate.ToString("dd/MM/yyyy")}\n"
                        + $"Event: {eventData.Title}";
                        
        var qrGenerator = new QRCodeGenerator();
        var qrCodeData = qrGenerator.CreateQrCode(information, QRCodeGenerator.ECCLevel.Q);
        var qrCode = new QRCode(qrCodeData);
        var qrCodeImage = qrCode.GetGraphic(20); // Set the pixel size of the QR code

        // Convert the QR code image to a MagickImage
        using (var stream = new MemoryStream())
        {
            qrCodeImage.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
            stream.Position = 0;
            return new MagickImage(stream);
        }
    }
}