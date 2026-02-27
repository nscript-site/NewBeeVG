namespace NewBeeMedia;

public unsafe static class Utils
{
    /// <summary>
    /// 快速的计算一个视频文件的hash值
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns></returns>
    public static String GetFileHash(String filePath)
    {
        if (String.IsNullOrEmpty(filePath) == true) return "0";

        if (filePath.StartsWith("http://") || filePath.StartsWith("https://"))
        {
            return CalcUrlHash(filePath);
        }

        using (FileStream fs = new FileStream(filePath, FileMode.Open))
        {
            long length = fs.Length;
            long hash = 0;
            if (length > 1024)
            {
                {
                    fs.Position = length / 4;
                    int i0 = fs.ReadByte();
                    int i1 = fs.ReadByte();
                    int i2 = fs.ReadByte();
                    int i3 = fs.ReadByte();
                    hash += i0 * 256 * 256 * 256;
                    hash += i1 * 256 * 256;
                    hash += i2 * 256;
                    hash += i3;
                }

                {
                    fs.Position = length / 2;
                    int i0 = fs.ReadByte();
                    int i1 = fs.ReadByte();
                    int i2 = fs.ReadByte();
                    int i3 = fs.ReadByte();
                    hash += i0 * 256 * 256 * 256;
                    hash += i1 * 256 * 256;
                    hash += i2 * 256;
                    hash += i3;
                }
            }

            return length.ToString("D16") + hash.ToString("D16");
        }
    }

    private static String CalcUrlHash(String url)
    {
        return url.GetHashCode().ToString() + (url + "media").GetHashCode().ToString();
    }

    public static double ToDouble(this AVRational val)
    {
        return val.num/(double)val.den;
    }

    public static String PtrToStringAuto(this IntPtr str)
    {
        return Marshal.PtrToStringAuto(str);
    }

    public static unsafe string GetErrorMessage(int error)
    {
        var bufferSize = 1024;
        var buffer = stackalloc byte[bufferSize];
        ffmpeg.av_strerror(error, buffer, (ulong)bufferSize);
        var message = Marshal.PtrToStringAnsi((IntPtr)buffer);
        return message;
    }

    public static int ThrowExceptionIfError(this int error)
    {
        if (error < 0) throw new ApplicationException(GetErrorMessage(error));
        return error;
    }

    /// <summary>
    /// 加载配置文件的工具方法
    /// </summary>
    /// <param name="fileName">配置文件名称</param>
    /// <param name="onMessage">加载过程消息接受函数，如果不设置，则默认使用 Console.WriteLine </param>
    /// <param name="cfgDirName">配置文件的目录名，默认使用 "conf"</param>
    /// <returns>配置文件内容。null 代表没找到配置文件。</returns>
    public static String LoadCfgFile(String fileName, Action<String> onMessage = null, String cfgDirName = "conf")
    {
        String path0 = cfgDirName + "/" + fileName;
        String path1 = "../" + cfgDirName + "/" + fileName;
        Action<String> onMsg = onMessage ?? Console.WriteLine;

        try
        {
            if (System.IO.File.Exists(path0))
            {
                onMsg($"Load Config File: " + new DirectoryInfo(path0).FullName);
                return System.IO.File.ReadAllText(path0);
            }
            else if (System.IO.File.Exists(path1))
            {
                onMsg($"Load Config File: " + new DirectoryInfo(path1).FullName);
                return System.IO.File.ReadAllText(path1);
            }
            else
            {
                onMsg($"Cann't find config file: " + fileName);
                return null;
            }
        }
        catch(IOException ex)
        {
            onMsg($"Load config file fail: " + ex.Message);
        }

        return null;
    }
}
