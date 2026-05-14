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
    public partial class Configuraciones : Form
    {
        public Configuraciones()
        {
            InitializeComponent();
        }

        private void btnCerrar_Click(object sender, EventArgs e)
        {
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

        }
    }
}
