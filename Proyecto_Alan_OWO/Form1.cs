using Emgu.CV;
using Emgu.CV.Structure;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Threading.Tasks;
using System.Windows.Forms;
using ZXing;
using ZXing.Common;
using ZXing.Windows.Compatibility;
using Application = System.Windows.Forms.Application;

namespace Proyecto_Alan_OWO
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        public string CorreoAdministrador { get; set; }
        public MySqlConnection con = new MySqlConnection("Server=localhost;Database=dbasistencias;Uid=root;Pwd=rootroot;");

        private int _frameContador = 0;

        private void ProcesarFrame(object sender, EventArgs e)
        {
            string QRresultado = string.Empty;

            Mat frame = captura.QueryFrame();
            if (frame == null) return;

            int lado = Math.Min(frame.Width, frame.Height);
            int rx = (frame.Width - lado) / 2;
            int ry = (frame.Height - lado) / 2;
            Mat frameRecortado = new Mat(frame, new Rectangle(rx, ry, lado, lado));

            Mat frameRedim = new Mat();
            CvInvoke.Resize(frameRecortado, frameRedim, pbCamara.Size);

            var imagen = frameRedim.ToBitmap();
            var anterior = pbCamara.Image;
            pbCamara.Image = imagen;
            anterior?.Dispose();

            _frameContador++;
            if (_frameContador % 3 != 0) return;

            var resultado = lector.Decode(imagen);
            if (resultado != null)
            {
                QRresultado = resultado.Text;
                if (QRresultado != "")
                {
                    pbFoto.Image = null;
                    timerCamara.Stop();
                    try
                    {
                        string query = "SELECT matricula, nombre, area, foto, horaentrada, horasalida, fechadealta " +
                                       "FROM empleados WHERE matricula = " + QRresultado + " AND activo = 1";
                        string queryAsistencia = @"SELECT idasistencia,
                           NULLIF(entrada,  '0000-00-00 00:00:00') AS entrada,
                           NULLIF(salida,   '0000-00-00 00:00:00') AS salida,
                           NULLIF(retardo,  '0000-00-00 00:00:00') AS retardo,
                           NULLIF(falta,    '0000-00-00')          AS falta,
                           empleados_matricula
                           FROM asistencia WHERE empleados_matricula = " + QRresultado + @"
                           AND DATE(entrada) = CURDATE()";

                        con.Open();
                        MySqlDataAdapter da = new MySqlDataAdapter(query, con);
                        DataTable dt = new DataTable();
                        da.Fill(dt);

                        if (dt.Rows.Count == 0)
                        {
                            MessageBox.Show("Acceso denegado. Empleado no activo en el sistema.");
                            con.Close();
                            timerCamara.Start();
                            return;
                        }

                        VerificarAsistencia(dt);

                        MySqlDataAdapter daAsis = new MySqlDataAdapter(queryAsistencia, con);
                        DataTable dtAsis = new DataTable();
                        daAsis.Fill(dtAsis);

                        txtMatricula.Text = dt.Rows[0]["matricula"].ToString();
                        txtNombre.Text = dt.Rows[0]["nombre"].ToString();
                        txtGrupo.Text = dt.Rows[0]["area"].ToString();

                        txtEntrada.Text = (dtAsis.Rows.Count > 0 && dtAsis.Rows[0]["entrada"] != DBNull.Value)
                            ? Convert.ToDateTime(dtAsis.Rows[0]["entrada"]).ToString("hh:mm:ss tt")
                            : "Sin registro";

                        txtSalida.Text = (dtAsis.Rows.Count > 0 && dtAsis.Rows[0]["salida"] != DBNull.Value)
                            ? Convert.ToDateTime(dtAsis.Rows[0]["salida"]).ToString("hh:mm:ss tt")
                            : "Sin registro";

                        var fotoRaw = dt.Rows[0]["foto"];
                        if (fotoRaw != DBNull.Value && fotoRaw is byte[] fotoBytes && fotoBytes.Length > 0)
                        {
                            var ms = new System.IO.MemoryStream(fotoBytes);
                            pbFoto.Image = new Bitmap(System.Drawing.Image.FromStream(ms));
                        }

                        con.Close();
                        QRresultado = null;
                        timerReset.Start();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error al Consultar: " + ex.Message);
                        timerCamara.Start();
                    }
                    finally
                    {
                        if (con.State == System.Data.ConnectionState.Open)
                            con.Close();
                    }
                }
            }
        }

        private void VerificarAsistencia(DataTable dt)
        {
            string matricula = dt.Rows[0]["matricula"].ToString();
            TimeSpan horaEntrada = (TimeSpan)dt.Rows[0]["horaentrada"];
            TimeSpan horaSalida = (TimeSpan)dt.Rows[0]["horasalida"];
            DateTime ahora = DateTime.Now;
            TimeSpan horaActual = ahora.TimeOfDay;

            if (ahora.DayOfWeek == DayOfWeek.Saturday || ahora.DayOfWeek == DayOfWeek.Sunday)
            {
                MessageBox.Show("Hoy no es día laboral.");
                timerCamara.Start();
                return;
            }

            string queryBuscar = @"SELECT * FROM asistencia 
                           WHERE empleados_matricula = @matricula 
                           AND DATE(entrada) = CURDATE()";
            MySqlCommand cmdBuscar = new MySqlCommand(queryBuscar, con);
            cmdBuscar.Parameters.AddWithValue("@matricula", matricula);
            MySqlDataAdapter daBuscar = new MySqlDataAdapter(cmdBuscar);
            DataTable dtAsistencia = new DataTable();
            daBuscar.Fill(dtAsistencia);

            DateTime fechaAlta = Convert.ToDateTime(dt.Rows[0]["fechadealta"]).Date;

            string queryRango = @"SELECT DATE(entrada) FROM asistencia 
                      WHERE empleados_matricula = @matricula AND entrada IS NOT NULL
                      UNION
                      SELECT falta FROM asistencia 
                      WHERE empleados_matricula = @matricula AND falta IS NOT NULL";
            MySqlCommand cmdRango = new MySqlCommand(queryRango, con);
            cmdRango.Parameters.AddWithValue("@matricula", matricula);
            MySqlDataAdapter daRango = new MySqlDataAdapter(cmdRango);
            DataTable dtRango = new DataTable();
            daRango.Fill(dtRango);

            HashSet<DateTime> diasConRegistro = new HashSet<DateTime>();
            foreach (DataRow r in dtRango.Rows)
                if (r[0] != DBNull.Value)
                    diasConRegistro.Add(Convert.ToDateTime(r[0]).Date);

            DateTime dia = DateTime.Today.AddDays(-1);
            while (dia >= fechaAlta)
            {
                if (dia.DayOfWeek == DayOfWeek.Saturday || dia.DayOfWeek == DayOfWeek.Sunday)
                { dia = dia.AddDays(-1); continue; }

                if (diasConRegistro.Contains(dia)) break;

                MySqlCommand cmdFalta = new MySqlCommand(
                    "INSERT INTO asistencia (falta, empleados_matricula) VALUES (@dia, @matricula)", con);
                cmdFalta.Parameters.AddWithValue("@dia", dia.Date);
                cmdFalta.Parameters.AddWithValue("@matricula", matricula);
                cmdFalta.ExecuteNonQuery();

                dia = dia.AddDays(-1);
            }

            if (dtAsistencia.Rows.Count == 0)
            {
                string queryInsert;
                MySqlCommand cmdInsert;

                if (horaActual > horaEntrada)
                {
                    queryInsert = @"INSERT INTO asistencia 
                            (entrada, retardo, empleados_matricula) 
                            VALUES (@entrada, @retardo, @matricula)";
                    cmdInsert = new MySqlCommand(queryInsert, con);
                    cmdInsert.Parameters.AddWithValue("@entrada", ahora);
                    cmdInsert.Parameters.AddWithValue("@retardo", ahora);
                    cmdInsert.Parameters.AddWithValue("@matricula", matricula);
                    string nombre = dt.Rows[0]["nombre"].ToString();
                    Task.Run(() => EnviarCorreoAsistencia(nombre, matricula, "retardo", ahora));
                }
                else
                {
                    queryInsert = @"INSERT INTO asistencia 
                            (entrada, empleados_matricula) 
                            VALUES (@entrada, @matricula)";
                    cmdInsert = new MySqlCommand(queryInsert, con);
                    cmdInsert.Parameters.AddWithValue("@entrada", ahora);
                    cmdInsert.Parameters.AddWithValue("@matricula", matricula);
                    string nombre = dt.Rows[0]["nombre"].ToString();
                    Task.Run(() => EnviarCorreoAsistencia(nombre, matricula, "entrada", ahora));
                }

                cmdInsert.ExecuteNonQuery();
            }
            else
            {
                if (dtAsistencia.Rows[0]["salida"] != DBNull.Value)
                {
                    timerCamara.Start();
                    return;
                }

                int idAsistencia = Convert.ToInt32(dtAsistencia.Rows[0]["idasistencia"]);
                MySqlCommand cmdUpdate = new MySqlCommand(
                    "UPDATE asistencia SET salida = @salida WHERE idasistencia = @id", con);
                cmdUpdate.Parameters.AddWithValue("@salida", ahora);
                cmdUpdate.Parameters.AddWithValue("@id", idAsistencia);
                cmdUpdate.ExecuteNonQuery();

                string nombreEmp = dt.Rows[0]["nombre"].ToString();
                Task.Run(() => EnviarCorreoAsistencia(nombreEmp, matricula, "salida", ahora));
            }
        }

        VideoCapture captura;
        ZXing.Windows.Compatibility.BarcodeReader lector = new ZXing.Windows.Compatibility.BarcodeReader();
        System.Windows.Forms.Timer timerReset = new System.Windows.Forms.Timer();
        System.Windows.Forms.Timer timerCamara = new System.Windows.Forms.Timer();

        private void IniciarCamara()
        {
            captura = new VideoCapture(0);
            timerCamara.Interval = 33;
            timerCamara.Tick -= ProcesarFrame;
            timerCamara.Tick += ProcesarFrame;
            timerCamara.Start();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            labelHora.Text = DateTime.Now.ToLongTimeString();
            labelFecha.Text = DateTime.Now.ToShortDateString();
            IniciarCamara();
            timerReset.Interval = 5000;
            timerReset.Tick += ReiniciarFormulario;
        }

        private void ReiniciarFormulario(object sender, EventArgs e)
        {
            timerReset.Stop();
            txtMatricula.Text = string.Empty;
            txtNombre.Text = string.Empty;
            txtGrupo.Text = string.Empty;
            pbFoto.Image = null;
            txtEntrada.Text = string.Empty;
            txtSalida.Text = string.Empty;
            timerCamara.Start();
        }

        private void btnConfig_Click(object sender, EventArgs e)
        {
            frmAdminvr adminVerif = new frmAdminvr();
            if (adminVerif.ShowDialog() == DialogResult.OK)
            {
                Configuraciones config = new Configuraciones();
                config.Show();
                this.Hide();
            }
        }

        private Bitmap GenerarQR(string texto)
        {
            ZXing.Windows.Compatibility.BarcodeWriter writer = new ZXing.Windows.Compatibility.BarcodeWriter();
            writer.Format = BarcodeFormat.QR_CODE;
            writer.Options = new EncodingOptions { Width = 150, Height = 150, Margin = 1 };
            return writer.Write(texto);
        }

        PrintDocument printDoc = new PrintDocument();
        Bitmap qrImage;

        private void button2_Click(object sender, EventArgs e)
        {
            qrImage = GenerarQR(txtprueba.Text);
            printDoc.PrintPage += PrintDoc_PrintPage;
            PrintPreviewDialog preview = new PrintPreviewDialog();
            preview.Document = printDoc;
            preview.ShowDialog();
        }

        private void PrintDoc_PrintPage(object sender, PrintPageEventArgs e)
        {
            Graphics dibujar = e.Graphics;
            int x = 50, y = 50, width = 350, height = 200;

            dibujar.FillRectangle(Brushes.Blue, x, y, width, height);
            dibujar.DrawRectangle(Pens.Black, x, y, width, height);

            if (pbFoto.Image != null)
                dibujar.DrawImage(pbFoto.Image, x + 10, y + 20, 100, 120);

            Font font = new Font("Arial", 10, FontStyle.Bold);
            dibujar.DrawString("Matricula:", font, Brushes.Black, x + 120, y + 20);
            dibujar.DrawString(txtprueba.Text, font, Brushes.Black, x + 120, y + 40);

            if (qrImage != null)
                dibujar.DrawImage(qrImage, x + 260, y + 20, 80, 80);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Application.Exit();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            labelHora.Text = DateTime.Now.ToLongTimeString();
            labelFecha.Text = DateTime.Now.ToShortDateString();
        }

        private void EnviarCorreoAsistencia(string nombre, string matricula, string tipo, DateTime hora)
        {
            try
            {
                var mensaje = new MimeMessage();
                mensaje.From.Add(new MailboxAddress("Control de Asistencia", "lenin07025@gmail.com"));
                mensaje.To.Add(new MailboxAddress(nombre, CorreoAdministrador));

                string asunto;
                if (tipo == "entrada") asunto = "✅ Entrada registrada — " + nombre;
                else if (tipo == "retardo") asunto = "⚠️ Entrada con retardo — " + nombre;
                else if (tipo == "salida") asunto = "🚪 Salida registrada — " + nombre;
                else asunto = "Registro de asistencia — " + nombre;

                mensaje.Subject = asunto;

                string cuerpoHtml;
                if (tipo == "entrada")
                    cuerpoHtml = "<h2 style='color:green;'>Entrada registrada correctamente</h2>" +
                                 "<p><b>Empleado:</b> " + nombre + " (" + matricula + ")</p>" +
                                 "<p><b>Hora de entrada:</b> " + hora.ToString("hh:mm:ss tt") + "</p>" +
                                 "<p><b>Fecha:</b> " + hora.ToString("dd/MM/yyyy") + "</p>";
                else if (tipo == "retardo")
                    cuerpoHtml = "<h2 style='color:orange;'>Entrada registrada con RETARDO</h2>" +
                                 "<p><b>Empleado:</b> " + nombre + " (" + matricula + ")</p>" +
                                 "<p><b>Hora de llegada:</b> " + hora.ToString("hh:mm:ss tt") + "</p>" +
                                 "<p><b>Fecha:</b> " + hora.ToString("dd/MM/yyyy") + "</p>";
                else if (tipo == "salida")
                    cuerpoHtml = "<h2 style='color:#1a73e8;'>Salida registrada</h2>" +
                                 "<p><b>Empleado:</b> " + nombre + " (" + matricula + ")</p>" +
                                 "<p><b>Hora de salida:</b> " + hora.ToString("hh:mm:ss tt") + "</p>" +
                                 "<p><b>Fecha:</b> " + hora.ToString("dd/MM/yyyy") + "</p>";
                else
                    cuerpoHtml = "<p>Registro de asistencia para " + nombre +
                                 " a las " + hora.ToString("HH:mm:ss") + "</p>";

                mensaje.Body = new TextPart("html") { Text = cuerpoHtml };

                using (var smtp = new SmtpClient())
                {
                    smtp.Connect("smtp.gmail.com", 587, SecureSocketOptions.StartTls);
                    smtp.Authenticate("lenin07025@gmail.com", "sifw rkxv nytc zlxj");
                    smtp.Send(mensaje);
                    smtp.Disconnect(true);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al enviar correo: " + ex.Message);
            }
        }
    }
}