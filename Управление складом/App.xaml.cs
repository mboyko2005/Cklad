using System.Configuration;
using System.Data;
using System.Windows;

namespace Управление_складом
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	/// </summary>
	/// Для сжатие в единый exe файл
	/// dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeAllContentForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:UseAppHost=true -p:PublishTrimmed=false -o publish
	///<summary>
	public partial class App : Application
	{
	}

}
