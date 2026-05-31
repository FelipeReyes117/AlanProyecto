using MySql.Data.MySqlClient;
using System;
using System.Windows.Forms;

namespace Proyecto_Alan_OWO
{
    public partial class frmInicio : Form
    {
        public frmInicio()
        {
            InitializeComponent();
        }

        private bool Verificacion(string correo, string contraseña)
        {
            MySqlConnection con = Conexion.GetConexion();
            if (con == null) return false;
            try
            {
                string sql = @"SELECT COUNT(*) FROM administradores 
                               WHERE correo = @correo AND contraseña = @contrasena";
                MySqlCommand cmd = new MySqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@correo", correo);
                cmd.Parameters.AddWithValue("@contrasena", contraseña);
                int resultado = Convert.ToInt32(cmd.ExecuteScalar());
                return resultado > 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error de inicio: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            finally
            {
                con.Close();
            }
        }

        private void btnInicio_Click(object sender, EventArgs e)
        {
            string correo = txtCorreo.Text.Trim();
            string contraseña = txtContraseña.Text.Trim();

            if (Verificacion(correo, contraseña))
            {
                MessageBox.Show("Bienvenido, sistema de asistencia funcionando. Espere a que carguen los datos...");
                Form1 frm = new Form1();
                frm.CorreoAdministrador = correo;
                frm.Show();
                this.Hide();
            }
            else
            {
                MessageBox.Show("Correo o contraseña incorrectos", "Error de inicio", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            frmCrearAdmin addAdmin = new frmCrearAdmin();
            addAdmin.Show();
        }

        private void txtCorreo_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 13)
                txtContraseña.Focus();
        }

        private void txtContraseña_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 13 && btnInicio.Enabled)
                btnInicio_Click(sender, e);
        }

        private void frmInicio_Load(object sender, EventArgs e)
        {
            btnInicio.Enabled = false;
        }

        private void txtCorreo_TextChanged(object sender, EventArgs e)
        {
            btnInicio.Enabled = txtCorreo.Text.Trim().Length > 0 && txtContraseña.Text.Trim().Length > 0;
        }

        private void txtContraseña_TextChanged(object sender, EventArgs e)
        {
            btnInicio.Enabled = txtCorreo.Text.Trim().Length > 0 && txtContraseña.Text.Trim().Length > 0;
        }
    }
}