using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Windows.Forms;

namespace Proyecto_Alan_OWO
{
    public partial class Configuraciones : Form
    {
        public Configuraciones()
        {
            InitializeComponent();
            cbFiltro.Items.AddRange(new string[] { "Activos", "Inactivos", "Todos" });
            cbFiltro.SelectedIndex = 0; // dispara cbFiltro_SelectedIndexChanged -> CargarEmpleados()
        }

        private string matriculaSeleccionada = null;
        private string nombreSeleccionado = null;
        PrintDocument printDoc = new PrintDocument();
        Bitmap qrImage;
        string nombreImpresion;
        string areaImpresion;
        Image fotoImpresion;

        public void RefrescarEmpleados()
        {
            CargarEmpleados();
        }

        private void CargarEmpleados()
        {
            string nombre = txtBuscar.Text.Trim();
            string filtro = cbFiltro.SelectedItem?.ToString() ?? "Activos";

            string condActivo = filtro == "Activos" ? "AND activo = 1"
                              : filtro == "Inactivos" ? "AND activo = 0"
                              : "";

            string query = $"SELECT matricula AS 'Matrícula', nombre AS 'Nombre', " +
                           $"area AS 'Área', horaentrada AS 'Entrada', horasalida AS 'Salida' " +
                           $"FROM empleados WHERE nombre LIKE @nombre {condActivo} ORDER BY matricula";

            using (var c = new MySqlConnection("Server=localhost;Database=dbasistencias;Uid=root;Pwd=rootroot;"))
            {
                try
                {
                    c.Open();
                    MySqlCommand cmd = new MySqlCommand(query, c);
                    cmd.Parameters.AddWithValue("@nombre", $"%{nombre}%");
                    MySqlDataAdapter da = new MySqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    dgEmpleados.DataSource = dt;
                    dgEmpleados.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                }
                catch (Exception ex) { MessageBox.Show("Error al cargar: " + ex.Message); }
            }
        }

        private void btnCerrar_Click(object sender, EventArgs e)
        {
            Application.OpenForms.OfType<Form1>().FirstOrDefault()?.Show();
            this.Close();
        }

        private void btnCrearAdm_Click(object sender, EventArgs e)
        {
            frmCrearAdmin frmad = new frmCrearAdmin();
            frmad.Show();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            frmAggEmpleados frm = new frmAggEmpleados();
            frm.Show();
            this.Hide();
        }

        private void btnModificar_Click(object sender, EventArgs e)
        {
            if (matriculaSeleccionada == null) return;
            frmModEmpleados mod = new frmModEmpleados(matriculaSeleccionada);
            mod.Show();
            this.Hide();
        }

        private void BtnBorrar_Click(object sender, EventArgs e)
        {
            if (matriculaSeleccionada == null) return;

            frmAdminvr adminVerif = new frmAdminvr();
            if (adminVerif.ShowDialog() != DialogResult.OK) return;

            var confirm = MessageBox.Show(
                $"¿Confirma la baja del empleado {nombreSeleccionado} (Matrícula: {matriculaSeleccionada})?\n\n" +
                "El empleado perderá acceso al sistema. Su historial de asistencia se conservará.",
                "Confirmar baja de empleado",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes) return;

            using (var c = new MySqlConnection("Server=localhost;Database=dbasistencias;Uid=root;Pwd=rootroot;"))
            {
                try
                {
                    c.Open();
                    MySqlCommand cmd = new MySqlCommand(
                        "UPDATE empleados SET activo = 0 WHERE matricula = @m", c);
                    cmd.Parameters.AddWithValue("@m", matriculaSeleccionada);
                    cmd.ExecuteNonQuery();
                    MessageBox.Show($"Empleado {nombreSeleccionado} dado de baja correctamente.\n" +
                                    "Su historial de asistencia permanece en el sistema.",
                                    "Baja registrada", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    matriculaSeleccionada = null;
                    nombreSeleccionado = null;
                    BtnBorrar.Enabled = false;
                    btnModificar.Enabled = false;
                    btnCredencial.Enabled = false;
                    CargarEmpleados();
                }
                catch (Exception ex) { MessageBox.Show("Error al registrar baja: " + ex.Message); }
            }
        }

        private void dgEmpleados_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            matriculaSeleccionada = dgEmpleados.Rows[e.RowIndex].Cells["Matrícula"].Value?.ToString();
            nombreSeleccionado = dgEmpleados.Rows[e.RowIndex].Cells["Nombre"].Value?.ToString();
            BtnBorrar.Enabled = true;
            btnModificar.Enabled = true;
            btnCredencial.Enabled = true;
        }

