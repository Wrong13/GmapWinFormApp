using GMap.NET;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace GmapWinFormApp
{
    public partial class Form1 : Form
    {
        private GMapMarker _selectedMarker;

        public Form1()
        {
            InitializeComponent();
            this.WindowState = FormWindowState.Maximized;

            GMap.NET.GMaps.Instance.Mode = GMap.NET.AccessMode.ServerAndCache;                
            gMapControl1.MapProvider = GMap.NET.MapProviders.GoogleMapProvider.Instance; 
            gMapControl1.MinZoom = 2; 
            gMapControl1.MaxZoom = 16; 
            gMapControl1.Zoom = 4; 
            gMapControl1.MouseWheelZoomType = GMap.NET.MouseWheelZoomType.MousePositionAndCenter; 
            gMapControl1.CanDragMap = true; 
            gMapControl1.DragButton = MouseButtons.Left; 
            
            gMapControl1.ShowCenter = false; 
            gMapControl1.ShowTileGridLines = false;

            gMapControl1.MouseUp += _gMapControl_MouseUp;
            gMapControl1.MouseDown += _gMapControl_MouseDown;

            gMapControl1.Overlays.Add(GetOverlayMarkers("test"));
        }
        private void Form1_ResizeEnd(object sender, EventArgs e)
        {
            gMapControl1.Width = this.Width;
            gMapControl1.Height = this.Height;
        }


        private async Task ExecuteSqlAsync(string sqlQuery)
        {
            string connectionString = @"Data Source=DESKTOP-SN00NCD\SQLEXPRESS;Initial Catalog=gMapdb;Integrated Security=True";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (SqlCommand command = new SqlCommand(sqlQuery, connection))
                {
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        // Асинхронный метод для обновления координат маркера в базе данных
        private async Task UpdateMarkerCoordinatesAsync(GMapMarker marker, int markId)
        {
            string lat = marker.Position.Lat.ToString().Replace(',', '.');
            string lon = marker.Position.Lng.ToString().Replace(',', '.');
            string sqlProc = $"declare @Id int, @lat float, @lon float;\r\n\r\nset @Id = {markId}\r\nset @lat = {lat}\r\nset @lon = {lon}\r\n\r\nexec sp_UpdateMark @Id, @lat, @lon";
            await ExecuteSqlAsync(sqlProc);
        }
        private GMapOverlay GetOverlayMarkers(string name, GMarkerGoogleType gMarkerGoogleType = GMarkerGoogleType.red)
        {
            GMapOverlay gMapMarkers = new GMapOverlay(name);

            string connectionString = @"Data Source=DESKTOP-SN00NCD\SQLEXPRESS;Initial Catalog=gMapdb;Integrated Security=True";
            string sqlProc = "sp_GetMarks";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand(sqlProc, connection);
                var reader = command.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        GMarkerGoogle marker = new GMarkerGoogle(new PointLatLng(reader.GetDouble(1),reader.GetDouble(2)),GMarkerGoogleType.orange);
                        marker.Tag = reader.GetInt32(0);
                        gMapMarkers.Markers.Add(marker);
                    }
                }
                reader.Close();
                connection.Close();
            }
            return gMapMarkers;
        }
            
        // Передвижение маркеров
        private void _gMapControl_MouseDown(object sender, MouseEventArgs e)
        {
            //находим тот маркер над которым нажали клавишу мыши
            _selectedMarker = gMapControl1.Overlays
                .SelectMany(o => o.Markers)
                .FirstOrDefault(m => m.IsMouseOver == true);            
        }

        private async void _gMapControl_MouseUp(object sender, MouseEventArgs e)
        {
            if (_selectedMarker is null)
                return;

            //переводим координаты курсора мыши в долготу и широту на карте
            var latlng = gMapControl1.FromLocalToLatLng(e.X, e.Y);
            int markerId = (int)_selectedMarker.Tag;
            //присваиваем новую позицию для маркера
            _selectedMarker.Position = latlng;
            await UpdateMarkerCoordinatesAsync(_selectedMarker,markerId);
            _selectedMarker = null;
        }
    }
}
