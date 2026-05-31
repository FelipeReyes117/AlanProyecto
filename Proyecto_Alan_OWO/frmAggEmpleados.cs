using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Proyecto_Alan_OWO
{
    public partial class frmAggEmpleados : Form
    {
        public frmAggEmpleados()
        {
            InitializeComponent();
            this.FormClosing += new FormClosingEventHandler(frmAggEmpleados_FormClosing);
            ActiveControl = txtNombre;
            txtNombre.Focus();
            LlenarCBArea();
            txtMatricula.Text = GenerarMatricula();
            dtpEntrada.Value = DateTime.Today.AddHours(7);
            dtpSalida.Value = DateTime.Today.AddHours(17);
        }

        public void LlenarCBArea()
        {
            string query = "SELECT DISTINCT area FROM empleados WHERE activo = 1";
            using (var c = new MySqlConnection("Server=localhost;Database=dbasistencias;Uid=root;Pwd=rootroot;"))
            {
                try
                {
                    c.Open();
                    MySqlDataAdapter da = new MySqlDataAdapter(query, c);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    cbArea.DisplayMember = "area";
                    cbArea.ValueMember = "area";
                    cbArea.DataSource = dt;
                }
                catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
            }
        }

        private byte[] ImageToBytes(Image img)
        {
            if (img == null) return null;
            using (var ms = new System.IO.MemoryStream())
            {
                img.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                return ms.ToArray();
            }
        }

        private string GenerarMatricula()
        {
            int anio = DateTime.Now.Year;
            string query = $"SELECT COUNT(*) FROM empleados WHERE matricula LIKE '{anio}%' AND activo = 1";
            using (var c = new MySqlConnection("Server=localhost;Database=dbasistencias;Uid=root;Pwd=rootroot;"))
            {
                c.Open();
                MySqlCommand cmd = new MySqlCommand(query, c);
                int total = Convert.ToInt32(cmd.ExecuteScalar());
                return $"{anio}{(total + 1):D3}";
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtNombre.Text))
            { MessageBox.Show("Ingresa el nombre."); txtNombre.Focus(); return; }

            if (pbFoto.Image == null)
            { MessageBox.Show("Agrega una foto."); return; }

            if (dtpEntrada.Value.TimeOfDay >= dtpSalida.Value.TimeOfDay)
            { MessageBox.Show("Hora de salida debe ser mayor a entrada."); return; }

            string query = "INSERT INTO empleados (matricula, nombre, area, foto, horaentrada, horasalida, fechadealta) " +
                           "VALUES (@matricula, @nombre, @area, @foto, @horaentrada, @horasalida, @fechaactual)";

            using (var c = new MySqlConnection("Server=localhost;Database=dbasistencias;Uid=root;Pwd=rootroot;"))
            {
                try
                {
                    c.Open();
                    MySqlCommand cmd = new MySqlCommand(query, c);
                    cmd.Parameters.AddWithValue("@matricula", txtMatricula.Text);
                    cmd.Parameters.AddWithValue("@nombre", txtNombre.Text);
                    cmd.Parameters.AddWithValue("@area", cbArea.SelectedValue.ToString());
                    cmd.Parameters.AddWithValue("@foto", ImageToBytes(pbFoto.Image));
                    cmd.Parameters.AddWithValue("@horaentrada", dtpEntrada.Value.TimeOfDay);
                    cmd.Parameters.AddWithValue("@horasalida", dtpSalida.Value.TimeOfDay);
                    cmd.Parameters.AddWithValue("@fechaactual", DateTime.Now.Date);
                    cmd.ExecuteNonQuery();
                    MessageBox.Show("Empleado guardado correctamente.");

                    txtNombre.Text = string.Empty;
                    cbArea.SelectedIndex = -1;
                    pbFoto.Image = null;
                    dtpEntrada.Value = DateTime.Today.AddHours(7);
                    dtpSalida.Value = DateTime.Today.AddHours(17);
                    txtMatricula.Text = GenerarMatricula();
                    txtNombre.Focus();
                }
                catch (Exception ex) { MessageBox.Show("Error al guardar: " + ex.Message); }
            }
        }

        private void btnFoto_Click(object sender, EventArgs e)
        {
            OpenFileDialog abrir = new OpenFileDialog();
            abrir.Filter = "Imagen|*.jpg;*.png;*.jpeg";
            if (abrir.ShowDialog() == DialogResult.OK)
            {
                pbFoto.Image = Image.FromFile(abrir.FileName);
                btnAgregar.Focus();
            }
        }

        private void txtNombre_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter) { e.Handled = true; cbArea.Focus(); }
        }

        private void cbArea_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter) { e.Handled = true; dtpEntrada.Focus(); }
        }

        private void dtpEntrada_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter) { e.Handled = true; dtpSalida.Focus(); }
        }

        private void dtpSalida_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter) { e.Handled = true; btnFoto.Focus(); }
        }

        private void frmAggEmpleados_FormClosing(object sender, FormClosingEventArgs e)
        {
            var config = Application.OpenForms.OfType<Configuraciones>().FirstOrDefault();
            if (config != null)
            {
                config.Show();
                config.RefrescarEmpleados();
            }
        }
    }
}