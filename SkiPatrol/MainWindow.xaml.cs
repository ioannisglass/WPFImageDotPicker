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
using System.Drawing;
using Microsoft.Win32;
using System.Windows.Controls.Primitives;
using System.IO;
using System.Text.RegularExpressions;
using WebAuto;

namespace SkiPatrol
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public Image m_imgMainMap = new Image();
        public Dictionary<string, int> m_accident_points = new Dictionary<string, int>();
        public static UserSetting g_setting = new UserSetting();
        public MainWindow()
        {
            InitializeComponent();
            m_imgMainMap.Width = 1440;
            m_imgMainMap.Height = 900;

            if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + "SkiMap.jpg"))
            {
                m_imgMainMap.Source = new BitmapImage(new Uri(AppDomain.CurrentDomain.BaseDirectory + "SkiMap.jpg"));
                m_imgMainMap.VerticalAlignment = System.Windows.VerticalAlignment.Stretch;
                m_imgMainMap.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;

                cvsMap.Children.Add(m_imgMainMap);
            }
            else
            {
                MessageBox.Show("No main map image in current folder.");
                System.Windows.Application.Current.Shutdown();
            }
            g_setting = UserSetting.Load();
            if (g_setting == null)
                g_setting = new UserSetting();
        }

        public void Load_Main_Map(string path)
        {
            m_imgMainMap.Source = new BitmapImage(new Uri(path));
        }

        private void btnLoadMap_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog op = new OpenFileDialog();
            op.Title = "Select a picture";
            op.Filter = "All supported graphics|*.jpg;*.jpeg;*.png|" +
              "JPEG (*.jpg;*.jpeg)|*.jpg;*.jpeg|" +
              "Portable Network Graphic (*.png)|*.png";
            if (op.ShowDialog() == true)
            {
                m_imgMainMap.Source = new BitmapImage(new Uri(op.FileName));
            }
        }

        private void cvsMap_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            ContextMenu m = ContextMenuService.GetContextMenu(sender as Canvas);
            
            m.Placement = PlacementMode.MousePoint;
            m.PlacementTarget = sender as Canvas;
            m.IsOpen = true;
        }

        private void saveMenuItem_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = "JPeg Image|*.jpg|PNG Image|*.png";
            dialog.Title = "Save the File";
            if (dialog.ShowDialog() == true)
            {
                //Rect bounds = VisualTreeHelper.GetDescendantBounds(cvsMap);
                double dpi = 96d;

                RenderTargetBitmap rtb = new RenderTargetBitmap((int)cvsMap.Width, (int)cvsMap.Height, dpi, dpi, System.Windows.Media.PixelFormats.Default);

                DrawingVisual dv = new DrawingVisual();
                using (DrawingContext dc = dv.RenderOpen())
                {
                    VisualBrush vb = new VisualBrush(cvsMap);
                    //dc.DrawRectangle(vb, null, new Rect(new Point(), bounds.Size));
                    dc.DrawRectangle(vb, null, new Rect(0, 0, cvsMap.Width, cvsMap.Height));
                }

                rtb.Render(dv);
                string filename = dialog.FileName;
                string extension = System.IO.Path.GetExtension(filename);

                if (extension.ToUpper() == ".png".ToUpper())
                    SaveToPng(filename, rtb);
                else if (extension.ToUpper() == ".jpg".ToUpper())
                    SaveToJpg(filename, rtb);
                else
                    MessageBox.Show("Check file extension.");
                MessageBox.Show("Save finished successfully.");
            }
        }

        private void closeMenuItem_Click(object sender, RoutedEventArgs e)
        {
            UserSetting.Save(g_setting);
            System.Windows.Application.Current.Shutdown();
        }

        private void SaveToJpg(string fileName, RenderTargetBitmap rtb)
        {
            var encoder = new JpegBitmapEncoder();
            SaveUsingEncoder(fileName, encoder, rtb);
        }

        private void SaveToPng(string fileName, RenderTargetBitmap rtb)
        {
            var encoder = new PngBitmapEncoder();
            SaveUsingEncoder(fileName, encoder, rtb);
        }

        private void SaveUsingEncoder(string fileName, BitmapEncoder encoder, RenderTargetBitmap rtb)
        {
            encoder.Frames.Add(BitmapFrame.Create(rtb));

            try
            {
                System.IO.MemoryStream ms = new System.IO.MemoryStream();

                encoder.Save(ms);
                ms.Close();

                System.IO.File.WriteAllBytes(fileName, ms.ToArray());
            }
            catch (Exception err)
            {
                MessageBox.Show(err.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void draw_red_point(int idx_X, int idx_Y, int accident_counts)
        {
            if (idx_X <= 0 || idx_X > 38 || idx_Y <= 0 || idx_Y > 20)
                return;

            Ellipse circle = new Ellipse();

            if (accident_counts > 0 && accident_counts <= 3)
            {
                Brush bru = new SolidColorBrush(Color.FromRgb(MainWindow.g_setting.first_level_value[0], MainWindow.g_setting.first_level_value[1], MainWindow.g_setting.first_level_value[2]));
                 circle = new Ellipse()
                 {
                     Width = 15,
                     Height = 15,
                     Fill = bru
                 };
            }
            else if (accident_counts > 3 && accident_counts <= 10)
            {
                Brush bru = new SolidColorBrush(Color.FromRgb(MainWindow.g_setting.second_level_value[0], MainWindow.g_setting.second_level_value[1], MainWindow.g_setting.second_level_value[2]));
                circle = new Ellipse()
                {
                    Width = 15,
                    Height = 15,
                    Fill = bru
                };
            }
            else if(accident_counts > 10)
            {
                Brush bru = new SolidColorBrush(Color.FromRgb(g_setting.third_level_value[0], g_setting.third_level_value[1], g_setting.third_level_value[2]));
                circle = new Ellipse()
                {
                    Width = 15,
                    Height = 15,
                    Fill = bru
                };
            }
            else
            {
                circle = new Ellipse()
                {
                    Width = 15,
                    Height = 15,
                    Fill = Brushes.Black
                };
            }

            cvsMap.Children.Add(circle);

            double x, y;

            double begin_Y = 87;
            double begin_X = 41;
            double cell_width = 35.5;
            double cell_height = 38;

            x = begin_X + cell_width * (idx_X - 0.5) - 7.5;
            y = begin_Y + cell_height * (idx_Y - 0.5) - 7.5;

            circle.SetValue(Canvas.LeftProperty, x);
            circle.SetValue(Canvas.TopProperty, y);
        }

        private void drawMenuItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog dlg = new OpenFileDialog();
                dlg.Title = "Open a file containing e-mail address";
                dlg.Filter = "CSV files|*.CSV|All files|*.*";
                dlg.RestoreDirectory = true;
                if (dlg.ShowDialog() == true)
                {
                    List<string> dot_list = File.ReadAllLines(dlg.FileName).ToList();
                    if (dot_list.Count == 0)
                    {
                        MessageBox.Show("No accident positions.");
                        return;
                    }
                    string dot_pattern = @"[A-Ta-t](1[0-9]|2[0-9]|3[0-8]|[1-9])";

                    foreach(string dot in dot_list)
                    {
                        var dot_arr = Regex.Matches(dot.Replace(" ", "").ToUpper(), dot_pattern)
                                        .OfType<Match>()
                                        .Select(m => m.Groups[0].Value)
                                        .ToArray();
                        if (dot_arr.Count() != 1)
                            continue;

                        if (m_accident_points.Count != 0 && m_accident_points.ContainsKey(dot_arr[0]))
                            m_accident_points[dot_arr[0]]++;
                        else
                            m_accident_points.Add(dot_arr[0], 1);                        
                    }
                    foreach(string key in m_accident_points.Keys)
                    {
                        int idx_dot_Y = key[0] - 64;
                        int idx_dot_X = int.Parse(key.Substring(1));
                        draw_red_point(idx_dot_X, idx_dot_Y, m_accident_points[key]);
                    }
                    MessageBox.Show("Draw finished.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