        private void txtBuscar_TextChanged(object sender, EventArgs e)
        {
            CargarEmpleados();
        }

        private void cbFiltro_SelectedIndexChanged(object sender, EventArgs e)
        {
            CargarEmpleados();
        }

        private void btnCredencial_Click(object sender, EventArgs e)
        {
            if (matriculaSeleccionada == null) return;

            using (var c = new MySqlConnection("Server=localhost;Database=dbasistencias;Uid=root;Pwd=rootroot;"))
            {
                try
                {
                    c.Open();
                    MySqlCommand cmd = new MySqlCommand(
                        "SELECT nombre, area, foto FROM empleados WHERE matricula = @m", c);
                    cmd.Parameters.AddWithValue("@m", matriculaSeleccionada);
                    MySqlDataReader dr = cmd.ExecuteReader();
                    if (dr.Read())
                    {
                        nombreImpresion = dr["nombre"].ToString();
                        areaImpresion = dr["area"].ToString();
                        fotoImpresion = null;
                        var raw = dr["foto"];
                        if (raw != DBNull.Value && raw is byte[] bytes && bytes.Length > 0)
                        {
                            var ms = new System.IO.MemoryStream(bytes);
                            fotoImpresion = new Bitmap(Image.FromStream(ms));
                        }
                        dr.Close();
                    }
                }
                catch (Exception ex) { MessageBox.Show("Error al cargar datos: " + ex.Message); return; }
            }

            qrImage = GenerarQR(matriculaSeleccionada);
            printDoc.PrintPage -= ImprimirCredencial;
            printDoc.PrintPage += ImprimirCredencial;

            PrintPreviewDialog preview = new PrintPreviewDialog();
            preview.Document = printDoc;
            preview.ShowDialog();
        }

        private void ImprimirCredencial(object sender, PrintPageEventArgs e)
        {
            Graphics g = e.Graphics;
            int x = 50, y = 50, w = 350, h = 200;

            g.FillRectangle(new SolidBrush(Color.FromArgb(30, 30, 90)), x, y, w, h);
            g.DrawRectangle(Pens.Black, x, y, w, h);

            if (fotoImpresion != null)
                g.DrawImage(fotoImpresion, x + 10, y + 20, 100, 120);

            Font bold = new Font("Arial", 10, FontStyle.Bold);
            Font regular = new Font("Arial", 9);

            g.DrawString("Matrícula:", bold, Brushes.White, x + 120, y + 20);
            g.DrawString(matriculaSeleccionada, regular, Brushes.White, x + 120, y + 38);
            g.DrawString("Nombre:", bold, Brushes.White, x + 120, y + 60);
            g.DrawString(nombreImpresion, regular, Brushes.White, x + 120, y + 78);
            g.DrawString("Área:", bold, Brushes.White, x + 120, y + 100);
            g.DrawString(areaImpresion, regular, Brushes.White, x + 120, y + 118);

            if (qrImage != null)
                g.DrawImage(qrImage, x + 260, y + 20, 80, 80);
        }

        private Bitmap GenerarQR(string texto)
        {
            var writer = new ZXing.Windows.Compatibility.BarcodeWriter();
            writer.Format = ZXing.BarcodeFormat.QR_CODE;
            writer.Options = new ZXing.Common.EncodingOptions { Width = 150, Height = 150, Margin = 1 };
            return writer.Write(texto);
        }
    }
}