﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using Encoder = System.Drawing.Imaging.Encoder;
using Microsoft.Win32;

namespace ImageOptimize
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void ListBox_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Link;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }


        public class Fileslist
        {
            public static List<string> SrcPath = new List<string>();//源路径
            public static List<string> SrcName = new List<string>();//源文件名
            public static List<long> Size = new List<long>();//文件大小
            public static List<int> Width = new List<int>();//宽度
            public static List<int> Height = new List<int>();//高度
            public static List<string> NewPath = new List<string>();//新路径
            public static List<int> STAT = new List<int>();//0不调整,1待调整,2已调整
            public static List<string> Quality = new List<string>();//最终jpg质量
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void ListBox_Drop(object sender, DragEventArgs e)
        {
            string[] DragFileNames;
            DragFileNames = e.Data.GetData(DataFormats.FileDrop, false) as String[];
            string[] supportExt = new string[] { ".jpg", ".jpeg", ".png", ".bmp" };
            System.Drawing.Image imgFile;
            foreach (string DragFileName in DragFileNames)
            {
                //仅允许文件类型相符且文件路径无重复
                if (supportExt.Contains(System.IO.Path.GetExtension(DragFileName).ToLower()) && (!(Fileslist.SrcPath.Contains(DragFileName))))
                {
                    Fileslist.SrcPath.Add(DragFileName);//写入源路径
                    Fileslist.SrcName.Add(System.IO.Path.GetFileName(DragFileName));//写入源文件名
                    Fileslist.NewPath.Add(System.IO.Path.GetDirectoryName(Fileslist.SrcPath.Last())+"\\"+ PrefixFileName.Text + Fileslist.SrcName.Last());//写入新路径+前缀
                    Fileslist.Size.Add(new System.IO.FileInfo(DragFileName).Length / 1024);
                    //文件体积小于限定的，读取宽高像素；大体积直接略过，写入0
                    if (Fileslist.Size.Last() <= Convert.ToInt32(targetSize.Text))
                    {
                        imgFile = new Bitmap(DragFileName);
                        Fileslist.Width.Add(imgFile.Width);
                        Fileslist.Height.Add(imgFile.Height);
                        imgFile.Dispose();
                    }
                    else
                    {
                        Fileslist.Width.Add(0);
                        Fileslist.Height.Add(0);
                    }
                    //体积或宽度超限，标记1 待调整
                    if (Fileslist.Size.Last() > Convert.ToInt32(targetSize.Text) || Fileslist.Width.Last() > Convert.ToInt32(targetWidth.Text))
                    {
                        Fileslist.STAT.Add(1);
                    }
                    else
                    {
                        Fileslist.STAT.Add(0);
                    }
                    Fileslist.Quality.Add("");
                }
            }
            ListRefresh();
            
            //ProgressBar01.Value = ProgressBar01.Maximum;
        }

        private void Button_Copy_Click(object sender, RoutedEventArgs e)
        {
            ListDel();
            listBox.Items.Clear();
        }

        private void ListDel()
        {
            Fileslist.SrcPath.Clear();
            Fileslist.SrcName.Clear();
            Fileslist.Size.Clear();
            Fileslist.Width.Clear();
            Fileslist.Height.Clear();
            Fileslist.NewPath.Clear();
            Fileslist.STAT.Clear();
            Fileslist.Quality.Clear();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            for (var i = 0; i < Fileslist.SrcName.Count; i++)
            {
                if (Fileslist.STAT[i] == 1)
                {
                    Fileslist.Quality[i] = ReSize(Fileslist.SrcPath[i], Fileslist.NewPath[i], Convert.ToInt32(targetSize.Text), Convert.ToInt32(targetWidth.Text), Convert.ToInt32(imgQuality.Text), checkBox.IsChecked.Value);
                    Fileslist.STAT[i] = 2;
                }
                ListRefresh();
                SaveReg();
            }
        }
        private string ReSize(string imgSrcPath,string imgNewPath,long tgtSize,int tgtWidth,long jpgQuality, bool autodown)
        {
            System.Drawing.Bitmap srcFile = new Bitmap(imgSrcPath);
            int imgWidth = tgtWidth;
            int imgHeight = srcFile.Height * imgWidth / srcFile.Width;
            System.Drawing.Bitmap newFile = new Bitmap(imgWidth, imgHeight);
            Graphics g = Graphics.FromImage(newFile);
            g.DrawImage(srcFile, 0, 0, imgWidth, imgHeight);
            srcFile.Dispose();

            ImageCodecInfo myImageCodecInfo;
            Encoder myEncoder;
            EncoderParameter myEncoderParameter;
            EncoderParameters myEncoderParameters;
            myImageCodecInfo = GetEncoderInfo("image/jpeg");
            myEncoder = Encoder.Quality;
            myEncoderParameters = new EncoderParameters(1);
            //long newFileSize;
            do
            {
                myEncoderParameter = new EncoderParameter(myEncoder, jpgQuality);
                myEncoderParameters.Param[0] = myEncoderParameter;

                newFile.Save(imgNewPath, myImageCodecInfo, myEncoderParameters);
                jpgQuality -= 5;
                
            }while (autodown && (new System.IO.FileInfo(imgNewPath).Length / 1024) > tgtSize);
            
            
            newFile.Dispose();
            return (jpgQuality + 5).ToString();
        }
        private static ImageCodecInfo GetEncoderInfo(String mimeType)
        {
            int j;
            ImageCodecInfo[] encoders;
            encoders = ImageCodecInfo.GetImageEncoders();
            for (j = 0; j < encoders.Length; ++j)
            {
                if (encoders[j].MimeType == mimeType)
                    return encoders[j];
            }
            return null;
        }
        private void ListRefresh()
        {
            listBox.Items.Clear();
            string itemText;
            for (var i = 0; i < Fileslist.SrcName.Count; i++)
            {
                itemText = Fileslist.SrcName[i] + "......" + Fileslist.Size[i].ToString() + "KB" + "......" + Fileslist.Width[i] + " px";
                if (Fileslist.STAT[i] == 0)
                {
                    itemText += "......无需调整";
                }
                if (Fileslist.STAT[i] == 1)
                {
                    itemText += "......待调整";
                }
                if (Fileslist.STAT[i] == 2)
                {
                    itemText += "......已调整......jpg质量";
                    itemText += Fileslist.Quality[i];
                }
                listBox.Items.Add(itemText);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Registry.CurrentUser.CreateSubKey("Software\\ImageOptimize");
            RegistryKey rsg = Registry.CurrentUser.OpenSubKey("Software\\ImageOptimize", true);
            if (rsg.GetValue("targetSize") != null)
            {
                targetSize.Text = rsg.GetValue("targetSize").ToString();
                targetWidth.Text = rsg.GetValue("targetWidth").ToString();
                imgQuality.Text = rsg.GetValue("imgQuality").ToString();
                PrefixFileName.Text = rsg.GetValue("PrefixFileName").ToString();
                checkBox.IsChecked = Convert.ToBoolean(rsg.GetValue("checkBox"));
                rsg.Dispose();
            }
            else
            {
                rsg.Dispose();
                SaveReg();
            }
            
        }
        private void SaveReg()
        {
            RegistryKey rsg = Registry.CurrentUser.OpenSubKey("Software\\ImageOptimize", true);
            rsg.SetValue("targetSize", targetSize.Text);
            rsg.SetValue("targetWidth", targetWidth.Text);
            rsg.SetValue("imgQuality", imgQuality.Text);
            rsg.SetValue("PrefixFileName", PrefixFileName.Text);
            rsg.SetValue("checkBox", checkBox.IsChecked);
            rsg.Dispose();
        }
    }
}
