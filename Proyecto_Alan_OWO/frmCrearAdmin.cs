using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Proyecto_Alan_OWO
{
    public partial class frmCrearAdmin : Form
    {
        public frmCrearAdmin()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            string nombre = txtNombre.Text.Trim();
            string correo = txtCorreo.Text.Trim();
            string contrasena = txtContraseña.Text.Trim();


            if (!correo.Contains("@") || !correo.Contains("."))
            {
                MessageBox.Show("Ingresa un correo válido.",
                    "Correo inválido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtCorreo.Focus();
                return;
            }

            if (guardarAdd(nombre, correo, contrasena))
            {
                MessageBox.Show("Administrador creado",
                    "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.Close();
            }
        }

        private void frmCrearAdmin_Load(object sender, EventArgs e)
        {
            btnGuardar.Enabled = false;
        }

        private void txtNombre_TextChanged(object sender, EventArgs e)
        {
            ValidarCampos();
        }

        private void txtCorreo_TextChanged(object sender, EventArgs e)
        {
            ValidarCampos();
        }

        private void txtContraseña_TextChanged(object sender, EventArgs e)
        {
            ValidarCampos();
        }

        private bool guardarAdd(string nombre, string correo, string contrasena)
        {
            MySqlConnection con = Conexion.GetConexion();
            if (con == null) return false;

            try
            {

                string sqlCheck = "SELECT COUNT(*) FROM administradores WHERE correo = @correo";
                MySqlCommand cmdCheck = new MySqlCommand(sqlCheck, con);
                cmdCheck.Parameters.AddWithValue("@correo", correo);
                int existe = Convert.ToInt32(cmdCheck.ExecuteScalar());

                if (existe > 0)
                {
                    MessageBox.Show("Ya existe un administrador con ese correo.",
                        "Correo duplicado", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }


                string sql = "INSERT INTO administradores (nombre, correo, contraseña) VALUES (@nombre, @correo, @contrasena)";
                MySqlCommand cmd = new MySqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@nombre", nombre);
                cmd.Parameters.AddWithValue("@correo", correo);
                cmd.Parameters.AddWithValue("@contrasena", contrasena);

                int filas = cmd.ExecuteNonQuery();
                return filas > 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al guardar: " + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            finally
            {
                con.Close();
            }
        }

        private void ValidarCampos()
        {
            btnGuardar.Enabled = txtNombre.Text.Trim().Length > 0
                              && txtCorreo.Text.Trim().Length > 0
                              && txtContraseña.Text.Trim().Length > 0;
        }
    }
}