namespace NewBeeMedia.Subtitles;

/// <summary>
/// 字幕的类型
/// </summary>
public enum ContentFlag
{
    LANG_ZH = 0,        // 简体中文,0
    LANG_EN = 1,        // 英文,1
    LANG_ZH_TRA = 2,  // 繁体中文,2
    LANG_JA = 3,        // 日语,3
    LANG_KO = 4,        // 韩语,4
    LANG_RU = 5,        // 俄语,5
    LANG_AR = 6,        // 阿拉伯语,6
    LANG_THAi = 101,      // 泰语,101
    TEXT = 1001,    // 文本
    TRANSLATE = 1002, // 译文
    None = 9099, 
    Tag0 = 10000,
    Tag1 = 10001,
    Tag2 = 10002,
    Tag3 = 10003
}
