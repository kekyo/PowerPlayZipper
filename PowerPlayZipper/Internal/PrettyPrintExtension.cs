namespace PowerPlayZipper.Internal
{
    internal static class PrettyPrintExtension
    {
        public static string ToByteSize(this long value)
        {
            if (value < 10000)
            {
                return $"{value}B";
            }
            else if (value < (long)(1024.0 * 1024.0 / 0.9))
            {
                return $"{value/1024.0:F2}KiB";
            }
            else if (value < (long)(1024.0 * 1024.0 * 1024.0 / 0.9))
            {
                return $"{value / 1024.0 / 1024.0:F2}MiB";
            }
            else if (value < (long)(1024.0 * 1024.0 * 1024.0 * 1024.0 / 0.9))
            {
                return $"{value / 1024.0 / 1024.0 / 1024.0:F2}GiB";
            }
            else
            {
                return $"{value / 1024.0 / 1024.0 / 1024.0 / 1024.0:F2}TiB";
            }
        }

        public static string ToByteSize(this double value)
        {
            if (value < 10000.0)
            {
                return $"{value:F2}B";
            }
            else if (value < (1024.0 * 1024.0 / 0.9))
            {
                return $"{value / 1024.0:F2}KiB";
            }
            else if (value < (1024.0 * 1024.0 * 1024.0 / 0.9))
            {
                return $"{value / 1024.0 / 1024.0:F2}MiB";
            }
            else if (value < (1024.0 * 1024.0 * 1024.0 * 1024.0 / 0.9))
            {
                return $"{value / 1024.0 / 1024.0 / 1024.0:F2}GiB";
            }
            else
            {
                return $"{value / 1024.0 / 1024.0 / 1024.0 / 1024.0:F2}TiB";
            }
        }
    }
}
