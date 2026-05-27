namespace NewBeeMedia;

using FFmpeg.AutoGen;
using System.Runtime.CompilerServices;

public unsafe class FFmpegUtils
{
    /// <summary>
    /// 查找 ffmpeg 库所在目录
    /// </summary>
    /// <returns></returns>
    public static String? ScanFFmpegLibraryPath()
    {
        String domainBaseDir = AppDomain.CurrentDomain.BaseDirectory;
        if (domainBaseDir == null) domainBaseDir = "./";

        String[] scanDirs = new string[] {  "./", "./bin", "./lib", "./ffmpeg",
            domainBaseDir,
            domainBaseDir + "./bin",
            domainBaseDir + "./lib",
            domainBaseDir + "./ffmpeg",
            "c://lib/ffmpeg",
            "d://lib/ffmpeg",
            "c://ffmpeg",
            "d://ffmpeg"
        };

        return ScanFFmpegLibraryPath(scanDirs);
    }

    private static String? ScanFFmpegLibraryPath(String[] dirs)
    {
        foreach (var item in dirs)
        {
            var path = TryFindFFmpegLibraryPath(item);
            if (path != null) return path;
        }
        return null;
    }

    private static String? TryFindFFmpegLibraryPath(String dir)
    {
        DirectoryInfo dirInfo = new DirectoryInfo(dir);
        if (dirInfo.Exists == false) return null;
        var files = dirInfo.GetFiles("*vcodec*.*");
        if (files == null || files.Length == 0) return null;
        else return dirInfo.FullName;
    }


    #region Private Declarations

    private static readonly object FFmpegLogBufferSyncLock = new object();
    private static readonly List<string> FFmpegLogBuffer = new List<string>(1024);

    private static readonly object SyncLock = new object();
    private static bool m_IsInitialized;
    private static string m_LibrariesPath = string.Empty;

    #endregion

    #region Properties

    /// <summary>
    /// True when libraries were initialized correctly.
    /// </summary>
    public static bool IsInitialized
    {
        get { lock (SyncLock) { return m_IsInitialized; } }
    }

    /// <summary>
    /// Gets the libraries path. Only filled when initialized correctly.
    /// </summary>
    public static string LibrariesPath
    {
        get { lock (SyncLock) { return m_LibrariesPath; } }
    }

    #endregion

    #region FFmpeg Registration

    public static bool Initialize(string? ffmpegLibraryPath = null)
    {
        var path = ffmpegLibraryPath ?? ScanFFmpegLibraryPath();
        if (path == null)
            throw new Exception("Can not find ffmpeg library.");

        return InitializeCore(path);
    }

    private static bool InitializeCore(string overridePath)
    {
        lock (SyncLock)
        {
            if (m_IsInitialized)
                return false;

            try
            {
                // Get the temporary path where FFmpeg binaries are located
                var ffmpegPath = Path.GetFullPath(overridePath);

                FFLibrary.Load(ffmpegPath);
                m_LibrariesPath = ffmpegPath;
                m_IsInitialized = true;
            }
            catch
            {
                m_LibrariesPath = string.Empty;
                m_IsInitialized = false;

                // rethrow the exception with the original stack trace.
                throw;
            }

            return m_IsInitialized;
        }
    }

    #endregion

    #region Interop Helper Methods

    /// <summary>
    /// Copies the contents of a managed string to an unmanaged, UTF8 encoded string.
    /// </summary>
    /// <param name="source">The string to copy.</param>
    /// <returns>A pointer to a string in unmanaged memory.</returns>
    public static byte* StringToBytePointerUTF8(string source)
    {
        var sourceBytes = Encoding.UTF8.GetBytes(source);
        var result = (byte*)ffmpeg.av_mallocz((ulong)sourceBytes.Length + 1);
        Marshal.Copy(sourceBytes, 0, new IntPtr(result), sourceBytes.Length);
        return result;
    }

