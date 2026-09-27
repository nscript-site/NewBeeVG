#!/usr/bin/env dotnet

 var content = """
     2025年，中国对外直接投资流量2135.8亿美元，比上年增长11.1%，对外投资存量3.4万亿美元，连续9年保持全球前三，占全球投资的比重增加到7.4%。对外投资行业覆盖国民经济的18个行业门类，主要集中在租赁和商务服务、批发零售、制造和金融四个领域，近年来逐步向绿色低碳、数字经济和绿色矿产等领域稳步拓展。

     截至2025年末，我国在境外设立企业5.8万家，遍布189个国家和地区，境外企业从业员工总数476.1万人，其中雇用外方员工296.5万人。
     """;

VStack([
    TextBlock("TypstContent 直接嵌入内容").Margin(10),
    TypstContent(content).PageMargin(10).PageSize(500,null)
        .PageBg(SKColors.White).ParagraphJustify()
        .Align(0,-1).Margin(20),
    TypstContent(content).PageMargin(10).PageSize(500,null)
        .Align(0,-1).Margin(20)
]).Margin(100)
.AsClip(out var clip, 30, name: "typstcontent");

run(stage(1920, 1080, bg: SKColors.Orange), [clip]);