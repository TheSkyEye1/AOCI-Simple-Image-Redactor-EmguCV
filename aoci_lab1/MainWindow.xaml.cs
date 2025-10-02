using Emgu.CV.Structure;
using Emgu.CV;
using System;
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
using Emgu.CV.Reg;
using System.Drawing;
using Microsoft.Win32;

namespace aoci_lab1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private Image<Bgr, byte> sourceImage;

        public MainWindow()
        {
            InitializeComponent();
        }
        public BitmapSource ToBitmapSource(Image<Bgr, byte> image)
        {
            var mat = image.Mat;

            return BitmapSource.Create(
                mat.Width,
                mat.Height,
                96d,
                96d,
                PixelFormats.Bgr24,
                null,
                mat.DataPointer,
                mat.Step * mat.Height,
                mat.Step);
        }
        public Image<Bgr, byte> ToEmguImage(BitmapSource source)
        {
            if (source == null) return null;

            FormatConvertedBitmap safeSource = new FormatConvertedBitmap();
            safeSource.BeginInit();
            safeSource.Source = source;
            safeSource.DestinationFormat = PixelFormats.Bgr24;
            safeSource.EndInit();

            Image<Bgr, byte> resultImage = new Image<Bgr, byte>(safeSource.PixelWidth, safeSource.PixelHeight);
            var mat = resultImage.Mat;

            safeSource.CopyPixels(
                new System.Windows.Int32Rect(0, 0, safeSource.PixelWidth, safeSource.PixelHeight),
                mat.DataPointer,
                mat.Step * mat.Height,
                mat.Step);

            return resultImage;
        }

        private void BrightnessSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (sourceImage == null) return;

            Image<Bgr, byte> lightImage = sourceImage.Clone();
            int brightness = (int)e.NewValue;

            for (int y = 0; y < lightImage.Rows; y++)
            {
                for (int x = 0; x < lightImage.Cols; x++)
                {
                    Bgr pixel = lightImage[y, x];
                    
                    int b = (int)pixel.Blue + brightness;
                    int g = (int)pixel.Green + brightness;
                    int r = (int)pixel.Red + brightness;

                    pixel.Blue = (byte)Math.Max(0, Math.Min(255, b));
                    pixel.Green = (byte)Math.Max(0, Math.Min(255, g));
                    pixel.Red = (byte)Math.Max(0, Math.Min(255, r));

                    lightImage[y, x] = pixel;
                }
            }
            MainImage.Source = ToBitmapSource(lightImage);
        }

        private void LoadImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Файлы изображений (*.jpg, *.jpeg, *.png)|*.jpg;*.jpeg;*.png";
            if (openFileDialog.ShowDialog() == true)
            {
                sourceImage = new Image<Bgr, byte>(openFileDialog.FileName);
                MainImage.Source = ToBitmapSource(sourceImage);
            }
        }

        private void Invert_Click(object sender, RoutedEventArgs e)
        {
            if (sourceImage == null) return;

            Image<Bgr, byte> invertedImage = sourceImage.Clone();

            for (int y = 0; y < invertedImage.Rows; y++)
            {
                for (int x = 0; x < invertedImage.Cols; x++)
                {
                    Bgr pixel = invertedImage[y, x];
                    pixel.Blue = 255 - pixel.Blue;
                    pixel.Green = 255 - pixel.Green;
                    pixel.Red = 255 - pixel.Red;
                    invertedImage[y, x] = pixel;
                }
            }

            MainImage.Source = ToBitmapSource(invertedImage);
        }

        private void Grayscale_Click(object sender, RoutedEventArgs e)
        {
            if (sourceImage == null) return;

            Image<Bgr, byte> grayscaleImage = sourceImage.Clone();

            for (int y = 0; y < grayscaleImage.Rows; y++)
            {
                for (int x = 0; x < grayscaleImage.Cols; x++)
                {
                    Bgr pixel = grayscaleImage[y, x];
                    byte gray = (byte)(pixel.Red * 0.299 + pixel.Green * 0.587 + pixel.Blue * 0.114);
                    grayscaleImage[y, x] = new Bgr(gray, gray, gray);
                }
            }
            MainImage.Source = ToBitmapSource(grayscaleImage);
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            if(sourceImage == null) return;

            MainImage.Source = ToBitmapSource(sourceImage);
        }

        private void ContrastSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (sourceImage == null) return;

            Image<Bgr, byte> contrastImage = sourceImage.Clone();
            double brightness = e.NewValue;

            for (int y = 0; y < contrastImage.Rows; y++)
            {
                for (int x = 0; x < contrastImage.Cols; x++)
                {
                    Bgr pixel = contrastImage[y, x];

                    int b = (int)(pixel.Blue * brightness);
                    int g = (int)(pixel.Green * brightness);
                    int r = (int)(pixel.Red * brightness);

                    pixel.Blue = (byte)Math.Max(0, Math.Min(255, b));
                    pixel.Green = (byte)Math.Max(0, Math.Min(255, g));
                    pixel.Red = (byte)Math.Max(0, Math.Min(255, r));

                    contrastImage[y, x] = pixel;
                }
            }
            MainImage.Source = ToBitmapSource(contrastImage);
        }

        private void RemoveRed_Click(object sender, RoutedEventArgs e)
        {
            Image<Bgr, byte> redImage = sourceImage.Clone();
            MainImage.Source = ToBitmapSource(RemoveChannels(redImage, new int[] { 1, 1, 0 }));
        }

        private void RemoveGreen_Click(object sender, RoutedEventArgs e)
        {
            Image<Bgr, byte> redImage = sourceImage.Clone();
            MainImage.Source = ToBitmapSource(RemoveChannels(redImage, new int[] { 1, 0, 1 }));
        }

        private void RemoveBlue_Click(object sender, RoutedEventArgs e)
        {
            Image<Bgr, byte> redImage = sourceImage.Clone();
            MainImage.Source = ToBitmapSource(RemoveChannels(redImage, new int[] { 0, 1, 1 }));
        }

        public Image<Bgr, byte> RemoveChannels(Image<Bgr, byte> image, int[] channels)
        {
            for (int y = 0; y < image.Rows; y++)
            {
                for (int x = 0; x < image.Cols; x++)
                {
                    Bgr pixel = image[y, x];

                    int b = (int)(pixel.Blue * channels[0]);
                    int g = (int)(pixel.Green * channels[1]);
                    int r = (int)(pixel.Red * channels[2]);

                    pixel.Blue = (byte)Math.Max(0, Math.Min(255, b));
                    pixel.Green = (byte)Math.Max(0, Math.Min(255, g));
                    pixel.Red = (byte)Math.Max(0, Math.Min(255, r));

                    image[y, x] = pixel;
                }
            }

            return image;
        }

        private void UpdateImage_Click(object sender, RoutedEventArgs e)
        {
            BitmapSource currentWpfImage = MainImage.Source as BitmapSource;

            if (currentWpfImage == null)
            {
                MessageBox.Show("Изображение отсутсвует");
                return;
            }


            sourceImage = ToEmguImage(currentWpfImage);

            MessageBox.Show("Изменения применены. Теперь это новый оригинал.");
        }

        private void SaveImage_Click(object sender, RoutedEventArgs e)
        {
            BitmapSource currentWpfImage = MainImage.Source as BitmapSource;
            if (currentWpfImage == null)
            {
                MessageBox.Show("Отсутсвует изображение");
                return;
            }

            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "PNG Image (*.png)|*.png|JPEG Image (*.jpg)|*.jpg|Bitmap Image (*.bmp)|*.bmp";

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    Image<Bgr, byte> imageToSave = ToEmguImage(currentWpfImage);

                    imageToSave.Save(saveFileDialog.FileName);

                    MessageBox.Show($"Изображение успешно сохранено в {saveFileDialog.FileName}");
                }
                catch (Exception ex)
                {
                    
                    MessageBox.Show($"Ошибка! Не могу сохранить файл. Подробности: {ex.Message}");
                }
            }
        }
    }
}
