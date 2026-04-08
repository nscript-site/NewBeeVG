using System;
using System.Collections.Generic;
using System.Text;

namespace NewBeeVG.Demo.Samples;

internal class TTSSample
{
    public void Run()
    {
        var clip6 = ttsClip("请求成功，正常返回结果", voice: "Kai", instructions: "语速较快");
        clip6.Prepare();
    }
}
