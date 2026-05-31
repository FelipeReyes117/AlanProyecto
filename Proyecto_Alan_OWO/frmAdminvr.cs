using System;
using System.Windows.Forms;

namespace Proyecto_Alan_OWO
{
    public partial class frmAdminvr : Form
    {
        public frmAdminvr()
        {
            InitializeComponent();
        }

        private void btnIniciar_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void btnSalir_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 13)
            {
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }
    }
}