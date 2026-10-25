# NCMDumperGUI  
一个使用Avalonia为ncmdump编写的GUI程序  
仅支持Windows x86_64   
解密后的.flac或.mp3会输出到与.ncm相同的位置   
> 程序依赖.NET Runtime运行，请确保目标机器已经安装了.NET 10 SDK或.NET 10 Desktop Runtime   

> 本地构建时可通过修改项目.csproj文件中`SelfContained`的值为`true`启用自包含将Runtime打包进程序内以脱离Runtime运行

## 使用  
### 本地构建
先决条件：需要安装[.NET SDK](https://dotnet.microsoft.com/download)(.NET 10或以上版本)  
> 请使用.NET SDK而不是Runtime，Runtime无法用于构建
   
   
1.下载仓库或使用git clone本仓库并进入到项目目录
~~~
git clone https://github.com/N4RNACACC/NCMDumper-GUI
cd NCMDumper-GUI
~~~   
2.还原Nuget依赖
~~~
dotnet restore
~~~
3.构建项目
~~~
dotnet build -c Release
~~~

  
## 引用  
本项目引用了来自[taurusxin/ncmdump](https://github.com/taurusxin/ncmdump)的示例代码及编译的DLL文件和其他第三方库  
详见THIRD_PARTY_LICENSES.txt

## LICENSE  
此项目使用MIT协议开源，详见LICENSE.txt