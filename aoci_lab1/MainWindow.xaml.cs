using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Emgu.CV.Structure;
using Emgu.CV;

namespace aoci_lab1
{
    public partial class MainWindow : Window
    {
        //Главный объект для работы с изображением в Emgu.CV.
        //Это поле будет хранить наше "оригинальное" изображение для всех операций.
        private Image<Bgr, byte> sourceImage;

        public MainWindow()
        {
            InitializeComponent();
        }

        // --- Методы-конвертеры (Мост между Emgu.CV и WPF) ---

        /*/
        Функция конвертирует изображение из формата Emgu.CV (Image<Bgr, byte>) в формат, понятный для WPF (BitmapSource).
        Это необходимо, чтобы мы могли отобразить результат обработки в элементе Image на нашем окне.
        /*/
        public BitmapSource ToBitmapSource(Image<Bgr, byte> image)
        {
            //У каждого объекта Image<,> есть свойство .Mat, которое предоставляет
            //доступ к матрице пикселей.
            var mat = image.Mat;

            return BitmapSource.Create(
                mat.Width,
                mat.Height,
                96d, //Горизонтальное разрешение (DPI), 96 - стандарт для экранов
                96d, //Вертикальное разрешение (DPI)
                PixelFormats.Bgr24, //Мы указываем WPF, что данные идут в формате Bgr, по 24 бита на пиксель (8 бит на синий, 8 на зеленый, 8 на красный).
                null, //Палитра не используется для 24-битных изображений
                mat.DataPointer, //Указатель на начало данных изображения в памяти.
                mat.Step * mat.Height, //Общий размер буфера данных в байтах.
                mat.Step); // Шаг - это длина одной строки изображения в байтах.
        }

        /*/
        Функция конвертирует изображение из формата WPF (BitmapSource) обратно в формат Emgu.CV (Image<Bgr, byte>).
        /*/
        public Image<Bgr, byte> ToEmguImage(BitmapSource source)
        {
            if (source == null) return null;

            //Чтобы гарантировать, что у нас есть данные в формате Bgr24, мы создаем "конвертер" FormatConvertedBitmap.
            FormatConvertedBitmap safeSource = new FormatConvertedBitmap();
            safeSource.BeginInit();
            safeSource.Source = source;
            safeSource.DestinationFormat = PixelFormats.Bgr24; //Явно указываем нужный нам формат
            safeSource.EndInit();

            //Создаем пустое изображение Emgu.CV нужного размера.
            Image<Bgr, byte> resultImage = new Image<Bgr, byte>(safeSource.PixelWidth, safeSource.PixelHeight);
            var mat = resultImage.Mat;

            //Копируем пиксели из WPF-изображения (safeSource) напрямую в память нашего Emgu.CV изображения (resultImage).
            safeSource.CopyPixels(
                new System.Windows.Int32Rect(0, 0, safeSource.PixelWidth, safeSource.PixelHeight), //Какую область копировать
                mat.DataPointer, //Куда копировать (в начало данных матрицы Emgu.CV)
                mat.Step * mat.Height, //Размер буфера назначения
                mat.Step); //Шаг в буфере назначения

            return resultImage;
        }



        // --- Обработчики событий от элементов UI ---


        //Функция загрузки изображения
        private void LoadImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Файлы изображений (*.jpg, *.jpeg, *.png)|*.jpg;*.jpeg;*.png";
            if (openFileDialog.ShowDialog() == true)
            {

                //Если пользователь выбрал файл, создаем объект Image<Bgr, byte>
                //напрямую из пути к файлу. Emgu.CV сам его загрузит и декодирует.
                sourceImage = new Image<Bgr, byte>(openFileDialog.FileName);

                //Конвертируем наше Emgu.CV изображение в понятный для WPF формат с помощью нашего конвертера.
                MainImage.Source = ToBitmapSource(sourceImage);
            }
        }

