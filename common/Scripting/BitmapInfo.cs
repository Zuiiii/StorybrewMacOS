namespace StorybrewCommon.Scripting
{
    public readonly struct BitmapInfo
    {
        public int Width { get; }
        public int Height { get; }

        public BitmapInfo(int width, int height)
        {
            Width = width;
            Height = height;
        }
    }
}
