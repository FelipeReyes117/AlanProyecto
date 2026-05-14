using Emgu.CV;
using Emgu.CV.Structure;
using MailKit.Net.Smtp;
using MailKit.Security;
using MailKit;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using ZXing;
using ZXing.Common;
using ZXing.Windows.Compatibility;
using static System.Net.Mime.MediaTypeNames;
using Application = System.Windows.Forms.Application;

namespace Proyecto_Alan_OWO
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        public MySqlConnection con = new MySqlConnection("Server=localhost;Database=dbasistencias;Uid=root;Pwd=rootroot;");


        private void ProcesarFrame(object sender, EventArgs e)
        {
            txtMatricula.Text = string.Empty;
            txtNombre.Text = string.Empty;
            txtGrupo.Text = string.Empty;
            string QRresultado = string.Empty;
            Mat frame = captura.QueryFrame();
            if (frame == null) return;
            var imagen = frame.ToBitmap();
            pbCamara.Image = imagen;

            // Lee Codigo QR
            var resultado = lector.Decode(imagen);
            if (resultado != null)
            {
                QRresultado = resultado.Text;
                if (QRresultado != "")
                {
                    pbFoto.Image = null;
                    Application.Idle -= ProcesarFrame;
                    captura.Dispose();
                    try
                    {
                        string query = "SELECT matricula ,nombre, area FROM empleados WHERE matricula = " + QRresultado + "";
                        MySqlCommand cmd = new MySqlCommand(query, con);
                        con.Open();
                        MySqlDataAdapter da = new MySqlDataAdapter(query, con);
                        DataTable dt = new DataTable();
                        da.Fill(dt);
                        txtMatricula.Text = dt.Rows[0]["matricula"].ToString();
                        txtNombre.Text = dt.Rows[0]["nombre"].ToString();
                        txtGrupo.Text = dt.Rows[0]["area"].ToString();
                        con.Close();
                        QRresultado = null;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error al Consultar: " + ex.Message);
                    }
                }
            }
        }

        VideoCapture captura;
        ZXing.Windows.Compatibility.BarcodeReader lector = new ZXing.Windows.Compatibility.BarcodeReader();
        private void IniciarCamara()
        {
            captura = new VideoCapture(0);
            Application.Idle += ProcesarFrame;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            IniciarCamara();
        }

        private void btnConfig_Click(object sender, EventArgs e)
        {
           frmAdminvr adminVerif= new frmAdminvr();
            adminVerif.Show();
        }

        private Bitmap GenerarQR(string texto)
        {
            ZXing.Windows.Compatibility.BarcodeWriter writer = new ZXing.Windows.Compatibility.BarcodeWriter();
            writer.Format = BarcodeFormat.QR_CODE;
            writer.Options = new EncodingOptions
            {
                Width = 150,
                Height = 150,
                Margin = 1
            };

            return writer.Write(texto);
        }

        PrintDocument printDoc = new PrintDocument();
        Bitmap qrImage;

        private void button2_Click(object sender, EventArgs e)
        {
            //PROVISIONAL
            qrImage = GenerarQR(txtprueba.Text);

            printDoc.PrintPage += PrintDoc_PrintPage;

            PrintPreviewDialog preview = new PrintPreviewDialog();
            preview.Document = printDoc;
            preview.ShowDialog();
        }

        private void PrintDoc_PrintPage(object sender, PrintPageEventArgs e)
        {
            Graphics dibujar = e.Graphics;

            // coordenadas
            int x = 50;
            int y = 50;
            // Tamaño credencial
            int width = 350;
            int height = 200;

            ////imagen de fondo
            //Image fondo = Image.FromFile("C:/Users/Melvita/Pictures/fondo.png");
            //dibujar.DrawImage(fondo, x, y, width, height);

            // Fondo
            dibujar.FillRectangle(Brushes.Blue, x, y, width, height);
            dibujar.DrawRectangle(Pens.Black, x, y, width, height);
            // Foto
            if (pbFoto.Image != null)
            {
                dibujar.DrawImage(pbFoto.Image, x + 10, y + 20, 100, 120);
            }
            // Datos del empleado
            Font font = new Font("Arial", 10, FontStyle.Bold);
            dibujar.DrawString("Matricula:", font, Brushes.Black, x + 120, y + 20);
            dibujar.DrawString(txtprueba.Text, font, Brushes.Black, x + 120, y + 40);
            // Codigo QR
            if (qrImage != null)
            {
                dibujar.DrawImage(qrImage, x + 260, y + 20, 80, 80);
            }
        }
    }
}
