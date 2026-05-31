using System;
using MySql.Data.MySqlClient;

namespace Proyecto_Alan_OWO
{
    internal class Conexion
    {
        private static string connectionString =
        "Server=localhost;Database=dbasistencias;Uid=root;Pwd=rootroot;";

        public static MySqlConnection GetConexion()
        {
            try
            {
                MySqlConnection con = new MySqlConnection(connectionString);
                con.Open();
                return con;
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(
                    "Error al conectar con la base de datos:\n" + ex.Message,
                    "Error de conexión",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Error);
                return null;
            }
        }
    }
}