        //Функция сохранения изображения
        private void SaveImage_Click(object sender, RoutedEventArgs e)
        {
            //Получаем текущее отображаемое изображение из элемента MainImage.
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

                    //Перед сохранением конвертируем изображение из WPF-формата в Emgu.CV.
                    Image<Bgr, byte> imageToSave = ToEmguImage(currentWpfImage);

                    //У Emgu.CV есть удобный встроенный метод .Save()
                    imageToSave.Save(saveFileDialog.FileName);

                    MessageBox.Show($"Изображение успешно сохранено в {saveFileDialog.FileName}");
                }
                catch (Exception ex)
                {

                    MessageBox.Show($"Ошибка! Не могу сохранить файл. Подробности: {ex.Message}");
                }
            }
        }

        //Функция обновления оригинального изображения
        private void UpdateImage_Click(object sender, RoutedEventArgs e)
        {
            //Эта функция позволяет "зафиксировать" текущие изменения.
            BitmapSource currentWpfImage = MainImage.Source as BitmapSource;

            if (currentWpfImage == null)
            {
                MessageBox.Show("Изображение отсутсвует");
                return;
            }

            //Мы берем то, что сейчас на экране, конвертируем в формат Emgu.CV
            //и перезаписываем нашу основную переменную `sourceImage`.
            sourceImage = ToEmguImage(currentWpfImage);

            MessageBox.Show("Изменения применены. Теперь это новый оригинал.");
        }




        // --- Методы обработки изображений ---


        //Функция инвертации значения пикселей
        private void Invert_Click(object sender, RoutedEventArgs e)
        {
            //Всегда проверяем, загружено ли изображение.
            if (sourceImage == null) return;

            //ВАЖНО: Мы не хотим изменять оригинальное изображение (`sourceImage`).
            //Вместо этого мы создаем его точную копию (клон) и работаем с ней.
            Image<Bgr, byte> invertedImage = sourceImage.Clone();

            //Проходим по каждому пикселю изображения.
            for (int y = 0; y < invertedImage.Rows; y++)
            {
                for (int x = 0; x < invertedImage.Cols; x++)
                {

                    //Получаем доступ к пикселю по его координатам (y, x).
                    //Emgu.CV возвращает структуру Bgr, у которой есть поля .Blue, .Green, .Red.
                    Bgr pixel = invertedImage[y, x];

                    // Инвертируем каждый цветовой канал. Максимальное значение - 255.
                    pixel.Blue = 255 - pixel.Blue;
                    pixel.Green = 255 - pixel.Green;
                    pixel.Red = 255 - pixel.Red;

                    //Записываем измененный пиксель обратно в изображение.
                    invertedImage[y, x] = pixel;
                }
            }

            //Отображаем результат в окне.
            MainImage.Source = ToBitmapSource(invertedImage);
        }

        //Функция перевода изображения в "черно-белый" вариант
        private void Grayscale_Click(object sender, RoutedEventArgs e)
        {
            if (sourceImage == null) return;

            //Снова создаем клон для безопасной работы.
            Image<Bgr, byte> grayscaleImage = sourceImage.Clone();

            for (int y = 0; y < grayscaleImage.Rows; y++)
            {
                for (int x = 0; x < grayscaleImage.Cols; x++)
                {
                    Bgr pixel = grayscaleImage[y, x];

                    //Вычисляем оттенок серого по стандартной формуле для восприятия человеческим глазом.
                    byte gray = (byte)(pixel.Red * 0.299 + pixel.Green * 0.587 + pixel.Blue * 0.114);

                    //Создаем новый пиксель, у которого все три канала (R, G, B) равны вычисленному значению серого.
                    grayscaleImage[y, x] = new Bgr(gray, gray, gray);
                }
            }
            MainImage.Source = ToBitmapSource(grayscaleImage);
        }

        //Функция сброса всех изменений
        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            if(sourceImage == null) return;

            //Просто отображаем наше исходное изображение.
            MainImage.Source = ToBitmapSource(sourceImage);
        }


        // --- Методы для работы с цветовыми каналами --

        //Вспомогательный метод для удаления или ослабления цветовых каналов.
        public Image<Bgr, byte> RemoveChannels(Image<Bgr, byte> image, int[] channels)
        {
            for (int y = 0; y < image.Rows; y++)
            {
                for (int x = 0; x < image.Cols; x++)
                {
                    Bgr pixel = image[y, x];

                    //Умножаем значение каждого канала на соответствующий коэффициент.
                    //Если коэффициент 0 - канал станет черным. Если 1 - останется как есть.
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

        private void RemoveRed_Click(object sender, RoutedEventArgs e)
        {
            Image<Bgr, byte> redImage = sourceImage.Clone();
            //Массив [B, G, R]. Устанавливаем красный (R) канал в 0.
            MainImage.Source = ToBitmapSource(RemoveChannels(redImage, new int[] { 1, 1, 0 }));
        }

        private void RemoveGreen_Click(object sender, RoutedEventArgs e)
        {
            Image<Bgr, byte> redImage = sourceImage.Clone();
            //Массив [B, G, R]. Устанавливаем зеленый (G) канал в 0.
            MainImage.Source = ToBitmapSource(RemoveChannels(redImage, new int[] { 1, 0, 1 }));
        }

        private void RemoveBlue_Click(object sender, RoutedEventArgs e)
        {
            Image<Bgr, byte> redImage = sourceImage.Clone();
            //Массив [B, G, R]. Устанавливаем синий (B) канал в 0.
            MainImage.Source = ToBitmapSource(RemoveChannels(redImage, new int[] { 0, 1, 1 }));
        }


        // --- Обработчики для слайдеров ---



        private void BrightnessSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (sourceImage == null) return;

            Image<Bgr, byte> lightImage = sourceImage.Clone();

            // Получаем новое значение со слайдера.
            int brightness = (int)e.NewValue;

            for (int y = 0; y < lightImage.Rows; y++)
            {
                for (int x = 0; x < lightImage.Cols; x++)
                {
                    Bgr pixel = lightImage[y, x];

                    //Прибавляем значение яркости к каждому каналу.
                    int b = (int)pixel.Blue + brightness;
                    int g = (int)pixel.Green + brightness;
                    int r = (int)pixel.Red + brightness;

                    //Результат сложения может выйти за пределы 0-255.
                    //Поэтому результат зажимается допустимом диапазоне.
                    pixel.Blue = (byte)Math.Max(0, Math.Min(255, b));
                    pixel.Green = (byte)Math.Max(0, Math.Min(255, g));
                    pixel.Red = (byte)Math.Max(0, Math.Min(255, r));

                    lightImage[y, x] = pixel;
                }
            }
            MainImage.Source = ToBitmapSource(lightImage);
        }
        private void ContrastSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (sourceImage == null) return;

            Image<Bgr, byte> contrastImage = sourceImage.Clone();

            //Для контраста мы умножаем, а не прибавляем.
            double brightness = e.NewValue;

            for (int y = 0; y < contrastImage.Rows; y++)
            {
                for (int x = 0; x < contrastImage.Cols; x++)
                {
                    Bgr pixel = contrastImage[y, x];

                    //Умножаем каждый канал на коэффициент контрастности.
                    //Если > 1, цвета станут контрастнее, если < 1 - бледнее, с результатом 0 изображение будет черным.
                    int b = (int)(pixel.Blue * brightness);
                    int g = (int)(pixel.Green * brightness);
                    int r = (int)(pixel.Red * brightness);

                    //Снова зажимаем результат в диапазоне 0-255.
                    pixel.Blue = (byte)Math.Max(0, Math.Min(255, b));
                    pixel.Green = (byte)Math.Max(0, Math.Min(255, g));
                    pixel.Red = (byte)Math.Max(0, Math.Min(255, r));

                    contrastImage[y, x] = pixel;
                }
            }

            MainImage.Source = ToBitmapSource(contrastImage);
        }

    }
}
