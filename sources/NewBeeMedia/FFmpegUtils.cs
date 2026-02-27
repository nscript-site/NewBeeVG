namespace NewBeeMedia;

using FFmpeg.AutoGen;

public enum FFmpegLoadMode
{
    FullFeatures, MinimumFeatures, AudioOnly, VideoOnly
}

public unsafe class FFmpegUtils
{
    /// <summary>
    /// Gets the individual library flag identifiers.
    /// </summary>
    public static IReadOnlyDictionary<string, int> LibraryFlags { get; } = FFLibrary.All.ToDictionary(k => k.Name, v => v.FlagId);

    /// <summary>
    /// The full features. Tries to load everything.
    /// </summary>
    public static int FullFeatures { get; } =
        FFLibrary.LibAVCodec.FlagId |
        FFLibrary.LibAVDevice.FlagId |
        FFLibrary.LibPostProc.FlagId |
        FFLibrary.LibAVFilter.FlagId |
        FFLibrary.LibAVFormat.FlagId |
        FFLibrary.LibAVUtil.FlagId |
        FFLibrary.LibSWResample.FlagId |
        FFLibrary.LibSWScale.FlagId;

    /// <summary>
    /// Loads everything except for AVDevice and AVFilter.
    /// </summary>
    public static int MinimumFeatures { get; } =
        FFLibrary.LibAVCodec.FlagId |
        FFLibrary.LibAVFormat.FlagId |
        FFLibrary.LibAVUtil.FlagId |
        FFLibrary.LibSWResample.FlagId |
        FFLibrary.LibSWScale.FlagId;

    /// <summary>
    /// Loads the minimum set for Audio-only programs.
    /// </summary>
    public static int AudioOnly { get; } =
        FFLibrary.LibAVCodec.FlagId |
        FFLibrary.LibAVFormat.FlagId |
        FFLibrary.LibAVUtil.FlagId |
        FFLibrary.LibSWResample.FlagId;

    /// <summary>
    /// Loads the minimum set for Video-only programs.
    /// </summary>
    public static int VideoOnly { get; } =
        FFLibrary.LibAVCodec.FlagId |
        FFLibrary.LibAVFormat.FlagId |
        FFLibrary.LibAVUtil.FlagId |
        FFLibrary.LibSWScale.FlagId;

    /// <summary>
    /// 查找 ffmpeg 库所在目录
    /// </summary>
    /// <returns></returns>
    public static String ScanFFmpegLibraryPath()
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

    private static String ScanFFmpegLibraryPath(String[] dirs)
    {
        foreach (var item in dirs)
        {
            String path = TryFindFFmpegLibraryPath(item);
            if (path != null) return path;
        }
        return null;
    }

    private static String TryFindFFmpegLibraryPath(String dir)
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
    //private static readonly List<OptionMetadata> EmptyOptionMetaList = new List<OptionMetadata>(0);
    //private static readonly av_log_set_callback_callback FFmpegLogCallback = OnFFmpegMessageLogged;
    //private static readonly ILoggingHandler LoggingHandler = new FFLoggingHandler();
    private static bool m_IsInitialized;
    private static string m_LibrariesPath = string.Empty;
    private static int m_LibraryIdentifiers;

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

    /// <summary>
    /// Gets the bitwise FFmpeg library identifiers that were loaded.
    /// </summary>
    public static int LibraryIdentifiers
    {
        get { lock (SyncLock) { return m_LibraryIdentifiers; } }
    }

    #endregion

    #region FFmpeg Registration

    public static bool Initialize(FFmpegLoadMode mode = FFmpegLoadMode.FullFeatures)
    {
        String path = ScanFFmpegLibraryPath();
        if (path == null)
            throw new Exception("Can not find ffmpeg library.");

        int id = FullFeatures;
        switch(mode)
        {
            case FFmpegLoadMode.VideoOnly:
                id = VideoOnly;
                break;
            case FFmpegLoadMode.AudioOnly:
                id = VideoOnly;
                break;
            case FFmpegLoadMode.MinimumFeatures:
                id = MinimumFeatures;
                break;
            case FFmpegLoadMode.FullFeatures:
            default:
                id = FullFeatures;
                break;
        }

        return Initialize(path, id);
    }

