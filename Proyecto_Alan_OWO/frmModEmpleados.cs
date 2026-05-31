using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Proyecto_Alan_OWO
{
    public partial class frmModEmpleados : Form
    {
        private string _matricula;

        public frmModEmpleados(string matricula)
        {
            InitializeComponent();
            this.FormClosing += new FormClosingEventHandler(frmModEmpleados_FormClosing);
            _matricula = matricula;
            LlenarCBArea();
            CargarEmpleado();
            ActiveControl = txtNombre;
            txtNombre.Focus();
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

        private void CargarEmpleado()
        {
            string query = "SELECT matricula, nombre, area, foto, horaentrada, horasalida " +
                           "FROM empleados WHERE matricula = @matricula";
            using (var c = new MySqlConnection("Server=localhost;Database=dbasistencias;Uid=root;Pwd=rootroot;"))
            {
                try
                {
                    c.Open();
                    MySqlCommand cmd = new MySqlCommand(query, c);
                    cmd.Parameters.AddWithValue("@matricula", _matricula);
                    MySqlDataReader reader = cmd.ExecuteReader();
                    if (reader.Read())
                    {
                        txtMatricula.Text = reader["matricula"].ToString();
                        txtMatricula.ReadOnly = true;
                        txtNombre.Text = reader["nombre"].ToString();
                        cbArea.SelectedValue = reader["area"].ToString();
                        dtpEntrada.Value = DateTime.Today.Add((TimeSpan)reader["horaentrada"]);
                        dtpSalida.Value = DateTime.Today.Add((TimeSpan)reader["horasalida"]);

                        if (reader["foto"] != DBNull.Value)
                        {
                            byte[] fotoBytes = (byte[])reader["foto"];
                            var ms = new System.IO.MemoryStream(fotoBytes);
                            pbFoto.Image = new Bitmap(System.Drawing.Image.FromStream(ms));
                        }
                    }
                    else
                    {
                        MessageBox.Show("No se encontró el empleado.");
                        this.Close();
                    }
                }
                catch (Exception ex) { MessageBox.Show("Error al cargar empleado: " + ex.Message); }
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

        private void btnGuardar_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtNombre.Text))
            { MessageBox.Show("Ingresa el nombre."); txtNombre.Focus(); return; }

            if (pbFoto.Image == null)
            { MessageBox.Show("Agrega una foto."); return; }

            if (dtpEntrada.Value.TimeOfDay >= dtpSalida.Value.TimeOfDay)
            { MessageBox.Show("Hora de salida debe ser mayor a entrada."); return; }

            string query = "UPDATE empleados SET nombre = @nombre, area = @area, foto = @foto, " +
                           "horaentrada = @horaentrada, horasalida = @horasalida " +
                           "WHERE matricula = @matricula";

            using (var c = new MySqlConnection("Server=localhost;Database=dbasistencias;Uid=root;Pwd=rootroot;"))
            {
                try
                {
                    c.Open();
                    MySqlCommand cmd = new MySqlCommand(query, c);
                    cmd.Parameters.AddWithValue("@nombre", txtNombre.Text);
                    cmd.Parameters.AddWithValue("@area", cbArea.SelectedValue.ToString());
                    cmd.Parameters.AddWithValue("@foto", ImageToBytes(pbFoto.Image));
                    cmd.Parameters.AddWithValue("@horaentrada", dtpEntrada.Value.TimeOfDay);
                    cmd.Parameters.AddWithValue("@horasalida", dtpSalida.Value.TimeOfDay);
                    cmd.Parameters.AddWithValue("@matricula", _matricula);
                    cmd.ExecuteNonQuery();
                    MessageBox.Show("Empleado actualizado correctamente.");
                    this.Close();
                }
                catch (Exception ex) { MessageBox.Show("Error al actualizar: " + ex.Message); }
            }
        }

        private void btnFoto_Click(object sender, EventArgs e)
        {
            OpenFileDialog abrir = new OpenFileDialog();
            abrir.Filter = "Imagen|*.jpg;*.png;*.jpeg";
            if (abrir.ShowDialog() == DialogResult.OK)
            {
                pbFoto.Image = Image.FromFile(abrir.FileName);
                btnGuardar.Focus();
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

        private void frmModEmpleados_FormClosing(object sender, FormClosingEventArgs e)
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