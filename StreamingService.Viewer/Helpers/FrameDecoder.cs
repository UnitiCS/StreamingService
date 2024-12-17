namespace StreamingService.Viewer.Helpers
{
    public static class FrameDecoder
    {
        public static Bitmap DecodeBitmap(byte[] data)
        {
            using (var ms = new MemoryStream(data))
            {
                return new Bitmap(ms);
            }
        }

        public static async Task<Bitmap> DecodeBitmapAsync(byte[] data)
        {
            using (var ms = new MemoryStream(data))
            {
                return await Task.Run(() => new Bitmap(ms));
            }
        }
    }
}