    /// <summary>
    /// Registers FFmpeg library and initializes its components.
    /// It only needs to be called once but calling it more than once
    /// has no effect. Returns the path that FFmpeg was registered from.
    /// This method is thread-safe.
    /// </summary>
    /// <param name="overridePath">The override path.</param>
    /// <param name="libIdentifiers">The bit-wise flag identifiers corresponding to the libraries.</param>
    /// <returns>
    /// Returns true if it was a new initialization and it succeeded. False if there was no need to initialize
    /// as there is already a valid initialization.
    /// </returns>
    /// <exception cref="FileNotFoundException">When ffmpeg libraries are not found.</exception>
    private static bool Initialize(string overridePath, int libIdentifiers)
    {
        lock (SyncLock)
        {
            if (m_IsInitialized)
                return false;

            try
            {
                // Get the temporary path where FFmpeg binaries are located
                var ffmpegPath = Path.GetFullPath(overridePath);

                var registrationIds = 0;

                // Load FFmpeg binaries by Library ID
                foreach (var lib in FFLibrary.All)
                {
                    var loadResult = lib.Load(ffmpegPath);
                    if (loadResult == false || lib.IsLoaded == false) Console.WriteLine($"ffmpeg lib load failed: {lib.Name}.{lib.Version}");
                    if ((lib.FlagId & libIdentifiers) != 0 && loadResult)
                        registrationIds |= lib.FlagId;
                }

                // Check if libraries were loaded correctly
                if (FFLibrary.All.All(lib => lib.IsLoaded == false))
                    throw new FileNotFoundException($"Unable to load FFmpeg binaries from folder '{ffmpegPath}'.");

                // Additional library initialization
                if (FFLibrary.LibAVDevice.IsLoaded)
                    ffmpeg.avdevice_register_all();

                // Set logging levels and callbacks
                ffmpeg.av_log_set_flags(ffmpeg.AV_LOG_SKIP_REPEATED);
                //ffmpeg.av_log_set_level(Library.FFmpegLogLevel);
                //ffmpeg.av_log_set_callback(FFmpegLogCallback);

                // set the static environment properties
                m_LibrariesPath = ffmpegPath;
                m_LibraryIdentifiers = registrationIds;
                m_IsInitialized = true;
            }
            catch
            {
                m_LibrariesPath = string.Empty;
                m_LibraryIdentifiers = 0;
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
    /// Retrieves the options information associated with the given AVClass.
    /// </summary>
    /// <param name="avClass">The av class.</param>
    /// <returns>A list of option metadata.</returns>
    //public static unsafe List<OptionMetadata> RetrieveOptions(AVClass* avClass)
    //{
    //    // see: https://github.com/FFmpeg/FFmpeg/blob/e0f32286861ddf7666ba92297686fa216d65968e/tools/enum_options.c
    //    var result = new List<OptionMetadata>(128);
    //    if (avClass == null) return result;

    //    var option = avClass->option;

    //    while (option != null)
    //    {
    //        if (option->type != AVOptionType.AV_OPT_TYPE_CONST)
    //            result.Add(new OptionMetadata(option));

    //        option = ffmpeg.av_opt_next(avClass, option);
    //    }

    //    return result;
    //}

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

    /// <summary>
    /// Retrieves the global format options.
    /// </summary>
    /// <returns>The collection of option infos.</returns>
    //public static unsafe List<OptionMetadata> RetrieveGlobalFormatOptions() =>
    //    RetrieveOptions(ffmpeg.avformat_get_class());

    /// <summary>
    /// Retrieves the global codec options.
    /// </summary>
    /// <returns>The collection of option infos.</returns>
    //public static unsafe List<OptionMetadata> RetrieveGlobalCodecOptions() =>
    //    RetrieveOptions(ffmpeg.avcodec_get_class());

    /// <summary>
    /// Retrieves the input format options.
    /// </summary>
    /// <param name="formatName">Name of the format.</param>
    /// <returns>The collection of option infos.</returns>
    //public static unsafe List<OptionMetadata> RetrieveInputFormatOptions(string formatName)
    //{
    //    var item = ffmpeg.av_find_input_format(formatName);
    //    return item == null ? EmptyOptionMetaList : RetrieveOptions(item->priv_class);
    //}

    /// <summary>
    /// Retrieves the codec options.
    /// </summary>
    /// <param name="codec">The codec.</param>
    /// <returns>The collection of option infos.</returns>
    //public static unsafe List<OptionMetadata> RetrieveCodecOptions(AVCodec* codec) =>
    //    RetrieveOptions(codec->priv_class);

    /// <summary>
    /// Log message callback from ffmpeg library.
    /// </summary>
    /// <param name="p0">The p0.</param>
    /// <param name="level">The level.</param>
    /// <param name="format">The format.</param>
    /// <param name="vl">The vl.</param>
    //private static unsafe void OnFFmpegMessageLogged(void* p0, int level, string format, byte* vl)
    //{
    //    const int lineSize = 1024;
    //    lock (FFmpegLogBufferSyncLock)
    //    {
    //        if (level > ffmpeg.av_log_get_level()) return;
    //        var lineBuffer = stackalloc byte[lineSize];
    //        var printPrefix = 1;
    //        ffmpeg.av_log_format_line(p0, level, format, vl, lineBuffer, lineSize, &printPrefix);
    //        var line = Utilities.PtrToStringUTF8(lineBuffer);
    //        FFmpegLogBuffer.Add(line);

    //        var messageType = MediaLogMessageType.Debug;
    //        if (FFmpegLogLevels.ContainsKey(level))
    //            messageType = FFmpegLogLevels[level];

    //        if (!line.EndsWith("\n", StringComparison.Ordinal)) return;
    //        line = string.Join(string.Empty, FFmpegLogBuffer);
    //        line = line.TrimEnd();
    //        FFmpegLogBuffer.Clear();
    //        Logging.Log(LoggingHandler, messageType, Aspects.FFmpegLog, line);
    //    }
    //}

    #endregion

    #region Supporting Classes

    /// <summary>
    /// Handles FFmpeg library messages.
    /// </summary>
    /// <seealso cref="ILoggingHandler" />
    //internal class FFLoggingHandler : ILoggingHandler
    //{
    //    /// <inheritdoc />
    //    void ILoggingHandler.HandleLogMessage(LoggingMessage message) =>
    //        MediaEngine.RaiseFFmpegMessageLogged(message);
    //}

    #endregion
}
