﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Forms;
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

        private void ListBox_DragEnter(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                e.Effects = System.Windows.DragDropEffects.Link;
            }
            else
            {
                e.Effects = System.Windows.DragDropEffects.None;
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
            public static List<int> STAT = new List<int>();//0不调整,1待调整,2已调整,3待扫描
            public static List<string> Quality = new List<string>();//最终jpg质量
        }

        private int FilesListLoad(string[] DragFileNames)
        {
            string[] supportExt = new string[] { ".jpg", ".jpeg", ".png", ".bmp" };
            int fileCount = 0;
            foreach (string DragFileName in DragFileNames)
            {
                //判断是否文件夹
                if (System.IO.Directory.Exists(DragFileName))
                {
                    //文件夹
                    if ((bool)subFolder_checkBox.IsChecked)
                    {
                        fileCount += FilesListLoad(Directory.GetFileSystemEntries(DragFileName));
                    }
                    else
                    {
                        fileCount += FilesListLoad(Directory.GetFiles(DragFileName));
                    }
                    
                }
                //仅允许文件类型相符且文件路径无重复
                if (supportExt.Contains(System.IO.Path.GetExtension(DragFileName).ToLower()) && (!Fileslist.SrcPath.Contains(DragFileName)))
                {
                    fileCount++;
                    Fileslist.SrcPath.Add(DragFileName);//写入源路径
                    Fileslist.SrcName.Add(System.IO.Path.GetFileName(DragFileName));//写入源文件名
                    Fileslist.NewPath.Add(System.IO.Path.GetDirectoryName(Fileslist.SrcPath.Last()) + "\\" + PrefixFileName.Text + Fileslist.SrcName.Last());//写入源路径+前缀，同位置保存
                    Fileslist.Size.Add(0);
                    Fileslist.Width.Add(0);
                    Fileslist.Height.Add(0);
                    Fileslist.STAT.Add(3);
                    Fileslist.Quality.Add("");
                }
            }
            return fileCount;
        }
        private void ListBox_Drop(object sender, System.Windows.DragEventArgs e)
        {
            string[] tempDragFileNames = e.Data.GetData(System.Windows.DataFormats.FileDrop, false) as string[];
            int fileCount= FilesListLoad(tempDragFileNames);
            labelfileCount.Content = "拖拽导入 " + fileCount.ToString() + " 个文件，合计 " + Fileslist.SrcName.Count.ToString();
            FileInfoRefresh();
            ListRefresh();
            
            //ProgressBar01.Value = ProgressBar01.Maximum;
        }

        private void Button_Copy_Click(object sender, RoutedEventArgs e)
        {
            ListDel();
            listBox.Items.Clear();
            buttonStart.IsEnabled = false;
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
            //读取参数
            long tgtSz = Convert.ToInt32(targetSize.Text);
            int tgtWd = Convert.ToInt32(targetWidth.Text);
            long jpgq = Convert.ToInt32(imgQuality.Text);
            bool cb = checkBox.IsChecked.Value;
            
            //如果选择使用新文件夹
            if (radioButton1.IsChecked==true)
            {
                //检查文件名是否有重复，避免不同文件夹内有同名文件
                string tempFileName = null;
                for(int i = 0; i<Fileslist.SrcName.Count; i++)
                {
                    tempFileName = Fileslist.SrcName[i];
                    Fileslist.SrcName[i] = "";
                    if (Fileslist.SrcName.Contains(tempFileName))
                    {
                        tempFileName = System.IO.Path.GetFileNameWithoutExtension(tempFileName) + "_"+i.ToString() + System.IO.Path.GetExtension(Fileslist.SrcPath[i]);
                    }
                    Fileslist.SrcName[i] = tempFileName;
                }


                string NewFold = newfoldBox.Text;
                string preFileName = PrefixFileName.Text;
                Parallel.For(0, Fileslist.SrcName.Count, i => Fileslist.NewPath[i] = NewFold + "\\" + preFileName + Fileslist.SrcName[i]);
            }
            Parallel.For(0, Fileslist.SrcName.Count, i =>
            {
                //调整标记为1的项
                if (Fileslist.STAT[i] == 1)
                {
                    Fileslist.Quality[i] = ReSize(Fileslist.SrcPath[i], Fileslist.NewPath[i], tgtSz, tgtWd, jpgq, cb);
                    Fileslist.STAT[i] = 2;
                }
            });
            ListRefresh();
            SaveReg();
        }
        private string ReSize(string imgSrcPath,
                              string imgNewPath,
                              long tgtSize,
                              int tgtWidth,
                              long jpgQuality,
                              bool autodown)
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
            do
            {
                myEncoderParameter = new EncoderParameter(myEncoder, jpgQuality);
                myEncoderParameters.Param[0] = myEncoderParameter;

                newFile.Save(imgNewPath, myImageCodecInfo, myEncoderParameters);
                jpgQuality -= 5;
                
            }while (autodown && jpgQuality>=5 && (new System.IO.FileInfo(imgNewPath).Length / 1024) > tgtSize);
            
            
            newFile.Dispose();
            myEncoderParameters.Dispose();
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
        private void FileInfoRefresh()
        {
            long maxFileSize,maxWidth;
            maxFileSize = Convert.ToInt32(targetSize.Text);
            maxWidth = Convert.ToInt32(targetWidth.Text);
            Parallel.For(0, Fileslist.SrcName.Count, i =>
            {
                if (Fileslist.STAT[i]==3)
                {
                    Fileslist.Size[i] = new FileInfo(Fileslist.SrcPath[i]).Length / 1024;
                    System.Drawing.Image imgFile = new Bitmap(Fileslist.SrcPath[i]);
                    Fileslist.Width[i] = imgFile.Width;
                    Fileslist.Height[i] = imgFile.Height;
                    imgFile.Dispose();
                    if (Fileslist.Size[i]>maxFileSize || Fileslist.Width[i]>maxWidth)
                    {
                        Fileslist.STAT[i] = 1;
                    }
                    else
                    {
                        Fileslist.STAT[i] = 0;
                    }
                }
            });
        }
        private void ListRefresh()
        {
            listBox.Items.Clear();
            string itemText;
            for (int i = 0; i < Fileslist.SrcName.Count; i++)
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
            if (Fileslist.STAT.Contains(1))
            {
                buttonStart.IsEnabled = true;
                buttonCalc.IsEnabled = true;
            }
            else
            {
                buttonStart.IsEnabled = false;
                buttonCalc.IsEnabled = false;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Registry.CurrentUser.CreateSubKey("Software\\ImageOptimize");
            RegistryKey rsg = Registry.CurrentUser.OpenSubKey("Software\\ImageOptimize", true);
            if (rsg.GetValue("targetSize") != null && 
                rsg.GetValue("targetWidth") != null && 
                rsg.GetValue("imgQuality") != null && 
                rsg.GetValue("PrefixFileName") != null && 
                rsg.GetValue("checkBox") != null)
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
            //检查剪贴板内是否文件路径
            if (System.Windows.Clipboard.ContainsFileDropList())
            {
                buttonPaste.IsEnabled = true;
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

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveReg();
        }

        private void RadioButton1_Checked(object sender, RoutedEventArgs e)
        {
            newfoldBox.IsEnabled = true;
            buttonFold.IsEnabled = true;
            
            
        }

        private void ButtonFold_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog
            {
                Description = "请选择文件路径"
            };
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                newfoldBox.Text = dialog.SelectedPath;
            }
            dialog.Dispose();
        }


        private void RadioButton_Click(object sender, RoutedEventArgs e)
        {
            newfoldBox.IsEnabled = false;
            buttonFold.IsEnabled = false;
        }

        private void ButtonPaste_Click(object sender, RoutedEventArgs e)
        {
            if (System.Windows.Clipboard.ContainsFileDropList()){
                string[] tempDragFileNames=new string[System.Windows.Clipboard.GetFileDropList().Count];
                System.Windows.Clipboard.GetFileDropList().CopyTo(tempDragFileNames,0);
                int fileCount = FilesListLoad(tempDragFileNames);
                labelfileCount.Content = "剪贴板导入 " + fileCount.ToString() + " 个文件，合计 "+Fileslist.SrcName.Count.ToString();
                FileInfoRefresh();
                ListRefresh();
            }
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            //检查剪贴板内是否文件路径
            if (System.Windows.Clipboard.ContainsFileDropList())
            {
                buttonPaste.IsEnabled = true;
            }
        }

        private void ButtonAdd_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog
            {
                Multiselect = true,
                CheckPathExists = true,
                CheckFileExists = true,
                Filter = "Image Files(*.jpg;*.jpeg;*.bmp;*.png)|*.BMP;*.JPG;*.JPEG,*.PNG"
            };
            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string[] tempDragFileNames=openFileDialog.FileNames;
                int fileCount= FilesListLoad(tempDragFileNames);
                labelfileCount.Content = "添加 " + fileCount.ToString() + " 个文件，合计 " + Fileslist.SrcName.Count.ToString();
                FileInfoRefresh();
                ListRefresh();
            }
            openFileDialog.Dispose();
        }

        private void buttonCalc_Click(object sender, RoutedEventArgs e)
        {
            targetSize.Text = (Convert.ToInt32(totalSize.Text)/Fileslist.SrcName.Count).ToString();
        }
    }
}
