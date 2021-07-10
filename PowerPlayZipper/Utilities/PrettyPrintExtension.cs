namespace PowerPlayZipper.Utilities
{
    public static class PrettyPrintExtension
    {
        public static string ToBinaryPrefixString(this long value)
        {
            if (value == 0)
            {
                return $"0Byte";
            }
            else if (value < 10000)
            {
                return $"{value}Bytes";
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

        public static string ToBinaryPrefixString(this double value)
        {
            if (value == 0.0)
            {
                return $"0Byte";
            }
            else if (value < 10000.0)
            {
                return $"{value:F2}Bytes";
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