    /// <summary>
    /// Converts a byte pointer to a UTF8 encoded string.
    /// </summary>
    /// <param name="stringAddress">The pointer to the starting character.</param>
    /// <returns>The string.</returns>
    public static unsafe string PtrToStringUTF8(byte* stringAddress)
    {
        if (stringAddress == null) return null;
        if (*stringAddress == 0) return string.Empty;

        var byteLength = 0;
        while (true)
        {
            if (stringAddress[byteLength] == 0)
                break;

            byteLength++;
        }

        var stringBuffer = stackalloc byte[byteLength];
        Buffer.MemoryCopy(stringAddress, stringBuffer, byteLength, byteLength);
        return Encoding.UTF8.GetString(stringBuffer, byteLength);
    }

    /// <summary>
    /// Gets the FFmpeg error message based on the error code.
    /// </summary>
    /// <param name="errorCode">The code.</param>
    /// <returns>The decoded error message.</returns>
    public static unsafe string DecodeMessage(int errorCode)
    {
        var bufferSize = 1024;
        var buffer = stackalloc byte[bufferSize];
        ffmpeg.av_strerror(errorCode, buffer, (ulong)bufferSize);
        var message = PtrToStringUTF8(buffer);
        return message;
    }

    /// <summary>
    /// Retrieves the codecs.
    /// </summary>
    /// <returns>The codecs.</returns>
    public static unsafe AVCodec*[] RetrieveCodecs()
    {
        var result = new List<IntPtr>(1024);
        void* iterator;
        AVCodec* item;
        while ((item = ffmpeg.av_codec_iterate(&iterator)) != null)
        {
            result.Add((IntPtr)item);
        }

        var collection = new AVCodec*[result.Count];
        for (var i = 0; i < result.Count; i++)
        {
            collection[i] = (AVCodec*)result[i];
        }

        return collection;
    }

    /// <summary>
    /// Retrieves the input format names.
    /// </summary>
    /// <returns>The collection of names.</returns>
    public static unsafe List<string> RetrieveInputFormatNames()
    {
        var result = new List<string>(128);
        void* iterator;
        AVInputFormat* item;
        while ((item = ffmpeg.av_demuxer_iterate(&iterator)) != null)
        {
            result.Add(PtrToStringUTF8(item->name));
        }

        return result;
    }

    /// <summary>
    /// Retrieves the decoder names.
    /// </summary>
    /// <param name="allCodecs">All codecs.</param>
    /// <returns>The collection of names.</returns>
    public static unsafe List<string> RetrieveDecoderNames(AVCodec*[] allCodecs)
    {
        var codecNames = new List<string>(allCodecs.Length);
        foreach (var c in allCodecs)
        {
            if (ffmpeg.av_codec_is_decoder(c) != 0)
                codecNames.Add(PtrToStringUTF8(c->name));
        }

        return codecNames;
    }

    /// <summary>
    /// Retrieves the encoder names.
    /// </summary>
    /// <param name="allCodecs">All codecs.</param>
    /// <returns>The collection of names.</returns>
    public static unsafe List<string> RetrieveEncoderNames(AVCodec*[] allCodecs)
    {
        var codecNames = new List<string>(allCodecs.Length);
        foreach (var c in allCodecs)
        {
            if (ffmpeg.av_codec_is_encoder(c) != 0)
                codecNames.Add(PtrToStringUTF8(c->name));
        }

        return codecNames;
    }

    #endregion

    #region util methods

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void FreeFrame(ref AVFrame* frame)
    {
        if (frame != null)
        {
            fixed (AVFrame** p = &frame)
            {
                ffmpeg.av_frame_free(p);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void FreeCodecContext(ref AVCodecContext* ctx)
    {
        if (ctx != null)
        {
            fixed (AVCodecContext** p = &ctx)
            {
                ffmpeg.avcodec_free_context(p);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void FreeFormatContext(ref AVFormatContext* ctx)
    {
        if (ctx != null)
        {
            fixed (AVFormatContext** p = &ctx)
            {
                ffmpeg.avformat_free_context(ctx);
            }
            ctx = null;
        }
    }

    #endregion
}
