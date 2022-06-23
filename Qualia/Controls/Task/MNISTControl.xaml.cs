﻿using Microsoft.Win32;
using Qualia.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Qualia.Controls
{
    sealed public partial class MNISTControl : BaseUserControl
    {
        public readonly List<MNISTImage> Images = new();

        private event Action OnChange = delegate { };

        public MNISTControl()
        {
            InitializeComponent();
        }

        public int MaxNumber => (int)CtlTask_MNIST_MaxNumber.Value;
        public int MinNumber => (int)CtlTask_MNIST_MinNumber.Value;

        private void Parameter_OnChanged()
        {
            if (IsValid())
            {
                OnChange();
            }
        }

        public void SetConfig(Config config)
        {
            Range.ForEach(this.FindVisualChildren<IConfigParam>(), param => param.SetConfig(config));
        }

        public void LoadConfig()
        {
            Range.ForEach(this.FindVisualChildren<IConfigParam>(), param => param.LoadConfig());

            var fileNameImagesBin = Extension.GetDirectoryName(CtlTask_MNIST_ImagesPath.Text,
                                                               App.WorkingDirectory + "MNIST") + Path.DirectorySeparatorChar + "images.bin";
            
            if (!File.Exists(fileNameImagesBin))
            {
                if (!File.Exists(CtlTask_MNIST_ImagesPath.Text))
                {
                    CtlTask_MNIST_ImagesPath.Text = App.WorkingDirectory + "MNIST" + Path.DirectorySeparatorChar + "train-images-idx3-ubyte.gz";
                }

                fileNameImagesBin = Extension.GetDirectoryName(CtlTask_MNIST_ImagesPath.Text,
                                                               App.WorkingDirectory + "MNIST") + Path.DirectorySeparatorChar + "images.bin";

                if (!File.Exists(fileNameImagesBin))
                {
                    fileNameImagesBin = App.WorkingDirectory + "MNIST" + Path.DirectorySeparatorChar + "images.bin";
                    if (!File.Exists(fileNameImagesBin))
                    {
                        try
                        {
                            if (!File.Exists(CtlTask_MNIST_ImagesPath.Text))
                            {
                                throw new Exception($"Cannot find file '{CtlTask_MNIST_ImagesPath.Text}'.");
                            }

                            Decompress(CtlTask_MNIST_ImagesPath.Text, fileNameImagesBin);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Cannot open MNIST images file.\r\n\r\n" + ex.Message);
                            return;
                        }
                    }
                }
            }

            LoadImages(fileNameImagesBin);
            var fileNameImagesGz = Extension.GetDirectoryName(fileNameImagesBin,
                                                              App.WorkingDirectory + "MNIST") + Path.DirectorySeparatorChar + "train-images-idx3-ubyte.gz";

            CtlTask_MNIST_ImagesPath.Text = fileNameImagesGz;

            //


            var fileNameLabelsBin = Extension.GetDirectoryName(CtlTask_MNIST_LabelsPath.Text,
                                                               App.WorkingDirectory + "MNIST") + Path.DirectorySeparatorChar + "labels.bin";

            if (!File.Exists(fileNameLabelsBin))
            {
                if (!File.Exists(CtlTask_MNIST_LabelsPath.Text))
                {
                    CtlTask_MNIST_LabelsPath.Text = App.WorkingDirectory + "MNIST" + Path.DirectorySeparatorChar + "train-labels-idx1-ubyte.gz";
                }

                fileNameLabelsBin = Extension.GetDirectoryName(CtlTask_MNIST_LabelsPath.Text,
                                                               App.WorkingDirectory + "MNIST") + Path.DirectorySeparatorChar + "labels.bin";

                if (!File.Exists(fileNameLabelsBin))
                {
                    fileNameLabelsBin = App.WorkingDirectory + "MNIST" + Path.DirectorySeparatorChar + "labels.bin";
                    if (!File.Exists(fileNameLabelsBin))
                    {
                        try
                        {
                            if (!File.Exists(CtlTask_MNIST_LabelsPath.Text))
                            {
                                throw new Exception($"Cannot find file '{CtlTask_MNIST_LabelsPath.Text}'.");
                            }

                            Decompress(CtlTask_MNIST_LabelsPath.Text, fileNameLabelsBin);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Cannot open MNIST labels file.\r\n\r\n" + ex.Message);
                            return;
                        }
                    }
                }
            }

            LoadLabels(fileNameLabelsBin);
            var fileNameLabelsGz = Extension.GetDirectoryName(fileNameLabelsBin,
                                                              App.WorkingDirectory + "MNIST") + Path.DirectorySeparatorChar + "train-labels-idx1-ubyte.gz";

            CtlTask_MNIST_LabelsPath.Text = fileNameLabelsGz;
        }

        private void LoadImages(string fileName)
        {
            Images.Clear();

            if (!File.Exists(fileName))
            {
                return;
            }

            var buffer = new byte[4];

            using var file = File.OpenRead(fileName);

            if (file.Read(buffer, 0, buffer.Length) != buffer.Length)
            {
                throw new Exception("Invalid MNIST images file format.");
            }

            if (BitConverter.IsLittleEndian)
            {
                buffer = buffer.Reverse().ToArray();
            }

            int magicNumber = BitConverter.ToInt32(buffer, 0);
            if (magicNumber != 2051)
            {
                throw new Exception("Invalid MNIST images file format.");
            }

            if (file.Read(buffer, 0, buffer.Length) != buffer.Length)
            {
                throw new Exception("Invalid MNIST images file format.");
            }

            if (BitConverter.IsLittleEndian)
            {
                buffer = buffer.Reverse().ToArray();
            }

            int numberOfImages = BitConverter.ToInt32(buffer, 0);

            if (file.Read(buffer, 0, buffer.Length) != buffer.Length)
            {
                throw new Exception("Invalid MNIST images file format.");
            }

            if (BitConverter.IsLittleEndian)
            {
                buffer = buffer.Reverse().ToArray();
            }

            int numberOfRows = BitConverter.ToInt32(buffer, 0);
            if (numberOfRows != 28)
            {
                throw new Exception("Invalid MNIST images file format.");
            }

            if (file.Read(buffer, 0, buffer.Length) != buffer.Length)
            {
                throw new Exception("Invalid MNIST images file format.");
            }

            if (BitConverter.IsLittleEndian)
            {
                buffer = buffer.Reverse().ToArray();
            }

            int numberOfColumns = BitConverter.ToInt32(buffer, 0);
            if (numberOfColumns != 28)
            {
                throw new Exception("Invalid MNIST images file format.");
            }

            for (int i = 0; i < numberOfImages; ++i)
            {
                MNISTImage image = new();
                if (file.Read(image.Image, 0, image.Image.Length) != image.Image.Length)
                {
                    Images.Clear();
                    throw new Exception("Invalid MNIST images file format.");
                }

                Images.Add(image);
            }
        }

        private void LoadLabels(string fileName)
        {
            if (!File.Exists(fileName))
            {
                return;
            }

            var buffer = new byte[4];

            using var file = File.OpenRead(fileName);

            if (file.Read(buffer, 0, buffer.Length) != buffer.Length)
            {
                throw new Exception("Invalid MNIST labels file format.");
            }

            if (BitConverter.IsLittleEndian)
            {
                buffer = buffer.Reverse().ToArray();
            }

            int magicNumber = BitConverter.ToInt32(buffer, 0);
            if (magicNumber != 2049)
            {
                throw new Exception("Invalid MNIST labels file format.");
            }

            if (file.Read(buffer, 0, buffer.Length) != buffer.Length)
            {
                throw new Exception("Invalid MNIST labels file format.");
            }

            if (BitConverter.IsLittleEndian)
            {
                buffer = buffer.Reverse().ToArray();
            }

            int numberOfImages = BitConverter.ToInt32(buffer, 0);
            if (numberOfImages != Images.Count)
            {
                throw new Exception("Invalid MNIST labels file format.");
            }

            for (int i = 0; i < numberOfImages; ++i)
            {
                var image = Images[i];
                image.Label = (byte)file.ReadByte();
            }
        }

        public void SaveConfig()
        {
            Range.ForEach(this.FindVisualChildren<IConfigParam>(), param => param.SaveConfig());
        }

        public void RemoveFromConfig()
        {
            Range.ForEach(this.FindVisualChildren<IConfigParam>(), param => param.RemoveFromConfig());
        }

        public bool IsValid()
        {
            return this.FindVisualChildren<IConfigParam>().All(param => param.IsValid());
        }

        public void SetChangeEvent(Action onChange)
        {
            OnChange -= onChange;
            OnChange += onChange;

            Range.ForEach(this.FindVisualChildren<IConfigParam>(), param => param.SetChangeEvent(Parameter_OnChanged));
        }

        public void InvalidateValue() => throw new InvalidOperationException();

        private void BrowseImagesPath_OnClick(object sender, RoutedEventArgs e)
        {
            BrowseFile(CtlTask_MNIST_ImagesPath, "images.bin");
        }

        private void BrowseLabelsPath_OnClick(object sender, RoutedEventArgs e)
        {
            BrowseFile(CtlTask_MNIST_LabelsPath, "labels.bin");
        }

        private void BrowseFile(TextBox textBox, string targetFileName)
        {
            var fileName = BrowseGzFile();
            if (string.IsNullOrEmpty(fileName))
            {
                return;
            }

            try
            {
                Decompress(fileName, Path.GetDirectoryName(fileName) + Path.DirectorySeparatorChar + targetFileName);
                textBox.Text = fileName;
            }
            catch (Exception ex)
            {
                textBox.Text = string.Empty;
                MessageBox.Show("Cannot unzip file with the following message:\r\n\r\n" + ex.Message);
            }
        }

        private string BrowseGzFile()
        {
            OpenFileDialog loadDialog = new()
            {
                InitialDirectory = Path.GetFullPath("."),
                DefaultExt = "gz",
                Filter = "WinZip files (*.gz)|*.gz|All files (*.*)|*.*",
                FilterIndex = 1,
                RestoreDirectory = true
            };

            if (loadDialog.ShowDialog() == true)
            {
                return loadDialog.FileName;
            }

            return null;
        }

        private void Decompress(string sourceGz, string destBin)
        {
            using var srcStream = File.OpenRead(sourceGz);
            using var targetStream = File.OpenWrite(destBin);
            using GZipStream decompressionStream = new(srcStream, CompressionMode.Decompress, false);
            decompressionStream.CopyTo(targetStream);
        }
    }

    sealed public class MNISTImage
    {
        public readonly byte[] Image = new byte[28 * 28];
        public byte Label;
    }
}
