using System.Drawing;
using System.Drawing.Imaging;

namespace StreamingService.Streamer.Helpers
{
    public static class FrameConverter
    {
        public static byte[] BitmapToByteArray(Bitmap bitmap, int quality)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                // Создаем параметры кодировщика для установки качества JPEG
                EncoderParameters encoderParams = new EncoderParameters(1);
                encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, quality);

                // Получаем кодировщик JPEG
                ImageCodecInfo jpegEncoder = GetEncoder(ImageFormat.Jpeg);

                // Сохраняем изображение в поток памяти
                bitmap.Save(ms, jpegEncoder, encoderParams);

                return ms.ToArray();
            }
        }

        private static ImageCodecInfo GetEncoder(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
            return codecs.First(codec => codec.FormatID == format.Guid);
        }
    }
}