# 项目目录

## 目录树

```
<project_root>
├── Directory.Build.props        # 项目级配置（SkiaSharp、NewBeeVG 包版本）
├── README.md                    # 本文件
│
├── modules/                     # 此处放置公共特效模板库
│   ├── README.md                # 逐个模板的说明（参数 / 示例）
│   └── GenRun.cs                # 扫描 modules 目录下的 *Test.cs 文件，生成批量测试脚本
│
├── assets/                      # 跨工程公共素材，如果素材多，请创建目录管理
│
├── scripts/                     # 视频工程，一个子目录 = 一部片
│   └── demo/                    # 示例项目
│       ├── README.md            # 示例项目的说明
│       ├── script.md            # 视频剧本
│       ├── storyboard.md        # 分镜脚本
│       ├── script.cs            # 视频脚本
│       ├── modules/             # 视频工程自用的特效模板
│       ├── assets/              # 项目所需非 AI 生成的素材放在这里，如果素材多，请创建目录管理
│       ├── gen/                 # AI 生成的产物放在这里
│       │   ├── images/          # AI 生成的图像与 prompts 文件
│       │   ├── apngs/           # AI 生成的 APNG 动画（后缀 .png）
│       │   ├── audios/          # AI 生成的声音文件
│       │   ├── videos/          # AI 生成的视频文件
│       │   ├── typsts/          # AI 生成的 typst 动画
│       │   └── svgs/            # AI 生成的 svg 文件
│       ├── tools/               # png_frames_to_apng.py（逐帧 PNG → 全帧 APNG）
│       └── output/              # 渲染产物（mp4 / 校对帧）
│
└── todo/                        # 待办事项放在这里
```

## 创作流程

（1）创建视频脚本 script.md，可以人工创作，也可以用 AI 创作，也可以 AI 创作后人工修改
（2）根据脚本创作分镜脚本 storyboard.md，建议以 AI 为主来创作，人工再修改审核，这一步通常也会合成音频
（3）根据分镜脚本或视频脚本，创作所需的素材以及可能用到的模板
（4）根据上述内容，生成 script.cs
（5）预览、调试修改 script.cs 直到满意
（6）导出视频